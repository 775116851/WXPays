using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using WXPays.Code;

namespace WXPays.Controllers
{
    public class HomeController : Controller
    {
        private ILog log = log4net.LogManager.GetLogger(typeof(HomeController));

        //网页验证授权(个人公众测试使用)
        public ActionResult Index()
        {
            Response.Clear();
            log.Info("进入方法A");
            log.Info("访问页面：" + Request.Url.ToString());
            //公众平台上开发者设置的token, appID, EncodingAESKey
            string sToken = "lxf2017";
            string sAppID = "wx333ef16cdee29db9";
            string sEncodingAESKey = "AO5KEQymEKbShSihOb7AmOJuveSakXq4FAjBfiAlL5l";
            log.Info("进入方法B");
            WXBizMsgCrypt wxcpt = new WXBizMsgCrypt(sToken, sEncodingAESKey, sAppID);
            log.Info("进入方法C：" + Request.QueryString);
            //?signature=2d5ab5fc4610b9fff7d92ca170de90e4d3461ba1&echostr=7428967735254842299&timestamp=1488179763&nonce=1404739141
            string rSignature = Request.QueryString["signature"];
            string rEchostr = Request.QueryString["echostr"];
            string rTimestamp = Request.QueryString["timestamp"];
            string rNonce = Request.QueryString["nonce"];
            ///* 1. 对用户回复的数据进行解密。
            // * 用户回复消息或者点击事件响应时，企业会收到回调消息，假设企业收到的推送消息：
            // * 	POST /cgi-bin/wxpush? msg_signature=477715d11cdb4164915debcba66cb864d751f3e6&timestamp=1409659813&nonce=1372623149 HTTP/1.1
            //    Host: qy.weixin.qq.com
            //    Content-Length: 613
            // *
            // * 	<xml>
            //        <ToUserName><![CDATA[wx5823bf96d3bd56c7]]></ToUserName>
            //        <Encrypt><![CDATA[RypEvHKD8QQKFhvQ6QleEB4J58tiPdvo+rtK1I9qca6aM/wvqnLSV5zEPeusUiX5L5X/0lWfrf0QADHHhGd3QczcdCUpj911L3vg3W/sYYvuJTs3TUUkSUXxaccAS0qhxchrRYt66wiSpGLYL42aM6A8dTT+6k4aSknmPj48kzJs8qLjvd4Xgpue06DOdnLxAUHzM6+kDZ+HMZfJYuR+LtwGc2hgf5gsijff0ekUNXZiqATP7PF5mZxZ3Izoun1s4zG4LUMnvw2r+KqCKIw+3IQH03v+BCA9nMELNqbSf6tiWSrXJB3LAVGUcallcrw8V2t9EL4EhzJWrQUax5wLVMNS0+rUPA3k22Ncx4XXZS9o0MBH27Bo6BpNelZpS+/uh9KsNlY6bHCmJU9p8g7m3fVKn28H3KDYA5Pl/T8Z1ptDAVe0lXdQ2YoyyH2uyPIGHBZZIs2pDBS8R07+qN+E7Q==]]></Encrypt>
            //    </xml>
            // */
            string sReqMsgSig = rSignature;
            string sReqTimeStamp = rTimestamp;
            string sReqNonce = rNonce;
            string sReqData = "<xml><ToUserName><![CDATA[wx5823bf96d3bd56c7]]></ToUserName><Encrypt><![CDATA[RypEvHKD8QQKFhvQ6QleEB4J58tiPdvo+rtK1I9qca6aM/wvqnLSV5zEPeusUiX5L5X/0lWfrf0QADHHhGd3QczcdCUpj911L3vg3W/sYYvuJTs3TUUkSUXxaccAS0qhxchrRYt66wiSpGLYL42aM6A8dTT+6k4aSknmPj48kzJs8qLjvd4Xgpue06DOdnLxAUHzM6+kDZ+HMZfJYuR+LtwGc2hgf5gsijff0ekUNXZiqATP7PF5mZxZ3Izoun1s4zG4LUMnvw2r+KqCKIw+3IQH03v+BCA9nMELNqbSf6tiWSrXJB3LAVGUcallcrw8V2t9EL4EhzJWrQUax5wLVMNS0+rUPA3k22Ncx4XXZS9o0MBH27Bo6BpNelZpS+/uh9KsNlY6bHCmJU9p8g7m3fVKn28H3KDYA5Pl/T8Z1ptDAVe0lXdQ2YoyyH2uyPIGHBZZIs2pDBS8R07+qN+E7Q==]]></Encrypt></xml>";
            string sMsg = "";  //解析之后的明文
            int ret = 0;
            //ret = wxcpt.DecryptMsg(sReqMsgSig, sReqTimeStamp, sReqNonce, sReqData, ref sMsg);
            //if (ret != 0)
            //{
            //    System.Console.WriteLine("ERR: Decrypt fail, ret: " + ret);
            //}
            //System.Console.WriteLine(sMsg);


            /*
             * 2. 企业回复用户消息也需要加密和拼接xml字符串。
             * 假设企业需要回复用户的消息为：
             * 		<xml>
             * 		<ToUserName><![CDATA[mycreate]]></ToUserName>
             * 		<FromUserName><![CDATA[wx5823bf96d3bd56c7]]></FromUserName>
             * 		<CreateTime>1348831860</CreateTime>
                    <MsgType><![CDATA[text]]></MsgType>
             *      <Content><![CDATA[this is a test]]></Content>
             *      <MsgId>1234567890123456</MsgId>
             *      </xml>
             * 生成xml格式的加密消息过程为：
             */
            string sRespData = "<xml><ToUserName><![CDATA[mycreate]]></ToUserName><FromUserName><![CDATA[wx582测试一下中文的情况，消息长度是按字节来算的396d3bd56c7]]></FromUserName><CreateTime>1348831860</CreateTime><MsgType><![CDATA[text]]></MsgType><Content><![CDATA[this is a test]]></Content><MsgId>1234567890123456</MsgId></xml>";
            string sEncryptMsg = ""; //xml格式的密文
            ret = wxcpt.EncryptMsg(sRespData, sReqTimeStamp, sReqNonce, ref sEncryptMsg);
            System.Console.WriteLine("sEncryptMsg");
            System.Console.WriteLine(sEncryptMsg);

            /*测试：
             * 将sEncryptMsg解密看看是否是原文
             * */
            //XmlDocument doc = new XmlDocument();
            //doc.LoadXml(sEncryptMsg);
            //log.Info("进入方法D");
            //XmlNode root = doc.FirstChild;
            //string sig = root["MsgSignature"].InnerText;
            //string enc = root["Encrypt"].InnerText;
            //string timestamp = root["TimeStamp"].InnerText;
            //string nonce = root["Nonce"].InnerText;
            //string stmp = "";
            //int ret = 0;
            //ret = wxcpt.DecryptMsg(sig, timestamp, nonce, sEncryptMsg, ref stmp);
            //System.Console.WriteLine("stemp");
            //System.Console.WriteLine(stmp + ret);
            //log.Info("最终结果：" + stmp + ret);
            //ViewData["PP"] = stmp + ret;
            //ViewData["PP"] = true;
            //Response.Write(sEncryptMsg);
            ViewData["PP"] = rEchostr;
            return View();
        }

        //网页验证授权(个人公众测试使用)
        public ActionResult Test()
        {
            string rUrl = ConfigurationManager.AppSettings["TestUrl"];
            var request = (HttpWebRequest)WebRequest.Create(rUrl);
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            log.Info("结果：" + responseString);
            Response.Write(responseString);
            return View();
        }

    }
}
