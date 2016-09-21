
namespace JWT
{
    public class SignatureVerificationException : System.Exception
    {
        public SignatureVerificationException(string message)
            : base(message)
        {
        }
    }
}