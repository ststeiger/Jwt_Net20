
namespace BouncyCastleTest
{


    // https://stackoverflow.com/questions/37526036/how-to-determine-the-public-key-size-from-the-csr-file-using-bouncy-castle-in-ja
    // https://searchcode.com/file/94872444/crypto/src/asn1/sec/SECNamedCurves.cs
    public class BouncyECDSA : System.Security.Cryptography.ECDsa // abstract class ECDsa : AsymmetricAlgorithm
    {
        Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters m_privKey;
        Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters m_pubKey;



        // https://stackoverflow.com/questions/18244630/elliptic-curve-with-digital-signature-algorithm-ecdsa-implementation-on-bouncy
        public static Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair GenerateEcdsaKeyPair()
        {
            Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator gen =
                new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();

            Org.BouncyCastle.Security.SecureRandom secureRandom =
                new Org.BouncyCastle.Security.SecureRandom();

            // https://github.com/bcgit/bc-csharp/blob/master/crypto/src/asn1/sec/SECNamedCurves.cs#LC1096
            Org.BouncyCastle.Asn1.X9.X9ECParameters ps =
            //Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
            Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp521r1");


            Org.BouncyCastle.Crypto.Parameters.ECDomainParameters ecParams =
                new Org.BouncyCastle.Crypto.Parameters.ECDomainParameters(ps.Curve, ps.G, ps.N, ps.H);

            Org.BouncyCastle.Crypto.Parameters.ECKeyGenerationParameters keyGenParam =
                new Org.BouncyCastle.Crypto.Parameters.ECKeyGenerationParameters(ecParams, secureRandom);

            gen.Init(keyGenParam);
            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair kp = gen.GenerateKeyPair();

            // Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters priv = 
            //     (Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters)kp.Private;

            return kp;
        } // End Function GenerateEcdsaKeyPair 



        public BouncyECDSA()
            : this(GenerateEcdsaKeyPair())
        { } // End Constructor 


        public BouncyECDSA(Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters privKey)
            : base()
        {
            this.m_privKey = privKey;
            this.KeySizeValue = this.m_privKey.Parameters.Curve.FieldSize;
        } // End Constructor 


        public BouncyECDSA(Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters pubKey)
            : base()
        {
            this.m_pubKey = pubKey;
            this.KeySizeValue = this.m_pubKey.Parameters.Curve.FieldSize;
        } // End Constructor 


        public BouncyECDSA(Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair kp)
            : base()
        {
            this.m_privKey = (Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters)kp.Private;
            this.m_pubKey = (Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters)kp.Public;
            //this.KeySizeValue = keySize;

            //var x = (Org.BouncyCastle.Crypto.Parameters.ECKeyParameters)kp.Public;

            // var x = (Org.BouncyCastle.Crypto.Parameters.DsaPublicKeyParameters)kp.Public;
            // var y = (Org.BouncyCastle.Crypto.Parameters.DsaPrivateKeyParameters)kp.Private;

            // this.KeySizeValue = x.Y.BitCount;
            // this.KeySizeValue = y.X.BitCount;
            this.KeySizeValue = this.m_privKey.Parameters.Curve.FieldSize;
        } // End Constructor 


        private byte[] DerEncode(Org.BouncyCastle.Math.BigInteger r, Org.BouncyCastle.Math.BigInteger s)
        {
            return new Org.BouncyCastle.Asn1.DerSequence(
                new Org.BouncyCastle.Asn1.Asn1Encodable[2]
                {
                    new Org.BouncyCastle.Asn1.DerInteger(r),
                    new Org.BouncyCastle.Asn1.DerInteger(s)
                }
            ).GetDerEncoded();
        } // End Function DerEncode 


        private Org.BouncyCastle.Math.BigInteger[] DerDecode(byte[] encoding)
        {
            Org.BouncyCastle.Asn1.Asn1Sequence asn1Sequence =
                (Org.BouncyCastle.Asn1.Asn1Sequence)
                Org.BouncyCastle.Asn1.Asn1Object.FromByteArray(encoding);

            return new Org.BouncyCastle.Math.BigInteger[2]
            {
                ((Org.BouncyCastle.Asn1.DerInteger) asn1Sequence[0]).Value,
                ((Org.BouncyCastle.Asn1.DerInteger) asn1Sequence[1]).Value
            };
        } // End Function DerDecode 




        public override void FromXmlString(string xmlString)
        {
            throw new System.NotImplementedException();
        }

        public override string ToXmlString(bool includePrivateParameters)
        {
            throw new System.NotImplementedException();
        }
        
        // Abstract    
        // throw new InvalidKeyException("EC private key required for signing");
        public override byte[] SignHash(byte[] hash)
        {
            byte[] encoded = SignHashInternal(hash);

            return AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(encoded, 0, encoded.Length, this.KeySize);
        } // End Function SignHash 


        public byte[] SignHashInternal(byte[] hash)
        {
            if (hash == null)
                throw new System.ArgumentNullException(nameof(hash));

            Org.BouncyCastle.Crypto.Signers.ECDsaSigner signer = new Org.BouncyCastle.Crypto.Signers.ECDsaSigner();
            signer.Init(true, this.m_privKey);

            Org.BouncyCastle.Math.BigInteger[] signature = signer.GenerateSignature(hash);
            byte[] encoded = this.DerEncode(signature[0], signature[1]);

            return encoded;
        } // End Function SignHashInternal


        // Abstract
        public override bool VerifyHash(byte[] hash, byte[] signature)
        {
            if (hash == null)
                throw new System.ArgumentNullException(nameof(hash));

            if (signature == null)
                throw new System.ArgumentNullException(nameof(signature));

            int num = 2 * AsymmetricAlgorithmHelpers.BitsToBytes(this.KeySize);
            if (signature.Length != num)
                return false;

            byte[] derSignature = AsymmetricAlgorithmHelpers.ConvertIeee1363ToDer(signature);

            Org.BouncyCastle.Crypto.Signers.ECDsaSigner signer =
                new Org.BouncyCastle.Crypto.Signers.ECDsaSigner();
            signer.Init(false, this.m_pubKey);

            Org.BouncyCastle.Math.BigInteger[] bigIntegerArray = this.DerDecode(derSignature);
            return signer.VerifySignature(hash, bigIntegerArray[0], bigIntegerArray[1]);
        } // End Function VerifyHash 


    }
    

}
