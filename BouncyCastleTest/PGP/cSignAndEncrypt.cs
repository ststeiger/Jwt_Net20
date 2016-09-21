
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;


namespace CSbouncyCastle
{


    // http://stackoverflow.com/questions/10209291/pgp-encrypt-and-decrypt
    public class PgpProcessor
    {


        public void SignAndEncryptFile(string strActualFileName, string strEmbeddedFileName,
            System.IO.Stream strmKeyIn, long lngKeyId, System.IO.Stream strmOutputStream,
            char[] szPassword, bool bArmor, bool bWithIntegrityCheck, PgpPublicKey PGP_PublicKey)
        {
            const int iBUFFER_SIZE = 1 << 16; // should always be power of 2

            if (bArmor)
                strmOutputStream = new ArmoredOutputStream(strmOutputStream);

            // Init encrypted data generator
            PgpEncryptedDataGenerator PGP_EncryptedDataGenerator = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.Cast5, bWithIntegrityCheck, new SecureRandom());
            PGP_EncryptedDataGenerator.AddMethod(PGP_PublicKey);
            System.IO.Stream strmEncryptedOut = PGP_EncryptedDataGenerator.Open(strmOutputStream, new byte[iBUFFER_SIZE]);

            // Init compression
            PgpCompressedDataGenerator PGP_CompressedDataGenerator = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
            System.IO.Stream strmCompressedOut = PGP_CompressedDataGenerator.Open(strmEncryptedOut);

            // Init signature
            PgpSecretKeyRingBundle PGP_SecretKeyBundle = new PgpSecretKeyRingBundle(PgpUtilities.GetDecoderStream(strmKeyIn));
            PgpSecretKey PGP_SecretKey = PGP_SecretKeyBundle.GetSecretKey(lngKeyId);
            if (PGP_SecretKey == null)
                throw new System.ArgumentException(lngKeyId.ToString("X") + " could not be found in specified key ring bundle.", "keyId");

            PgpPrivateKey PGP_PrivateKey = PGP_SecretKey.ExtractPrivateKey(szPassword);
            PgpSignatureGenerator PGP_SignatureGenerator = new PgpSignatureGenerator(PGP_SecretKey.PublicKey.Algorithm, HashAlgorithmTag.Sha1);
            PGP_SignatureGenerator.InitSign(PgpSignature.BinaryDocument, PGP_PrivateKey);

            foreach (string strUserId in PGP_SecretKey.PublicKey.GetUserIds())
            {
                PgpSignatureSubpacketGenerator PGP_SignatureSubpacketGenerator = new PgpSignatureSubpacketGenerator();
                PGP_SignatureSubpacketGenerator.SetSignerUserId(false, strUserId);
                PGP_SignatureGenerator.SetHashedSubpackets(PGP_SignatureSubpacketGenerator.Generate());
                // Just the first one!
                break;
            }
            PGP_SignatureGenerator.GenerateOnePassVersion(false).Encode(strmCompressedOut);

            // Create the Literal Data generator output stream
            PgpLiteralDataGenerator PGP_LiteralDataGenerator = new PgpLiteralDataGenerator();
            System.IO.FileInfo fiEmbeddedFile = new System.IO.FileInfo(strEmbeddedFileName);
            System.IO.FileInfo fiActualFile = new System.IO.FileInfo(strActualFileName);
            // TODO: Use lastwritetime from source file
            System.IO.Stream strmLiteralOut = PGP_LiteralDataGenerator.Open(strmCompressedOut, PgpLiteralData.Binary,
                fiEmbeddedFile.Name, fiActualFile.LastWriteTime, new byte[iBUFFER_SIZE]);

            // Open the input file
            System.IO.FileStream strmInputStream = fiActualFile.OpenRead();

            byte[] baBuffer = new byte[iBUFFER_SIZE];
            int iReadLength;
            while ((iReadLength = strmInputStream.Read(baBuffer, 0, baBuffer.Length)) > 0)
            {
                strmLiteralOut.Write(baBuffer, 0, iReadLength);
                PGP_SignatureGenerator.Update(baBuffer, 0, iReadLength);
            }

            strmLiteralOut.Close();
            PGP_LiteralDataGenerator.Close();
            PGP_SignatureGenerator.Generate().Encode(strmCompressedOut);
            strmCompressedOut.Close();
            PGP_CompressedDataGenerator.Close();
            strmEncryptedOut.Close();
            PGP_EncryptedDataGenerator.Close();
            strmInputStream.Close();

            if (bArmor)
                strmOutputStream.Close();
        }


    } // End class


} // End namespace
