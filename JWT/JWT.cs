
using System.Security.Cryptography;

// See MS code @
// https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/tree/master/src/System.IdentityModel.Tokens.Jwt
// https://stackoverflow.com/questions/10055158/is-there-a-json-web-token-jwt-example-in-c
// https://auth0.com/docs/tutorials/generate-jwt-dotnet


namespace JWT
{


    /// <summary>
    /// Provides methods for encoding and decoding JSON Web Tokens.
    /// </summary>
    public static class JsonWebToken
    {
        private delegate byte[] GenericHashFunction_t(byte[] arg1, byte[] arg2);


        /// <summary>
        /// Pluggable JSON Serializer
        /// </summary>
        public static IJsonSerializer JsonSerializer;

        private static readonly System.DateTime UnixEpoch;

        private static readonly System.Collections.Generic.IDictionary<JwtHashAlgorithm, GenericHashFunction_t> HashAlgorithms;


        // http://crypto.stackexchange.com/questions/5646/what-are-the-differences-between-a-digital-signature-a-mac-and-a-hash

        // Integrity:        Can the recipient be confident that the message has not been accidentally modified?
        // Authentication:   Can the recipient be confident that the message originates from the sender?
        // Non-repudiation:  If the recipient passes the message and the proof to a third party, 
        //                   can the third party be confident that the message originated from the sender? 

        // Cryptographic primitive | Hash |    MAC    | Digital
        // Security Goal           |      |           | signature
        // ------------------------+------+-----------+-------------
        // Integrity               |  Yes |    Yes    |   Yes
        // Authentication          |  No  |    Yes    |   Yes
        // Non-repudiation         |  No  |    No     |   Yes
        // ------------------------+------+-----------+-------------
        // Kind of keys            | none | symmetric | asymmetric
        //                         |      |    keys   |    keys

        static JsonWebToken()
        {
            JsonSerializer = new DefaultJsonSerializer();
            UnixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);


