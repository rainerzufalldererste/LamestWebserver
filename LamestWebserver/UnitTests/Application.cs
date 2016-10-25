using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    public static class Application
    {
        public static void Main(string[] args)
        {
            new CollectionUnitTests().testAVLHashMaps();
            new CollectionUnitTests().testAVLTrees();
            new CollectionUnitTests().testQueuedAVLTrees();
            new PacketUnitTest().TestCookies();
        }
    }
}
