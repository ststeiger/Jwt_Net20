using System;
using System.Collections.Generic;
using System.Text;


//using Org.BouncyCastle.Crypto.Parameters.;



namespace BouncyCastleTest
{


    // https://stackoverflow.com/questions/29423997/bouncy-castle-sign-and-verify-sha256-certificate-with-c-sharp
    class TestECDSA
    {

        public static string SignData(string msg, Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters privKey)
        {
            try
            {
                byte[] msgBytes = Encoding.UTF8.GetBytes(msg);


                // https://github.com/neoeinstein/bouncycastle/blob/master/crypto/src/security/SignerUtilities.cs
                // algorithms["SHA-256/ECDSA"] = "SHA-256withECDSA";
                // algorithms["SHA-384/ECDSA"] = "SHA-384withECDSA";
                // algorithms["SHA-512/ECDSA"] = "SHA-512withECDSA";

                Org.BouncyCastle.Crypto.ISigner signer = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA-256withECDSA");
                signer.Init(true, privKey);
                signer.BlockUpdate(msgBytes, 0, msgBytes.Length);
                byte[] sigBytes = signer.GenerateSignature();

                return Convert.ToBase64String(sigBytes);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Signing Failed: " + exc.ToString());
                return null;
            }
        }

        public static bool VerifySignature(Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters pubKey, string signature, string msg)
        {
            try
            {
                byte[] msgBytes = Encoding.UTF8.GetBytes(msg);
                byte[] sigBytes = Convert.FromBase64String(signature);

                Org.BouncyCastle.Crypto.ISigner signer = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA-256withECDSA");
                signer.Init(false, pubKey);
                signer.BlockUpdate(msgBytes, 0, msgBytes.Length);
                return signer.VerifySignature(sigBytes);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Verification failed with the error: " + exc.ToString());
                return false;
            }
        }


        // https://stackoverflow.com/questions/18244630/elliptic-curve-with-digital-signature-algorithm-ecdsa-implementation-on-bouncy



        public static void GenerateEcdsaKeyPair()
        {
            Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator gen = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();
            var secureRandom = new Org.BouncyCastle.Security.SecureRandom();
            var ps = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
            var ecParams = new Org.BouncyCastle.Crypto.Parameters.ECDomainParameters(ps.Curve, ps.G, ps.N, ps.H);
            var keyGenParam = new Org.BouncyCastle.Crypto.Parameters.ECKeyGenerationParameters(ecParams, secureRandom);
            gen.Init(keyGenParam);

            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair kp = gen.GenerateKeyPair();

            Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters priv = (Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters)kp.Private;
        }


        public static void Test()
        {
            Console.WriteLine("Attempting to load cert...");
            System.Security.Cryptography.X509Certificates.X509Certificate2 thisCert = null; // LoadCertificate();

            Console.WriteLine(thisCert.IssuerName.Name);
            Console.WriteLine("Signing the text - Mary had a nuclear bomb");

            byte[] pkcs12Bytes = thisCert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs12, "dummy");
            Org.BouncyCastle.Pkcs.Pkcs12Store pkcs12 = new Org.BouncyCastle.Pkcs.Pkcs12StoreBuilder().Build();

            pkcs12.Load(new System.IO.MemoryStream(pkcs12Bytes, false), "dummy".ToCharArray());

            Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters privKey = null;
            foreach (string alias in pkcs12.Aliases)
            {
                if (pkcs12.IsKeyEntry(alias))
                {
                    privKey = (Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters)pkcs12.GetKey(alias).Key;
                    break;
                }
            }

            string signature = SignData("Mary had a nuclear bomb", privKey);

            Console.WriteLine("Signature: " + signature);

            Console.WriteLine("Verifying Signature");

            Org.BouncyCastle.X509.X509Certificate bcCert = Org.BouncyCastle.Security.DotNetUtilities.FromX509Certificate(thisCert);
            if (VerifySignature((Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters)bcCert.GetPublicKey(), signature, "Mary had a nuclear bomb."))
                Console.WriteLine("Valid Signature!");
            else
                Console.WriteLine("Signature NOT valid!");
        }



    }
}