            // https://stackoverflow.com/questions/10055158/is-there-a-json-web-token-jwt-example-in-c 
            // https://stackoverflow.com/questions/34403823/verifying-jwt-signed-with-the-rs256-algorithm-using-public-key-in-c-sharp
            // http://codingstill.com/2016/01/verify-jwt-token-signed-with-rs256-using-the-public-key/
            HashAlgorithms = new System.Collections.Generic.Dictionary<JwtHashAlgorithm, GenericHashFunction_t>
            {
                { JwtHashAlgorithm.None,  (key, value) => { using (HMACSHA512 sha = new HMACSHA512(key)) { throw new TokenAlgorithmRefusedException(); } } },
                { JwtHashAlgorithm.HS256, (key, value) => { using (HMACSHA256 sha = new HMACSHA256(key)) { return sha.ComputeHash(value); } } },
                { JwtHashAlgorithm.HS384, (key, value) => { using (HMACSHA384 sha = new HMACSHA384(key)) { return sha.ComputeHash(value); } } },
                { JwtHashAlgorithm.HS512, (key, value) => { using (HMACSHA512 sha = new HMACSHA512(key)) { return sha.ComputeHash(value); } } },
                { JwtHashAlgorithm.RS256, (key, value) => 
                    { 
                        using (SHA256 sha = SHA256.Create()) 
                        {
                            // https://github.com/mono/mono/blob/master/mcs/class/referencesource/mscorlib/system/security/cryptography/asymmetricsignatureformatter.cs
                            // https://github.com/mono/mono/blob/master/mcs/class/corlib/System.Security.Cryptography/RSAPKCS1SignatureFormatter.cs
                            // https://github.com/mono/mono/blob/master/mcs/class/Mono.Security/Mono.Security.Cryptography/PKCS1.cs
                            using (RSACryptoServiceProvider rsa = JWT.RSA.PEM.CreateRsaProvider())
                            {
                                // System.Security.Cryptography.RSAPKCS1SignatureFormatter
                                RSAPKCS1SignatureFormatter RSAFormatter = new RSAPKCS1SignatureFormatter(rsa);
                                RSAFormatter.SetHashAlgorithm("SHA256");

                                //Create a signature for HashValue and return it.
                                return RSAFormatter.CreateSignature(sha.ComputeHash(value));
                            }
                        } 
                    } 
                }

                ,
                 { JwtHashAlgorithm.RS384, (key, value) => { 
                     using (SHA384 sha = System.Security.Cryptography.SHA384.Create()) 
                     {
                         using (RSACryptoServiceProvider rsa = JWT.RSA.PEM.CreateRsaProvider())
                            {
                                RSAPKCS1SignatureFormatter RSAFormatter = new RSAPKCS1SignatureFormatter(rsa);
                                RSAFormatter.SetHashAlgorithm("SHA384");
                                return RSAFormatter.CreateSignature(sha.ComputeHash(value));
                            }
                        } 
                    } 
                }
                ,
                 { JwtHashAlgorithm.RS512, (key, value) => { 
                     using (SHA512 sha = System.Security.Cryptography.SHA512.Create()) 
                     {
                         using (RSACryptoServiceProvider rsa = JWT.RSA.PEM.CreateRsaProvider())
                            {
                                RSAPKCS1SignatureFormatter RSAFormatter = new RSAPKCS1SignatureFormatter(rsa);
                                RSAFormatter.SetHashAlgorithm("SHA512");
                                return RSAFormatter.CreateSignature(sha.ComputeHash(value));
                            }
                        } 
                    } 
                }
#if false 
                // https://github.com/mono/mono/tree/master/mcs/class/referencesource/System.Core/System/Security/Cryptography
                // https://github.com/mono/mono/blob/master/mcs/class/referencesource/System.Core/System/Security/Cryptography/ECDsaCng.cs
                // https://github.com/mono/mono/blob/master/mcs/class/referencesource/System.Core/System/Security/Cryptography/ECDsa.cs
                // ECDsaCng => next generation cryptography
                // Is just a wrapper around ncrypt, plus some constructors throw on mono/netstandard... in short - horrible thing
                 ,
                 { JwtHashAlgorithm.ES256, (key, value) => { 
                        // using (ECDsaCng ecd = new System.Security.Cryptography.ECDsaCng(256))
                        using (ECDsaCng ecd = JWT.RSA.PEM.CreateEcdProvider()) 
                        {
                            ecd.HashAlgorithm = CngAlgorithm.Sha256;
                            byte[] publickey = ecd.Key.Export(CngKeyBlobFormat.EccPublicBlob);
                            return ecd.SignData(value);
                        }
                     }
                 }
                 ,
                 { JwtHashAlgorithm.ES384, (key, value) => { 
                        // using (ECDsaCng ecd = new System.Security.Cryptography.ECDsaCng(384))
                        using (ECDsaCng ecd = JWT.RSA.PEM.CreateEcdProvider()) 
                        {
                            ecd.HashAlgorithm = CngAlgorithm.Sha384;
                            return ecd.SignData(value);
                        }
                     }
                 }
                 ,
                 { JwtHashAlgorithm.ES512, (key, value) => { 
                        // using (ECDsaCng ecd = new System.Security.Cryptography.ECDsaCng(512))
                        using (ECDsaCng ecd = JWT.RSA.PEM.CreateEcdProvider()) 
                        {
                            ecd.HashAlgorithm = CngAlgorithm.Sha512;
                            return ecd.SignData(value);
                        }
                     }
                 }
#endif

            };

        } // End Constructor 


        /// <summary>
        /// Creates a JWT given a payload, the signing key, and the algorithm to use.
        /// </summary>
        /// <param name="payload">An arbitrary payload (must be serializable to JSON via <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).</param>
        /// <param name="key">The key used to sign the token.</param>
        /// <param name="algorithm">The hash algorithm to use.</param>
        /// <returns>The generated JWT.</returns>
        public static string Encode(object payload, string key, JwtHashAlgorithm algorithm)
        {
            return Encode(new System.Collections.Generic.Dictionary<string, object>(), payload, System.Text.Encoding.UTF8.GetBytes(key), algorithm);
        } // End Function Encode


        /// <summary>
        /// Creates a JWT given a payload, the signing key, and the algorithm to use.
        /// </summary>
        /// <param name="payload">An arbitrary payload (must be serializable to JSON via <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).</param>
        /// <param name="key">The key used to sign the token.</param>
        /// <param name="algorithm">The hash algorithm to use.</param>
        /// <returns>The generated JWT.</returns>
        public static string Encode(object payload, byte[] key, JwtHashAlgorithm algorithm)
        {
            return Encode(new System.Collections.Generic.Dictionary<string, object>(), payload, key, algorithm);
        } // End Function Encode


