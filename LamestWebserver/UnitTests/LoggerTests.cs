using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Core;
using System.IO;
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
        private const string PATH_TO_NEW_FILE = "newFile.log";

        [TestMethod]
        public void LoggerTestFileSwitch()
        {
            
            for (int i = 0; i < 1000; i++)
            {
                Logger.LogInformation(i + "stuff");
            }

            Assert.IsTrue(File.Exists(Logger.FilePath));
            Assert.IsTrue(new FileInfo(Logger.FilePath).Length > 0);
            
            Logger.FilePath = PATH_TO_NEW_FILE;

            for (int i = 0; i < 1000; i++)
            {
                Logger.LogInformation(i + " stuff2");
            }

            Assert.IsTrue(File.Exists(PATH_TO_NEW_FILE));
            Assert.IsTrue(new FileInfo(PATH_TO_NEW_FILE).Length > 0);

            Logger.customStreams.Add(File.OpenWrite("parallelLog.log"));

            for (int i = 0; i < 1000; i++)
            {
                Logger.LogInformation(i + " stuff3");
            }
        }

        [TestMethod]
        public void LoggerTestActionListTest()
        {
            int operationCount = 0;
            ActionList<int> actionList = new ActionList<int>(
                () => operationCount++);

            actionList.Add(5);
            actionList.Add(4);
            actionList.Add(12);
            Assert.IsFalse(operationCount == 10);
            Assert.IsTrue(operationCount == 3);
        }

    }
}
