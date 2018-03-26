using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Caching;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests
{
    [TestClass]
    public class ResponseCacheTest
    {
        [TestMethod]
        public void TestResponseCache()
        {
            Console.WriteLine("Testing Response Cache...");

            List<KeyValuePair<string, string>> comparer = new List<KeyValuePair<string, string>>();

            ResponseCache cache = new ResponseCache();
            cache.MaximumStringResponseCacheSize = 1024 * 128;
            cache.CacheMakeRoom_AdditionalFreeSpacePercentage.Value = 0;

            string s = new string((char)(0 + 50), 1024 * 2 - 1);
            cache.SetCachedStringResponse(0.ToString(), s);
            comparer.Add(new KeyValuePair<string, string>(0.ToString(), s));

            for (int i = 1; i < 127; i++)
            {
                s = new string((char)(i + 50), 1024);
                cache.SetCachedStringResponse(i.ToString(), s);
                comparer.Add(new KeyValuePair<string, string>(i.ToString(), s));
                Assert.AreEqual((ulong)comparer.Sum(x => x.Value.Length), cache.CurrentStringResponseCacheSize);
            }

            for (int i = 0; i < 127; i++)
            {
                Assert.IsTrue(cache.GetCachedStringResponse(comparer[i].Key, out s));
                Assert.AreEqual(comparer[i].Value, s);
            }

            int num = -1;
            s = new string((char)(num + 50), 1024);
            cache.SetCachedStringResponse(num.ToString(), s);
            comparer.Add(new KeyValuePair<string, string>(num.ToString(), s));

            string response;

            Assert.IsFalse(cache.GetCachedStringResponse(comparer[0].Key, out response));

            comparer.RemoveAt(0);

            Assert.AreEqual((ulong)comparer.Sum(x => x.Value.Length), cache.CurrentStringResponseCacheSize);

            Assert.IsTrue(cache.GetCachedStringResponse(comparer.Last().Key, out response));
            Assert.AreEqual(s, response);

            for (int i = comparer.Count - 1; i >= 0; i--)
            {
                cache.RemoveCachedString(comparer[i].Key);
                comparer.RemoveAt(i);
                Assert.AreEqual((ulong)comparer.Sum(x => x.Value.Length), cache.CurrentStringResponseCacheSize);
            }

            Assert.AreEqual(0ul, cache.CurrentStringResponseCacheSize);

            s = new string((char)(num + 50), 1024);
            cache.SetCachedStringResponse(num.ToString(), s);
            comparer.Add(new KeyValuePair<string, string>(num.ToString(), s));

            cache.Clear();
            comparer.Clear();
            
            Assert.AreEqual(0ul, cache.CurrentStringResponseCacheSize);
        }
    }
}