        /// <summary>
        /// Creates a JWT given a set of arbitrary extra headers, a payload, the signing key, and the algorithm to use.
        /// </summary>
        /// <param name="extraHeaders">An arbitrary set of extra headers. Will be augmented with the standard "typ" and "alg" headers.</param>
        /// <param name="payload">An arbitrary payload (must be serializable to JSON via <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).</param>
        /// <param name="key">The key bytes used to sign the token.</param>
        /// <param name="algorithm">The hash algorithm to use.</param>
        /// <returns>The generated JWT.</returns>
        public static string Encode(System.Collections.Generic.IDictionary<string, object> extraHeaders, object payload, string key, JwtHashAlgorithm algorithm)
        {
            return Encode(extraHeaders, payload, System.Text.Encoding.UTF8.GetBytes(key), algorithm);
        } // End Function Encode
        

        /// <summary>
        /// Creates a JWT given a header, a payload, the signing key, and the algorithm to use.
        /// </summary>
        /// <param name="extraHeaders">An arbitrary set of extra headers. Will be augmented with the standard "typ" and "alg" headers.</param>
        /// <param name="payload">An arbitrary payload (must be serializable to JSON via <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).</param>
        /// <param name="key">The key bytes used to sign the token.</param>
        /// <param name="algorithm">The hash algorithm to use.</param>
        /// <returns>The generated JWT.</returns>
        public static string Encode(System.Collections.Generic.IDictionary<string, object> extraHeaders, object payload, byte[] key, JwtHashAlgorithm algorithm)
        {
            string retVal = null;

            System.Collections.Generic.Dictionary<string, object> header = new System.Collections.Generic.Dictionary<string, object>(extraHeaders)
            {
                { "typ", "JWT" },
                { "alg", algorithm.ToString() }
            };

            byte[] headerBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header));
            byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(Base64UrlEncode(headerBytes));
            sb.Append(".");
            sb.Append(Base64UrlEncode(payloadBytes));

            byte[] bytesToSign = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            byte[] signature = HashAlgorithms[algorithm](key, bytesToSign);
            sb.Append(".");
            sb.Append(Base64UrlEncode(signature));

            retVal = sb.ToString();
            sb.Length = 0;
            sb = null;
            return retVal;
        } // End Function Encode


        /// <summary>
        /// Given a JWT, decode it and return the JSON payload.
        /// </summary>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key that was used to sign the JWT.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <returns>A string containing the JSON payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        /// <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        public static string Decode(string token, string key, bool verify = true)
        {
            return Decode(token, System.Text.Encoding.UTF8.GetBytes(key), verify);
        } // End Function Decode


        /// <summary>
        /// Given a JWT, decode it and return the JSON payload.
        /// </summary>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key bytes that were used to sign the JWT.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <returns>A string containing the JSON payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        /// <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        public static string Decode(string token, byte[] key, bool verify = true)
        {
            string[] parts = token.Split('.');
            if (parts.Length != 3)
            {
                throw new System.ArgumentException("Token must consist from 3 delimited by dot parts");
            } // End if (parts.Length != 3) 

            string header = parts[0];
            string payload = parts[1];
            byte[] crypto = Base64UrlDecode(parts[2]);

            string headerJson = System.Text.Encoding.UTF8.GetString(Base64UrlDecode(header));
            string payloadJson = System.Text.Encoding.UTF8.GetString(Base64UrlDecode(payload));

            System.Collections.Generic.Dictionary<string, object> headerData = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(headerJson);

            if (verify)
            {
                byte[] bytesToSign = System.Text.Encoding.UTF8.GetBytes(string.Concat(header, ".", payload));
                string algorithm = (string)headerData["alg"];

                byte[] signature = HashAlgorithms[GetHashAlgorithm(algorithm)](key, bytesToSign);
                string decodedCrypto = System.Convert.ToBase64String(crypto);
                string decodedSignature = System.Convert.ToBase64String(signature);

                Verify(decodedCrypto, decodedSignature, payloadJson);
            } // End if (verify) 

            return payloadJson;
        } // End Function Decode


        /// <summary>
        /// Given a JWT, decode it and return the payload as an object (by deserializing it with <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).
        /// </summary>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key that was used to sign the JWT.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <returns>An object representing the payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        /// <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        public static object DecodeToObject(string token, string key, bool verify = true)
        {
            return DecodeToObject(token, System.Text.Encoding.UTF8.GetBytes(key), verify);
        } // End Function DecodeToObject


        /// <summary>
        /// Given a JWT, decode it and return the payload as an object (by deserializing it with <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).
        /// </summary>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key that was used to sign the JWT.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <returns>An object representing the payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        /// <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        public static object DecodeToObject(string token, byte[] key, bool verify = true)
        {
            string payloadJson = Decode(token, key, verify);
            return JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(payloadJson);
        } // End Function DecodeToObject


        /// <summary>
        /// Given a JWT, decode it and return the payload as an object (by deserializing it with <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to return</typeparam>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key that was used to sign the JWT.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <returns>An object representing the payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        /// <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        public static T DecodeToObject<T>(string token, string key, bool verify = true)
        {
            return DecodeToObject<T>(token, System.Text.Encoding.UTF8.GetBytes(key), verify);
        } // End Function DecodeToObject


        /// <summary>
        /// Given a JWT, decode it and return the payload as an object (by deserializing it with <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to return</typeparam>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key that was used to sign the JWT.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <returns>An object representing the payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        /// <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        public static T DecodeToObject<T>(string token, byte[] key, bool verify = true)
        {
            string payloadJson = Decode(token, key, verify);
            return JsonSerializer.Deserialize<T>(payloadJson);
        } // End Function DecodeToObject


        /// <remarks>From JWT spec</remarks>
        public static string Base64UrlEncode(byte[] input)
        {
            string output = null;;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(System.Convert.ToBase64String(input));

            for(int iLength = sb.Length - 1; iLength > -1 && sb[iLength] == '='; --iLength)
                sb.Remove(iLength, 1);

            sb.Replace('+', '-');
            sb.Replace('/', '_');

            output = sb.ToString();
            sb.Length = 0;
            sb = null;

            // output = output.Split('=')[0]; // Remove any trailing '='s
            // output = output.Replace('+', '-'); // 62nd char of encoding
            // output = output.Replace('/', '_'); // 63rd char of encoding
            return output;
        } // End Function Base64UrlEncode


        /// <remarks>From JWT spec</remarks>
        public static byte[] Base64UrlDecode(string input)
        {
            string output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break;  // One pad char
                default: throw new System.FormatException("Illegal base64url string!");
            } // End switch (output.Length % 4)  

            byte[] converted = System.Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        } // End Function Base64UrlDecode 


        private static JwtHashAlgorithm GetHashAlgorithm(string algorithm)
        {
            switch (algorithm)
            {
                case "HS256": return JwtHashAlgorithm.HS256;
                case "HS384": return JwtHashAlgorithm.HS384;
                case "HS512": return JwtHashAlgorithm.HS512;

                case "RS256": return JwtHashAlgorithm.RS256;
                case "RS384": return JwtHashAlgorithm.RS384;
                case "RS512": return JwtHashAlgorithm.RS512;

                case "ES256": return JwtHashAlgorithm.ES256;
                case "ES384": return JwtHashAlgorithm.ES384;
                case "ES512": return JwtHashAlgorithm.ES512;

                default: throw new SignatureVerificationException("Algorithm not supported.");
            } // End switch (algorithm) 

        } // End Function GetHashAlgorithm 


        private static void Verify(string decodedCrypto, string decodedSignature, string payloadJson)
        {
            if (decodedCrypto != decodedSignature)
            {
                throw new SignatureVerificationException(string.Format("Invalid signature. Expected {0} got {1}", decodedCrypto, decodedSignature));
            }

            // verify exp claim https://tools.ietf.org/html/draft-ietf-oauth-json-web-token-32#section-4.1.4
            System.Collections.Generic.Dictionary<string, object> payloadData = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(payloadJson);
            object expObj;

            if (!payloadData.TryGetValue("exp", out expObj) || expObj == null)
            {
                return;
            }

            long expInt;
            try
            {
                expInt = System.Convert.ToInt64(expObj);
            }
            catch (System.FormatException)
            {
                throw new SignatureVerificationException("Claim 'exp' must be an integer.");
            }

            long secondsSinceEpoch = (long)((System.DateTime.UtcNow - UnixEpoch).TotalSeconds);
            if (secondsSinceEpoch >= expInt)
            {
                throw new TokenExpiredException("Token has expired.");
            } // End if (secondsSinceEpoch >= expInt)

        } // End Sub Verify 


    } // End Class JsonWebToken


} // End Namespace JWT 
