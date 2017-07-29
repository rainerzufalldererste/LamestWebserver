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
            new ResponseCacheTest().TestResponseCache();
            new PacketUnitTest().TestCookies();
            new PacketUnitTest().TestHttpHead();
            new PacketUnitTest().TestHttpPost();
            new PacketUnitTest().TestHttpCombined();
            new PacketUnitTest().TestHttpWebSocketUpgrade();
            new PacketUnitTest().TestHttpModifiedDate();
            new CompressionTest().TestCompressDecompress();
            new EncryptDecryptTests().TestEncryptDecrypt();
            new CollectionSerializerTests().TestCollectionJsonSerialisazion();
            new UsableMutexTest().TestUsableMutexes();
            new WriteLockTest().TestWriteLock();
            new CollectionUnitTests().TestSerializeMultiple();
            new CollectionUnitTests().TestSerializeClassAvlTree();
            new CollectionUnitTests().TestSerializeClassAvlHashMap();
            new CollectionUnitTests().TestAvlHashMaps();
            new CollectionUnitTests().TestAvlTrees();
            new CollectionUnitTests().TestQueuedAvlTreesError();
            new CollectionUnitTests().TestQueuedAvlTrees();
            new PasswordTest().TestPassword();
            new PasswordTest().TestSerializePassword();

            LamestWebserver.ServerHandler.StopHandler();
            LamestWebserver.Master.StopServers();
        }
    }
}
