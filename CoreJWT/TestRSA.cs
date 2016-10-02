
namespace BouncyCastleTest
{


    class TestRSA
    {


        // https://stackoverflow.com/questions/22008337/generating-keypair-using-bouncy-castle
        // https://stackoverflow.com/questions/14052485/converting-a-public-key-in-subjectpublickeyinfo-format-to-rsapublickey-format-ja
        // https://stackoverflow.com/questions/10963756/get-der-encoded-public-key
        // http://www.programcreek.com/java-api-examples/index.php?api=org.bouncycastle.crypto.util.SubjectPublicKeyInfoFactory
        public static void CerKeyInfo(Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair keyPair)
        {
            Org.BouncyCastle.Asn1.Pkcs.PrivateKeyInfo pkInfo = Org.BouncyCastle.Pkcs.PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);
            string privateKey = System.Convert.ToBase64String(pkInfo.GetDerEncoded());

            // and following for public:
            Org.BouncyCastle.Asn1.X509.SubjectPublicKeyInfo info = Org.BouncyCastle.X509.SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public);
            string publicKey = System.Convert.ToBase64String(info.GetDerEncoded());
        } // End Sub CerKeyInfo 


        public static void GenerateRsaKeyPair()
        {
            Org.BouncyCastle.Crypto.Generators.RsaKeyPairGenerator gen = new Org.BouncyCastle.Crypto.Generators.RsaKeyPairGenerator();

            // Pseudo Random Number Generators(PRNGs).
            // NIST calls them Deterministic Random Bit Generators (DRBGs).T
            var secureRandom = new Org.BouncyCastle.Security.SecureRandom(new Org.BouncyCastle.Crypto.Prng.CryptoApiRandomGenerator());
            var keyGenParam = new Org.BouncyCastle.Crypto.KeyGenerationParameters(secureRandom, 1024);
            gen.Init(keyGenParam);

            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair kp = gen.GenerateKeyPair();
            Org.BouncyCastle.Crypto.AsymmetricKeyParameter priv = (Org.BouncyCastle.Crypto.AsymmetricKeyParameter)kp.Private;
        } // End Sub GenerateRsaKeyPair 


        // Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair
        //public static string SignData(string msg, Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters privKey)
        public static string SignData(string msg, Org.BouncyCastle.Crypto.AsymmetricKeyParameter privKey)
        {
            try
            {
                byte[] msgBytes = System.Text.Encoding.UTF8.GetBytes(msg);


                // https://github.com/neoeinstein/bouncycastle/blob/master/crypto/src/security/SignerUtilities.cs
                // algorithms["SHA-256WITHRSA"] = "SHA-256withRSA";
                // algorithms["SHA-384WITHRSA"] = "SHA-384withRSA";
                // algorithms["SHA-512WITHRSA"] = "SHA-512withRSA";

                Org.BouncyCastle.Crypto.ISigner signer = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA-256withRSA");
                signer.Init(true, privKey);
                signer.BlockUpdate(msgBytes, 0, msgBytes.Length);
                byte[] sigBytes = signer.GenerateSignature();

                return System.Convert.ToBase64String(sigBytes);
            }
            catch (System.Exception exc)
            {
                System.Console.WriteLine("Signing Failed: " + exc.ToString());
                return null;
            }

        } // End Function SignData 


        public static bool VerifySignature(Org.BouncyCastle.Crypto.AsymmetricKeyParameter pubKey, string signature, string msg)
        {
            try
            {
                byte[] msgBytes = System.Text.Encoding.UTF8.GetBytes(msg);
                byte[] sigBytes = System.Convert.FromBase64String(signature);

                Org.BouncyCastle.Crypto.ISigner signer = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA-256withRSA");
                signer.Init(false, pubKey);
                signer.BlockUpdate(msgBytes, 0, msgBytes.Length);
                return signer.VerifySignature(sigBytes);
            }
            catch (System.Exception exc)
            {
                System.Console.WriteLine("Verification failed with the error: " + exc.ToString());
                return false;
            }

        } // End Function VerifySignature 


    } // End Class TestRSA 


} // End Namespace BouncyCastleTest 
