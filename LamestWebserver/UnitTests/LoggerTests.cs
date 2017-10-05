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
        private const string Pat_TO_PARALLEL_FILE = "parallelLog.log";

        [TestMethod]
        public void LoggerTestFileSwitch()
        {
            string autoLogFile = Logger.FilePath;
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

            Logger.customStreams.Add(File.Open(Pat_TO_PARALLEL_FILE, FileMode.Append,FileAccess.Write));

            for (int i = 0; i < 1000; i++)
            {
                Logger.LogInformation(i + " stuff3");
            }

          
            Logger.OutputSourceFlags = Logger.EOutputSource.None;
            long FI = new FileInfo(PATH_TO_NEW_FILE).Length;

            for (int i = 0; i < 1000; i++)
            {
                Logger.LogInformation(i + " stuff4");
            }
            Assert.IsTrue(new FileInfo(PATH_TO_NEW_FILE).Length == FI);


            File.Delete(autoLogFile);
            File.Delete(PATH_TO_NEW_FILE);
            File.Delete(Pat_TO_PARALLEL_FILE);            

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
