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
                // ExtentionMethods.DecodeHtml
                // ExtentionMethods.EncodeHtml

                Dictionary<string, string> html = new Dictionary<string, string> { { "", "" }, { "&amp;", "&" }, { "hello", "hello" }, { "&#228;", "ä" }, { "&lt;&gt;", "<>" } };

                foreach (var kvpair in html)
                {
                    Assert.AreEqual(kvpair.Value, kvpair.Key.DecodeHtml());
                    Assert.AreEqual(kvpair.Value.EncodeHtml(), kvpair.Key);
                    Assert.AreEqual(kvpair.Value.EncodeHtml().DecodeHtml(), kvpair.Key.DecodeHtml());
                    Assert.AreEqual(kvpair.Value.EncodeHtml(), kvpair.Key.DecodeHtml().EncodeHtml());
                }
            }

            {
                // ExtentionMethods.DecodeUrl
                // ExtentionMethods.EncodeUrl

                Dictionary<string, string> html = new Dictionary<string, string> { { "", "" }, { "+", " " }, { "%26", "&" }, { "hello", "hello" } };

                foreach (var kvpair in html)
                {
                    Assert.AreEqual(kvpair.Value, kvpair.Key.DecodeUrl());
                    Assert.AreEqual(kvpair.Value.EncodeUrl(), kvpair.Key);
                    Assert.AreEqual(kvpair.Value.EncodeUrl().DecodeUrl(), kvpair.Key.DecodeUrl());
                    Assert.AreEqual(kvpair.Value.EncodeUrl(), kvpair.Key.DecodeUrl().EncodeUrl());
                }
            }

            {
                // ExtentionMethods.GetIndex

                List<int> a = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };
                int[] b = new[] { 0, 1, 2, 3, 4, 5, 6, 7 };
                List<int> c = new List<int> { 140, 3460, 11, 0, -11, 2409, 10 };

                for (int i = 0; i < a.Count; i++)
                    Assert.AreEqual(i, a.GetIndex(i));

                for (int i = 0; i < b.Length; i++)
                    Assert.AreEqual(i, b.GetIndex(i));

                for (int i = 0; i < c.Count; i++)
                    Assert.AreEqual(i, c.GetIndex(c[i]));

                Assert.AreEqual(null, a.GetIndex(-1));
                Assert.AreEqual(null, b.GetIndex(-1));
                Assert.AreEqual(null, b.GetIndex(-1));

                List<string> d = new List<string> { };

                Assert.AreEqual(null, d.GetIndex(null));

                d.Add("");
                Assert.AreEqual(null, d.GetIndex(null));
                Assert.AreEqual(0, d.GetIndex(""));

                d.Add("");
                Assert.AreEqual(0, d.GetIndex(""));

                d[0] = "not empty";
                Assert.AreEqual(1, d.GetIndex(""));
            }

            {
                // ExtentionMethods.GetRelativeLink

                Assert.AreEqual("a", "../a".GetRelativeLink("abc/"));
                Assert.AreEqual("abc/a", "a".GetRelativeLink("abc/"));
                Assert.AreEqual("abc/a", "./a".GetRelativeLink("abc/"));
                Assert.AreEqual("a", "/a".GetRelativeLink("abc/"));
                Assert.AreEqual("http://test.com/abc/a", "../a".GetRelativeLink("http://test.com/abc/b/"));
                Assert.AreEqual("http://test.com/a", "../../a".GetRelativeLink("http://test.com/abc/b/"));
                Assert.AreEqual("http://test.com/abc/a", "a".GetRelativeLink("http://test.com/abc/"));
                Assert.AreEqual("http://test.com/abc/a", "./a".GetRelativeLink("http://test.com/abc/"));
                Assert.AreEqual("http://test.com/a", "/a".GetRelativeLink("http://test.com/abc/"));
            }

            {
                // ExtentionMethods.SafeMessage

                string message = "This is the message.";
                Assert.AreEqual(message, new Exception(message).SafeMessage());
            }

            {
                // ExtentionMethods.SafeToString

                try
                {
                    new Exception().SafeToString();
                }
                catch
                {
                    Assert.Fail();
                }
            }

            {
                // ExtentionMethods.Contains
                // ExtentionMethods.SubsequenceContains
                // ExtentionMethods.SubsequenceContainsString

                List<int[]> a = new List<int[]> { new[] { 1, 2, 3, 4, 5 }, new[] { 6, 7, 8, 9, 10 }, new[] { -1, -2, -3, -4, -5 }, new[] { -1, -1, -1, -1 } };
                List<string> b = new List<string> { "123", "456", "789", "sample text", "test text", "TEST_TEXT" };

                Assert.IsTrue(a.SubsequenceContains(new[] { 2, 3, 4 }));
                Assert.IsTrue(a.SubsequenceContains(new[] { 1, 2, 3, 4 }));
                Assert.IsTrue(a.SubsequenceContains(new[] { 1, 2, 3, 4, 5 }));
                Assert.IsTrue(a.SubsequenceContains(new[] { 4, 5 }));
                Assert.IsTrue(a.SubsequenceContains(new[] { 5 }));
                Assert.IsTrue(a.SubsequenceContains(new[] { 1 }));
                Assert.IsTrue(a.SubsequenceContains(new[] { 3 }));
                Assert.IsTrue(a.SubsequenceContains(new[] { -1 }));
                Assert.IsTrue(a.SubsequenceContains(new[] { -1, -1, -1 }));
                Assert.IsTrue(a.SubsequenceContains(new[] { -1, -2, -3, -4, -5 }));
                Assert.IsTrue(a.SubsequenceContains(new[] { -2, -3, -4, -5 }));
                Assert.IsTrue(a.SubsequenceContains(new[] { -1, -2, -3, -4 }));
                Assert.IsTrue(a.SubsequenceContains(new[] { -3, -4 }));
                Assert.IsTrue(a.SubsequenceContains(new[] { -3 }));

                Assert.IsFalse(a.SubsequenceContains(new[] { 0 }));
                Assert.IsFalse(a.SubsequenceContains(new[] { 1, 2, 3, 4, 5, 6 }));
                Assert.IsFalse(a.SubsequenceContains(new[] { -1, -1, -1, -1, -1, -1 }));

                Assert.IsTrue(b.SubsequenceContainsString("2"));
                Assert.IsTrue(b.SubsequenceContainsString("456"));
                Assert.IsTrue(b.SubsequenceContainsString("78"));
                Assert.IsTrue(b.SubsequenceContainsString("89"));
                Assert.IsTrue(b.SubsequenceContainsString("TEST_TEXT"));
                Assert.IsTrue(b.SubsequenceContainsString("test text"));

                Assert.IsFalse(b.SubsequenceContainsString("test_text"));
                Assert.IsFalse(b.SubsequenceContainsString("sample text "));
            }

            {
                // ExtentionMethods.ToBitString
                // ExtentionMethods.ToHexString

                Assert.AreEqual("", 0.ToBitString().TrimStart('0'));
                Assert.AreEqual("1", 1.ToBitString().TrimStart('0'));

                for (int i = 0; i < sizeof(ulong); i++)
                    Assert.AreEqual("1" + new string('0', i), ((ulong)(1 << i)).ToBitString().TrimStart('0'));

                Assert.AreEqual("110110", ((byte)54).ToBitString().TrimStart('0'));
                Assert.AreEqual(sizeof(byte) << 3, ((byte)54).ToBitString().Length);
                Assert.AreEqual("110110", ((sbyte)54).ToBitString().TrimStart('0'));
                Assert.AreEqual(sizeof(sbyte) << 3, ((sbyte)54).ToBitString().Length);
                Assert.AreEqual("110110", ((short)54).ToBitString().TrimStart('0'));
                Assert.AreEqual(sizeof(short) << 3, ((short)54).ToBitString().Length);
                Assert.AreEqual("110110", ((ushort)54).ToBitString().TrimStart('0'));
                Assert.AreEqual(sizeof(ushort) << 3, ((ushort)54).ToBitString().Length);
                Assert.AreEqual("110110", ((int)54).ToBitString().TrimStart('0'));
                Assert.AreEqual(sizeof(int) << 3, ((int)54).ToBitString().Length);
                Assert.AreEqual("110110", ((uint)54).ToBitString().TrimStart('0'));
                Assert.AreEqual(sizeof(uint) << 3, ((uint)54).ToBitString().Length);
                Assert.AreEqual("110110", ((long)54).ToBitString().TrimStart('0'));
                Assert.AreEqual(sizeof(long) << 3, ((long)54).ToBitString().Length);
                Assert.AreEqual("110110", ((ulong)54).ToBitString().TrimStart('0'));
                Assert.AreEqual(sizeof(ulong) << 3, ((ulong)54).ToBitString().Length);

                Assert.AreEqual("0BADF00D", 0x0BADF00D.ToHexString());
                Assert.AreEqual("00000000", 0.ToHexString());

                Assert.AreEqual("FF", ((byte)0xFF).ToHexString());
                Assert.AreEqual("7F", ((sbyte)0x7F).ToHexString());
                Assert.AreEqual("FFFF", ((ushort)0xFFFF).ToHexString());
                Assert.AreEqual("7FFF", ((short)0x7FFF).ToHexString());
                Assert.AreEqual("FFFFFFFF", ((uint)0xFFFFFFFF).ToHexString());
                Assert.AreEqual("7FFFFFFF", ((int)0x7FFFFFFF).ToHexString());
                Assert.AreEqual("FFFFFFFFFFFFFFFF", ((ulong)0xFFFFFFFFFFFFFFFF).ToHexString());
                Assert.AreEqual("7FFFFFFFFFFFFFFF", ((long)0x7FFFFFFFFFFFFFFF).ToHexString());
            }

            {
                // ExtentionMethods.ToEnumerable
                // ExtentionMethods.StartsWith

                object a = 1;
                object b = "b";
                object c = new Exception();
                object d = new List<string>() { "hello" };
                object e = new[] { -1 };
                object f = new int?(5);
                object g = null;

                object[] all = { a, b, c, d, e, f, g };

                var t0 = Tuple.Create(a);
                var t1 = Tuple.Create(a, b);
                var t2 = Tuple.Create(a, b, c);
                var t3 = Tuple.Create(a, b, c, d);
                var t4 = Tuple.Create(a, b, c, d, e);
                var t5 = Tuple.Create(a, b, c, d, e, f);
                var t6 = Tuple.Create(a, b, c, d, e, f, g);

                Assert.IsTrue(all.StartsWith(t0.ToEnumerable()));
                Assert.IsTrue(all.StartsWith(t1.ToEnumerable()));
                Assert.IsTrue(all.StartsWith(t2.ToEnumerable()));
                Assert.IsTrue(all.StartsWith(t3.ToEnumerable()));
                Assert.IsTrue(all.StartsWith(t4.ToEnumerable()));
                Assert.IsTrue(all.StartsWith(t5.ToEnumerable()));
                Assert.IsTrue(all.StartsWith(t6.ToEnumerable()));

                Assert.IsTrue(t1.ToEnumerable().StartsWith(t0.ToEnumerable()));
                Assert.IsTrue(t2.ToEnumerable().StartsWith(t1.ToEnumerable()));
                Assert.IsTrue(t3.ToEnumerable().StartsWith(t2.ToEnumerable()));
                Assert.IsTrue(t4.ToEnumerable().StartsWith(t3.ToEnumerable()));
                Assert.IsTrue(t5.ToEnumerable().StartsWith(t4.ToEnumerable()));
                Assert.IsTrue(t6.ToEnumerable().StartsWith(t5.ToEnumerable()));

                Assert.IsFalse(t0.ToEnumerable().StartsWith(t1.ToEnumerable()));

                Assert.IsTrue(all.StartsWith(new[] { a, b, c, d, e, f, g }));
                Assert.IsTrue(all.StartsWith(new[] { a, b, c }));
                Assert.IsTrue(all.StartsWith(new[] { a }));
                Assert.IsTrue(all.StartsWith(new object[0]));

                Assert.IsFalse(all.StartsWith(new[] { g }));
                Assert.IsFalse(all.StartsWith(new[] { a, b, c, d, e, f, f }));
                Assert.IsFalse(all.StartsWith(new[] { a, b, c, d, e, g }));
            }

            {
                // ExtentionMethods.ToSeparatedValueString

                Assert.AreEqual("", new string[] { }.ToSeparatedValueString());
                Assert.AreEqual(", ", new string[] { "", "" }.ToSeparatedValueString());
                Assert.AreEqual(". ", new string[] { "", "" }.ToSeparatedValueString(". "));
                Assert.AreEqual("a", new string[] { "a" }.ToSeparatedValueString());
                Assert.AreEqual("a, , ", new string[] { "a", null, null }.ToSeparatedValueString());
            }
        }
    }
}
