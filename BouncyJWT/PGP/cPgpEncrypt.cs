
using Org.BouncyCastle.Bcpg.OpenPgp;


namespace CSbouncyCastle
{


    public class PgpEncryptionKeys
    {
        public PgpPublicKey PublicKey { get; private set; }
        public PgpPrivateKey PrivateKey { get; private set; }
        public PgpSecretKey SecretKey { get; private set; }


        /// <summary>
        /// Initializes a new instance of the EncryptionKeys class.
        /// Two keys are required to encrypt and sign data. Your private key and the recipients public key.
        /// The data is encrypted with the recipients public key and signed with your private key.
        /// </summary>
        /// <param name="publicKeyPath">The key used to encrypt the data</param>
        /// <param name="privateKeyPath">The key used to sign the data.</param>
        /// <param name="passPhrase">The (your) password required to access the private key</param>
        /// <exception cref="ArgumentException">Public key not found. Private key not found. Missing password</exception>
        public PgpEncryptionKeys(string strPublicKeyPath, string strPrivateKeyPath, string strPassPhrase)
        {
            if (!System.IO.File.Exists(strPublicKeyPath))
                throw new System.ArgumentException("Public key file not found", "publicKeyPath");

            if (!System.IO.File.Exists(strPrivateKeyPath))
                throw new System.ArgumentException("Private key file not found", "privateKeyPath");

            if (string.IsNullOrEmpty(strPassPhrase))
                throw new System.ArgumentException("passPhrase is null or empty.", "passPhrase");

            PublicKey = ReadPublicKey(strPublicKeyPath);
            SecretKey = ReadSecretKey(strPrivateKeyPath);
            PrivateKey = ReadPrivateKey(strPassPhrase);
        }


        #region Secret Key
        protected PgpSecretKey ReadSecretKey(string strPrivateKeyPath)
        {
            using (System.IO.Stream strmPrivateKeyIn = System.IO.File.OpenRead(strPrivateKeyPath))
            using (System.IO.Stream strmPrivateKeyInputStream = PgpUtilities.GetDecoderStream(strmPrivateKeyIn))
            {
                PgpSecretKeyRingBundle PGP_SecretKeyRingBundle = new PgpSecretKeyRingBundle(strmPrivateKeyInputStream);
                PgpSecretKey PGP_ThisSecretKey = GetFirstSecretKey(PGP_SecretKeyRingBundle);

                if (PGP_ThisSecretKey != null)
                    return PGP_ThisSecretKey;
            }


            throw new System.ArgumentException("Can't find signing key in key ring.");
        }


        /// <summary>
        /// Return the first key we can use to encrypt.
        /// Note: A file can contain multiple keys (stored in "key rings")
        /// </summary>
        protected PgpSecretKey GetFirstSecretKey(PgpSecretKeyRingBundle PGP_SecretKeyRingBundle)
        {

            foreach (PgpSecretKeyRing PGP_SecretKeyRing in PGP_SecretKeyRingBundle.GetKeyRings())
            {
                foreach (PgpSecretKey PGP_SecretKey in PGP_SecretKeyRing.GetSecretKeys())
                {
                    if (PGP_SecretKey.IsSigningKey)
                    {
                        return PGP_SecretKey;
                    }
                }
            }

            return null;
        }
        #endregion


        #region Public Key
        protected PgpPublicKey ReadPublicKey(string strPublicKeyPath)
        {
            using (System.IO.Stream strmPublicKeyIn = System.IO.File.OpenRead(strPublicKeyPath))
            using (System.IO.Stream strmPublicKeyInputStream = PgpUtilities.GetDecoderStream(strmPublicKeyIn))
            {
                PgpPublicKeyRingBundle PGP_PublicKeyRingBundle = new PgpPublicKeyRingBundle(strmPublicKeyInputStream);
                PgpPublicKey PGP_ThisPublicKey = GetFirstPublicKey(PGP_PublicKeyRingBundle);
                
                if (PGP_ThisPublicKey != null)
                    return PGP_ThisPublicKey;
            }

            throw new System.ArgumentException("No encryption key found in public key ring.");
        }


        protected PgpPublicKey GetFirstPublicKey(PgpPublicKeyRingBundle PGP_PublicKeyRingBundle)
        {

            foreach (PgpPublicKeyRing PGP_Public_kRing in PGP_PublicKeyRingBundle.GetKeyRings())
            {
                foreach (PgpPublicKey PGP_PublicKey in PGP_Public_kRing.GetPublicKeys())
                {
                    if (PGP_PublicKey.IsEncryptionKey)
                    {
                        return PGP_PublicKey;
                    }
                }
            }

            return null;
        }
        #endregion


        #region Private Key
        protected PgpPrivateKey ReadPrivateKey(string strPassPhrase)
        {
            PgpPrivateKey PGP_PrivateKey = SecretKey.ExtractPrivateKey(strPassPhrase.ToCharArray());
            if (PGP_PrivateKey != null)
                return PGP_PrivateKey;

            throw new System.ArgumentException("No private key found in secret key.");
        }
        #endregion


    } // End class class PgpEncryptionKeys


} // End namespace CSbouncyCastle
