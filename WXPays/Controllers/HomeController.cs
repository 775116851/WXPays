using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml;
using WXPays.Code;

namespace WXPays.Controllers
{
    public class HomeController : Controller
    {
        private ILog log = log4net.LogManager.GetLogger(typeof(HomeController));
        SybWxPayService sybService = new SybWxPayService();

        public ActionResult Index()
        {
            //log.Info("回调参数：" + Request.Url.ToString());
            //GetOpenID();
            return View();
        }

        public ActionResult TTT()
        {
            log.Info("回调参数：" + Request.Url.ToString());
            string k = Convert.ToString(Session["fff"]);
            GetOpenID();
            //WeChatHelper.GetOpenID("");
            return View();
        }

        public ActionResult Pay()
        {
            //Session["fff"] = "aaa";
            //GetOpenID();
            //return View();
            try
            {
                Dictionary<String, String> rsp = sybService.pay(1, "131332659649680792", "W02", "商品内容", "备注", "oJ9W_jhNs3sm1jxgQyBDNbAlR7ZI", "", "http://baidu.com", "");
                printRsp(rsp);
            }
            catch (Exception ex)
            {
                //this.tblank.Value = ex.Message;
                log.Error("支付异常结果：" + ex.Message);
            }
            return View();
        }

        public ActionResult Cancel()
        {
            try
            {
                Dictionary<String, String> rsp = sybService.cancel(1, DateTime.Now.ToFileTime().ToString(), "12525075", "");
                printRsp(rsp);
            }
            catch (Exception ex)
            {
                //this.tblank.Value = ex.Message;
                log.Error("撤销异常结果：" + ex.Message);
            }
            return View();
        }

        public ActionResult Refund()
        {
            try
            {
                Dictionary<String, String> rsp = sybService.refund(1, DateTime.Now.ToFileTime().ToString(), "12525075", "");
                printRsp(rsp);
            }
            catch (Exception ex)
            {
                //this.tblank.Value = ex.Message;
                log.Error("退款异常结果：" + ex.Message);
            }
            return View();
        }

        public ActionResult Query()
        {
            try
            {
                Dictionary<String, String> rsp = sybService.query("", "17273218");
                printRsp(rsp);
            }
            catch (Exception ex)
            {
                //this.tblank.Value = ex.Message;
                log.Error("查询异常结果：" + ex.Message);
            }
            return View();
        }

        private void doRequest(Dictionary<String, String> param, String url)
        {
            String rsp = HttpUtil.CreatePostHttpResponse(AppConstants.API_URL + url, param, Encoding.UTF8);
            Dictionary<String, String> rspDic = (Dictionary<String, String>)JsonConvert.DeserializeObject(rsp, typeof(Dictionary<String, String>));
            rsp = "请求返回数据:" + rsp + "\n";
            if ("SUCCESS".Equals(rspDic["retcode"]))//验签
            {
                String signRsp = rspDic["sign"];
                rspDic.Remove("sign");
                String sign = AppUtil.signParam(rspDic, AppConstants.APPKEY);
                if (signRsp.Equals(sign))
                {
                    rsp = rsp + "验签成功";
                }
                else
                    rsp = rsp + "验签失败";

            }
            log.Info("请求结果：" + rsp);
            //this.tblank.Value = rsp;
        }

        private void printRsp(Dictionary<String, String> rspDic)
        {
            string rsp = "请求返回数据:\n";
            foreach (var item in rspDic)
            {
                rsp += item.Key + "-----" + item.Value + ";\n";
            }
            log.Info("请求打印结果：" + rsp);
            //this.tblank.Value = rsp;
        }

        #region
        public string GetOpenID()
        {

            string WXOpenID = Convert.ToString(Session["WXOpenIDS"]);
            if (string.IsNullOrEmpty(WXOpenID) || WXOpenID == "StringNull")
            {
                log.Info("OpenID为空进入，开始获取Code");
                WXPayHelper wxph = new WXPayHelper();
                string code = wxph.GetCode();   //获取code
                Session["fff"] = "BBB";
                if (!string.IsNullOrEmpty(code) && code != "StringNull")
                {

                    log.Info("OpenID为空进入，开始获取AccessToken");
                    Tuple<int, string, WXUserInfo> result = wxph.AccessToken(code); //获取accessToken
                    Session["fff"] = "CCC";
                    switch (result.Item1)
                    {
                        case -1://异常

                            break;
                        case 0://成功

                            Session["WXRefreshToken"] = result.Item3.RefreshToken;
                            Tuple<int, string, WXUserInfo> reUI = wxph.WXUserInfo(result.Item3.AccessToken, result.Item3.OpenID, result.Item3.RefreshToken);
                            if (reUI.Item1 == 0)//成功
                            {
                                Session["fff"] = "ddd";
                                //将必要信息同步到数据库会员

                                Session["WXUserInfo"] = result.Item3;
                                Session["WXOpenIDS"] = result.Item3.OpenID;
                            }
                            else
                            {

                            }
                            break;
                        case -2://刷新AccessToken
                            string refreshtoken = Convert.ToString(Session["WXRefreshToken"]);//数据库读取
                            if (!string.IsNullOrEmpty(refreshtoken) && refreshtoken != "StringNull")
                            {
                                Tuple<int, string, string> reR = wxph.RefreshAccessToken(refreshtoken);
                                switch (reR.Item1)
                                {
                                    case -1://异常

                                        break;
                                    case 0://成功
                                        //result = wxph.AccessToken(code);
                                        //if (result.Item1 == 0)
                                        //{
                                        //    Session["WXRefreshToken"] = result.Item3.RefreshToken;
                                        //}
                                        //else
                                        //{
                                        //}
                                        break;
                                    case -2://Refreshtoken过期 重新发起授权
                                        Session["WXOpenID"] = "";
                                        wxph.CodeURL();
                                        break;
                                }
                            }
                            else
                            {
                                //??
                            }
                            break;
                    }

                }
            }
            else
            {
                log.Info("OpenID为空进入，存在WXOpenID");
            }
            return WXOpenID;
        }
        #endregion


        #region 网页验证授权(个人公众测试使用)

        //网页验证授权(个人公众测试使用)
        public ActionResult IndexX()
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
        public ActionResult TestX()
        {
            string rUrl = ConfigurationManager.AppSettings["TestUrl"];
            var request = (HttpWebRequest)WebRequest.Create(rUrl);
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            log.Info("结果：" + responseString);
            Response.Write(responseString);
            return View();
        }

        #endregion
        

    }
}
