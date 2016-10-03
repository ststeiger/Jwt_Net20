
namespace JWT
{


    // RSACryptoServiceProvider provider = PemKeyUtils.GetRSAProviderFromPemFile(  @"public_key.pem" );
    // http://stackoverflow.com/questions/11506891/how-to-load-the-rsa-public-key-from-file-in-c-sharp
    public class PemKeyUtils
    {
        const string pemprivheader = "-----BEGIN RSA PRIVATE KEY-----";
        const string pemprivfooter = "-----END RSA PRIVATE KEY-----";
        const string pempubheader = "-----BEGIN PUBLIC KEY-----";
        const string pempubfooter = "-----END PUBLIC KEY-----";
        const string pemp8header = "-----BEGIN PRIVATE KEY-----";
        const string pemp8footer = "-----END PRIVATE KEY-----";
        const string pemp8encheader = "-----BEGIN ENCRYPTED PRIVATE KEY-----";
        const string pemp8encfooter = "-----END ENCRYPTED PRIVATE KEY-----";

        static bool verbose = false;

        public static System.Security.Cryptography.RSACryptoServiceProvider GetRSAProviderFromPemFile(string pemfile)
        {
            bool isPrivateKeyFile = true;
            string pemstr = System.IO.File.ReadAllText(pemfile).Trim();
            if (pemstr.StartsWith(pempubheader) && pemstr.EndsWith(pempubfooter))
                isPrivateKeyFile = false;

            byte[] pemkey;
            if (isPrivateKeyFile)
                pemkey = DecodeOpenSSLPrivateKey(pemstr);
            else
                pemkey = DecodeOpenSSLPublicKey(pemstr);

            if (pemkey == null)
                return null;

            if (isPrivateKeyFile)
                return DecodeRSAPrivateKey(pemkey);
            else
                return DecodeX509PublicKey(pemkey);

        }



        //--------   Get the binary RSA PUBLIC key   --------
        static byte[] DecodeOpenSSLPublicKey(string instr)
        {
            const string pempubheader = "-----BEGIN PUBLIC KEY-----";
            const string pempubfooter = "-----END PUBLIC KEY-----";
            string pemstr = instr.Trim();
            byte[] binkey;
            if (!pemstr.StartsWith(pempubheader) || !pemstr.EndsWith(pempubfooter))
                return null;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(pemstr);
            sb.Replace(pempubheader, "");  //remove headers/footers, if present
            sb.Replace(pempubfooter, "");

            string pubstr = sb.ToString().Trim();   //get string after removing leading/trailing whitespace

            try
            {
                binkey = System.Convert.FromBase64String(pubstr);
            }
            catch (System.FormatException)
            {       //if can't b64 decode, data is not valid
                return null;
            }
            return binkey;
        }


        //------- Parses binary asn.1 X509 SubjectPublicKeyInfo; returns RSACryptoServiceProvider ---
        static System.Security.Cryptography.RSACryptoServiceProvider DecodeX509PublicKey(byte[] x509key)
        {
            // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
            byte[] SeqOID = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
            byte[] seq = new byte[15];
            // ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
            System.IO.MemoryStream mem = new System.IO.MemoryStream(x509key);
            System.IO.BinaryReader binr = new System.IO.BinaryReader(mem);    //wrap Memory Stream with BinaryReader for easy reading
            byte bt = 0;
            ushort twobytes = 0;

            try
            {

                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                    binr.ReadByte();    //advance 1 byte
                else if (twobytes == 0x8230)
                    binr.ReadInt16();   //advance 2 bytes
                else
                    return null;

                seq = binr.ReadBytes(15);     //read the Sequence OID
                if (!CompareBytearrays(seq, SeqOID))  //make sure Sequence for OID is correct
                    return null;

                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8103) //data read as little endian order (actual data order for Bit String is 03 81)
                    binr.ReadByte();    //advance 1 byte
                else if (twobytes == 0x8203)
                    binr.ReadInt16();   //advance 2 bytes
                else
                    return null;

                bt = binr.ReadByte();
                if (bt != 0x00)     //expect null byte next
                    return null;

                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                    binr.ReadByte();    //advance 1 byte
                else if (twobytes == 0x8230)
                    binr.ReadInt16();   //advance 2 bytes
                else
                    return null;

                twobytes = binr.ReadUInt16();
                byte lowbyte = 0x00;
                byte highbyte = 0x00;

                if (twobytes == 0x8102) //data read as little endian order (actual data order for Integer is 02 81)
                    lowbyte = binr.ReadByte();  // read next bytes which is bytes in modulus
                else if (twobytes == 0x8202)
                {
                    highbyte = binr.ReadByte(); //advance 2 bytes
                    lowbyte = binr.ReadByte();
                }
                else
                    return null;
                byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };   //reverse byte order since asn.1 key uses big endian order
                int modsize = System.BitConverter.ToInt32(modint, 0);

