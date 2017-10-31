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
            new FixedSizeQueueTests().TestFixedSizeQueue();
            new FlushableMemoryPoolTests().TestFlushableMemoryPool();
            new BitListTests().TestBitList();
            new IDTests().TestID();
            new IDTests().TestLongID();
            new ClampedValueTest().TestClampedValue();
            new WebCrawlerTest().TestWebCrawler();
            new StringExtentionTests().TestParsingStringExtentions_TestSplitIncludingDelimiters();
            new StringExtentionTests().TestParsingStringExtentions_TestStringBetween();
            new StringExtentionTests().TestParsingStringExtentions_TestSubStringIndex();
            new StringExtentionTests().TestParsingStringExtentions_TestKMP();
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
            new UsableLockerSimpleTest().TestUsableLockerSimple();
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
