
namespace TestApplication
{


    /// <summary>
    /// Summary description for CookieReader
    /// </summary>
    public class CookieReader : System.Web.IHttpHandler
    {


        public void ProcessRequest(System.Web.HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            System.Web.HttpCookie myCookie = context.Request.Cookies["AuthCookie"];

            // Read the cookie information and display it.
            if (myCookie != null)
                context.Response.Write("Found cookie \"" + myCookie.Name + "\" with value \"" + myCookie.Value + "\".");
            else
                context.Response.Write("AuthCookie not found.");
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
