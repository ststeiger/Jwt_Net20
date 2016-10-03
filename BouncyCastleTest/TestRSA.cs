
namespace BouncyCastleTest
{


    class TestRSA
    {


        public static void WritePrivatePublic(Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair keyPair)
        {
            string privateKey = null;
            string publicKey = null;

            // id_rsa
            using (System.IO.TextWriter textWriter = new System.IO.StringWriter())
            {
                Org.BouncyCastle.OpenSsl.PemWriter pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(textWriter);
                pemWriter.WriteObject(keyPair.Private);
                pemWriter.Writer.Flush();

                privateKey = textWriter.ToString();
            } // End Using textWriter 

            // id_rsa.pub
            using (System.IO.TextWriter textWriter = new System.IO.StringWriter())
            {
                Org.BouncyCastle.OpenSsl.PemWriter pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(textWriter);
                pemWriter.WriteObject(keyPair.Public);
                pemWriter.Writer.Flush();

                publicKey = textWriter.ToString();
            } // End Using textWriter 

            System.Console.WriteLine(privateKey);
        } // End Sub WritePrivatePublic


        public static void ReadPrivateKey(string privateKeyFileName)
        {
            Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters key = null;

            using (System.IO.StreamReader streamReader = System.IO.File.OpenText(privateKeyFileName))
            {
                Org.BouncyCastle.OpenSsl.PemReader pemReader = 
                    new Org.BouncyCastle.OpenSsl.PemReader(streamReader);
                key = (Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters) pemReader.ReadObject();
            } // End Using streamReader 

            // Note: 
            // cipher.Init(false, key);
            // !!!
        } // End Function ReadPrivateKey


        public Org.BouncyCastle.Crypto.AsymmetricKeyParameter ReadPublicKey(string pemFilename)
        {
            Org.BouncyCastle.Crypto.AsymmetricKeyParameter keyParameter = null;

            using (System.IO.StreamReader streamReader = System.IO.File.OpenText(pemFilename))
            {
                Org.BouncyCastle.OpenSsl.PemReader pemReader = new Org.BouncyCastle.OpenSsl.PemReader(streamReader);
                keyParameter = (Org.BouncyCastle.Crypto.AsymmetricKeyParameter)pemReader.ReadObject ();
            } // End Using fileStream 

            return keyParameter;
        } // End Function ReadPublicKey 


        public static Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair ImportKeyPair(string fileName)
        {
            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair KeyPair = null;

            //  Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            using (System.IO.FileStream fs = System.IO.File.OpenRead(fileName))
            {
                
                using (System.IO.StreamReader sr =  new System.IO.StreamReader(fs) )
                {
                    Org.BouncyCastle.OpenSsl.PemReader pemReader = new Org.BouncyCastle.OpenSsl.PemReader(sr);
                    KeyPair = (Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)pemReader.ReadObject();
                    // System.Security.Cryptography.RSAParameters rsa = Org.BouncyCastle.Security.
                    //     DotNetUtilities.ToRSAParameters((Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters)KeyPair.Private);
                } // End Using sr 

            } // End Using fs 

            return KeyPair;
        } // End Function ImportKeyPair 


        public static void ExportKeyPair(Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair keyPair)
        {
            string privateKey = null;

            using (System.IO.TextWriter textWriter = new System.IO.StringWriter())
            {
                Org.BouncyCastle.OpenSsl.PemWriter pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(textWriter);
                pemWriter.WriteObject(keyPair.Private);
                pemWriter.Writer.Flush();

                privateKey = textWriter.ToString();
            } // End Using textWriter 

            System.Console.WriteLine(privateKey);
        } // End Sub ExportKeyPair 


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
            
            // new Org.BouncyCastle.Crypto.Parameters.RsaKeyGenerationParameters()

            Org.BouncyCastle.Security.SecureRandom secureRandom = 
                new Org.BouncyCastle.Security.SecureRandom(new Org.BouncyCastle.Crypto.Prng.CryptoApiRandomGenerator());

            Org.BouncyCastle.Crypto.KeyGenerationParameters keyGenParam = 
                new Org.BouncyCastle.Crypto.KeyGenerationParameters(secureRandom, 1024);


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
