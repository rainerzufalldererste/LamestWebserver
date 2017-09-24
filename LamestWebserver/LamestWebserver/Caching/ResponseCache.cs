using System;
using System.Collections.Generic;
using System.Linq;
using LamestWebserver.Core;
using LamestWebserver.Collections;
using LamestWebserver.Synchronization;

namespace LamestWebserver.Caching
{
    /// <summary>
    /// A general purpose key-value string cache.
    /// </summary>
    public class ResponseCache : NullCheckable
    {
        /// <summary>
        /// The main ResponseCache instance for LamestWebserver.
        /// </summary>
        public readonly static Singleton<ResponseCache> CurrentCacheInstance = new Singleton<ResponseCache>();

        /// <summary>
        /// The size of the underlying hashmap for the cached items.
        /// </summary>
        public static int StringResponseCacheHashmapSize = 4096;

        /// <summary>
        /// The current size of the cache.
        /// </summary>
        public ulong CurrentStringResponseCacheSize { get; protected set; } = 0;

        /// <summary>
        /// The Maximum Response cache size. if null: unlimited.
        /// <para/>
        /// Defaults to 1024 * 1024 * 256 = 512 MBytes (because of two byte characters).
        /// </summary>
        public ulong? MaximumStringResponseCacheSize = 1024 * 1024 * 256;

        private readonly AVLHashMap<string, ResponseCacheEntry<string>> StringResponses = new AVLHashMap<string, ResponseCacheEntry<string>>(StringResponseCacheHashmapSize);
        private UsableWriteLock StringResponseLock = new UsableWriteLock();

        /// <summary>
        /// Retrieves a cached string response if it is cached.
        /// </summary>
        /// <param name="key">The key of the cached response</param>
        /// <param name="response">The cached response.</param>
        /// <returns>Returns true if the response could be retrieved.</returns>
        public bool GetCachedStringResponse(string key, out string response)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            ResponseCacheEntry<string> result;

            using (StringResponseLock.LockRead())
                result = StringResponses[key];

            if (!result)
            {
                response = null;
                return false;
            }

            if (!result.RefreshTime.HasValue || result.LastUpdatedDateTime + result.RefreshTime.Value > DateTime.UtcNow)
            {
                result.Increment();
                response = result.Response;
                return true;
            }
            else
            {
                RemoveStringResponseEntry(key, result);

                response = null;
                return false;
            }
        }

        /// <summary>
        /// Sets a response to the cache.
        /// </summary>
        /// <param name="key">The key of the response.</param>
        /// <param name="response">The response.</param>
        /// <param name="refreshTime">The lifetime of this entry. If null this entry doesn't have to be refreshed.</param>
        public void SetCachedStringResponse(string key, string response, TimeSpan? refreshTime = null)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (response == null)
                throw new ArgumentNullException(nameof(response));

            if (MaximumStringResponseCacheSize.HasValue && (ulong)response.Length > MaximumStringResponseCacheSize.Value)
            {
                Logger.LogDebugExcept($"The given response for '{key}' is to big to be cached.");
                return;
            }

