﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Security;
using LamestWebserver;
using LamestWebserver.Serialization;

namespace UnitTests
{
    [TestClass]
    public class PasswordTest
    {
        [TestMethod]
        public void TestPassword()
        {
            Console.WriteLine("Testing Passwords...\n" + new string('_', 1024/16));

            for (int i = 0; i < 1024; i++)
            {
                if (i % 16 == 0)
                    Console.Write(".");

                string passw = SessionContainer.GenerateHash();

                Password password = new Password(passw);
                
                Assert.IsTrue(password.IsValid(passw));

                for (int j = 0; j < 2048; j++)
                {
                    Assert.IsFalse(password.IsValid(SessionContainer.GenerateHash()));
                }
            }

            Console.WriteLine();
        }

        [TestMethod]
        public void TestSerializePassword()
        {
            Console.WriteLine("Testing Serialized Passwords...\n" + new string('_', 1024 / 16));

            for (int i = 0; i < 512; i++)
            {
                if (i % 8 == 0)
                    Console.Write(".");

                string passw = SessionContainer.GenerateComplexHash();

                Password password = new Password(passw);

                Assert.IsTrue(password.IsValid(passw));

                for (int j = 0; j < 128; j++)
                {
                    Assert.IsFalse(password.IsValid(SessionContainer.GenerateComplexHash()));
                }

                Serializer.WriteXmlData(new Password[] { password, password, new Password(" ") }, "pw");

                Password[] pws = Serializer.ReadXmlData<Password[]>("pw");
                
                Assert.IsTrue(pws.Length == 3);

                Assert.IsTrue(pws[0] != null);
                Assert.IsTrue(pws[1] != null);
                Assert.IsTrue(pws[2] != null);

                Assert.IsTrue(pws[0].IsValid(passw));
                Assert.IsTrue(pws[1].IsValid(passw));
                Assert.IsFalse(pws[2].IsValid(passw));
                Assert.IsTrue(pws[2].IsValid(" "));

                for (int j = 0; j < 256; j++)
                {
                    Assert.IsFalse(pws[0].IsValid(SessionContainer.GenerateComplexHash()));
                    Assert.IsFalse(pws[1].IsValid(SessionContainer.GenerateComplexHash()));
                    Assert.IsFalse(pws[2].IsValid(SessionContainer.GenerateComplexHash()));
                }
            }

            Console.WriteLine();
        }
    }
}
