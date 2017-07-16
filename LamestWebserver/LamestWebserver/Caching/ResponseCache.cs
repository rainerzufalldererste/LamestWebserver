using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LamestWebserver.Core;
using LamestWebserver.Collections;
using LamestWebserver.Synchronization;

namespace LamestWebserver.Caching
{
    public class ResponseCache : NullCheckable
    {
        public readonly static Singleton<ResponseCache> SingletonCacheInstance = new Singleton<ResponseCache>();
        public static int StringResponseCacheHashmapSize = 4096;

        public ulong CurrentStringResponseCacheSize { get; protected set; } = 0;
        public ulong? StringResponseCacheMaxSize = 1024 * 1024 * 256; // 512 MBytes because of two byte characters.

        private readonly AVLHashMap<string, ResponseCacheEntry<string>> StringResponses = new AVLHashMap<string, ResponseCacheEntry<string>>(StringResponseCacheHashmapSize);
        private UsableWriteLock StringResponseLock = new UsableWriteLock();

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
                using (StringResponseLock.LockWrite())
                {
                    CurrentStringResponseCacheSize -= (ulong)result.Response.Length;
                    StringResponses.Remove(key);
                }

                response = null;
                return false;
            }
        }

        public void SetCachedStringResponse(string key, string response, TimeSpan? refreshTime = null)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (response == null)
                throw new ArgumentNullException(nameof(response));

            if (StringResponseCacheMaxSize.HasValue && (ulong)response.Length > StringResponseCacheMaxSize.Value)
                Logger.LogDebugExcept("The given response is to big to be cached.");

            if (StringResponseCacheMaxSize.HasValue && CurrentStringResponseCacheSize + (ulong)response.Length > StringResponseCacheMaxSize.Value)
                MakeRoom((ulong)response.Length);

            ResponseCacheEntry<string> entry = new ResponseCacheEntry<string>()
            {
                LastUpdatedDateTime = DateTime.UtcNow,
                RefreshTime = refreshTime,
                Response = response
            };

            using (StringResponseLock.LockWrite())
            {
                CurrentStringResponseCacheSize += (ulong)entry.Response.Length;

                StringResponses[key] = entry;
            }
        }

        public virtual string GetCachedString(string key, Func<string> sourceIfNotCached, TimeSpan? refreshTime = null)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (GetCachedStringResponse(key, out string response))
            {
                return response;
            }
            else
            {
                if (sourceIfNotCached == null)
                    throw new ArgumentNullException(nameof(sourceIfNotCached));

                string toCache = sourceIfNotCached();
                SetCachedStringResponse(key, toCache, refreshTime);

                return toCache;
            }
        }

        public virtual void Clear()
        {
            using (StringResponseLock.LockWrite())
            {
                StringResponses.Clear();
                CurrentStringResponseCacheSize = 0;
            }
        }

        public ulong CacheMakeRoom_AdditionalFreeSpace = 1024 * 1024 * 16; // 32 MByte due to two byte characters.

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

        protected virtual void MakeRoom(ulong requestedSpace)
        {
            if (!StringResponses.Any())
                return;

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

            using (StringResponseLock.LockWrite())
            {
                var worker0 = ThreadedWorker.CurrentWorker.Instance.EnqueueJob((Action)(() => DateSorted = StringResponses.OrderBy((kvpair) => kvpair.Value.LastRequestedDateTime).ToArray()));
                var worker1 = ThreadedWorker.CurrentWorker.Instance.EnqueueJob((Action)(() => CountSorted = StringResponses.OrderBy((kvpair) => kvpair.Value.Count).ToArray()));
                var worker2 = ThreadedWorker.CurrentWorker.Instance.EnqueueJob((Action)(() => SizeSorted = StringResponses.OrderBy((kvpair) => kvpair.Value.Response.Length).ToArray()));

                ThreadedWorker.JoinTasks(worker0, worker1, worker2);

                upperTenthPercentileDate = DateSorted[(int)(DateSorted.Length * (1.0 - CacheMakeRoom_UpperPercentile_Date))].Value.LastRequestedDateTime;
                upperTenthPercentileCount = CountSorted[(int)(CountSorted.Length * (1.0 - CacheMakeRoom_UpperPercentile_Count))].Value.Count;
                
                // Remove by size if not in upper tenth percentile.
                for (int i = (int)(SizeSorted.Length * CacheMakeRoom_RemoveBySizePercentage); i >= 0; i--)
                {
                    if (CurrentStringResponseCacheSize + requestedSpace <= StringResponseCacheMaxSize) // First run without additional size
                        return;

                    var response = StringResponses[SizeSorted[i].Key];

                    if (response.Count <= upperTenthPercentileCount && response.LastRequestedDateTime <= upperTenthPercentileDate)
                        RemoveStringResponseEntry(SizeSorted[i].Key, response);
                }

                int j = 0;

                // Remove by count and last access time if not in upper tenth percentile.
                while (CurrentStringResponseCacheSize + requestedSpace + CacheMakeRoom_AdditionalFreeSpace > StringResponseCacheMaxSize && StringResponses.Any())
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
                    if (CurrentStringResponseCacheSize + requestedSpace + CacheMakeRoom_AdditionalFreeSpace <= StringResponseCacheMaxSize)
                        return;

                    var response = StringResponses[SortedTimeLeftTillRefresh[j].kvpair.Key];

                    RemoveStringResponseEntry(SortedTimeLeftTillRefresh[j].kvpair.Key, response);
                }

                // Remove by count and last accesstime regardless of percentiles.
                j = 0;
                
                while (CurrentStringResponseCacheSize + requestedSpace + CacheMakeRoom_AdditionalFreeSpace > StringResponseCacheMaxSize && StringResponses.Any())
                {
                    var response = StringResponses[DateSorted[j].Key];

                    if (response)
                        RemoveStringResponseEntry(DateSorted[j].Key, response);

                    if (!(CurrentStringResponseCacheSize + requestedSpace + CacheMakeRoom_AdditionalFreeSpace > StringResponseCacheMaxSize && StringResponses.Any()))
                        return;

                    response = StringResponses[CountSorted[j].Key];

                    if (response)
                        RemoveStringResponseEntry(CountSorted[j].Key, response);

                    j++;
                }
            }
        }

        protected void RemoveStringResponseEntry(string key, ResponseCacheEntry<string> value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (!value)
                value = StringResponses[key];

            if (!value)
                return;

            using (StringResponseLock.LockWrite())
            {
                CurrentStringResponseCacheSize -= (ulong)value.Response.Length;
                StringResponses.Remove(key);
            }
        }

        protected void AddStringResponseEntry(string key, ResponseCacheEntry<string> value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            using (StringResponseLock.LockWrite())
            {
                CurrentStringResponseCacheSize -= (ulong)value.Response.Length;
                StringResponses.Remove(key);
            }
        }

        protected class ResponseCacheEntry<T> : NullCheckable
        {
            internal DateTime LastUpdatedDateTime;
            internal TimeSpan? RefreshTime;
            internal T Response;
            internal ulong Count = 0;
            internal DateTime LastRequestedDateTime = DateTime.UtcNow;

            internal void Increment()
            {
                Count++;
                LastRequestedDateTime = DateTime.UtcNow;
            }
        }
    }
}
