
namespace TestApplication
{


    /// <summary>
    /// Summary description for CookieDeleter
    /// </summary>
    public class CookieDeleter : System.Web.IHttpHandler
    {


        public void ProcessRequest(System.Web.HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            if (context.Request.Cookies["AuthCookie"] != null)
            {
                context.Response.Cookies["AuthCookie"].Expires = System.DateTime.Now.AddDays(-1);
                context.Response.Write("AuthCookie deleted");
            }
            else
                context.Response.Write("AuthCookie deleted");
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
