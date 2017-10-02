using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver.Core.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class StringExtentionTests
    {
        [TestMethod]
        public void TestParsingStringExtentions_TestKMP()
        {
            string a = "abcabcccabcabccacacbbabca";
            int[] a_ = new int[] { 0,0,0,0,1,2,3,0,0,1,2,3,4,5,6,7,1,0,1,0,0,0,1,2,3 };
            int[] a_ret = a.GetKMP();

            Assert.AreEqual(a_.Length, a_ret.Length);

            for (int i = 0; i < a_.Length; i++)
                Assert.AreEqual(a_[i], a_ret[i]);
        }

        [TestMethod]
        public void TestParsingStringExtentions_TestSubStringIndex()
        {
            string a = "0123456789";
            int a_index, b_index;
            Assert.IsTrue(a.FindString("5", out a_index));
            Assert.AreEqual(5, a_index);
            Assert.IsFalse(a.FindString("a", out a_index));

            string b = "abcdefgABCabcDEFG";
            Assert.IsFalse(b.FindString("ABCD", out b_index));
            Assert.IsTrue(b.FindString("ABC", out b_index));
            Assert.AreEqual(7, b_index);
        }

        [TestMethod]
        public void TestParsingStringExtentions_TestStringBetween()
        {
            string a = "012BEFOREchickenAFTER0";
            Assert.AreEqual("chicken", a.FindBetween("BEFORE", "AFTER"));
            Assert.IsNull(a.FindBetween("BEFORE", "Echicken"));
            Assert.IsNull(a.FindBetween("BEFORE", "AFTER01"));
            Assert.AreEqual("chicken", a.FindBetween("BEFORE", "AFTER0"));
            Assert.AreEqual("chicken", a.FindBetween("012BEFORE", "AFTER0"));
        }

        [TestMethod]
        public void TestParsingStringExtentions_TestSplitIncludingDelimiters()
        {
            string a = ".........";
            string[] a_ = new string[] { ".....", "...", "." };
            List<string> a_ret = a.Parse(true, ".....", "...", ".");
            
            Assert.AreEqual(a_.Length, a_ret.Count);

            for (int i = 0; i < a_.Length; i++)
                Assert.AreEqual(a_[i], a_ret[i]);
            

            string b = "..hello......world.!...!";
            string[] b_ = new string[] { ".", ".", "hello", ".....", ".", "world", ".", "!", "...", "!" };
            List<string> b_ret = b.Parse(true, ".....", "...", ".");

            Assert.AreEqual(b_.Length, b_ret.Count);

            for (int i = 0; i < b_.Length; i++)
                Assert.AreEqual(b_[i], b_ret[i]);
        }
    }
}
