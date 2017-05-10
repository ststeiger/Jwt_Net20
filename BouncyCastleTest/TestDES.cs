
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



// using Org.BouncyCastle.Crypto;
// using Org.BouncyCastle.Crypto.Engines;


// using Org.BouncyCastle.Crypto.Parameters;
// using Org.BouncyCastle.Crypto.Modes;
// using Org.BouncyCastle.Crypto.Paddings;


namespace BouncyCastleTest
{

    
    class TestDES
    {

        
        // https://markgamache.blogspot.ch/2013/01/ntlm-challenge-response-is-100-broken.html
        // https://asecuritysite.com/encryption/lmhash

        // http://www.programcreek.com/java-api-examples/index.php?api=org.bouncycastle.crypto.engines.DESEngine
        // http://bouncy-castle.1462172.n4.nabble.com/Sombody-tried-to-integrate-C-and-Java-with-BC-td1463998.html
        // http://www.java2s.com/Code/Java/Security/BasicsymmetricencryptionexamplewithpaddingandECBusingDES.htm
        // https://examples.javacodegeeks.com/core-java/security/des-with-ecb-example/
        // https://bouncycastle.org/specifications.html
        public static void Test()
        {
            byte[] nonce = System.Text.Encoding.UTF8.GetBytes("Hello world");
            string pw = "peanutbutter";
            // pw = pw.Substring(0, 8);
            
            // 8: 8 bytes = 64 Bit (NTLM)
            CalculateLM(nonce, pw);
            CalculateLMWithBouncy(nonce, pw);

            byte[] key = System.Text.Encoding.UTF8.GetBytes(pw);
            key = PasswordToKey(pw, 0);

            string inputString = "foobar";
            byte[] input = System.Text.Encoding.UTF8.GetBytes(inputString);
            byte[] magic = { 0x4B, 0x47, 0x53, 0x21, 0x40, 0x23, 0x24, 0x25 };
            input = magic;


            /*
             * This will use a supplied key, and encrypt the data
             * This is the equivalent of DES/CBC/PKCS5Padding
             */


            Org.BouncyCastle.Crypto.Engines.DesEngine engine = new Org.BouncyCastle.Crypto.Engines.DesEngine();

            // Org.BouncyCastle.Security.CipherUtilities.GetCipher("DES/ECB/PKCS7Padding"); // Wrong - not ECB...
            // BufferedBlockCipher cipher = new PaddedBlockCipher(new CBCCipher(engine)); // Doesn't compile
            // BufferedBlockCipher cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(engine), new ZeroBytePadding() ); // Wrong - not ECB...
            // BufferedBlockCipher cipher = cipher = new BufferedBlockCipher(engine);
            Org.BouncyCastle.Crypto.BufferedBlockCipher cipher = cipher = 
                new Org.BouncyCastle.Crypto.BufferedBlockCipher(
                    new Org.BouncyCastle.Crypto.Engines.DesEngine()
            );

            
            cipher.Init(true, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(key));

            // lmBuffer = cyphertext
            // byte[] lmBuffer = new byte[cipher.GetOutputSize(input.Length)];
            byte[] lmBuffer = new byte[21];

            int outputLen = cipher.ProcessBytes(input, 0, input.Length, lmBuffer, 0);
            
            try
            {
                cipher.DoFinal(lmBuffer, outputLen);



                
                key = PasswordToKey(pw, 7);
                cipher.Init(true, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(key));

                // des.CreateEncryptor().TransformBlock(magic, 0, 8, lmBuffer, 8);
                outputLen = cipher.ProcessBytes(input, 0, 8, lmBuffer, 8);
                cipher.DoFinal(lmBuffer, outputLen);
                
                


                
                System.Console.WriteLine(lmBuffer);



                // Here must be: array[20]
                //168
                //19
                //199
                //248
                //123
                //109
                //80
                //19
                // [0] ...
            }
            catch (Org.BouncyCastle.Crypto.CryptoException ce)
            {
                System.Console.Error.WriteLine(ce.Message);
                System.Environment.Exit(1);
            }
        }


