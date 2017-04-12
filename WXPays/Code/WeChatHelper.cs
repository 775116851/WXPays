using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.Web.Script.Serialization;

namespace WXPays.Code
{
    public class WeChatHelper
    {
        public static string appId = "wx333ef16cdee29db9";
        public static string secret = "6968ce6705582747c3ce5fb9fa2c78c7";
        public static string code = "";

        /// <summary>
        /// 获取openID
        /// </summary>
        /// <param name="type">回调页面的绝对路径</param>
        public static void GetOpenID(string type)
        {
            if ((HttpContext.Current.Request.Cookies["openid"]) == null)
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                Dictionary<string, object> dict = new Dictionary<string, object>();
                string code = GetCode(type);   //获取code
                AccessToken(code); //获取accessToken
                HttpContext.Current.Response.Redirect("/WeChat/" + type + ".aspx");
            }
        }
        /// <summary>
        /// 获取code代码
        /// </summary>
        /// <returns></returns>
        public static string GetCode(string TypeName)
        {
            if (HttpContext.Current.Request.QueryString["Code"] != null)  //判断code是否存在
            {
                if (HttpContext.Current.Request.Cookies["Code"] == null)  //判断是否是第二次进入
                {
                    SetCookie("code", HttpContext.Current.Request.QueryString["Code"], 365);  //写code 保存到cookies
                    code = HttpContext.Current.Request.QueryString["Code"];
                }
                else
                {
                    delCookies("code"); //删除cookies

                    CodeURL(TypeName); //code重新跳转URL
                }
            }
            else
            {
                CodeURL(TypeName); //code跳转URL
            }
            return code;
        }
        /// <summary>
        /// 获取AccessToken
        /// </summary>
        /// <returns></returns>
        public static string AccessToken(string code)
        {
            Dictionary<string, string> obj = new Dictionary<string, string>();
            var client = new System.Net.WebClient();
            var serializer = new JavaScriptSerializer();
            string url = string.Format("https://api.weixin.qq.com/sns/oauth2/access_token?appid={0}&secret={1}&code={2}&grant_type=authorization_code", appId, secret, code);
            client.Encoding = System.Text.Encoding.UTF8;
            string dataaccess = "";
            try
            {
                dataaccess = client.DownloadString(url);
            }
            catch (Exception e)
            {
                //存log方法
            }
            //获取字典
            obj = serializer.Deserialize<Dictionary<string, string>>(dataaccess);
            string accessToken = "";
            if (obj.TryGetValue("access_token", out accessToken))  //判断access_Token是否存在
            {
                SetCookie("openid", obj["openid"], 365);
            }
            else  //access_Token 失效时重新发送。
            {
                //存log方法
            }
            return accessToken;
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
        /// 跳转codeURL
        /// </summary>
        /// <param name="TypeName"></param>
        public static void CodeURL(string TypeName)
        {
            string url = "";
            //string locationhref = "http://网站域名/WeChat/" + TypeName + ".aspx";
            string locationhref = "http://feng2017.cn/Home/Index";
            url = string.Format("https://open.weixin.qq.com/connect/oauth2/authorize?appid={0}&redirect_uri={1}&response_type=code&scope=snsapi_userinfo&state=STATE#wechat_redirect", appId, locationhref);
            HttpContext.Current.Response.Redirect(url);
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
    }
}