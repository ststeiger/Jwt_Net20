
namespace JWT
{


    public class TokenExpiredException : System.Exception
    {
        public TokenExpiredException(string message)
            : base(message)
        {
        }
    }


    public class TokenAlgorithmRefusedException : System.Exception
    {
        public TokenAlgorithmRefusedException() 
            : this("Acceptance of the specified algorithm is denied.\nReason: Braindead JWT spec represents an unacceptable security risk.\n")
        {}

        public TokenAlgorithmRefusedException(string message)
            : base(message)
        {
        }
    }


    public class UnknownTokenAlgorithmException : System.Exception
    {
        public UnknownTokenAlgorithmException(string message)
            : base(message)
        {
        }
    }


}
