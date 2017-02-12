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
            new CollectionSerializerTests().testCollectionJsonSerialisazion();
            new UsableMutexTest().TestUsableMutexes();
            new WriteLockTest().TestWriteLock();
            new CollectionUnitTests().testSerializeMultiple();
            new CollectionUnitTests().testSerializeClassAVLTree();
            new CollectionUnitTests().testSerializeClassAVLHashMap();
            new CollectionUnitTests().testSerializeClassQueuedAVLTree();
            new CollectionUnitTests().testAVLHashMaps();
            new CollectionUnitTests().testAVLTrees();
            new CollectionUnitTests().testQueuedAVLTrees();
            new PacketUnitTest().TestCookies();
            new PasswordTest().testPassword();
            new PasswordTest().testSerializePassword();
        }
    }
}