                byte firstbyte = binr.ReadByte();
                binr.BaseStream.Seek(-1, System.IO.SeekOrigin.Current);

                if (firstbyte == 0x00)
                {   //if first byte (highest order) of modulus is zero, don't include it
                    binr.ReadByte();    //skip this null byte
                    modsize -= 1;   //reduce modulus buffer size by 1
                }

                byte[] modulus = binr.ReadBytes(modsize); //read the modulus bytes

                if (binr.ReadByte() != 0x02)            //expect an Integer for the exponent data
                    return null;
                int expbytes = (int)binr.ReadByte();        // should only need one byte for actual exponent data (for all useful values)
                byte[] exponent = binr.ReadBytes(expbytes);


                showBytes("\nExponent", exponent);
                showBytes("\nModulus", modulus);

                // ------- create RSACryptoServiceProvider instance and initialize with public key -----
                System.Security.Cryptography.RSACryptoServiceProvider RSA = new System.Security.Cryptography.RSACryptoServiceProvider();
                System.Security.Cryptography.RSAParameters RSAKeyInfo = new System.Security.Cryptography.RSAParameters();
                RSAKeyInfo.Modulus = modulus;
                RSAKeyInfo.Exponent = exponent;
                RSA.ImportParameters(RSAKeyInfo);
                return RSA;
            }
            catch (System.Exception)
            {
                return null;
            }

