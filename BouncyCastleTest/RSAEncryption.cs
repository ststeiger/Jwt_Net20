
// using Org.BouncyCastle;
// using Org.BouncyCastle.Crypto;
// using Org.BouncyCastle.Crypto.Encodings;
// using Org.BouncyCastle.Crypto.Engines;

// using Org.BouncyCastle.Utilities.IO.Pem.PemReader; // Nooooooooooooooo
// using Org.BouncyCastle.OpenSsl.PemReader; // Yeeeeees


namespace BouncyCastleTest
{


    // https://stackoverflow.com/questions/28086321/c-sharp-bouncycastle-rsa-encryption-with-public-private-keys
    public class TFRSAEncryption
    {


        public string RsaEncryptWithPublic(string clearText, string publicKey)
        {
            byte[] bytesToEncrypt = System.Text.Encoding.UTF8.GetBytes(clearText);
            Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding encryptEngine =
                new Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding(new Org.BouncyCastle.Crypto.Engines.RsaEngine());

            using (System.IO.StringReader txtreader = new System.IO.StringReader(publicKey))
            {
                Org.BouncyCastle.Crypto.AsymmetricKeyParameter keyParameter =
                    (Org.BouncyCastle.Crypto.AsymmetricKeyParameter)new Org.BouncyCastle.OpenSsl.PemReader(txtreader).ReadObject();

                encryptEngine.Init(true, keyParameter);
            }

            string encrypted = System.Convert.ToBase64String(encryptEngine.ProcessBlock(bytesToEncrypt, 0, bytesToEncrypt.Length));
            return encrypted;
        }


        public string RsaEncryptWithPrivate(string clearText, string privateKey)
        {
            byte[] bytesToEncrypt = System.Text.Encoding.UTF8.GetBytes(clearText);
            Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding encryptEngine =
                new Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding(new Org.BouncyCastle.Crypto.Engines.RsaEngine());
            
            using (System.IO.StringReader txtreader = new System.IO.StringReader(privateKey))
            {
                Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair keyPair =
                    (Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)new Org.BouncyCastle.OpenSsl.PemReader(txtreader).ReadObject();

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

            Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding decryptEngine =
                new Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding(new Org.BouncyCastle.Crypto.Engines.RsaEngine());

            using (System.IO.StringReader txtreader = new System.IO.StringReader(privateKey))
            {
                keyPair = (Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)new Org.BouncyCastle.OpenSsl.PemReader(txtreader).ReadObject();
                decryptEngine.Init(false, keyPair.Private);
            }

            string decrypted = System.Text.Encoding.UTF8.GetString(decryptEngine.ProcessBlock(bytesToDecrypt, 0, bytesToDecrypt.Length));
            return decrypted;
        }


        public string RsaDecryptWithPublic(string base64Input, string publicKey)
        {
            byte[] bytesToDecrypt = System.Convert.FromBase64String(base64Input);
            Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding decryptEngine =
                new Org.BouncyCastle.Crypto.Encodings.Pkcs1Encoding(new Org.BouncyCastle.Crypto.Engines.RsaEngine());

            using (System.IO.StringReader txtreader = new System.IO.StringReader(publicKey))
            {
                Org.BouncyCastle.Crypto.AsymmetricKeyParameter keyParameter =
                    (Org.BouncyCastle.Crypto.AsymmetricKeyParameter)new Org.BouncyCastle.OpenSsl.PemReader(txtreader).ReadObject();

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
                Org.BouncyCastle.OpenSsl.PemReader pemReader = new Org.BouncyCastle.OpenSsl.PemReader(fileStream);
                Org.BouncyCastle.Crypto.AsymmetricKeyParameter KeyParameter = (Org.BouncyCastle.Crypto.AsymmetricKeyParameter)pemReader.ReadObject();
                return KeyParameter;
            }
        }

        // https://stackoverflow.com/questions/6029937/net-private-key-rsa-encryption
        public static void DotNetRsaParamtersFromPemFile()
        {
            using (System.IO.StreamReader sr = new System.IO.StreamReader("../../privatekey.pem"))
            {
                Org.BouncyCastle.OpenSsl.PemReader pr = new Org.BouncyCastle.OpenSsl.PemReader(sr);
                Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair KeyPair = (Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)pr.ReadObject();

                System.Security.Cryptography.RSAParameters rsa = Org.BouncyCastle.Security.DotNetUtilities.ToRSAParameters(
                    (Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters)KeyPair.Private
                );
            }
        }


        public static void Test()
        {
            // Set up 
            string input = "Perceived determine departure explained no forfeited";
            TFRSAEncryption enc = new TFRSAEncryption();
            string publicKey = "-----BEGIN PUBLIC KEY----- // Base64 string omitted // -----END PUBLIC KEY-----";
            string privateKey = "-----BEGIN PRIVATE KEY----- // Base64 string omitted// -----END PRIVATE KEY-----";

            // Encrypt it
            string encryptedWithPublic = enc.RsaEncryptWithPublic(input, publicKey);
            string encryptedWithPrivate = enc.RsaEncryptWithPrivate(input, privateKey);

            // Decrypt
            string output1 = enc.RsaDecryptWithPrivate(encryptedWithPublic, privateKey);
            string output2 = enc.RsaDecryptWithPublic(encryptedWithPrivate, publicKey);

            System.Console.WriteLine(output1 == output2 && output2 == input);
            System.Console.Read();
        }


    }

}
