
namespace BouncyCastleTest
{


    // DSA: Digital Signature Algorithm
    // ecDSA Elliptic Curve Digital Signature Algorithm
    public class BouncyECDSA : System.Security.Cryptography.ECDsa
    {
        Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters m_privKey;
        Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters m_pubKey;


        public BouncyECDSA(Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters privKey)
        {
            this.m_privKey = privKey;
        }


        public BouncyECDSA(Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters pubKey)
        {
            this.m_pubKey = pubKey;
        }


        public BouncyECDSA(Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair kp)
        {
            this.m_privKey = (Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters)kp.Private;
            this.m_pubKey = (Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters)kp.Public;
        }


        public override byte[] SignHash(byte[] hash)
        {
            // byte[] hash = System.Text.Encoding.UTF8.GetBytes(strHash);


            // https://github.com/neoeinstein/bouncycastle/blob/master/crypto/src/security/SignerUtilities.cs
            // algorithms["SHA-256/ECDSA"] = "SHA-256withECDSA";
            // algorithms["SHA-384/ECDSA"] = "SHA-384withECDSA";
            // algorithms["SHA-512/ECDSA"] = "SHA-512withECDSA";

            // base.SignatureAlgorithm
            Org.BouncyCastle.Crypto.ISigner signer = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(true, m_privKey);
            signer.BlockUpdate(hash, 0, hash.Length);
            byte[] sigBytes = signer.GenerateSignature();

            return sigBytes;
        } // End Function SignHash 


        public override bool VerifyHash(byte[] hash, byte[] signature)
        {
            // byte[] hash = System.Text.Encoding.UTF8.GetBytes(strHash);
            // byte[] signature = System.Convert.FromBase64String(strSignature);

            // base.SignatureAlgorithm
            Org.BouncyCastle.Crypto.ISigner signer = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(false, m_pubKey);
            signer.BlockUpdate(hash, 0, hash.Length);
            return signer.VerifySignature(signature);
        } // End Function VerifyHash 


        public override void FromXmlString(string xmlString)
        {
            throw new System.NotImplementedException();
        }

        public override string ToXmlString(bool includePrivateParameters)
        {
            throw new System.NotImplementedException();
        }
    }


}
