using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver;
using System.Net;
using System.Linq;
using System.Collections.Generic;

namespace UnitTests
{
    [TestClass]
    public class PacketUnitTest
    {
        [TestMethod]
        public void TestCookies()
        {
            Console.WriteLine("Testing HttpRequest Cookies...");

            string packet = "POST /secure/Passport.aspx?popup=1&wa=wsignin1.0 HTTP/1.1\r\nHost: www.blob.com\r\nUser-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64; rv:48.0) Gecko/20100101 Firefox/48.0\r\nAccept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\nAccept-Language: de,en-US;q=0.7,en;q=0.3\r\nAccept-Encoding: gzip, deflate, br\r\nReferer: https://login.live.com/login.srf?wa=wsignin1.0&rpsnv=123&ct=123&rver=6.0.5286.0&wp=MBI&wreply=https:%2F%2fwww.bing.com%2Fsecure%2FPassport.aspx%3Fpopup%3D1&lc=123&id=123&checkda=1\r\nCookie: SRCHD=AF=NOFORM; SRCHUID=V=2&GUID=CA60C8BCDD883D55ABC6A3FD52; SRCHUSR=DOB=20160000; _SS=SID=3614F82F6F05A0F4B8&bIm=4254; _EDGE_S=F=1&SID=3614F7636664B8; _EDGE_V=1; MUID=39415A1EAC6C76; MUIDB=3941EAC6C76; SRCHHPGUSR=CW=12&CH=19&DPR=1&UTC=10; WLS=TS=63651\r\nConnection: keep-alive\r\nUpgrade-Insecure-Requests: 1\r\n\r\n\r\n<content>";

            HttpRequest p = HttpRequest.Constructor(ref packet, null, null);

            Assert.IsTrue(p.Cookies.Count == 10);

            string[] cookieNames = { "SRCHD", "SRCHUID", "SRCHUSR", "_SS", "_EDGE_S", "_EDGE_V", "MUID", "MUIDB", "SRCHHPGUSR", "WLS" };
            string[] cookieValues = { "AF=NOFORM", "V=2&GUID=CA60C8BCDD883D55ABC6A3FD52", "DOB=20160000", "SID=3614F82F6F05A0F4B8&bIm=4254", "F=1&SID=3614F7636664B8", "1", "39415A1EAC6C76", "3941EAC6C76", "CW=12&CH=19&DPR=1&UTC=10", "TS=63651" };

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(p.Cookies[i], new KeyValuePair<string, string>(cookieNames[i], cookieValues[i]));
            }

        }

