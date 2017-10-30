using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Core;
using LamestWebserver.Collections;
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
        private const string PathToNewFile = "newFile.log";
        private const string PathToParallelLog = "parallelLog.log";

        [TestMethod]
        public void TestLoggerFileSwitch()
        {
            Logger.OutputSource = Logger.EOutputSource.File;

            string autoLogFile = Logger.CurrentLogger.Instance.FilePath;

            for (int i = 0; i < 1000; i++)
            {
                Logger.LogInformation(i + "stuff");
            }

            Assert.IsTrue(File.Exists(Logger.CurrentLogger.Instance.FilePath));
            Assert.IsTrue(new FileInfo(Logger.CurrentLogger.Instance.FilePath).Length > 0);

            Logger.CurrentLogger.Instance.FilePath = PathToNewFile;

            for (int i = 0; i < 1000; i++)
            {
                Logger.LogInformation(i + " stuff2");
            }

            Assert.IsTrue(File.Exists(PathToNewFile));
            Assert.IsTrue(new FileInfo(PathToNewFile).Length > 0);

            Logger.CurrentLogger.Instance.AddCustomStream(File.Open(PathToParallelLog, FileMode.Append,FileAccess.Write));
            Logger.CurrentLogger.Instance.RestartStream();

            for (int i = 0; i < 1000; i++)
            {
                Logger.LogInformation(i + " stuff3");
            }

            Logger.CurrentLogger.Instance.OutputSourceFlags = Logger.EOutputSource.None;
            long FI = new FileInfo(PathToNewFile).Length;

            for (int i = 0; i < 1000; i++)
            {
                Logger.LogInformation(i + " stuff4");
            }
            Assert.IsTrue(new FileInfo(PathToNewFile).Length == FI);

            //Clean up
            File.Delete(autoLogFile);
            File.Delete(PathToNewFile);
            File.Delete(PathToParallelLog);
        }

        [TestMethod]
        public void TestLoggerActionList()
        {
            int operationCount = 0;
            ActionList<int> actionList = new ActionList<int>(() => operationCount++);

            actionList.Add(5);
            actionList.Add(4);
            actionList.Add(12);

            Assert.AreEqual(3, operationCount);

            Assert.AreEqual(5, actionList[0]);
            Assert.AreEqual(4, actionList[1]);
            Assert.AreEqual(12, actionList[2]);

            actionList.RemoveAt(0);

            Assert.AreEqual(4, operationCount);

            try
            {
                actionList.RemoveAt(300);
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException) { }

            Assert.AreEqual(4, operationCount);
        }

    }
}
