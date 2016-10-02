
namespace TestApplication
{


    /// <summary>
    /// Summary description for CookieWriter
    /// </summary>
    public class CookieWriter : System.Web.IHttpHandler
    {


        public void ProcessRequest(System.Web.HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            // https://jwt.io/
            System.Web.HttpCookie myCookie = new System.Web.HttpCookie("AuthCookie");
            myCookie["header"] = "JWT-HEADERS";
            myCookie["payload"] = "JSON";
            myCookie["signature"] = "HMAC";
            myCookie.Expires = System.DateTime.Now.AddDays(1d);

            if(context.Request.IsSecureConnection)
                myCookie.Secure = true;

            myCookie.HttpOnly = true;
            context.Response.Cookies.Add(myCookie);

            context.Response.Write("AuthCookie written.");
        }


        public bool IsReusable
        {
            get
            {
                return false;
            }
        }


    }


}