        [TestMethod]
        public void TestHttpHead()
        {
            Console.WriteLine("Testing HttpRequest Head Variables...");

            string packet = "GET /data?bla=blob HTTP/1.1\r\n\r\nHost: www.blob.com\r\n\r\n";

            HttpRequest p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 1);
            Assert.AreEqual(p.VariablesHttpHead["bla"], "blob");
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);

            packet = "GET /data?bla HTTP/1.1\r\n\r\nHost: www.blob.com\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 1);
            Assert.AreEqual(p.VariablesHttpHead["bla"], "");
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);

            packet = "GET /data? HTTP/1.1\r\n\r\nHost: www.blob.com\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);

            packet = "GET /data?bla& HTTP/1.1\r\n\r\nHost: www.blob.com\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 1);
            Assert.AreEqual(p.VariablesHttpHead["bla"], "");
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);

            packet = "GET /data?bla&blob=2 HTTP/1.1\r\n\r\nHost: www.blob.com\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 2);
            Assert.AreEqual(p.VariablesHttpHead.Keys.ToList()[0], "bla");
            Assert.AreEqual(p.VariablesHttpHead.Values.ToList()[0], "");
            Assert.AreEqual(p.VariablesHttpHead.Keys.ToList()[1], "blob");
            Assert.AreEqual(p.VariablesHttpHead.Values.ToList()[1], "2");
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);

            packet = "GET /data?bla=&blob= HTTP/1.1\r\n\r\nHost: www.blob.com\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 2);
            Assert.AreEqual(p.VariablesHttpHead["bla"], "");
            Assert.AreEqual(p.VariablesHttpHead["blob"], "");
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);

            packet = "GET /data?bla=2&blob=& HTTP/1.1\r\n\r\nHost: www.blob.com\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 2);
            Assert.AreEqual(p.VariablesHttpHead["bla"], "2");
            Assert.AreEqual(p.VariablesHttpHead["blob"], "");
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);

            packet = "GET /search?q=🌵%20%20{}{!(*#blob HTTP/1.1\r\nHost: blob.net\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/search");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 1);
            Assert.AreEqual(p.VariablesHttpHead["q"], "🌵  {}{!(*");
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);

            packet = "GET /search?q=?banana%3Dchicken%26salad%26cheese# HTTP/1.1\r\nHost: blob.net\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/search");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 1);
            Assert.AreEqual(p.VariablesHttpHead["q"], "?banana=chicken&salad&cheese");
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);

            packet = "GET /search?q=?banana%3Dchicken%26salad%26cheese&# HTTP/1.1\r\nHost: blob.net\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/search");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 1);
            Assert.AreEqual(p.VariablesHttpHead["q"], "?banana=chicken&salad&cheese");
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);

            packet = "GET /search?q=?banana%3Dchicken%26salad%26cheese%23&&#&?cheese=false HTTP/1.1\r\nHost: blob.net\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/search");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 1);
            Assert.AreEqual(p.VariablesHttpHead["q"], "?banana=chicken&salad&cheese#");
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);

            packet = "GET /data?bla=2&blob=&&&=meow HTTP/1.1\r\n\r\nHost: www.blob.com\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 3);
            Assert.AreEqual(p.VariablesHttpHead["bla"], "2");
            Assert.AreEqual(p.VariablesHttpHead["blob"], "");
            Assert.AreEqual(p.VariablesHttpHead[""], "meow");
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);
            
            packet = "GET /data%20page?bla=2&blob=&&&=meow%20meow HTTP/1.1\r\n\r\nHost: www.blob.com\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data page");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 3);
            Assert.AreEqual(p.VariablesHttpHead["bla"], "2");
            Assert.AreEqual(p.VariablesHttpHead["blob"], "");
            Assert.AreEqual(p.VariablesHttpHead[""], "meow meow");
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);
        }

        [TestMethod]
        public void TestHttpPost()
        {
            Console.WriteLine("Testing HttpRequest Post Variables...");

            string packet = "POST /data HTTP/1.1\r\n\r\n";

            HttpRequest p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);
            Assert.IsFalse(p.IsIncompleteRequest);

            packet = "POST /data%20page HTTP/1.1\r\nContent-Length: 64\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);
            
            Assert.IsTrue(p.IsIncompleteRequest);

            string newPacket = "search=true";

            p = HttpRequest.Constructor(ref newPacket, packet, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data page");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 1);
            Assert.AreEqual(p.VariablesHttpPost["search"], "true");
            Assert.IsFalse(p.IsIncompleteRequest);

            newPacket = "search=";

            p = HttpRequest.Constructor(ref newPacket, packet, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data page");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 1);
            Assert.AreEqual(p.VariablesHttpPost["search"], "");
            Assert.IsFalse(p.IsIncompleteRequest);

            newPacket = "search=&true=false&";

            p = HttpRequest.Constructor(ref newPacket, packet, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data page");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 2);
            Assert.AreEqual(p.VariablesHttpPost["search"], "");
            Assert.AreEqual(p.VariablesHttpPost["true"], "false");
            Assert.IsFalse(p.IsIncompleteRequest);

            packet = "POST /data%20page%20test HTTP/1.1\r\nContent-Length: 64\r\n\r\nsearch=&q";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data page test");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 2);
            Assert.AreEqual(p.VariablesHttpPost["search"], "");
            Assert.AreEqual(p.VariablesHttpPost["q"], "");
            Assert.AreEqual(p.VariablesHttpPost["anyOther"], null);
            Assert.IsFalse(p.IsIncompleteRequest);

            packet = "POST /data HTTP/1.1\r\nContent-Length: 64\r\n\r\nsearch=&q&&=";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 2);
            Assert.AreEqual(p.VariablesHttpPost["search"], "");
            Assert.AreEqual(p.VariablesHttpPost["q"], "");
            Assert.AreEqual(p.VariablesHttpPost[""], null);
            Assert.IsFalse(p.IsIncompleteRequest);

            packet = "POST /data HTTP/1.1\r\nContent-Length: 64\r\n\r\nsearch=&q&&=aa";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 3);
            Assert.AreEqual(p.VariablesHttpPost["search"], "");
            Assert.AreEqual(p.VariablesHttpPost["q"], "");
            Assert.AreEqual(p.VariablesHttpPost[""], "aa");
            Assert.AreEqual(p.VariablesHttpPost["aa"], null);
            Assert.IsFalse(p.IsIncompleteRequest);

            packet = "POST /data HTTP/1.1\r\nContent-Length: 64\r\n\r\nsearch=q%20qq%20qqq&a=b%20c";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 2);
            Assert.AreEqual(p.VariablesHttpPost["search"], "q qq qqq");
            Assert.AreEqual(p.VariablesHttpPost["a"], "b c");
            Assert.AreEqual(p.VariablesHttpPost["aa"], null);
            Assert.IsFalse(p.IsIncompleteRequest);
        }

        [TestMethod]
        public void TestHttpCombined()
        {
            Console.WriteLine("Testing HttpRequest Head & Post Variables Combined...");

            string packet = "POST /data?bla=blob HTTP/1.1\r\nContent-Length: 64\r\n\r\n";

            HttpRequest p = HttpRequest.Constructor(ref packet, null, null);

            Assert.IsTrue(p.IsIncompleteRequest);

            string newPacket = "search=true";

            p = HttpRequest.Constructor(ref newPacket, packet, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 1);
            Assert.AreEqual(p.VariablesHttpHead["bla"], "blob");
            Assert.AreEqual(p.VariablesHttpPost.Count, 1);
            Assert.AreEqual(p.VariablesHttpPost["search"], "true");
            Assert.IsFalse(p.IsIncompleteRequest);

            packet = "POST /data?bla=blob&a&=b& HTTP/1.1\r\n\r\nsearch=&q&&=aa";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 3);
            Assert.AreEqual(p.VariablesHttpHead["bla"], "blob");
            Assert.AreEqual(p.VariablesHttpHead["a"], "");
            Assert.AreEqual(p.VariablesHttpHead[""], "b");
            Assert.AreEqual(p.VariablesHttpPost.Count, 3);
            Assert.AreEqual(p.VariablesHttpPost["search"], "");
            Assert.AreEqual(p.VariablesHttpPost["q"], "");
            Assert.AreEqual(p.VariablesHttpPost[""], "aa");
            Assert.IsFalse(p.IsIncompleteRequest);

            packet = "POST /data? HTTP/1.1\r\n\r\n&=";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);
            Assert.IsFalse(p.IsIncompleteRequest);

            packet = "POST /data?bla=?banana%3Dchicken%26salad%26cheese%23&a=bla& HTTP/1.1\r\n\r\nsearch=?banana%3Dchicken%26salad%26cheese%23&";
            
            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/data");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 2);
            Assert.AreEqual(p.VariablesHttpHead["bla"], "?banana=chicken&salad&cheese#");
            Assert.AreEqual(p.VariablesHttpHead["a"], "bla");
            Assert.AreEqual(p.VariablesHttpPost.Count, 1);
            Assert.AreEqual(p.VariablesHttpPost["search"], "?banana=chicken&salad&cheese#");
            Assert.IsFalse(p.IsIncompleteRequest);
        }

        [TestMethod]
        public void TestHttpWebSocketUpgrade()
        {
            Console.WriteLine("Testing HttpRequest WebSocket Upgrade Requests...");

            string packet = "GET /chat HTTP/1.1\r\nHost: server.example.com\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nOrigin: http://example.com\r\nSec-WebSocket-Protocol: chat, superchat\r\nSec-WebSocket-Version: 13\r\n\r\n";

            HttpRequest p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, true);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/chat");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);
            Assert.IsFalse(p.IsIncompleteRequest);

            packet = "POST /chat HTTP/1.1\r\nHost: server.example.com\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nOrigin: http://example.com\r\nSec-WebSocket-Protocol: chat, superchat\r\nSec-WebSocket-Version: 13\r\n\r\na=b";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, true);
            Assert.AreEqual(p.ModifiedDate, null);
            Assert.AreEqual(p.RequestUrl, "/chat");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 1);
            Assert.AreEqual(p.VariablesHttpPost["a"], "b");
            Assert.IsFalse(p.IsIncompleteRequest);
        }

        [TestMethod]
        public void TestHttpModifiedDate()
        {
            Console.WriteLine("Testing HttpRequest Modified Date parameter...");

            string packet = "GET index.html HTTP/1.0\r\nIf-Modified-Since: Wed, 15 Nov 2000 13:11:23\r\n\r\n";

            HttpRequest p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.0");
            Assert.AreEqual(p.HttpType, HttpType.Get);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, new DateTime(2000, 11, 15, 13, 11, 23));
            Assert.AreEqual(p.RequestUrl, "index.html");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);
            Assert.IsFalse(p.IsIncompleteRequest);

            packet = "POST index.html HTTP/1.1\r\nIf-Modified-Since: Wed, 15 Nov 2000 13:11:23\r\n\r\n";

            p = HttpRequest.Constructor(ref packet, null, null);

            Assert.AreEqual(p.Version, "HTTP/1.1");
            Assert.AreEqual(p.HttpType, HttpType.Post);
            Assert.AreEqual(p.IsWebsocketUpgradeRequest, false);
            Assert.AreEqual(p.ModifiedDate, new DateTime(2000, 11, 15, 13, 11, 23));
            Assert.AreEqual(p.RequestUrl, "index.html");
            Assert.AreEqual(p.Stream, null);
            Assert.AreEqual(p.VariablesHttpHead.Count, 0);
            Assert.AreEqual(p.VariablesHttpPost.Count, 0);
            Assert.IsFalse(p.IsIncompleteRequest);
        }
    }
}
