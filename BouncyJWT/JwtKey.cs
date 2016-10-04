
namespace BouncyJWT
{


    public class JwtKey
    {
        public byte[] MacKeyBytes;
        public Org.BouncyCastle.Crypto.AsymmetricKeyParameter PrivateKey;


        public string MacKey
        {
            get { return System.Text.Encoding.UTF8.GetString(this.MacKeyBytes); }
            set { this.MacKeyBytes = System.Text.Encoding.UTF8.GetBytes(value); }
        }


        public JwtKey()
        { }


        public JwtKey(string macKey)
        {
            this.MacKey = macKey;
        }


        public JwtKey(byte[] macKey)
        {
            this.MacKeyBytes = macKey;
        }


        public JwtKey(Org.BouncyCastle.Crypto.AsymmetricKeyParameter rsaPrivateKey)
        {
            this.PrivateKey = rsaPrivateKey;
        }


        public string PemPrivateKey
        {
            get { return Crypto.StringifyAsymmetricKey(this.PrivateKey); }
            set { this.PrivateKey = Crypto.ReadPrivateKey(value); }
        }


    } // End Class JwtKey 


} // End Namespace BouncyJWT 
