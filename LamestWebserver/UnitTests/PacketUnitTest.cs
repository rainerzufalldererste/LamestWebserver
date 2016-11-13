using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver;
using System.Net;
using System.Collections.Generic;

namespace UnitTests
{
    [TestClass]
    public class PacketUnitTest
    {
        [TestMethod]
        public void TestCookies()
        {
            string packet = "POST /secure/Passport.aspx?popup=1&wa=wsignin1.0 HTTP/1.1\r\nHost: www.bing.com\r\nUser -Agent: Mozilla/5.0 (Windows NT 10.0; WOW64; rv:48.0) Gecko/20100101 Firefox/48.0\r\nAccept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\nAccept-Language: de,en-US;q=0.7,en;q=0.3\r\nAccept-Encoding: gzip, deflate, br\r\nReferer: https://login.live.com/login.srf?wa=wsignin1.0&rpsnv=123&ct=123&rver=6.0.5286.0&wp=MBI&wreply=https:%2F%2fwww.bing.com%2Fsecure%2FPassport.aspx%3Fpopup%3D1&lc=123&id=123&checkda=1\r\nCookie: SRCHD=AF=NOFORM; SRCHUID=V=2&GUID=CA60C8BCDD883D55ABC6A3FD52; SRCHUSR=DOB=20160000; _SS=SID=3614F82F6F05A0F4B8&bIm=4254; _EDGE_S=F=1&SID=3614F7636664B8; _EDGE_V=1; MUID=39415A1EAC6C76; MUIDB=3941EAC6C76; SRCHHPGUSR=CW=12&CH=19&DPR=1&UTC=10; WLS=TS=63651\r\nConnection: keep-alive\r\nUpgrade-Insecure-Requests: 1\r\n\r\n\r\n<content>";

            HTTP_Packet p = HTTP_Packet.Constructor(ref packet, new IPEndPoint(1, 1337), null);

            Assert.IsTrue(p.cookies.Count == 10);

            string[] cookieNames = { "SRCHD", "SRCHUID", "SRCHUSR", "_SS", "_EDGE_S", "_EDGE_V", "MUID", "MUIDB", "SRCHHPGUSR", "WLS" };
            string[] cookieValues = { "AF=NOFORM", "V=2&GUID=CA60C8BCDD883D55ABC6A3FD52", "DOB=20160000", "SID=3614F82F6F05A0F4B8&bIm=4254", "F=1&SID=3614F7636664B8", "1", "39415A1EAC6C76", "3941EAC6C76", "CW=12&CH=19&DPR=1&UTC=10", "TS=63651" };

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(p.cookies[i], new KeyValuePair<string, string>(cookieNames[i], cookieValues[i]));
            }

        }
    }
}