            using (StringResponseLock.LockWrite())
            {
                if (MaximumStringResponseCacheSize.HasValue && CurrentStringResponseCacheSize + (ulong)response.Length > MaximumStringResponseCacheSize.Value)
                    MakeRoom((ulong)response.Length);

                ResponseCacheEntry<string> entry = new ResponseCacheEntry<string>()
                {
                    LastUpdatedDateTime = DateTime.UtcNow,
                    RefreshTime = refreshTime,
                    Response = response
                };

                AddStringResponseEntry(key, entry);
            }
        }

        /// <summary>
        /// Retrueves a cached string or caches it using the source function.
        /// </summary>
        /// <param name="key">The key of the response.</param>
        /// <param name="sourceIfNotCached">A function that retrieves the source if it's not cached yet.</param>
        /// <param name="refreshTime">The lifetime of this entry. If null this entry doesn't have to be refreshed. This lifetime will not override the original lifetime of this entry.</param>
        /// <returns>The cached response.</returns>
        public virtual string GetCachedString(string key, Func<string> sourceIfNotCached, TimeSpan? refreshTime = null)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (sourceIfNotCached == null)
                throw new ArgumentNullException(nameof(sourceIfNotCached));

            string response;

            if (GetCachedStringResponse(key, out response))
            {
                return response;
            }
            else
            {
                string toCache = sourceIfNotCached();
                SetCachedStringResponse(key, toCache, refreshTime);

                return toCache;
            }
        }

        /// <summary>
        /// Removes a cached string from the cache.
        /// </summary>
        /// <param name="key">the key of the cached entry.</param>
        public void RemoveCachedString(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            RemoveStringResponseEntry(key, null);
        }

        /// <summary>
        /// Clears the cache entirely.
        /// </summary>
        public virtual void Clear()
        {
            using (StringResponseLock.LockWrite())
            {
                StringResponses.Clear();
                CurrentStringResponseCacheSize = 0;
            }
        }

        /// <summary>
        /// The additional free space to make - relative to the maximum cache size, when the cache is overflowing.
        /// </summary>
        public double CacheMakeRoom_AdditionalFreeSpacePercentage
        {
            get
            {
                return _cacheMakeRoom_AdditionalFreeSpacePercentage; 
            }

            set
            {
                _cacheMakeRoom_AdditionalFreeSpacePercentage = value.Clamp(0.0, 1.0);
            }
        }

        private double _cacheMakeRoom_AdditionalFreeSpacePercentage = 0.0625;

        /// <summary>
        /// The upper percentile for the date based cache cleaning. (between 0 and 1)
        /// </summary>
        public double CacheMakeRoom_UpperPercentile_Date
        {
            get
            {
                return _cacheMakeRoom_UpperPercentile_Date; 
            }

            set
            {
                _cacheMakeRoom_UpperPercentile_Date = value.Clamp(0.0, 1.0);
            }
        }

        private double _cacheMakeRoom_UpperPercentile_Date = 0.1;

        /// <summary>
        /// The upper percentile for the access count based cache cleaning. (between 0 and 1)
        /// </summary>
        public double CacheMakeRoom_UpperPercentile_Count
        {
            get
            {
                return _cacheMakeRoom_UpperPercentile_Count;
            }

            set
            {
                _cacheMakeRoom_UpperPercentile_Count = value.Clamp(0.0, 1.0);
            }
        }

        private double _cacheMakeRoom_UpperPercentile_Count = 0.1;

        /// <summary>
        /// The maximum percentage of entries to remove by size. (between 0 and 1)
        /// </summary>
        public double CacheMakeRoom_RemoveBySizePercentage
        {
            get
            {
                return _cacheMakeRoom_RemoveBySizePercentage;
            }

            set
            {
                _cacheMakeRoom_RemoveBySizePercentage = value.Clamp(0.0, 1.0);
            }
        }

        private double _cacheMakeRoom_RemoveBySizePercentage = 0.25;

        /// <summary>
        /// The maximum percentage of entries to remove by lifetime left. (between 0 and 1)
        /// </summary>
        public double CacheMakeRoom_RemoveByTimePercentage
        {
            get
            {
                return _cacheMakeRoom_RemoveByTimePercentage;
            }

            set
            {
                _cacheMakeRoom_RemoveByTimePercentage = value.Clamp(0.0, 1.0);
            }
        }

        private double _cacheMakeRoom_RemoveByTimePercentage = 0.25;

        /// <summary>
        /// Makes room when the cache is overflowing.
        /// </summary>
        /// <param name="requestedSpace">The requested amount of free space. (Not including CacheMakeRoom_AdditionalFreeSpace)</param>
        protected virtual void MakeRoom(ulong requestedSpace)
        {
            if (!StringResponses.Any())
                return;

            if (!MaximumStringResponseCacheSize.HasValue)
                return;

            Logger.LogInformation($"Cache space has been overflowed. Making Room... ({StringResponses.Count} Entries) ({CurrentStringResponseCacheSize} + {requestedSpace} ( + {(ulong)(CacheMakeRoom_AdditionalFreeSpacePercentage * MaximumStringResponseCacheSize)} additional ) of {MaximumStringResponseCacheSize.Value} -> {(100.0 * ((CurrentStringResponseCacheSize + requestedSpace) / (double)MaximumStringResponseCacheSize.Value)).ToString("0.00")}% filled)");

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            KeyValuePair<string, ResponseCacheEntry<string>>[] responses;

            using (StringResponseLock.LockRead())
                responses = StringResponses.ToArray();

            // Remove outdated.
            foreach (var kvpair in responses)
                if (kvpair.Value.RefreshTime != null && kvpair.Value.LastUpdatedDateTime + kvpair.Value.RefreshTime < DateTime.UtcNow)
                    RemoveStringResponseEntry(kvpair.Key, kvpair.Value);

            DateTime upperTenthPercentileDate;
            ulong upperTenthPercentileCount;

            KeyValuePair<string, ResponseCacheEntry<string>>[] DateSorted = null, CountSorted = null, SizeSorted = null;


            var worker0 = ThreadedWorker.CurrentWorker.Instance.EnqueueJob((Action)(() => DateSorted = StringResponses.OrderBy((kvpair) => kvpair.Value.LastRequestedDateTime).ToArray()));
            var worker1 = ThreadedWorker.CurrentWorker.Instance.EnqueueJob((Action)(() => CountSorted = StringResponses.OrderBy((kvpair) => kvpair.Value.Count).ToArray()));
            var worker2 = ThreadedWorker.CurrentWorker.Instance.EnqueueJob((Action)(() => SizeSorted = StringResponses.OrderBy((kvpair) => kvpair.Value.Response.Length).ToArray()));

            ThreadedWorker.JoinTasks(worker0, worker1, worker2);

            upperTenthPercentileDate = DateSorted[(int)(DateSorted.Length * (1.0 - CacheMakeRoom_UpperPercentile_Date))].Value.LastRequestedDateTime;
            upperTenthPercentileCount = CountSorted[(int)(CountSorted.Length * (1.0 - CacheMakeRoom_UpperPercentile_Count))].Value.Count;

            // Remove by size descending if not in upper tenth percentile.
            int lastRemoveBySizeIndex = (int)(SizeSorted.Length * CacheMakeRoom_RemoveBySizePercentage);

            for (int i = SizeSorted.Length - 1; i >= lastRemoveBySizeIndex; i--)
            {
                if (CurrentStringResponseCacheSize + requestedSpace <= MaximumStringResponseCacheSize.Value) // First run without additional size
                    break;

                var response = StringResponses[SizeSorted[i].Key];

                if (response.Count <= upperTenthPercentileCount && response.LastRequestedDateTime <= upperTenthPercentileDate)
                    RemoveStringResponseEntry(SizeSorted[i].Key, response);
            }

            int j = 0;

            // Remove by count and last access time if not in upper tenth percentile.
            while (CurrentStringResponseCacheSize + requestedSpace + (ulong)(MaximumStringResponseCacheSize * CacheMakeRoom_AdditionalFreeSpacePercentage) > MaximumStringResponseCacheSize && StringResponses.Any())
            {
                var response = StringResponses[DateSorted[j].Key];

                if (response && response.Count <= upperTenthPercentileCount)
                    RemoveStringResponseEntry(DateSorted[j].Key, response);

                response = StringResponses[CountSorted[j].Key];

                if (response && response.LastRequestedDateTime <= upperTenthPercentileDate)
                    RemoveStringResponseEntry(CountSorted[j].Key, response);

                j++;
            }

            // Remove by time left till refresh till percentage.
            var SortedTimeLeftTillRefresh = (from p in StringResponses where p.Value.RefreshTime.HasValue select new { kvpair = p, timeLeft = DateTime.UtcNow - p.Value.LastUpdatedDateTime + p.Value.RefreshTime }).OrderBy((k) => k.timeLeft).ToArray();

            for (int i = 0; i < SortedTimeLeftTillRefresh.Length * CacheMakeRoom_RemoveByTimePercentage; i++)
            {
                if (CurrentStringResponseCacheSize + requestedSpace + (ulong)(MaximumStringResponseCacheSize * CacheMakeRoom_AdditionalFreeSpacePercentage) <= MaximumStringResponseCacheSize)
                    goto epilogue;

                var response = StringResponses[SortedTimeLeftTillRefresh[j].kvpair.Key];

                RemoveStringResponseEntry(SortedTimeLeftTillRefresh[j].kvpair.Key, response);
            }

            // Remove by count and last accesstime regardless of percentiles.
            j = 0;

            while (CurrentStringResponseCacheSize + requestedSpace + (ulong)(MaximumStringResponseCacheSize * CacheMakeRoom_AdditionalFreeSpacePercentage) > MaximumStringResponseCacheSize && StringResponses.Any())
            {
                var response = StringResponses[DateSorted[j].Key];

                if (response)
                    RemoveStringResponseEntry(DateSorted[j].Key, response);

                if (!(CurrentStringResponseCacheSize + requestedSpace + (ulong)(MaximumStringResponseCacheSize * CacheMakeRoom_AdditionalFreeSpacePercentage) > MaximumStringResponseCacheSize && StringResponses.Any()))
                    goto epilogue;

                response = StringResponses[CountSorted[j].Key];

                if (response)
                    RemoveStringResponseEntry(CountSorted[j].Key, response);

                j++;
            }

            epilogue:
            stopwatch.Stop();

            Logger.LogInformation($"Cache space has been cleared. ({stopwatch.ElapsedMilliseconds} ms) ({StringResponses.Count} Entries => {(100.0 * ((CurrentStringResponseCacheSize + requestedSpace) / (double)MaximumStringResponseCacheSize.Value)).ToString("0.00")}% filled)");
        }

        /// <summary>
        /// Removes an entry from the cache.
        /// <para/> 
        /// This method can handle non-existent items.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The response if already retrieved. If null, the response will be retrieved automatically.</param>
        protected void RemoveStringResponseEntry(string key, ResponseCacheEntry<string> value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (!value)
                value = StringResponses[key];

            if (!value) // if it couldn't be retrieved.
                return;

            using (StringResponseLock.LockWrite())
            {
                CurrentStringResponseCacheSize -= (ulong)value.Response.Length;
                StringResponses.Remove(key);
            }
        }

        /// <summary>
        /// Adds an Entry to the cache.
        /// <para/> 
        /// This method can handle already existent items.
        /// </summary>
        /// <param name="key">The key of the response.</param>
        /// <param name="value">The response.</param>
        protected void AddStringResponseEntry(string key, ResponseCacheEntry<string> value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var old = StringResponses[key];

            if (old)
                RemoveStringResponseEntry(key, old);

            using (StringResponseLock.LockWrite())
            {
                CurrentStringResponseCacheSize += (ulong)value.Response.Length;
                StringResponses[key] = value;
            }
        }

        /// <summary>
        /// A cached response.
        /// </summary>
        /// <typeparam name="T">The type of the cached element.</typeparam>
        protected class ResponseCacheEntry<T> : NullCheckable
        {
            /// <summary>
            /// The time when this entry was created or updated.
            /// </summary>
            public DateTime LastUpdatedDateTime;

            /// <summary>
            /// The lifetime of this entry. If null it doesn't have to be refreshed.
            /// </summary>
            public TimeSpan? RefreshTime;

            /// <summary>
            /// The Response to retrieve from this entry.
            /// </summary>
            public T Response;

            /// <summary>
            /// The amount of times this entry has been requested.
            /// </summary>
            public ulong Count = 0;

            /// <summary>
            /// The time when this entry was accessed for the last time.
            /// </summary>
            public DateTime LastRequestedDateTime = DateTime.UtcNow;

            /// <summary>
            /// Creates a new ResponseCacheEntry instance.
            /// </summary>
            public ResponseCacheEntry()
            {

            }

            /// <summary>
            /// Creates a new ResponseCacheEntry instance.
            /// </summary>
            /// <param name="respose">The response.</param>
            /// <param name="refreshTime">The lifetime of the cached entry. If null this entry doesn't have to be refreshed.</param>
            public ResponseCacheEntry(T respose, TimeSpan? refreshTime = null)
            {
                Response = respose;
                RefreshTime = refreshTime;
            }

            /// <summary>
            /// Increments the Count and Updates the LastRequestedDateTime.
            /// </summary>
            public void Increment()
            {
                Count++;
                LastRequestedDateTime = DateTime.UtcNow;
            }
        }
    }
}
