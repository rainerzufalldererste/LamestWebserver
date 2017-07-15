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
        public ulong? StringResponseCacheMaxSize = 1024 * 1024 * 256; // 512 MBytes because of two byte characters bt default. 

        private readonly Singleton<AVLHashMap<string, ResponseCacheEntry<string>>> StringResponses = new Singleton<AVLHashMap<string, ResponseCacheEntry<string>>>(() => new AVLHashMap<string, ResponseCacheEntry<string>>(StringResponseCacheHashmapSize));
        private UsableWriteLock StringResponseLock = new UsableWriteLock();

        public bool GetCachedStringResponse(string key, out string response)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            ResponseCacheEntry<string> result;

            using (StringResponseLock.LockRead())
                result = StringResponses.Instance[key];

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
                    StringResponses.Instance.Remove(key);
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

            ResponseCacheEntry<string> entry = new ResponseCacheEntry<string>()
            {
                LastUpdatedDateTime = DateTime.UtcNow,
                RefreshTime = refreshTime,
                Response = response
            };

            using (StringResponseLock.LockWrite())
            {
                CurrentStringResponseCacheSize += (ulong)entry.Response.Length;

                StringResponses.Instance[key] = entry;
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

        public ulong CacheMakeRoom_AdditionalFreeSpace = 1024 * 1024 * 16; // 32 MByte due to two byte characters.

        public virtual void MakeRoom(ulong requestedSpace)
        {
            if (!StringResponses.Instance.Any())
                return;

            KeyValuePair<string, ResponseCacheEntry<string>>[] responses;

            using (StringResponseLock.LockRead())
                responses = StringResponses.Instance.ToArray();

            // Remove outdated.
            foreach (var kvpair in responses)
                if (kvpair.Value.RefreshTime != null && kvpair.Value.LastUpdatedDateTime + kvpair.Value.RefreshTime < DateTime.UtcNow)
                    RemoveStringResponseEntry(kvpair.Key, kvpair.Value);

            DateTime upperTenthPercentileDate;
            ulong upperTenthPercentileCount;
            int upperTenthPercentileLength;

            KeyValuePair<string, ResponseCacheEntry<string>>[] DateSorted, CountSorted, SizeSorted;

            using (StringResponseLock.LockWrite())
            {
                DateSorted = StringResponses.Instance.OrderBy((kvpair) => { return kvpair.Value.LastRequestedDateTime; }).ToArray();
                upperTenthPercentileDate = DateSorted.ToArray()[(int)(DateSorted.Length * 0.9)].Value.LastRequestedDateTime;

                CountSorted = StringResponses.Instance.OrderBy((kvpair) => { return kvpair.Value.Count; }).ToArray();
                upperTenthPercentileCount = CountSorted.ToArray()[(int)(CountSorted.Length * 0.9)].Value.Count;

                SizeSorted = StringResponses.Instance.OrderBy((kvpair) => { return kvpair.Value.Response.Length; }).ToArray();
                upperTenthPercentileLength = CountSorted.ToArray()[(int)(SizeSorted.Length * 0.9)].Value.Response.Length;
                
                // Remove by size if not in upper tenth percentile.
                for (int i = (int)(SizeSorted.Length * 0.25); i >= 0; i--)
                {
                    if (CurrentStringResponseCacheSize + requestedSpace <= StringResponseCacheMaxSize)
                        return;

                    var response = StringResponses.Instance[SizeSorted[i].Key];

                    if (response.Count <= upperTenthPercentileCount && response.LastRequestedDateTime <= upperTenthPercentileDate)
                        RemoveStringResponseEntry(SizeSorted[i].Key, response);
                }

                int j = 0;

                // Remove by count and last access time if not in upper tenth percentile.
                while (CurrentStringResponseCacheSize + requestedSpace + CacheMakeRoom_AdditionalFreeSpace > StringResponseCacheMaxSize && StringResponses.Instance.Any())
                {
                    var response = StringResponses.Instance[DateSorted[j].Key];

                    if (response && response.Count <= upperTenthPercentileCount)
                        RemoveStringResponseEntry(DateSorted[j].Key, response);

                    response = StringResponses.Instance[CountSorted[j].Key];

                    if (response && response.LastRequestedDateTime <= upperTenthPercentileDate)
                        RemoveStringResponseEntry(CountSorted[j].Key, response);

                    j++;
                }

                // Remove by time left till refresh till percentage.
                var SortedTimeLeftTillRefresh = (from p in StringResponses.Instance where p.Value.RefreshTime.HasValue select new { kvpair = p, timeLeft = DateTime.UtcNow - p.Value.LastUpdatedDateTime + p.Value.RefreshTime }).OrderBy((k) => k.timeLeft).ToArray();

                for (int i = 0; i < SortedTimeLeftTillRefresh.Length * 0.25; i++)
                {
                    if (CurrentStringResponseCacheSize + requestedSpace + CacheMakeRoom_AdditionalFreeSpace <= StringResponseCacheMaxSize)
                        return;

                    var response = StringResponses.Instance[SortedTimeLeftTillRefresh[j].kvpair.Key];

                    RemoveStringResponseEntry(SortedTimeLeftTillRefresh[j].kvpair.Key, response);
                }

                // Remove by count and last accesstime regardless of percentiles.
                j = 0;

                // Remove by count and last access time if not in upper tenth percentile.
                while (CurrentStringResponseCacheSize + requestedSpace + CacheMakeRoom_AdditionalFreeSpace > StringResponseCacheMaxSize && StringResponses.Instance.Any())
                {
                    var response = StringResponses.Instance[DateSorted[j].Key];
                    RemoveStringResponseEntry(DateSorted[j].Key, response);

                    if (!(CurrentStringResponseCacheSize + requestedSpace + CacheMakeRoom_AdditionalFreeSpace > StringResponseCacheMaxSize && StringResponses.Instance.Any()))
                        return;

                    response = StringResponses.Instance[CountSorted[j].Key];
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
                value = StringResponses.Instance[key];

            if (!value)
                return;

            using (StringResponseLock.LockWrite())
            {
                CurrentStringResponseCacheSize -= (ulong)value.Response.Length;
                StringResponses.Instance.Remove(key);
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
                StringResponses.Instance.Remove(key);
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
