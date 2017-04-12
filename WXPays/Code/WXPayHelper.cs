using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Script.Serialization;

namespace WXPays.Code
{
    public class WXPayHelper
    {
        private ILog log = log4net.LogManager.GetLogger(typeof(WXPayHelper));
        #region 获取OpenID相关

        /// <summary>
        /// 获取微信OpenID
        /// </summary>
        /// <returns></returns>
        //public string GetOpenID()
        //{
        //    string WXOpenID = Convert.ToString(HttpContext.Current.Session["WXOpenID"]);
        //    if (string.IsNullOrEmpty(WXOpenID) || WXOpenID == "StringNull")
        //    {
        //        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        //        log.Info("OpenID为空进入，开始获取Code");
        //        string code = GetCode();   //获取code
        //        if (!string.IsNullOrEmpty(code) && code != "StringNull")
        //        {
        //            log.Info("OpenID为空进入，开始获取AccessToken");
        //            string m = AccessToken(code); //获取accessToken
        //        }
        //    }
        //    else
        //    {
        //        log.Info("OpenID为空进入，存在WXOpenID");
        //    }
        //    return WXOpenID;
        //}

        /// <summary>
        /// 获取code
        /// </summary>
        /// <returns></returns>
        public string GetCode()
        {
            string code = string.Empty;
            if (HttpContext.Current.Request.QueryString["Code"] != null)  //判断code是否存在
            {
                log.Info("GetCode进入C");
                SetCookie("code", HttpContext.Current.Request.QueryString["Code"], 365);  //写code 保存到cookies
                code = HttpContext.Current.Request.QueryString["Code"];
                //log.Info("GetCode进入D");
                //if (HttpContext.Current.Request.Cookies["code"] == null)  //判断是否是第二次进入
                //{
                //    log.Info("GetCode进入C");
                //    SetCookie("code", HttpContext.Current.Request.QueryString["Code"], 365);  //写code 保存到cookies
                //    code = HttpContext.Current.Request.QueryString["Code"];
                //}
                //else
                //{
                //    log.Info("GetCode进入B");
                //    code = HttpContext.Current.Request.Cookies["code"].Value;
                //    //delCookies("code"); //删除cookies
                //    //CodeURL(); //code重新跳转URL
                //}
            }
            else
            {
                log.Info("GetCode进入A");
                CodeURL(); //code跳转URL
            }
            return code;
        }

        /// <summary>
        /// 跳转codeURL
        /// </summary>
        public void CodeURL()
        {
            string appID = "wxefcb380b85e5f16d";//配置
            string url = "";
            string locationhref = HttpUtility.HtmlEncode("http://www.soonwill.com/swapp/Pay/Test");//配置
            url = string.Format("https://open.weixin.qq.com/connect/oauth2/authorize?appid={0}&redirect_uri={1}&response_type=code&scope=snsapi_userinfo&state=234#wechat_redirect", appID, locationhref);
            log.Info("CodeURL跳转地址:" + url);
            HttpContext.Current.Response.Redirect(url);
            return;
        }

        /// <summary>
        /// 获取AccessToken
        /// </summary>
        /// <returns></returns>
        public Tuple<int, string, WXUserInfo> AccessToken(string code)
        {
            log.Info("进入获取AccessToken，Code:" + code);
            string appID = "wxefcb380b85e5f16d";//配置
            string appSecret = "e18746584cd17e751806ad385d5a08ef";//配置
            Dictionary<string, string> obj = new Dictionary<string, string>();
            var client = new System.Net.WebClient();
            var serializer = new JavaScriptSerializer();
            string url = string.Format("https://api.weixin.qq.com/sns/oauth2/access_token?appid={0}&secret={1}&code={2}&grant_type=authorization_code", appID, appSecret, code);
            log.Info("进入获取AccessToken，Url:" + url);
            client.Encoding = System.Text.Encoding.UTF8;
            string dataaccess = "";
            try
            {
                dataaccess = client.DownloadString(url);
            }
            catch (Exception e)
            {
                log.Error("AccessToken方法出现异常:" + e.Message + ";异常详情:" + e);
                return Tuple.Create<int, string, WXUserInfo>(-1, "AccessToken调用接口异常:" + e.Message, null);
            }
            log.Info("进入获取AccessToken，Dataaccess:" + dataaccess);
            //JObject jo = (JObject)JsonConvert.DeserializeObject(dataaccess);

            //获取字典
            obj = serializer.Deserialize<Dictionary<string, string>>(dataaccess);
            string WXAccessToken = "";
            if (obj.TryGetValue("access_token", out WXAccessToken))  //判断access_Token是否存在
            {
                string WXRefreshToken = obj["refresh_token"];
                string WXOpenId = obj["openid"];
                WXUserInfo wui = new WXUserInfo();
                wui.OpenID = WXOpenId;
                wui.AccessToken = WXAccessToken;
                wui.RefreshToken = WXRefreshToken;
                return Tuple.Create<int, string, WXUserInfo>(0, WXOpenId + "|" + WXAccessToken, wui);
                //HttpContext.Current.Session["WXRefreshToken"] = WXRefreshToken;
                ////获取用户基础信息
                //WXUserInfo(accessToken, WXOpenId, WXRefreshToken);
            }
            else  //access_Token 失效时重新发送。
            {
                delCookies("code"); //删除cookies
                log.Info("进入获取AccessToken，未能获取到AccessToken调用RefreshAccessToken方法");
                return Tuple.Create<int, string, WXUserInfo>(-2, "", null);
                //accessToken = RefreshAccessToken();
            }
        }