        public static string ByteArrayToString(byte[] ba)
        {
            string s = "";
            foreach (byte b in ba)
            {
                s += b.ToString() + ",";
            }

            return s;
        }


        private static byte[] calc_respWithBouncy(byte[] nonce, byte[] data)
        {
            /*
             * takes a 21 byte array and treats it as 3 56-bit DES keys. The
             * 8 byte nonce is encrypted with each key and the resulting 24
             * bytes are stored in the results array.
            */

            byte[] response = new byte[24];

            Org.BouncyCastle.Crypto.BufferedBlockCipher des = 
                new Org.BouncyCastle.Crypto.BufferedBlockCipher(
                    new Org.BouncyCastle.Crypto.Engines.DesEngine()
            );

            byte[] key = null;
            
            key = setup_des_key(data, 0);
            des.Init(true, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(key));
            int outputLen = des.ProcessBytes(nonce, 0, 8, response, 0);
            des.DoFinal(response, outputLen);

            key = setup_des_key(data, 7);
            des.Init(true, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(key));
            outputLen = des.ProcessBytes(nonce, 0, 8, response, 8);
            des.DoFinal(response, outputLen);

            key = setup_des_key(data, 14);
            des.Init(true, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(key));
            outputLen = des.ProcessBytes(nonce, 0, 8, response, 16);
            des.DoFinal(response, outputLen);

            string s = ByteArrayToString(response);
            System.Console.WriteLine(s);

            return response;
        }


        /// <summary>
        /// Calculates NTLM NT response.
        /// </summary>
        /// <param name="nonce">Server nonce.</param>
        /// <param name="password">Password.</param>
        /// <returns>Returns NTLM NT response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>nonce</b> or <b>password</b> is null reference.</exception>
        public static byte[] CalculateLMWithBouncy(byte[] nonce, string password)
        {
            if (nonce == null)
            {
                throw new ArgumentNullException("nonce");
            }
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            byte[] lmBuffer = new byte[21];
            byte[] magic = { 0x4B, 0x47, 0x53, 0x21, 0x40, 0x23, 0x24, 0x25 };
            byte[] nullEncMagic = { 0xAA, 0xD3, 0xB4, 0x35, 0xB5, 0x14, 0x04, 0xEE };

            // create Lan Manager password 
            Org.BouncyCastle.Crypto.BufferedBlockCipher des = 
                new Org.BouncyCastle.Crypto.BufferedBlockCipher(
                    new Org.BouncyCastle.Crypto.Engines.DesEngine()
            );
            byte[] key = null;


            // Note: In .NET DES cannot accept a weak key 
            // this can happen for a null password 
            if (password.Length < 1)
            {
                Buffer.BlockCopy(nullEncMagic, 0, lmBuffer, 0, 8);
            }
            else
            {
                key = PasswordToKey(password, 0);
                des.Init(true, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(key));

                int outputLen = des.ProcessBytes(magic, 0, 8, lmBuffer, 0);
                des.DoFinal(lmBuffer, outputLen);
            }

            // and if a password has less than 8 characters 
            if (password.Length < 8)
            {
                Buffer.BlockCopy(nullEncMagic, 0, lmBuffer, 8, 8);
            }
            else
            {
                key = PasswordToKey(password, 7);
                des.Init(true, new Org.BouncyCastle.Crypto.Parameters.KeyParameter(key));

                int outputLen = des.ProcessBytes(magic, 0, 8, lmBuffer, 8);
                des.DoFinal(lmBuffer, outputLen);
            }


            string s = ByteArrayToString(lmBuffer);
            System.Console.WriteLine(s);

            // Orig:   168,19,199,248,123,109,80,19,178,195,174,214,245,208,171,89,0,0,0,0,0,
            // Bouncy: 168,19,199,248,123,109,80,19,178,195,174,214,245,208,171,89,0,0,0,0,0,

            return calc_respWithBouncy(nonce, lmBuffer);
        }




