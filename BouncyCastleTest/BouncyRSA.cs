
using System.Security.Cryptography;

namespace BouncyCastleTest
{


    // RSA: (Ron) Rivest, (Adi) Shamir, (Leonard) Adleman
    public class BouncyRsa : System.Security.Cryptography.RSA
    {

        private Org.BouncyCastle.Crypto.AsymmetricKeyParameter m_keyParameter;
        private Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair m_keyPair;


        public BouncyRsa(string publicKey, bool b)
        {
            using (System.IO.StringReader keyReader = new System.IO.StringReader(publicKey))
            {
                m_keyParameter = (Org.BouncyCastle.Crypto.AsymmetricKeyParameter)new Org.BouncyCastle.OpenSsl.PemReader(keyReader).ReadObject();
            }
        }


        public BouncyRsa(string privateKey)
        {

            using (System.IO.StringReader txtreader = new System.IO.StringReader(privateKey))
            {
                m_keyPair = (Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)new Org.BouncyCastle.OpenSsl.PemReader(txtreader).ReadObject();
            }

            m_keyParameter = m_keyPair.Public;
        }


        public override byte[] DecryptValue(byte[] bytesToDecrypt)
        {
            Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding decryptEngine =
                new Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding(new Org.BouncyCastle.Crypto.Engines.RsaEngine());

            decryptEngine.Init(false, m_keyPair.Private);

            // string decrypted = System.Text.Encoding.UTF8.GetString(
            return decryptEngine.ProcessBlock(bytesToDecrypt, 0, bytesToDecrypt.Length);
            //);
        }


        public override byte[] EncryptValue(byte[] bytesToEncrypt)
        {
            Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding encryptEngine =
                new Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding(new Org.BouncyCastle.Crypto.Engines.RsaEngine());

            encryptEngine.Init(true, m_keyParameter);

            // string encrypted = System.Convert.ToBase64String(
            return encryptEngine.ProcessBlock(bytesToEncrypt, 0, bytesToEncrypt.Length);
            // );
        }


        public override System.Security.Cryptography.RSAParameters ExportParameters(bool includePrivateParameters)
        {
            throw new System.NotImplementedException();
        }


        public override void ImportParameters(System.Security.Cryptography.RSAParameters parameters)
        {
            throw new System.NotImplementedException();
        }


        protected override void Dispose(bool disposing)
        {
            throw new System.NotImplementedException();
        }


        public override string KeyExchangeAlgorithm
        {
            get { throw new System.NotImplementedException(); }
        }


        public override string SignatureAlgorithm
        {
            get { throw new System.NotImplementedException(); }
        }

    }


}