        /// <summary>
        /// 刷新RefreshAccessToken
        /// </summary>
        /// <returns></returns>
        public Tuple<int, string, string> RefreshAccessToken(string wxRefreshToken)
        {
            log.Info("进入获取Refreshtoken");
            string appID = "wxefcb380b85e5f16d";//配置
            string refreshtoken = Convert.ToString(wxRefreshToken);//数据库读取
            Dictionary<string, string> obj = new Dictionary<string, string>();
            var client = new System.Net.WebClient();
            var serializer = new JavaScriptSerializer();
            string url = string.Format("https://api.weixin.qq.com/sns/oauth2/refresh_token?appid={0}&grant_type=refresh_token&refresh_token={1} ", appID, refreshtoken);
            log.Info("进入获取Refreshtoken，Url:" + url);
            client.Encoding = System.Text.Encoding.UTF8;
            string dataaccess = "";
            try
            {
                dataaccess = client.DownloadString(url);
            }
            catch (Exception e)
            {
                log.Error("RefreshAccessToken方法出现异常:" + e.Message + ";异常详情:" + e);
                return Tuple.Create<int, string, string>(-1, "RefreshAccessToken方法异常:" + e.Message, "");
            }
            log.Info("进入获取Refreshtoken，Dataaccess:" + dataaccess);
            //获取字典
            obj = serializer.Deserialize<Dictionary<string, string>>(dataaccess);
            string WXAccessToken = "";
            if (obj.TryGetValue("access_token", out WXAccessToken))  //判断access_Token是否存在
            {
                return Tuple.Create<int, string, string>(0, "成功", WXAccessToken);
                //SetCookie("openid", obj["openid"], 365);
            }
            else  //access_Token 失效时重新发送。
            {
                ////Refreshtoken过期 重新发起授权
                //HttpContext.Current.Session["WXOpenID"] = "";
                //CodeURL(); //code跳转URL
                return Tuple.Create<int, string, string>(-2, "Refreshtoken过期,重新发起授权", "");
            }
        }

        /// <summary>
        /// 获取微信用户信息
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="openid"></param>
        /// <returns></returns>
        public Tuple<int, string, WXUserInfo> WXUserInfo(string accessToken, string openid, string refreshToken)
        {
            var client = new System.Net.WebClient();
            var serializer = new JavaScriptSerializer();
            string url = string.Format("https://api.weixin.qq.com/sns/userinfo?access_token={0}&openid={1}&lang=zh_CN  ", accessToken, openid);
            log.Info("进入获取WXUserInfo，Url:" + url);
            client.Encoding = System.Text.Encoding.UTF8;
            string dataaccess = "";
            try
            {
                dataaccess = client.DownloadString(url);
            }
            catch (Exception e)
            {
                log.Error("WXUserInfo方法出现异常:" + e.Message + ";异常详情:" + e);
                return Tuple.Create<int, string, WXUserInfo>(-1, "WXUserInfo方法出行异常:" + e.Message, null);
            }
            log.Info("进入获取WXUserInfo，Dataaccess:" + dataaccess);
            JObject jo = (JObject)JsonConvert.DeserializeObject(dataaccess);
            if (jo != null && jo["openid"] != null)
            {
                WXUserInfo wxUI = new WXUserInfo();
                wxUI.OpenID = Convert.ToString(jo["openid"]);
                wxUI.NickName = Convert.ToString(jo["nickname"]);
                wxUI.Sex = Convert.ToString(jo["sex"]);
                wxUI.Province = Convert.ToString(jo["province"]);
                wxUI.City = Convert.ToString(jo["city"]);
                wxUI.Country = Convert.ToString(jo["country"]);
                wxUI.Headimgurl = Convert.ToString(jo["headimgurl"]);
                wxUI.Privilege = Convert.ToString(jo["privilege"]);
                wxUI.Unionid = Convert.ToString(jo["unionid"]);

                wxUI.RefreshToken = refreshToken;
                return Tuple.Create<int, string, WXUserInfo>(0, "成功", wxUI);
                ////将必要信息同步到数据库会员

                //HttpContext.Current.Session["WXUserInfo"] = wxUI;
                //HttpContext.Current.Session["WXOpenID"] = wxUI.OpenID;
                //return "0";
            }
            else
            {
                string errCode = Convert.ToString(jo["errcode"]);
                string errMsg = Convert.ToString(jo["errmsg"]);
                log.Info(string.Format("微信获取用户信息失败;微信ID:{0};失败错误码:{1};失败错误信息:{2}", openid, errCode, errMsg));
                return Tuple.Create<int, string, WXUserInfo>(-1, "WXUserInfo方法出行异常:" + errMsg, null);
            }
        }

        /// <summary>
        /// 设置cookies
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="time"></param>
        public static void SetCookie(string name, string value, int time)
        {
            HttpCookie cookies = new HttpCookie(name);
            cookies.Name = name;
            cookies.Value = value;
            cookies.Expires = DateTime.Now.AddDays(time);
            HttpContext.Current.Response.Cookies.Add(cookies);

        }

        /// <summary>
        /// 删除cookies
        /// </summary>
        /// <param name="name"></param>
        public static void delCookies(string name)
        {
            foreach (string cookiename in HttpContext.Current.Request.Cookies.AllKeys)
            {
                HttpCookie cookies = HttpContext.Current.Request.Cookies[name];
                if (cookies != null)
                {
                    cookies.Expires = DateTime.Today.AddDays(-1);
                    HttpContext.Current.Response.Cookies.Add(cookies);
                    HttpContext.Current.Request.Cookies.Remove(name);
                }
            }
        }
        #endregion
    }

    [Serializable]
    public class WXUserInfo
    {
        public string OpenID { get; set; }
        public string NickName { get; set; }
        public string Sex { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Headimgurl { get; set; }
        public string Privilege { get; set; }
        public string Unionid { get; set; }

        public int CustomerSysNo { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}