
namespace BouncyJWT
{


    public class Crypto
    {


        // GenerateRsaKeyPair(1024)
        public static Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair GenerateRsaKeyPair(int strength)
        {
            Org.BouncyCastle.Crypto.Generators.RsaKeyPairGenerator gen = new Org.BouncyCastle.Crypto.Generators.RsaKeyPairGenerator();
            Org.BouncyCastle.Security.SecureRandom secureRandom = new Org.BouncyCastle.Security.SecureRandom(new Org.BouncyCastle.Crypto.Prng.CryptoApiRandomGenerator());
            Org.BouncyCastle.Crypto.KeyGenerationParameters keyGenParam = new Org.BouncyCastle.Crypto.KeyGenerationParameters(secureRandom, strength);

            gen.Init(keyGenParam);

            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair kp = gen.GenerateKeyPair();
            return kp;
        } // End Function GenerateRsaKeyPair 


        public static string GenerateRandomRsaPrivateKey(int strength)
        {
            Org.BouncyCastle.Crypto.Generators.RsaKeyPairGenerator gen = new Org.BouncyCastle.Crypto.Generators.RsaKeyPairGenerator();
            Org.BouncyCastle.Security.SecureRandom secureRandom = new Org.BouncyCastle.Security.SecureRandom(new Org.BouncyCastle.Crypto.Prng.CryptoApiRandomGenerator());
            Org.BouncyCastle.Crypto.KeyGenerationParameters keyGenParam = new Org.BouncyCastle.Crypto.KeyGenerationParameters(secureRandom, strength);

            gen.Init(keyGenParam);

            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair kp = gen.GenerateKeyPair();

            return StringifyAsymmetricKey(kp.Private);
        } // End Function GenerateRsaKeyPair 


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
            System.Console.WriteLine(publicKey);
        } // End Sub WritePrivatePublic


        public static string StringifyAsymmetricKey(Org.BouncyCastle.Crypto.AsymmetricKeyParameter privateOrPublicKey)
        {
            string key = null;

            using (System.IO.TextWriter textWriter = new System.IO.StringWriter())
            {
                Org.BouncyCastle.OpenSsl.PemWriter pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(textWriter);
                pemWriter.WriteObject(privateOrPublicKey);
                pemWriter.Writer.Flush();

                key = textWriter.ToString();
            } // End Using textWriter 

            return key;
        } // End Function StringifyAsymmetricKey 


        public static Org.BouncyCastle.Crypto.AsymmetricKeyParameter ReadPrivateKey(string privateKey)
        {
            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair keyPair = null;

            using (System.IO.TextReader reader = new System.IO.StringReader(privateKey))
            {
                Org.BouncyCastle.OpenSsl.PemReader pemReader = new Org.BouncyCastle.OpenSsl.PemReader(reader);

                object obj = pemReader.ReadObject();

                if (obj is Org.BouncyCastle.Crypto.AsymmetricKeyParameter)
                {
                    throw new System.ArgumentException("The given privateKey is a public key, not a privateKey...", "privateKey");
                }

                if (!(obj is Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair))
                {
                    throw new System.ArgumentException("The given privateKey is not a valid assymetric key.", "privateKey");
                }

                keyPair = (Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)obj;
            } // End Using reader 

            // Org.BouncyCastle.Crypto.AsymmetricKeyParameter priv = keyPair.Private;
            // Org.BouncyCastle.Crypto.AsymmetricKeyParameter pub = keyPair.Public;

            // Note: 
            // cipher.Init(False, key);
            // !!!

            return keyPair.Private;
        } // End Function ReadPrivateKey


        public Org.BouncyCastle.Crypto.AsymmetricKeyParameter ReadPublicKeyFile(string pemFilename)
        {
            Org.BouncyCastle.Crypto.AsymmetricKeyParameter keyParameter = null;

            using (System.IO.StreamReader streamReader = System.IO.File.OpenText(pemFilename))
            {
                Org.BouncyCastle.OpenSsl.PemReader pemReader = new Org.BouncyCastle.OpenSsl.PemReader(streamReader);
                keyParameter = (Org.BouncyCastle.Crypto.AsymmetricKeyParameter)pemReader.ReadObject();
            } // End Using fileStream 

            return keyParameter;
        } // End Function ReadPublicKey 


    } // End Class Crypto 


} // End Namespace BouncyJWT 
