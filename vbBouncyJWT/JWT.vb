﻿

Imports System.Security.Cryptography

' See MS code @
' https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/tree/master/src/System.IdentityModel.Tokens.Jwt
' https://stackoverflow.com/questions/10055158/is-there-a-json-web-token-jwt-example-in-c
' https://auth0.com/docs/tutorials/generate-jwt-dotnet


Namespace BouncyJWT


    ''' <summary>
    ''' Provides methods for encoding and decoding JSON Web Tokens.
    ''' </summary>
    Public NotInheritable Class JsonWebToken


        Private Delegate Function GenericHashFunction_t(key As JwtKey, valueToHash As Byte()) As Byte()


        ''' <summary>
        ''' Pluggable JSON Serializer
        ''' </summary>
        Public Shared JsonSerializer As IJsonSerializer

        Private Shared ReadOnly UnixEpoch As System.DateTime

        Private Shared ReadOnly HashAlgorithms As System.Collections.Generic.IDictionary(Of JwtHashAlgorithm, GenericHashFunction_t)


        ' http://crypto.stackexchange.com/questions/5646/what-are-the-differences-between-a-digital-signature-a-mac-and-a-hash

        ' Integrity:        Can the recipient be confident that the message has not been accidentally modified?
        ' Authentication:   Can the recipient be confident that the message originates from the sender?
        ' Non-repudiation:  If the recipient passes the message and the proof to a third party, 
        '                   can the third party be confident that the message originated from the sender? 

        ' Cryptographic primitive | Hash |    MAC    | Digital
        ' Security Goal           |      |           | signature
        ' ------------------------+------+-----------+-------------
        ' Integrity               |  Yes |    Yes    |   Yes
        ' Authentication          |  No  |    Yes    |   Yes
        ' Non-repudiation         |  No  |    No     |   Yes
        ' ------------------------+------+-----------+-------------
        ' Kind of keys            | none | symmetric | asymmetric
        '                         |      |    keys   |    keys

        Shared Sub New()
            JsonSerializer = New DefaultJsonSerializer()
            UnixEpoch = New System.DateTime(1970, 1, 1, 0, 0, 0, _
                0, System.DateTimeKind.Utc)


            ' https://stackoverflow.com/questions/10055158/is-there-a-json-web-token-jwt-example-in-c 
            ' https://stackoverflow.com/questions/34403823/verifying-jwt-signed-with-the-rs256-algorithm-using-public-key-in-c-sharp
            ' http://codingstill.com/2016/01/verify-jwt-token-signed-with-rs256-using-the-public-key/
            HashAlgorithms = New System.Collections.Generic.Dictionary(Of JwtHashAlgorithm, GenericHashFunction_t)() From {
                {JwtHashAlgorithm.None, Function(key, value)
                                            Throw New TokenAlgorithmRefusedException()
                                        End Function},
                {JwtHashAlgorithm.HS256, Function(key, value)
                                             'Using sha As New HMACSHA256(key)
                                             '    Return sha.ComputeHash(value)
                                             'End Using

                                             Dim hmac As New Org.BouncyCastle.Crypto.Macs.HMac(New Org.BouncyCastle.Crypto.Digests.Sha256Digest())

                                             hmac.Init(New Org.BouncyCastle.Crypto.Parameters.KeyParameter(key.MacKeyBytes))

                                             Dim result As Byte() = New Byte(hmac.GetMacSize() - 1) {}
                                             hmac.BlockUpdate(value, 0, value.Length)
                                             hmac.DoFinal(result, 0)

                                             Return result

                                         End Function},
                {JwtHashAlgorithm.HS384, Function(key, value)
                                             'Using sha As New HMACSHA384(key)
                                             '    Return sha.ComputeHash(value)
                                             'End Using
                                             Dim hmac As New Org.BouncyCastle.Crypto.Macs.HMac(New Org.BouncyCastle.Crypto.Digests.Sha384Digest())

                                             hmac.Init(New Org.BouncyCastle.Crypto.Parameters.KeyParameter(key.MacKeyBytes))

                                             Dim result As Byte() = New Byte(hmac.GetMacSize() - 1) {}
                                             hmac.BlockUpdate(value, 0, value.Length)
                                             hmac.DoFinal(result, 0)

                                             Return result
                                         End Function},
                {JwtHashAlgorithm.HS512, Function(key, value)
                                             'Using sha As New HMACSHA512(key)
                                             '    Return sha.ComputeHash(value)
                                             'End Using
                                             Dim hmac As New Org.BouncyCastle.Crypto.Macs.HMac(New Org.BouncyCastle.Crypto.Digests.Sha512Digest())


                                             hmac.Init(New Org.BouncyCastle.Crypto.Parameters.KeyParameter(key.MacKeyBytes))

                                             Dim result As Byte() = New Byte(hmac.GetMacSize() - 1) {}
                                             hmac.BlockUpdate(value, 0, value.Length)
                                             hmac.DoFinal(result, 0)

                                             Return result
                                         End Function},
                {JwtHashAlgorithm.RS256, Function(key, value)
                                             Dim privKey As Org.BouncyCastle.Crypto.AsymmetricKeyParameter = key.PrivateKey
                                             ' https://github.com/neoeinstein/bouncycastle/blob/master/crypto/src/security/SignerUtilities.cs
                                             Dim signer As Org.BouncyCastle.Crypto.ISigner = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA-256withRSA")
                                             signer.Init(True, privKey)
                                             signer.BlockUpdate(value, 0, value.Length)
                                             Return signer.GenerateSignature()
                                         End Function},
                {JwtHashAlgorithm.RS384, Function(key, value)
                                             Dim privKey As Org.BouncyCastle.Crypto.AsymmetricKeyParameter = key.PrivateKey
                                             ' https://github.com/neoeinstein/bouncycastle/blob/master/crypto/src/security/SignerUtilities.cs
                                             Dim signer As Org.BouncyCastle.Crypto.ISigner = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA-384withRSA")
                                             signer.Init(True, privKey)
                                             signer.BlockUpdate(value, 0, value.Length)
                                             Return signer.GenerateSignature()
                                         End Function},
                {JwtHashAlgorithm.RS512, Function(key, value)
                                             Dim privKey As Org.BouncyCastle.Crypto.AsymmetricKeyParameter = key.PrivateKey
                                             ' https://github.com/neoeinstein/bouncycastle/blob/master/crypto/src/security/SignerUtilities.cs
                                             Dim signer As Org.BouncyCastle.Crypto.ISigner = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA-512withRSA")

                                             signer.Init(True, privKey)
                                             signer.BlockUpdate(value, 0, value.Length)
                                             Return signer.GenerateSignature()
                                         End Function},
                {JwtHashAlgorithm.ES256, Function(key, value)
                                             Dim privKey As Org.BouncyCastle.Crypto.AsymmetricKeyParameter = key.PrivateKey
                                             ' https://github.com/neoeinstein/bouncycastle/blob/master/crypto/src/security/SignerUtilities.cs 
                                             Dim signer As Org.BouncyCastle.Crypto.ISigner = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA-256withECDSA")

                                             signer.Init(True, privKey)
                                             signer.BlockUpdate(value, 0, value.Length)
                                             Return signer.GenerateSignature()
                                         End Function},
            {JwtHashAlgorithm.ES384, Function(key, value)
                                         Dim privKey As Org.BouncyCastle.Crypto.AsymmetricKeyParameter = key.PrivateKey
                                         ' https://github.com/neoeinstein/bouncycastle/blob/master/crypto/src/security/SignerUtilities.cs 
                                         Dim signer As Org.BouncyCastle.Crypto.ISigner = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA-384withECDSA")

                                         signer.Init(True, privKey)
                                         signer.BlockUpdate(value, 0, value.Length)
                                         Return signer.GenerateSignature()
                                     End Function},
            {JwtHashAlgorithm.ES512, Function(key, value)
                                         Dim privKey As Org.BouncyCastle.Crypto.AsymmetricKeyParameter = key.PrivateKey
                                         ' https://github.com/neoeinstein/bouncycastle/blob/master/crypto/src/security/SignerUtilities.cs 
                                         Dim signer As Org.BouncyCastle.Crypto.ISigner = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA-512withECDSA")

                                         signer.Init(True, privKey)
                                         signer.BlockUpdate(value, 0, value.Length)
                                         Return signer.GenerateSignature()
                                     End Function}
            }

        End Sub ' Constructor 


        ''' <summary>
        ''' Creates a JWT given a payload, the signing key, and the algorithm to use.
        ''' </summary>
        ''' <param name="payload">An arbitrary payload (must be serializable to JSON via System.Web.Script.Serialization.JavaScriptSerializer).</param>
        ''' <param name="key">The key used to sign the token.</param>
        ''' <param name="algorithm">The hash algorithm to use.</param>
        ''' <returns>The generated JWT.</returns>
        Public Shared Function Encode(payload As Object, key As String, algorithm As JwtHashAlgorithm) As String
            Return Encode(New System.Collections.Generic.Dictionary(Of String, Object)(), payload, New JwtKey(key), algorithm)
        End Function ' Encode


        ''' <summary>
        ''' Creates a JWT given a payload, the signing key, and the algorithm to use.
        ''' </summary>
        ''' <param name="payload">An arbitrary payload (must be serializable to JSON via System.Web.Script.Serialization.JavaScriptSerializer).</param>
        ''' <param name="key">The key used to sign the token.</param>
        ''' <param name="algorithm">The hash algorithm to use.</param>
        ''' <returns>The generated JWT.</returns>
        Public Shared Function Encode(payload As Object, key As Byte(), algorithm As JwtHashAlgorithm) As String
            Return Encode(New System.Collections.Generic.Dictionary(Of String, Object)(), payload, New JwtKey(key), algorithm)
        End Function ' Encode


        ''' <summary>
        ''' Creates a JWT given a payload, the signing key, and the algorithm to use.
        ''' </summary>
        ''' <param name="payload">An arbitrary payload (must be serializable to JSON via System.Web.Script.Serialization.JavaScriptSerializer).</param>
        ''' <param name="key">The key used to sign the token.</param>
        ''' <param name="algorithm">The hash algorithm to use.</param>
        ''' <returns>The generated JWT.</returns>
        Public Shared Function Encode(payload As Object, key As JwtKey, algorithm As JwtHashAlgorithm) As String
            Return Encode(New System.Collections.Generic.Dictionary(Of String, Object)(), payload, key, algorithm)
        End Function ' Encode


        ''' <summary>
        ''' Creates a JWT given a set of arbitrary extra headers, a payload, the signing key, and the algorithm to use.
        ''' </summary>
        ''' <param name="extraHeaders">An arbitrary set of extra headers. Will be augmented with the standard "typ" and "alg" headers.</param>
        ''' <param name="payload">An arbitrary payload (must be serializable to JSON via System.Web.Script.Serialization.JavaScriptSerializer).</param>
        ''' <param name="key">The key bytes used to sign the token.</param>
        ''' <param name="algorithm">The hash algorithm to use.</param>
        ''' <returns>The generated JWT.</returns>
        Public Shared Function Encode(extraHeaders As System.Collections.Generic.IDictionary(Of String, Object), payload As Object, key As String, algorithm As JwtHashAlgorithm) As String
            Return Encode(extraHeaders, payload, New JwtKey(key), algorithm)
        End Function ' Encode


        ''' <summary>
        ''' Creates a JWT given a header, a payload, the signing key, and the algorithm to use.
        ''' </summary>
        ''' <param name="extraHeaders">An arbitrary set of extra headers. Will be augmented with the standard "typ" and "alg" headers.</param>
        ''' <param name="payload">An arbitrary payload (must be serializable to JSON via System.Web.Script.Serialization.JavaScriptSerializer).</param>
        ''' <param name="key">The key bytes used to sign the token.</param>
        ''' <param name="algorithm">The hash algorithm to use.</param>
        ''' <returns>The generated JWT.</returns>
        Public Shared Function Encode(extraHeaders As System.Collections.Generic.IDictionary(Of String, Object), payload As Object, key As JwtKey, algorithm As JwtHashAlgorithm) As String
            Dim retVal As String = Nothing

            Dim header As New System.Collections.Generic.Dictionary(Of String, Object)(extraHeaders) From { _
                {"typ", "JWT"}, _
                {"alg", algorithm.ToString()} _
            }

            Dim headerBytes As Byte() = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header))
            Dim payloadBytes As Byte() = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))

            Dim sb As New System.Text.StringBuilder()
            sb.Append(Base64UrlEncode(headerBytes))
            sb.Append(".")
            sb.Append(Base64UrlEncode(payloadBytes))

            Dim bytesToSign As Byte() = System.Text.Encoding.UTF8.GetBytes(sb.ToString())
            Dim signature As Byte() = HashAlgorithms(algorithm)(key, bytesToSign)
            sb.Append(".")
            sb.Append(Base64UrlEncode(signature))

            retVal = sb.ToString()
            sb.Length = 0
            sb = Nothing
            Return retVal
        End Function ' Encode


        ''' <summary>
        ''' Given a JWT, decode it and return the JSON payload.
        ''' </summary>
        ''' <param name="token">The JWT.</param>
        ''' <param name="key">The key that was used to sign the JWT.</param>
        ''' <param name="verify">Whether to verify the signature (default is true).</param>
        ''' <returns>A string containing the JSON payload.</returns>
        ''' <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        ''' <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        Public Shared Function Decode(token As String, key As String, Optional verify As Boolean = True) As String
            Return Decode(token, New JwtKey(key), verify)
        End Function ' Decode


        ''' <summary>
        ''' Given a JWT, decode it and return the JSON payload.
        ''' </summary>
        ''' <param name="token">The JWT.</param>
        ''' <param name="key">The key bytes that were used to sign the JWT.</param>
        ''' <param name="verifySignature">Whether to verify the signature (default is true).</param>
        ''' <returns>A string containing the JSON payload.</returns>
        ''' <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        ''' <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        Public Shared Function Decode(token As String, key As JwtKey, Optional verifySignature As Boolean = True) As String
            Dim parts As String() = token.Split("."c)
            If parts.Length <> 3 Then
                Throw New System.ArgumentException("Token must consist from 3 delimited by dot parts")
            End If
            ' End if (parts.Length != 3) 
            Dim header As String = parts(0)
            Dim payload As String = parts(1)
            Dim crypto As Byte() = Base64UrlDecode(parts(2))

            Dim headerJson As String = System.Text.Encoding.UTF8.GetString(Base64UrlDecode(header))
            Dim payloadJson As String = System.Text.Encoding.UTF8.GetString(Base64UrlDecode(payload))

            Dim headerData As System.Collections.Generic.Dictionary(Of String, Object) = JsonSerializer.Deserialize(Of System.Collections.Generic.Dictionary(Of String, Object))(headerJson)

            If verifySignature Then
                Dim bytesToSign As Byte() = System.Text.Encoding.UTF8.GetBytes(String.Concat(header, ".", payload))
                Dim algorithm As String = DirectCast(headerData("alg"), String)

                Dim signature As Byte() = HashAlgorithms(GetHashAlgorithm(algorithm))(key, bytesToSign)
                Dim decodedCrypto As String = System.Convert.ToBase64String(crypto)
                Dim decodedSignature As String = System.Convert.ToBase64String(signature)

                Verify(decodedCrypto, decodedSignature, payloadJson)
            End If ' verifySignature 

            Return payloadJson
        End Function ' Decode


        ''' <summary>
        ''' Given a JWT, decode it and return the payload as an object (by deserializing it with System.Web.Script.Serialization.JavaScriptSerializer).
        ''' </summary>
        ''' <param name="token">The JWT.</param>
        ''' <param name="key">The key that was used to sign the JWT.</param>
        ''' <param name="verify">Whether to verify the signature (default is true).</param>
        ''' <returns>An object representing the payload.</returns>
        ''' <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        ''' <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        Public Shared Function DecodeToObject(token As String, key As String, Optional verify As Boolean = True) As Object
            Return DecodeToObject(token, System.Text.Encoding.UTF8.GetBytes(key), verify)
        End Function ' DecodeToObject


        ''' <summary>
        ''' Given a JWT, decode it and return the payload as an object (by deserializing it with System.Web.Script.Serialization.JavaScriptSerializer).
        ''' </summary>
        ''' <param name="token">The JWT.</param>
        ''' <param name="key">The key that was used to sign the JWT.</param>
        ''' <param name="verify">Whether to verify the signature (default is true).</param>
        ''' <returns>An object representing the payload.</returns>
        ''' <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        ''' <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        Public Shared Function DecodeToObject(token As String, key As Byte(), Optional verify As Boolean = True) As Object
            Dim payloadJson As String = Decode(token, New JwtKey(key), verify)
            Return JsonSerializer.Deserialize(Of System.Collections.Generic.Dictionary(Of String, Object))(payloadJson)
        End Function ' DecodeToObject


        ''' <summary>
        ''' Given a JWT, decode it and return the payload as an object (by deserializing it with System.Web.Script.Serialization.JavaScriptSerializer).
        ''' </summary>
        ''' <param name="token">The JWT.</param>
        ''' <param name="key">The key that was used to sign the JWT.</param>
        ''' <param name="verify">Whether to verify the signature (default is true).</param>
        ''' <returns>An object representing the payload.</returns>
        ''' <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        ''' <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        Public Shared Function DecodeToObject(token As String, key As JwtKey, Optional verify As Boolean = True) As Object
            Dim payloadJson As String = Decode(token, key, verify)
            Return JsonSerializer.Deserialize(Of System.Collections.Generic.Dictionary(Of String, Object))(payloadJson)
        End Function ' DecodeToObject


        ''' <summary>
        ''' Given a JWT, decode it and return the payload as an object (by deserializing it with System.Web.Script.Serialization.JavaScriptSerializer).
        ''' </summary>
        ''' <typeparam name="T">The <see cref="System.Type"/> to return</typeparam>
        ''' <param name="token">The JWT.</param>
        ''' <param name="key">The key that was used to sign the JWT.</param>
        ''' <param name="verify">Whether to verify the signature (default is true).</param>
        ''' <returns>An object representing the payload.</returns>
        ''' <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        ''' <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        Public Shared Function DecodeToObject(Of T)(token As String, key As String, Optional verify As Boolean = True) As T
            Return DecodeToObject(Of T)(token, System.Text.Encoding.UTF8.GetBytes(key), verify)
        End Function ' DecodeToObject


        ''' <summary>
        ''' Given a JWT, decode it and return the payload as an object (by deserializing it with System.Web.Script.Serialization.JavaScriptSerializer).
        ''' </summary>
        ''' <typeparam name="T">The <see cref="System.Type"/> to return</typeparam>
        ''' <param name="token">The JWT.</param>
        ''' <param name="key">The key that was used to sign the JWT.</param>
        ''' <param name="verify">Whether to verify the signature (default is true).</param>
        ''' <returns>An object representing the payload.</returns>
        ''' <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        ''' <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        Public Shared Function DecodeToObject(Of T)(token As String, key As Byte(), Optional verify As Boolean = True) As T
            Dim payloadJson As String = Decode(token, New JwtKey(key), verify)
            Return JsonSerializer.Deserialize(Of T)(payloadJson)
        End Function ' DecodeToObject


        ''' <summary>
        ''' Given a JWT, decode it and return the payload as an object (by deserializing it with System.Web.Script.Serialization.JavaScriptSerializer).
        ''' </summary>
        ''' <typeparam name="T">The <see cref="System.Type"/> to return</typeparam>
        ''' <param name="token">The JWT.</param>
        ''' <param name="key">The key that was used to sign the JWT.</param>
        ''' <param name="verify">Whether to verify the signature (default is true).</param>
        ''' <returns>An object representing the payload.</returns>
        ''' <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        ''' <exception cref="TokenExpiredException">Thrown if the verify parameter was true and the token has an expired exp claim.</exception>
        Public Shared Function DecodeToObject(Of T)(token As String, key As JwtKey, Optional verify As Boolean = True) As T
            Dim payloadJson As String = Decode(token, key, verify)
            Return JsonSerializer.Deserialize(Of T)(payloadJson)
        End Function ' DecodeToObject


        ''' <remarks>From JWT spec</remarks>
        Public Shared Function Base64UrlEncode(input As Byte()) As String
            Dim output As String = Nothing


            Dim sb As New System.Text.StringBuilder(System.Convert.ToBase64String(input))

            Dim iLength As Integer = sb.Length - 1
            While iLength > -1 AndAlso sb(iLength) = "="c
                sb.Remove(iLength, 1)
                iLength -= 1
            End While

            sb.Replace("+"c, "-"c)
            sb.Replace("/"c, "_"c)

            output = sb.ToString()
            sb.Length = 0
            sb = Nothing

            ' output = output.Split('=')(0) // Remove any trailing '='s
            ' output = output.Replace('+', '-') // 62nd char of encoding
            ' output = output.Replace('/', '_') // 63rd char of encoding
            Return output
        End Function ' Base64UrlEncode


        ''' <remarks>From JWT spec</remarks>
        Public Shared Function Base64UrlDecode(input As String) As Byte()
            Dim output As String = input
            output = output.Replace("-"c, "+"c)
            ' 62nd char of encoding
            output = output.Replace("_"c, "/"c)
            ' 63rd char of encoding
            Select Case output.Length Mod 4
                ' Pad with trailing '='s
                Case 0
                    Exit Select
                    ' No pad chars in this case
                Case 2
                    output += "=="
                    Exit Select
                    ' Two pad chars
                Case 3
                    output += "="
                    Exit Select
                Case Else
                    ' One pad char
                    Throw New System.FormatException("Illegal base64url string!")
            End Select ' (output.Length % 4)  

            Dim converted As Byte() = System.Convert.FromBase64String(output)
            ' Standard base64 decoder
            Return converted
        End Function ' Base64UrlDecode 


        Private Shared Function GetHashAlgorithm(algorithm As String) As JwtHashAlgorithm
            If algorithm Is Nothing Then
                Throw New System.ArgumentNullException("algorithm", "Algorithm must be non-NULL. Passed NULL algorithm.")
            End If

            algorithm = algorithm.ToUpperInvariant()

            Select Case algorithm
                Case "HS256"
                    Return JwtHashAlgorithm.HS256
                Case "HS384"
                    Return JwtHashAlgorithm.HS384
                Case "HS512"
                    Return JwtHashAlgorithm.HS512

                Case "RS256"
                    Return JwtHashAlgorithm.RS256
                Case "RS384"
                    Return JwtHashAlgorithm.RS384
                Case "RS512"
                    Return JwtHashAlgorithm.RS512

                Case "ES256"
                    Return JwtHashAlgorithm.ES256
                Case "ES384"
                    Return JwtHashAlgorithm.ES384
                Case "ES512"
                    Return JwtHashAlgorithm.ES512


                Case "NONE"
                    Throw New TokenAlgorithmRefusedException()
                Case Else

                    Throw New SignatureVerificationException("Algorithm not supported.")
            End Select ' algorithm 

        End Function ' GetHashAlgorithm 


        Private Shared Sub Verify(decodedCrypto As String, decodedSignature As String, payloadJson As String)
            If decodedCrypto <> decodedSignature Then
                ' My oh my - please don't donate the correct signature to a wannabe-attacker...
                ' Throw New SignatureVerificationException(String.Format("Invalid signature. Expected {0} got {1}", decodedCrypto, decodedSignature))
                Throw New SignatureVerificationException(String.Format("Invalid signature. Expected {0} got {1}", "SEE_LOG", decodedSignature))
            End If

            ' verify exp claim https://tools.ietf.org/html/draft-ietf-oauth-json-web-token-32#section-4.1.4
            Dim payloadData As System.Collections.Generic.Dictionary(Of String, Object) = JsonSerializer.Deserialize(Of System.Collections.Generic.Dictionary(Of String, Object))(payloadJson)
            Dim expObj As Object = Nothing

            If Not payloadData.TryGetValue("exp", expObj) OrElse expObj Is Nothing Then
                Return
            End If

            Dim expInt As Long
            Try
                expInt = System.Convert.ToInt64(expObj)
            Catch generatedExceptionName As System.FormatException
                Throw New SignatureVerificationException("Claim 'exp' must be an integer.")
            End Try

            Dim secondsSinceEpoch As Long = CLng((System.DateTime.UtcNow - UnixEpoch).TotalSeconds)
            If secondsSinceEpoch >= expInt Then
                Throw New TokenExpiredException("Token has expired.")
            End If
            ' End if (secondsSinceEpoch >= expInt)
        End Sub ' Verify 


    End Class ' JsonWebToken


End Namespace ' JWT 
