
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;


namespace CSbouncyCastle
{


    /// <summary>
    /// Wrapper around Bouncy Castle OpenPGP library.
    /// Bouncy documentation can be found here: http://www.bouncycastle.org/docs/pgdocs1.6/index.html
    /// </summary>
    public class PgpEncrypt
    {


        protected PgpEncryptionKeys m_encryptionKeys;
        protected const int BufferSize = 0x10000; // should always be power of 2 


        /// <summary>
        /// Instantiate a new PgpEncrypt class with initialized PgpEncryptionKeys.
        /// </summary>
        /// <param name="encryptionKeys"></param>
        /// <exception cref="ArgumentNullException">encryptionKeys is null</exception>
        public PgpEncrypt(PgpEncryptionKeys encryptionKeys)
        {
            if (encryptionKeys == null)
                throw new System.ArgumentNullException("encryptionKeys", "encryptionKeys is null.");
            
            m_encryptionKeys = encryptionKeys;
        }


        /// <summary>
        /// Encrypt and sign the file pointed to by unencryptedFileInfo and
        /// write the encrypted content to outputStream.
        /// </summary>
        /// <param name="outputStream">The stream that will contain the
        /// encrypted data when this method returns.</param>
        /// <param name="fileName">FileInfo of the file to encrypt</param>
        public void EncryptAndSign(System.IO.Stream outputStream, System.IO.FileInfo unencryptedFileInfo)
        {
            if (outputStream == null)
                throw new System.ArgumentNullException("outputStream", "outputStream is null.");
            
            if (unencryptedFileInfo == null)
                throw new System.ArgumentNullException("unencryptedFileInfo", "unencryptedFileInfo is null.");
            
            if (!System.IO.File.Exists(unencryptedFileInfo.FullName))
                throw new System.ArgumentException("File to encrypt not found.");
            
            using (System.IO.Stream encryptedOut = ChainEncryptedOut(outputStream))
            using (System.IO.Stream compressedOut = ChainCompressedOut(encryptedOut))
            {
                PgpSignatureGenerator signatureGenerator = InitSignatureGenerator(compressedOut);
                using (System.IO.Stream literalOut = ChainLiteralOut(compressedOut, unencryptedFileInfo))
                using (System.IO.FileStream inputFile = unencryptedFileInfo.OpenRead())
                {
                    WriteOutputAndSign(compressedOut, literalOut, inputFile, signatureGenerator);
                }
            }
        } // End Sub EncryptAndSign


        protected static void WriteOutputAndSign(System.IO.Stream compressedOut,
            System.IO.Stream literalOut,
            System.IO.FileStream inputFile,
            PgpSignatureGenerator signatureGenerator)
        {
            int length = 0;
            byte[] buf = new byte[BufferSize];
            
            while ((length = inputFile.Read(buf, 0, buf.Length)) > 0)
            {
                literalOut.Write(buf, 0, length);
                signatureGenerator.Update(buf, 0, length);
            } // Whend

            signatureGenerator.Generate().Encode(compressedOut);
        } // End sub WriteOutputAndSign


        protected System.IO.Stream ChainEncryptedOut(System.IO.Stream outputStream)
        {
            PgpEncryptedDataGenerator encryptedDataGenerator;
            encryptedDataGenerator = new PgpEncryptedDataGenerator(SymmetricKeyAlgorithmTag.TripleDes, new SecureRandom());
            encryptedDataGenerator.AddMethod(m_encryptionKeys.PublicKey);
            return encryptedDataGenerator.Open(outputStream, new byte[BufferSize]);
        } // End Sub ChainEncryptedOut


        protected static System.IO.Stream ChainCompressedOut(System.IO.Stream encryptedOut)
        {
            PgpCompressedDataGenerator compressedDataGenerator = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);
            return compressedDataGenerator.Open(encryptedOut);
        } // End function ChainCompressedOut


        protected static System.IO.Stream ChainLiteralOut(System.IO.Stream compressedOut, System.IO.FileInfo file)
        {
            PgpLiteralDataGenerator pgpLiteralDataGenerator = new PgpLiteralDataGenerator();
            return pgpLiteralDataGenerator.Open(compressedOut, PgpLiteralData.Binary, file);
        } // End function ChainLiteralOut


        protected PgpSignatureGenerator InitSignatureGenerator(System.IO.Stream compressedOut)
        {
            const bool IsCritical = false;
            const bool IsNested = false;
            PublicKeyAlgorithmTag tag = m_encryptionKeys.SecretKey.PublicKey.Algorithm;
            PgpSignatureGenerator pgpSignatureGenerator = new PgpSignatureGenerator(tag, HashAlgorithmTag.Sha1);

            pgpSignatureGenerator.InitSign(PgpSignature.BinaryDocument, m_encryptionKeys.PrivateKey);
            foreach (string userId in m_encryptionKeys.SecretKey.PublicKey.GetUserIds())
            {
                PgpSignatureSubpacketGenerator subPacketGenerator = new PgpSignatureSubpacketGenerator();
                subPacketGenerator.SetSignerUserId(IsCritical, userId);
                pgpSignatureGenerator.SetHashedSubpackets(subPacketGenerator.Generate());
                // Just the first one!
                break;
            } // Next userId

            pgpSignatureGenerator.GenerateOnePassVersion(IsNested).Encode(compressedOut);
            return pgpSignatureGenerator;
        } // End function InitSignatureGenerator


    } // End class pgpencrypt


} // End namespace CSbouncyCastle
