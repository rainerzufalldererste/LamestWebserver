using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Core;

namespace UnitTests
{
    [TestClass]
    public class CoreUnitTests
    {
        [TestMethod]
        public void TestExtentionMethods()
        {
            {
                // ExtentionMethods.ContainsEqualSequence
                List<int[]> a = new List<int[]> { new[] { 1, 2, 3, 4 }, new[] { 5, 6, 7 }, new[] { 11, 12, 13 }, new[] { 0 }, new[] { -1, -2, -3 } };
                List<int[]> b = new List<int[]> { new[] { 1, 2, 3, 4, 5 }, new[] { 5, 6, 7, 8 }, new[] { 11, 12, 13, 14 }, new[] { 0, 1 }, new[] { -1, -2, -3, -4 } };
                List<int[]> c = new List<int[]> { new[] { 1 }, new[] { 2 }, new[] { 3 }, new[] { 4 }, new[] { -1, -2 } };
                List<int> d = new List<int>();
                List<int> e = new List<int> { 0 };
                List<int> f = new List<int> { 5, 6, 7 };

                foreach (var list in a)
                    Assert.IsTrue(a.ContainsEqualSequence(list));

                foreach (var list in b)
                    Assert.IsFalse(a.ContainsEqualSequence(list));

                foreach (var list in c)
                    Assert.IsFalse(a.ContainsEqualSequence(list));

                Assert.IsFalse(a.ContainsEqualSequence(d));
                Assert.IsTrue(a.ContainsEqualSequence(e));
                Assert.IsTrue(a.ContainsEqualSequence(f));
            }

            {
                //ExtentionMethods.DecodeHtml
                //ExtentionMethods.EncodeHtml

                Dictionary<string, string> html = new Dictionary<string, string> { { "", "" }, { "&amp;", "&" }, { "hello", "hello" }, { "&auml;", "ä" }, { "&ouml;", "ö" }, { "&uuml;", "ü" }, { "&szlig;", "ß" }, { "&Auml;", "Ä" }, { "&Uuml;", "Ü" } };

                foreach (var kvpair in html)
                {
                    Assert.AreEqual(kvpair.Value, kvpair.Key.DecodeHtml());
                    Assert.AreEqual(kvpair.Value.EncodeHtml(), kvpair.Key);
                    Assert.AreEqual(kvpair.Value.EncodeHtml().DecodeHtml(), kvpair.Key.DecodeHtml());
                    Assert.AreEqual(kvpair.Value.EncodeHtml(), kvpair.Key.DecodeHtml().EncodeHtml());
                }
            }
        }
    }
}
