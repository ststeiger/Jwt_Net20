
using System.Security.Cryptography;


// http://goboex.com/book-de.php?url=arbeitsplatz-der-zukunft-gestaltungsansatze-und-good-practice-beispiele&src=gdrive
namespace JWT.RSA 
{


    // http://stackoverflow.com/questions/11506891/how-to-load-the-rsa-public-key-from-file-in-c-sharp
    public class PEM    
    {



        /// <summary>
        /// Export a certificate to a PEM format string
        /// </summary>
        /// <param name="cert">The certificate to export</param>
        /// <returns>A PEM encoded string</returns>
        public static string ExportToPEM(System.Security.Cryptography.X509Certificates.X509Certificate cert)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();            

            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            builder.AppendLine(
                System.Convert.ToBase64String(
                    cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert)
                    ,System.Base64FormattingOptions.InsertLineBreaks)
            );
            builder.AppendLine("-----END CERTIFICATE-----");

            return builder.ToString();
        } // End Function ExportToPEM 

#if false
        // http://stackoverflow.com/questions/11244333/how-can-i-use-ecdsa-in-c-sharp-with-a-key-size-of-less-than-the-built-in-minimum
        public static void ExportEcdsaKey()
        {
            byte[] publicKey = null;
            byte[] privateKey = null;

            using (var dsa = new ECDsaCng(256))
            {
                dsa.HashAlgorithm = CngAlgorithm.Sha256;

                // publicKey = dsa.Key.Export(CngKeyBlobFormat.EccPublicBlob);
                // privateKey = dsa.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
            }

            CngKey k = CngKey.Create(CngAlgorithm.ECDsaP256, "myECDH", new CngKeyCreationParameters
            {
                ExportPolicy = CngExportPolicies.AllowPlaintextExport,
                KeyCreationOptions = CngKeyCreationOptions.MachineKey,
                KeyUsage = CngKeyUsages.AllUsages,
                Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                UIPolicy = new CngUIPolicy(CngUIProtectionLevels.None)
            });




            System.Console.WriteLine(publicKey);
            System.Console.WriteLine(privateKey);
        }


        public static System.Security.Cryptography.ECDsaCng CreateEcdProvider()
        {
            byte[] publicKey = new byte[] { 21, 5, 8, 12, 207 };

            return new ECDsaCng(CngKey.Import(publicKey, CngKeyBlobFormat.EccPublicBlob));
        } // End Function CreateEcdProvider 

