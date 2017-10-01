using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class LoggerTests
    {
        [TestMethod]
        public void LoggerTest() => Logger.LogInformation("Bla Bla");

        [TestMethod]
        public void LoggerTestFileSwitch()
        {
            for (int i = 0; i < 1000; i++)
            {
                Logger.LogInformation(i +" stuff");
            }

            Logger.FilePath = "newFile.log";

            for (int i = 0; i < 1000; i++)
            {
                Logger.LogInformation(i + " stuff2");
            }



        }

    }
}
