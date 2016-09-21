
namespace BouncyCastleTest
{


    // https://stackoverflow.com/questions/22580853/reliable-implementation-of-pbkdf2-hmac-sha256-for-java
    // https://stackoverflow.com/questions/36876641/generate-hmac-sha256-hash-with-bouncycastle
    class TestSha
    {

        // A message authentication code (MAC) is prod
        // HMAC = Hash-based MAC
        public static byte[] Sha512Managed(string text)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
            byte[] result;
            using (System.Security.Cryptography.SHA512 shaM = new System.Security.Cryptography.SHA512Managed())
            {
                result = shaM.ComputeHash(data);
            }

            return result;
        }


        public static byte[] SHA512(string text)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);

            Org.BouncyCastle.Crypto.Digests.Sha512Digest digester = new Org.BouncyCastle.Crypto.Digests.Sha512Digest();
            byte[] retValue = new byte[digester.GetDigestSize()];
            digester.BlockUpdate(bytes, 0, bytes.Length);
            digester.DoFinal(retValue, 0);
            return retValue;
        }


        public static byte[] HmacSha256(string text, string key)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
            
            var hmac = new Org.BouncyCastle.Crypto.Macs.HMac(new Org.BouncyCastle.Crypto.Digests.Sha256Digest());
            hmac.Init(new Org.BouncyCastle.Crypto.Parameters.KeyParameter(System.Text.Encoding.UTF8.GetBytes(key)));
            
            byte[] result = new byte[hmac.GetMacSize()];
            hmac.BlockUpdate(bytes, 0, bytes.Length);
            hmac.DoFinal(result, 0);

            return result;
        }


        public static byte[] HmacSha384(string text, string key)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);

            var hmac = new Org.BouncyCastle.Crypto.Macs.HMac(new Org.BouncyCastle.Crypto.Digests.Sha384Digest());
            hmac.Init(new Org.BouncyCastle.Crypto.Parameters.KeyParameter(System.Text.Encoding.UTF8.GetBytes(key)));
            
            byte[] result = new byte[hmac.GetMacSize()];
            hmac.BlockUpdate(bytes, 0, bytes.Length);
            hmac.DoFinal(result, 0);

            return result;
        }


        public static byte[] HmacSha512(string text, string key)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);

            var hmac = new Org.BouncyCastle.Crypto.Macs.HMac(new Org.BouncyCastle.Crypto.Digests.Sha512Digest());
            hmac.Init(new Org.BouncyCastle.Crypto.Parameters.KeyParameter(System.Text.Encoding.UTF8.GetBytes(key)));
            
            byte[] result = new byte[hmac.GetMacSize()];
            hmac.BlockUpdate(bytes, 0, bytes.Length);
            hmac.DoFinal(result, 0);

            return result;
        }


    }


}