#endif

        public static System.Security.Cryptography.RSACryptoServiceProvider CreateRsaProvider()
        {
            string publicPrivateKeyXML = "<RSAKeyValue><Modulus>jwC1EyNgHkh3Q3J3ITmh6EkbsTSKJuuCYsg9UsaYA9+Trwlp4v37VVc3b2jTsUHaEcG1IYGQbQBu/IIxiDlmDFiQPpN8UrhLz0ZQ9SzWRONSzRC97DR08epl4JtO86uAWYR9+iEnxIaeiRG6i32sjZYaiqwBuvNzli94Wtz7yQDNlH6FmdkMp0n9Hg8MslRXbRbINXhW/nJ4zOggmRLQfOzc2ZxyARgmXvpmxPxaaawtBj919VHFishyU0u7CbzpQ3J5dldV1d+FTICZN5AveF4qM4tzWMBZdCubdcKiMnIGNgBO/mUb/SbWwlu4OuXG8vOr5aqYIuaWUKBFEyK3pQ==</Modulus><Exponent>AQAB</Exponent><P>uSoRvVLPX8EdlsYbCcVTgjqpP0e/UsXeBRXTMjxaHLDYgvgSUADhAd5ECscsNdb5sLUy18xSd9pVLFRMr7lqfstiY1tJDyLrP+54TON4KUhFkV1ntf5R1bGzvxlY3po3vUz+2a8fLq4F9ennaT6/NKyn4d7WH+qmr6oSbrgGLgU=</P><Q>xbWTfXb9UIGLN/j/V+TVukMSP8GmSXj+tKnrr5XjdwB4yRyf6krw4ntIU/wnS6in5TRy5R3Wdy8ixGCB9zghQnjShhCKprD2nx4f74BzpL62Y6qI+lsH17AZAov0Olpe3MPGFo+F6UXqlRQPEAfh2Wxv6hiCcfucP7gEy5Tc9SE=</Q><DP>JneN7eX5POxSqFMJpPMAkUp8hK/0GE8Q+793+7S8B7/ZiwPcUhCMriWtvwt3rMu3XbWXFWvWKh4KmcX9lHgRnrvD+d4qBGH9u29gQKD1AqaIBVYBSLbH63waWnX6l2w0bjhDrZeLA9iVVmw8bgniESBZVDxGAaVu8YmEgMnsRr0=</DP><DQ>etVV/hRIS5VAdpUHp4bv1ppHIz9f3bQDoyES4fMg8FVltaVIIVtQD5YCmNNHYrU1Iq0UWQ7RqRiq5BEFjh/cYh0IxuxOCERX5QHlW3qV3pvyWzefhNO7qqCo2TE0mnB9EXG8h1XCH+0lUlu1BAOxqNC7M1jo6oIlUF029XjWUqE=</DQ><InverseQ>aZwOBDDsp0tZSs2p418syfQwxyWUJ/kdu4D08x+LtI0Fd31LzHbD1Ogs6aBlAFf8wPFE4mNsHDrkjujLKoEmnwt8SEMSIXKz2EuDG4E7wKgTT3w0Dy3Vydo4Zh6kGJF+bQ2DP3LhwoHZBt06CPeFvsUBOnM/nKn9ICJeKHwyfkQ=</InverseQ><D>Cgxtgj9ESUx5SPLZqSrbaYRS9FRHTZh99y1alcmhCUtOyDLGpIM0A9lW6ra4gruoXwK4FMx9wWhm5B/NQDpxpSDHXgZPavaf/nF9Tdp34gEBTVSbATvnEyVlQZpYOr93Nj3Hpmm/BCHGMRve+l9QiTseJAFrNl+rZHHIfhtfe/kTZu+Klhelmb1JEgtqRe7Ve91JGoDka4L9GP0oIqD9nnxmI1gmpaUeuDKfpbzfeoQCU1RVUDyZ5MirRbTvUmCIjs3Hed2pmDTigkTJ2mYHd4gESXXyCYVa8Qgw5mOOoHCd5viG3hLYQGzMIoGkzKr+5vF63kxtkMRFKNIra3vbQQ==</D></RSAKeyValue>";
            // string publicOnlyKeyXML = "<RSAKeyValue><Modulus>jwC1EyNgHkh3Q3J3ITmh6EkbsTSKJuuCYsg9UsaYA9+Trwlp4v37VVc3b2jTsUHaEcG1IYGQbQBu/IIxiDlmDFiQPpN8UrhLz0ZQ9SzWRONSzRC97DR08epl4JtO86uAWYR9+iEnxIaeiRG6i32sjZYaiqwBuvNzli94Wtz7yQDNlH6FmdkMp0n9Hg8MslRXbRbINXhW/nJ4zOggmRLQfOzc2ZxyARgmXvpmxPxaaawtBj919VHFishyU0u7CbzpQ3J5dldV1d+FTICZN5AveF4qM4tzWMBZdCubdcKiMnIGNgBO/mUb/SbWwlu4OuXG8vOr5aqYIuaWUKBFEyK3pQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

            System.Security.Cryptography.RSACryptoServiceProvider rSACryptoServiceProvider = 
                new System.Security.Cryptography.RSACryptoServiceProvider(
                    new System.Security.Cryptography.CspParameters
                    {
                        Flags = System.Security.Cryptography.CspProviderFlags.UseMachineKeyStore
                    }
                );

            rSACryptoServiceProvider.FromXmlString(publicPrivateKeyXML);
            return rSACryptoServiceProvider;
        } // End Function CreateRsaProvider 


        public static string ExportPrivateKey(System.Security.Cryptography.RSACryptoServiceProvider csp)
        {
            string retVal = null;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            using (System.IO.StringWriter sw = new System.IO.StringWriter(sb))
            {
                ExportPrivateKey(csp, sw);
                retVal = sb.ToString();
                sb.Length = 0;
                sb = null;
            }

            return retVal;
        }


        public static string ExportPublicKey(System.Security.Cryptography.RSACryptoServiceProvider csp)
        {
            string retVal = null;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            using (System.IO.StringWriter sw = new System.IO.StringWriter(sb))
            {
                ExportPublicKey(csp, sw);
                retVal = sb.ToString();
                sb.Length = 0;
                sb = null;
            }

            return retVal;
        }



        // http://stackoverflow.com/questions/23734792/c-sharp-export-private-public-rsa-key-from-rsacryptoserviceprovider-to-pem-strin
        public static void ExportPrivateKey(RSACryptoServiceProvider csp, System.IO.TextWriter outputStream)
        {
            if (csp.PublicOnly) 
                throw new System.ArgumentException("CSP does not contain a private key", "csp");

            RSAParameters parameters = csp.ExportParameters(true);
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream))
                {
                    writer.Write((byte)0x30); // SEQUENCE
                    using (System.IO.MemoryStream innerStream = new System.IO.MemoryStream())
                    {

                        using (System.IO.BinaryWriter innerWriter = new System.IO.BinaryWriter(innerStream))
                        {
                            EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 }); // Version
                            EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
                            EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
                            EncodeIntegerBigEndian(innerWriter, parameters.D);
                            EncodeIntegerBigEndian(innerWriter, parameters.P);
                            EncodeIntegerBigEndian(innerWriter, parameters.Q);
                            EncodeIntegerBigEndian(innerWriter, parameters.DP);
                            EncodeIntegerBigEndian(innerWriter, parameters.DQ);
                            EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
                            int length = (int)innerStream.Length;
                            EncodeLength(writer, length);
                            writer.Write(innerStream.GetBuffer(), 0, length);
                        } // End Using innerWriter 

                    } // End Using innerStream 


                    char[] base64 = System.Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                    // From here on, stream is no longer needed 


                    outputStream.WriteLine("-----BEGIN RSA PRIVATE KEY-----");
                    // Output as Base64 with lines chopped at 64 characters
                    for (int i = 0; i < base64.Length; i += 64)
                    {
                        outputStream.WriteLine(base64, i, System.Math.Min(64, base64.Length - i));
                    } // Next i 
                    outputStream.WriteLine("-----END RSA PRIVATE KEY-----");

                } // End Using writer 

            } // End Using stream 

        } // End Sub ExportPrivateKey


        // http://stackoverflow.com/questions/28406888/c-sharp-rsa-public-key-output-not-correct/28407693#28407693
        private static void ExportPublicKey(RSACryptoServiceProvider csp, System.IO.TextWriter outputStream)
        {
            RSAParameters parameters = csp.ExportParameters(false);
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream))
                {
                    writer.Write((byte)0x30); // SEQUENCE
                    using (System.IO.MemoryStream innerStream = new System.IO.MemoryStream())
                    {
                        System.IO.BinaryWriter innerWriter = new System.IO.BinaryWriter(innerStream);
                        innerWriter.Write((byte)0x30); // SEQUENCE
                        EncodeLength(innerWriter, 13);
                        innerWriter.Write((byte)0x06); // OBJECT IDENTIFIER
                        byte[] rsaEncryptionOid = new byte[] { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01 };
                        EncodeLength(innerWriter, rsaEncryptionOid.Length);
                        innerWriter.Write(rsaEncryptionOid);
                        innerWriter.Write((byte)0x05); // NULL
                        EncodeLength(innerWriter, 0);
                        innerWriter.Write((byte)0x03); // BIT STRING
                        using (System.IO.MemoryStream bitStringStream = new System.IO.MemoryStream())
                        {
                            using (System.IO.BinaryWriter bitStringWriter = new System.IO.BinaryWriter(bitStringStream))
                            {
                                bitStringWriter.Write((byte)0x00); // # of unused bits
                                bitStringWriter.Write((byte)0x30); // SEQUENCE
                                using (System.IO.MemoryStream paramsStream = new System.IO.MemoryStream())
                                {

                                    using (System.IO.BinaryWriter paramsWriter = new System.IO.BinaryWriter(paramsStream))
                                    {
                                        EncodeIntegerBigEndian(paramsWriter, parameters.Modulus); // Modulus
                                        EncodeIntegerBigEndian(paramsWriter, parameters.Exponent); // Exponent
                                        int paramsLength = (int)paramsStream.Length;
                                        EncodeLength(bitStringWriter, paramsLength);
                                        bitStringWriter.Write(paramsStream.GetBuffer(), 0, paramsLength);
                                    } // End Using paramsWriter 

                                } // End Using paramsStream 

                                int bitStringLength = (int)bitStringStream.Length;
                                EncodeLength(innerWriter, bitStringLength);
                                innerWriter.Write(bitStringStream.GetBuffer(), 0, bitStringLength);
                            } // End Using bitStringWriter 

                        } // End Using bitStringStream 

                        int length = (int)innerStream.Length;
                        EncodeLength(writer, length);
                        writer.Write(innerStream.GetBuffer(), 0, length);
                    } // End Using innerStream 

                    char[] base64 = System.Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                    // From here on, stream is no longer needed 

                    outputStream.WriteLine("-----BEGIN PUBLIC KEY-----");
                    for (int i = 0; i < base64.Length; i += 64)
                    {
                        outputStream.WriteLine(base64, i, System.Math.Min(64, base64.Length - i));
                    } // Next i 
                    outputStream.WriteLine("-----END PUBLIC KEY-----");

                } // End Using writer

            } // End Using stream

        } // End Sub ExportPublicKey 


        private static void EncodeLength(System.IO.BinaryWriter stream, int length)
        {
            if (length < 0) 
                throw new System.ArgumentOutOfRangeException("length", "Length must be non-negative");

            if (length < 0x80)
            {
                // Short form
                stream.Write((byte)length);
            }
            else
            {
                // Long form
                var temp = length;
                var bytesRequired = 0;
                while (temp > 0)
                {
                    temp >>= 8;
                    bytesRequired++;
                }
                stream.Write((byte)(bytesRequired | 0x80));
                for (var i = bytesRequired - 1; i >= 0; i--)
                {
                    stream.Write((byte)(length >> (8 * i) & 0xff));
                }
            }

        } // End Sub EncodeLength 


        private static void EncodeIntegerBigEndian(System.IO.BinaryWriter stream, byte[] value)
        {
            EncodeIntegerBigEndian(stream, value, true);
        } // End Sub EncodeIntegerBigEndian 


        private static void EncodeIntegerBigEndian(System.IO.BinaryWriter stream, byte[] value, bool forceUnsigned)
        {
            stream.Write((byte)0x02); // INTEGER
            var prefixZeros = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != 0) break;
                prefixZeros++;
            }
            if (value.Length - prefixZeros == 0)
            {
                EncodeLength(stream, 1);
                stream.Write((byte)0);
            }
            else
            {
                if (forceUnsigned && value[prefixZeros] > 0x7f)
                {
                    // Add a prefix zero to force unsigned if the MSB is 1
                    EncodeLength(stream, value.Length - prefixZeros + 1);
                    stream.Write((byte)0);
                }
                else
                {
                    EncodeLength(stream, value.Length - prefixZeros);
                }
                for (var i = prefixZeros; i < value.Length; i++)
                {
                    stream.Write(value[i]);
                }
            }
        } // End Sub EncodeIntegerBigEndian 


    } // End Class PEM 


} // End Namespace JWT.RSA.KeyManagement 
