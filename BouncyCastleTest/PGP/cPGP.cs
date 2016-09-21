
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

// http://www.bouncycastle.org/csharp/
// http://stackoverflow.com/questions/892249/problem-generating-pgp-keys
// http://blogs.microsoft.co.il/blogs/kim/archive/2009/01/23/pgp-zip-encrypted-files-with-c.aspx
// http://jopinblog.wordpress.com/2008/06/23/pgp-single-pass-sign-and-encrypt-with-bouncy-castle/


// http://cephas.net/blog/2004/04/01/pgp-encryption-using-bouncy-castle/


namespace CSbouncyCastle
{


    class cPGP
    {


        // CSbouncyCastle.cPGP.GenerateKey
        public static void GenerateKey(string username, string password, string keyStoreUrl)
        {
            IAsymmetricCipherKeyPairGenerator kpg = new RsaKeyPairGenerator();
            kpg.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x13), new SecureRandom(), 1024, 8));
            AsymmetricCipherKeyPair kp = kpg.GenerateKeyPair();

            System.IO.FileStream out1 = new System.IO.FileInfo(string.Format(@"{0}\{1}.priv.asc", keyStoreUrl, username)).OpenWrite();
            System.IO.FileStream out2 = new System.IO.FileInfo(string.Format(@"{0}\{1}.pub.asc", keyStoreUrl, username)).OpenWrite();

            ExportKeyPair(out1, out2, kp.Public, kp.Private, username, password.ToCharArray(), true);
            kpg = null;
        }


        private static void ExportKeyPair(
                    System.IO.Stream secretOut,
                    System.IO.Stream publicOut,
                    AsymmetricKeyParameter publicKey,
                    AsymmetricKeyParameter privateKey,
                    string identity,
                    char[] passPhrase,
                    bool armor)
        {
            if (armor)
            {
                secretOut = new ArmoredOutputStream(secretOut);
            }

            PgpSecretKey secretKey = new PgpSecretKey(
                PgpSignature.DefaultCertification,
                PublicKeyAlgorithmTag.RsaGeneral,
                publicKey,
                privateKey,
                System.DateTime.Now,
                identity,
                SymmetricKeyAlgorithmTag.Cast5,
                passPhrase,
                null,
                null,
                new SecureRandom()
                //                ,"BC"
                );

            secretKey.Encode(secretOut);

            secretOut.Close();

            if (armor)
            {
                publicOut = new ArmoredOutputStream(publicOut);
            }

            PgpPublicKey key = secretKey.PublicKey;

            key.Encode(publicOut);

            publicOut.Close();
        }


    } // End class cPGP


} // End namespace CSbouncyCastle
