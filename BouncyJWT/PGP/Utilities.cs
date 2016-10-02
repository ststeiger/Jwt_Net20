
namespace BouncyCastleTest.PGP
{


    internal class PgpExampleUtilities
    {


        internal static byte[] CompressFile(string fileName, Org.BouncyCastle.Bcpg.CompressionAlgorithmTag algorithm)
        {
            System.IO.MemoryStream bOut = new System.IO.MemoryStream();
            Org.BouncyCastle.Bcpg.OpenPgp.PgpCompressedDataGenerator comData = new Org.BouncyCastle.Bcpg.OpenPgp.PgpCompressedDataGenerator(algorithm);
            Org.BouncyCastle.Bcpg.OpenPgp.PgpUtilities.WriteFileToLiteralData(comData.Open(bOut), Org.BouncyCastle.Bcpg.OpenPgp.PgpLiteralData.Binary,
                new System.IO.FileInfo(fileName));
            comData.Close();
            return bOut.ToArray();
        }


        /**
         * Search a secret key ring collection for a secret key corresponding to keyID if it
         * exists.
         * 
         * @param pgpSec a secret key ring collection.
         * @param keyID keyID we want.
         * @param pass passphrase to decrypt secret key with.
         * @return
         * @throws PGPException
         * @throws NoSuchProviderException
         */
        internal static Org.BouncyCastle.Bcpg.OpenPgp.PgpPrivateKey FindSecretKey(Org.BouncyCastle.Bcpg.OpenPgp.PgpSecretKeyRingBundle pgpSec, long keyID, char[] pass)
        {
            Org.BouncyCastle.Bcpg.OpenPgp.PgpSecretKey pgpSecKey = pgpSec.GetSecretKey(keyID);

            if (pgpSecKey == null)
            {
                return null;
            }

            return pgpSecKey.ExtractPrivateKey(pass);
        }


        internal static Org.BouncyCastle.Bcpg.OpenPgp.PgpPublicKey ReadPublicKey(string fileName)
        {
            using (System.IO.Stream keyIn = System.IO.File.OpenRead(fileName))
            {
                return ReadPublicKey(keyIn);
            }
        }

        /**
         * A simple routine that opens a key ring file and loads the first available key
         * suitable for encryption.
         * 
         * @param input
         * @return
         * @throws IOException
         * @throws PGPException
         */
        internal static Org.BouncyCastle.Bcpg.OpenPgp.PgpPublicKey ReadPublicKey(System.IO.Stream input)
        {
            Org.BouncyCastle.Bcpg.OpenPgp.PgpPublicKeyRingBundle pgpPub = new Org.BouncyCastle.Bcpg.OpenPgp.PgpPublicKeyRingBundle(
                Org.BouncyCastle.Bcpg.OpenPgp.PgpUtilities.GetDecoderStream(input));

            //
            // we just loop through the collection till we find a key suitable for encryption, in the real
            // world you would probably want to be a bit smarter about this.
            //

            foreach (Org.BouncyCastle.Bcpg.OpenPgp.PgpPublicKeyRing keyRing in pgpPub.GetKeyRings())
            {
                foreach (Org.BouncyCastle.Bcpg.OpenPgp.PgpPublicKey key in keyRing.GetPublicKeys())
                {
                    if (key.IsEncryptionKey)
                    {
                        return key;
                    }
                }
            }

            throw new System.ArgumentException("Can't find encryption key in key ring.");
        }


        internal static Org.BouncyCastle.Bcpg.OpenPgp.PgpSecretKey ReadSecretKey(string fileName)
        {
            using (System.IO.Stream keyIn = System.IO.File.OpenRead(fileName))
            {
                return ReadSecretKey(keyIn);
            }
        }


        /**
         * A simple routine that opens a key ring file and loads the first available key
         * suitable for signature generation.
         * 
         * @param input stream to read the secret key ring collection from.
         * @return a secret key.
         * @throws IOException on a problem with using the input stream.
         * @throws PGPException if there is an issue parsing the input stream.
         */
        internal static Org.BouncyCastle.Bcpg.OpenPgp.PgpSecretKey ReadSecretKey(System.IO.Stream input)
        {
            Org.BouncyCastle.Bcpg.OpenPgp.PgpSecretKeyRingBundle pgpSec = new Org.BouncyCastle.Bcpg.OpenPgp.PgpSecretKeyRingBundle(
                Org.BouncyCastle.Bcpg.OpenPgp.PgpUtilities.GetDecoderStream(input));

            //
            // we just loop through the collection till we find a key suitable for encryption, in the real
            // world you would probably want to be a bit smarter about this.
            //

            foreach (Org.BouncyCastle.Bcpg.OpenPgp.PgpSecretKeyRing keyRing in pgpSec.GetKeyRings())
            {
                foreach (Org.BouncyCastle.Bcpg.OpenPgp.PgpSecretKey key in keyRing.GetSecretKeys())
                {
                    if (key.IsSigningKey)
                    {
                        return key;
                    }
                }
            }

            throw new System.ArgumentException("Can't find signing key in key ring.");
        }


    }


}