            finally { binr.Close(); }

        }

        //------- Parses binary ans.1 RSA private key; returns RSACryptoServiceProvider  ---
        static System.Security.Cryptography.RSACryptoServiceProvider DecodeRSAPrivateKey(byte[] privkey)
        {
            byte[] MODULUS, E, D, P, Q, DP, DQ, IQ;

            // ---------  Set up stream to decode the asn.1 encoded RSA private key  ------
            System.IO.MemoryStream mem = new System.IO.MemoryStream(privkey);
            System.IO.BinaryReader binr = new System.IO.BinaryReader(mem);    //wrap Memory Stream with BinaryReader for easy reading
            byte bt = 0;
            ushort twobytes = 0;
            int elems = 0;
            try
            {
                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130) //data read as little endian order (actual data order for Sequence is 30 81)
                    binr.ReadByte();    //advance 1 byte
                else if (twobytes == 0x8230)
                    binr.ReadInt16();   //advance 2 bytes
                else
                    return null;

                twobytes = binr.ReadUInt16();
                if (twobytes != 0x0102) //version number
                    return null;
                bt = binr.ReadByte();
                if (bt != 0x00)
                    return null;


                //------  all private key components are Integer sequences ----
                elems = GetIntegerSize(binr);
                MODULUS = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                E = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                D = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                P = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                Q = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                DP = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                DQ = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                IQ = binr.ReadBytes(elems);

                System.Console.WriteLine("showing components ..");
                if (verbose)
                {
                    showBytes("\nModulus", MODULUS);
                    showBytes("\nExponent", E);
                    showBytes("\nD", D);
                    showBytes("\nP", P);
                    showBytes("\nQ", Q);
                    showBytes("\nDP", DP);
                    showBytes("\nDQ", DQ);
                    showBytes("\nIQ", IQ);
                }

                // ------- create RSACryptoServiceProvider instance and initialize with public key -----
                System.Security.Cryptography.RSACryptoServiceProvider RSA = new System.Security.Cryptography.RSACryptoServiceProvider();
                System.Security.Cryptography.RSAParameters RSAparams = new System.Security.Cryptography.RSAParameters();
                RSAparams.Modulus = MODULUS;
                RSAparams.Exponent = E;
                RSAparams.D = D;
                RSAparams.P = P;
                RSAparams.Q = Q;
                RSAparams.DP = DP;
                RSAparams.DQ = DQ;
                RSAparams.InverseQ = IQ;
                RSA.ImportParameters(RSAparams);
                return RSA;
            }
            catch (System.Exception)
            {
                return null;
            }
            finally { binr.Close(); }
        }

        private static int GetIntegerSize(System.IO.BinaryReader binr)
        {
            byte bt = 0;
            byte lowbyte = 0x00;
            byte highbyte = 0x00;
            int count = 0;
            bt = binr.ReadByte();
            if (bt != 0x02)     //expect integer
                return 0;
            bt = binr.ReadByte();

            if (bt == 0x81)
                count = binr.ReadByte();    // data size in next byte
            else
                if (bt == 0x82)
                {
                    highbyte = binr.ReadByte(); // data size in next 2 bytes
                    lowbyte = binr.ReadByte();
                    byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                    count = System.BitConverter.ToInt32(modint, 0);
                }
                else
                {
                    count = bt;     // we already have the data size
                }



            while (binr.ReadByte() == 0x00)
            {   //remove high order zeros in data
                count -= 1;
            }
            binr.BaseStream.Seek(-1, System.IO.SeekOrigin.Current);     //last ReadByte wasn't a removed zero, so back up a byte
            return count;
        }

        //-----  Get the binary RSA PRIVATE key, decrypting if necessary ----
        static byte[] DecodeOpenSSLPrivateKey(string instr)
        {
            const string pemprivheader = "-----BEGIN RSA PRIVATE KEY-----";
            const string pemprivfooter = "-----END RSA PRIVATE KEY-----";
            string pemstr = instr.Trim();
            byte[] binkey;
            if (!pemstr.StartsWith(pemprivheader) || !pemstr.EndsWith(pemprivfooter))
                return null;

            System.Text.StringBuilder sb = new System.Text.StringBuilder(pemstr);
            sb.Replace(pemprivheader, "");  //remove headers/footers, if present
            sb.Replace(pemprivfooter, "");

            string pvkstr = sb.ToString().Trim();   //get string after removing leading/trailing whitespace

            try
            {        // if there are no PEM encryption info lines, this is an UNencrypted PEM private key
                binkey = System.Convert.FromBase64String(pvkstr);
                return binkey;
            }
            catch (System.FormatException)
            {       //if can't b64 decode, it must be an encrypted private key
                //Console.WriteLine("Not an unencrypted OpenSSL PEM private key");  
            }

            System.IO.StringReader str = new System.IO.StringReader(pvkstr);

            //-------- read PEM encryption info. lines and extract salt -----
            if (!str.ReadLine().StartsWith("Proc-Type: 4,ENCRYPTED"))
                return null;
            string saltline = str.ReadLine();
            if (!saltline.StartsWith("DEK-Info: DES-EDE3-CBC,"))
                return null;
            string saltstr = saltline.Substring(saltline.IndexOf(",") + 1).Trim();
            byte[] salt = new byte[saltstr.Length / 2];
            for (int i = 0; i < salt.Length; i++)
                salt[i] = System.Convert.ToByte(saltstr.Substring(i * 2, 2), 16);
            if (!(str.ReadLine() == ""))
                return null;

            //------ remaining b64 data is encrypted RSA key ----
            string encryptedstr = str.ReadToEnd();

            try
            {   //should have b64 encrypted RSA key now
                binkey = System.Convert.FromBase64String(encryptedstr);
            }
            catch (System.FormatException)
            {  // bad b64 data.
                return null;
            }

            //------ Get the 3DES 24 byte key using PDK used by OpenSSL ----

            System.Security.SecureString despswd = GetSecPswd("Enter password to derive 3DES key==>");
            //Console.Write("\nEnter password to derive 3DES key: ");
            //String pswd = Console.ReadLine();
            byte[] deskey = GetOpenSSL3deskey(salt, despswd, 1, 2);    // count=1 (for OpenSSL implementation); 2 iterations to get at least 24 bytes
            if (deskey == null)
                return null;
            //showBytes("3DES key", deskey) ;

            //------ Decrypt the encrypted 3des-encrypted RSA private key ------
            byte[] rsakey = DecryptKey(binkey, deskey, salt); //OpenSSL uses salt value in PEM header also as 3DES IV
            if (rsakey != null)
                return rsakey;  //we have a decrypted RSA private key
            else
            {
                System.Console.WriteLine("Failed to decrypt RSA private key; probably wrong password.");
                return null;
            }
        }


        // ----- Decrypt the 3DES encrypted RSA private key ----------

        static byte[] DecryptKey(byte[] cipherData, byte[] desKey, byte[] IV)
        {
            System.IO.MemoryStream memst = new System.IO.MemoryStream();
            System.Security.Cryptography.TripleDES alg = System.Security.Cryptography.TripleDES.Create();
            alg.Key = desKey;
            alg.IV = IV;
            try
            {
                System.Security.Cryptography.CryptoStream cs = new System.Security.Cryptography.CryptoStream(memst, alg.CreateDecryptor(), System.Security.Cryptography.CryptoStreamMode.Write);
                cs.Write(cipherData, 0, cipherData.Length);
                cs.Close();
            }
            catch (System.Exception exc)
            {
                System.Console.WriteLine(exc.Message);
                return null;
            }
            byte[] decryptedData = memst.ToArray();
            return decryptedData;
        }

        //-----   OpenSSL PBKD uses only one hash cycle (count); miter is number of iterations required to build sufficient bytes ---
        static byte[] GetOpenSSL3deskey(byte[] salt, System.Security.SecureString secpswd, int count, int miter)
        {
            System.IntPtr unmanagedPswd = System.IntPtr.Zero;
            int HASHLENGTH = 16;    //MD5 bytes
            byte[] keymaterial = new byte[HASHLENGTH * miter];     //to store contatenated Mi hashed results


            byte[] psbytes = new byte[secpswd.Length];

            unmanagedPswd = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocAnsi(secpswd);
            System.Runtime.InteropServices.Marshal.Copy(unmanagedPswd, psbytes, 0, psbytes.Length);
            System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocAnsi(unmanagedPswd);

            //UTF8Encoding utf8 = new UTF8Encoding();
            //byte[] psbytes = utf8.GetBytes(pswd);

            // --- contatenate salt and pswd bytes into fixed data array ---
            byte[] data00 = new byte[psbytes.Length + salt.Length];
            System.Array.Copy(psbytes, data00, psbytes.Length);      //copy the pswd bytes
            System.Array.Copy(salt, 0, data00, psbytes.Length, salt.Length); //concatenate the salt bytes

            // ---- do multi-hashing and contatenate results  D1, D2 ...  into keymaterial bytes ----
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] result = null;
            byte[] hashtarget = new byte[HASHLENGTH + data00.Length];   //fixed length initial hashtarget

            for (int j = 0; j < miter; j++)
            {
                // ----  Now hash consecutively for count times ------
                if (j == 0)
                    result = data00;    //initialize 
                else
                {
                    System.Array.Copy(result, hashtarget, result.Length);
                    System.Array.Copy(data00, 0, hashtarget, result.Length, data00.Length);
                    result = hashtarget;
                    //Console.WriteLine("Updated new initial hash target:") ;
                    //showBytes(result) ;
                }

                for (int i = 0; i < count; i++)
                    result = md5.ComputeHash(result);

                System.Array.Copy(result, 0, keymaterial, j * HASHLENGTH, result.Length);  //contatenate to keymaterial
            }
            //showBytes("Final key material", keymaterial);
            byte[] deskey = new byte[24];
            System.Array.Copy(keymaterial, deskey, deskey.Length);

            System.Array.Clear(psbytes, 0, psbytes.Length);
            System.Array.Clear(data00, 0, data00.Length);
            System.Array.Clear(result, 0, result.Length);
            System.Array.Clear(hashtarget, 0, hashtarget.Length);
            System.Array.Clear(keymaterial, 0, keymaterial.Length);

            return deskey;
        }

        static System.Security.SecureString GetSecPswd(string prompt)
        {
            System.Security.SecureString password = new System.Security.SecureString();

            System.Console.ForegroundColor = System.ConsoleColor.Gray;
            System.Console.Write(prompt);
            System.Console.ForegroundColor = System.ConsoleColor.Magenta;

            while (true)
            {
                System.ConsoleKeyInfo cki = System.Console.ReadKey(true);
                if (cki.Key == System.ConsoleKey.Enter)
                {
                    System.Console.ForegroundColor = System.ConsoleColor.Gray;
                    System.Console.WriteLine();
                    return password;
                }
                else if (cki.Key == System.ConsoleKey.Backspace)
                {
                    // remove the last asterisk from the screen...
                    if (password.Length > 0)
                    {
                        System.Console.SetCursorPosition(System.Console.CursorLeft - 1, System.Console.CursorTop);
                        System.Console.Write(" ");
                        System.Console.SetCursorPosition(System.Console.CursorLeft - 1, System.Console.CursorTop);
                        password.RemoveAt(password.Length - 1);
                    }
                }
                else if (cki.Key == System.ConsoleKey.Escape)
                {
                    System.Console.ForegroundColor = System.ConsoleColor.Gray;
                    System.Console.WriteLine();
                    return password;
                }
                else if (System.Char.IsLetterOrDigit(cki.KeyChar) || System.Char.IsSymbol(cki.KeyChar))
                {
                    if (password.Length < 20)
                    {
                        password.AppendChar(cki.KeyChar);
                        System.Console.Write("*");
                    }
                    else
                    {
                        System.Console.Beep();
                    }
                }
                else
                {
                    System.Console.Beep();
                }
            }
        }

        static bool CompareBytearrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            int i = 0;
            foreach (byte c in a)
            {
                if (c != b[i])
                    return false;
                i++;
            }
            return true;
        }

        static void showBytes(string info, byte[] data)
        {
            System.Console.WriteLine("{0}  [{1} bytes]", info, data.Length);
            for (int i = 1; i <= data.Length; i++)
            {
                System.Console.Write("{0:X2}  ", data[i - 1]);
                if (i % 16 == 0)
                    System.Console.WriteLine();
            }
            System.Console.WriteLine("\n\n");
        }

    }

}