        private static byte[] setup_des_key(byte[] key56bits, int position)
        {
            byte[] key = new byte[8];
            key[0] = key56bits[position];
            key[1] = (byte)((key56bits[position] << 7) | (key56bits[position + 1] >> 1));
            key[2] = (byte)((key56bits[position + 1] << 6) | (key56bits[position + 2] >> 2));
            key[3] = (byte)((key56bits[position + 2] << 5) | (key56bits[position + 3] >> 3));
            key[4] = (byte)((key56bits[position + 3] << 4) | (key56bits[position + 4] >> 4));
            key[5] = (byte)((key56bits[position + 4] << 3) | (key56bits[position + 5] >> 5));
            key[6] = (byte)((key56bits[position + 5] << 2) | (key56bits[position + 6] >> 6));
            key[7] = (byte)(key56bits[position + 6] << 1);

            return key;
        }


        // https://github.com/dubeaud/bugnet/blob/master/src/LumiSoft.Net/AUTH/AUTH_SASL_Client_Ntlm.cs
        private static byte[] PasswordToKey(string password, int position)
        {
            byte[] key7 = new byte[7];
            int len = System.Math.Min(password.Length - position, 7);
            Encoding.ASCII.GetBytes(password.ToUpper(System.Globalization.CultureInfo.CurrentCulture), position, len, key7, 0);
            byte[] key8 = setup_des_key(key7, 0);

            return key8;
        }


        private static byte[] calc_resp(byte[] nonce, byte[] data)
        {
            /*
             * takes a 21 byte array and treats it as 3 56-bit DES keys. The
             * 8 byte nonce is encrypted with each key and the resulting 24
             * bytes are stored in the results array.
            */

            byte[] response = new byte[24];
            System.Security.Cryptography.DES des = System.Security.Cryptography.DES.Create();
            des.Mode = System.Security.Cryptography.CipherMode.ECB;

            des.Key = setup_des_key(data, 0);
            System.Security.Cryptography.ICryptoTransform ct = des.CreateEncryptor();
            ct.TransformBlock(nonce, 0, 8, response, 0);

            des.Key = setup_des_key(data, 7);
            ct = des.CreateEncryptor();
            ct.TransformBlock(nonce, 0, 8, response, 8);

            des.Key = setup_des_key(data, 14);
            ct = des.CreateEncryptor();
            ct.TransformBlock(nonce, 0, 8, response, 16);

            string s = ByteArrayToString(response);
            System.Console.WriteLine(s);

            return response;
        }


        /// <summary>
        /// Calculates NTLM NT response.
        /// </summary>
        /// <param name="nonce">Server nonce.</param>
        /// <param name="password">Password.</param>
        /// <returns>Returns NTLM NT response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>nonce</b> or <b>password</b> is null reference.</exception>
        public static byte[] CalculateLM(byte[] nonce, string password)
        {
            if (nonce == null)
            {
                throw new ArgumentNullException("nonce");
            }
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            byte[] lmBuffer = new byte[21];
            byte[] magic = { 0x4B, 0x47, 0x53, 0x21, 0x40, 0x23, 0x24, 0x25 };
            byte[] nullEncMagic = { 0xAA, 0xD3, 0xB4, 0x35, 0xB5, 0x14, 0x04, 0xEE };

            // create Lan Manager password 
            System.Security.Cryptography.DES des = System.Security.Cryptography.DES.Create();
            des.Mode = System.Security.Cryptography.CipherMode.ECB;

            // Note: In .NET DES cannot accept a weak key 
            // this can happen for a null password 
            if (password.Length < 1)
            {
                Buffer.BlockCopy(nullEncMagic, 0, lmBuffer, 0, 8);
            }
            else
            {
                des.Key = PasswordToKey(password, 0);
                des.CreateEncryptor().TransformBlock(magic, 0, 8, lmBuffer, 0);
            }

            // and if a password has less than 8 characters 
            if (password.Length < 8)
            {
                Buffer.BlockCopy(nullEncMagic, 0, lmBuffer, 8, 8);
            }
            else
            {
                des.Key = PasswordToKey(password, 7);
                des.CreateEncryptor().TransformBlock(magic, 0, 8, lmBuffer, 8);
            }

            string s = ByteArrayToString(lmBuffer);
            System.Console.WriteLine(s);

            return calc_resp(nonce, lmBuffer);
        }


    }


}
