
// using Org.BouncyCastle;
// using Org.BouncyCastle.Crypto;
// using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;


///////////// using Org.BouncyCastle.Utilities.IO.Pem; // Nooooooooooooooo
using Org.BouncyCastle.OpenSsl;


namespace BouncyCastleTest
{


    // https://stackoverflow.com/questions/28086321/c-sharp-bouncycastle-rsa-encryption-with-public-private-keys
    public class TFRSAEncryption
    {


        public string RsaEncryptWithPublic(string clearText, string publicKey)
        {
            byte[] bytesToEncrypt = System.Text.Encoding.UTF8.GetBytes(clearText);
            var encryptEngine = new Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding(new RsaEngine());

            using (System.IO.StringReader txtreader = new System.IO.StringReader(publicKey))
            {
                var keyParameter = (Org.BouncyCastle.Crypto.AsymmetricKeyParameter)new PemReader(txtreader).ReadObject();

                encryptEngine.Init(true, keyParameter);
            }

            string encrypted = System.Convert.ToBase64String(encryptEngine.ProcessBlock(bytesToEncrypt, 0, bytesToEncrypt.Length));
            return encrypted;
        }


        public string RsaEncryptWithPrivate(string clearText, string privateKey)
        {
            byte[] bytesToEncrypt = System.Text.Encoding.UTF8.GetBytes(clearText);
            var encryptEngine = new Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding(new RsaEngine());
            
            using (System.IO.StringReader txtreader = new System.IO.StringReader(privateKey))
            {
                var keyPair = (Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)new PemReader(txtreader).ReadObject();

                encryptEngine.Init(true, keyPair.Private);
            }

            string encrypted = System.Convert.ToBase64String(encryptEngine.ProcessBlock(bytesToEncrypt, 0, bytesToEncrypt.Length));
            return encrypted;
        }


        // Decryption:
        public string RsaDecryptWithPrivate(string base64Input, string privateKey)
        {
            byte[] bytesToDecrypt = System.Convert.FromBase64String(base64Input);

            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair keyPair;
            var decryptEngine = new Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding(new RsaEngine());

            using (System.IO.StringReader txtreader = new System.IO.StringReader(privateKey))
            {
                keyPair = (Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)new PemReader(txtreader).ReadObject();

                var pm = new PemReader(txtreader);
                var po = pm.ReadPemObject();



                decryptEngine.Init(false, keyPair.Private);
            }

            string decrypted = System.Text.Encoding.UTF8.GetString(decryptEngine.ProcessBlock(bytesToDecrypt, 0, bytesToDecrypt.Length));
            return decrypted;
        }


        public string RsaDecryptWithPublic(string base64Input, string publicKey)
        {
            byte[] bytesToDecrypt = System.Convert.FromBase64String(base64Input);
            var decryptEngine = new Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding(new RsaEngine());

            using (System.IO.StringReader txtreader = new System.IO.StringReader(publicKey))
            {
                var keyParameter = (Org.BouncyCastle.Crypto.AsymmetricKeyParameter)new PemReader(txtreader).ReadObject();

                decryptEngine.Init(false, keyParameter);
            }

            string decrypted = System.Text.Encoding.UTF8.GetString(decryptEngine.ProcessBlock(bytesToDecrypt, 0, bytesToDecrypt.Length));
            return decrypted;
        }


    }





    class TestRsaEncryption
    {
        // https://stackoverflow.com/questions/11346200/reading-pem-rsa-public-key-only-using-bouncy-castle
        // Reads only public key
        public Org.BouncyCastle.Crypto.AsymmetricKeyParameter ReadAsymmetricKeyParameter(string pemFilename)
        {
            using (System.IO.StreamReader fileStream = System.IO.File.OpenText(pemFilename))
            {
                var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(fileStream);
                var KeyParameter = (Org.BouncyCastle.Crypto.AsymmetricKeyParameter)pemReader.ReadObject();
                return KeyParameter;
            }
        }

        // https://stackoverflow.com/questions/6029937/net-private-key-rsa-encryption
        public static void DotNetRsaParamtersFromPemFile()
        {
            using (System.IO.StreamReader sr = new System.IO.StreamReader("../../privatekey.pem"))
            {
                PemReader pr = new PemReader(sr);
                Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair KeyPair = (Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)pr.ReadObject();

                System.Security.Cryptography.RSAParameters rsa = Org.BouncyCastle.Security.DotNetUtilities.ToRSAParameters(
                    (Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters)KeyPair.Private
                );
            }
        }


        public static void Test()
        {
            // Set up 
            var input = "Perceived determine departure explained no forfeited";
            var enc = new TFRSAEncryption();
            var publicKey = "-----BEGIN PUBLIC KEY----- // Base64 string omitted // -----END PUBLIC KEY-----";
            var privateKey = "-----BEGIN PRIVATE KEY----- // Base64 string omitted// -----END PRIVATE KEY-----";

            // Encrypt it
            var encryptedWithPublic = enc.RsaEncryptWithPublic(input, publicKey);

            var encryptedWithPrivate = enc.RsaEncryptWithPrivate(input, privateKey);

            // Decrypt
            var output1 = enc.RsaDecryptWithPrivate(encryptedWithPublic, privateKey);

            var output2 = enc.RsaDecryptWithPublic(encryptedWithPrivate, publicKey);

            System.Console.WriteLine(output1 == output2 && output2 == input);
            System.Console.Read();
        }


    }

}
