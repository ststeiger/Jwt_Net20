
Namespace BouncyJWT


    Public Class Crypto


        ' GenerateRsaKeyPair(1024)
        Public Shared Function GenerateRsaKeyPair(strength As Integer) As Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair
            Dim gen As New Org.BouncyCastle.Crypto.Generators.RsaKeyPairGenerator()
            Dim secureRandom As New Org.BouncyCastle.Security.SecureRandom(New Org.BouncyCastle.Crypto.Prng.CryptoApiRandomGenerator())
            Dim keyGenParam As New Org.BouncyCastle.Crypto.KeyGenerationParameters(secureRandom, strength)

            gen.Init(keyGenParam)

            Dim kp As Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair = gen.GenerateKeyPair()
            Return kp
        End Function ' GenerateRsaKeyPair 


        Public Shared Function GenerateRandomRsaPrivateKey(strength As Integer) As String
            Dim gen As New Org.BouncyCastle.Crypto.Generators.RsaKeyPairGenerator()
            Dim secureRandom As New Org.BouncyCastle.Security.SecureRandom(New Org.BouncyCastle.Crypto.Prng.CryptoApiRandomGenerator())
            Dim keyGenParam As New Org.BouncyCastle.Crypto.KeyGenerationParameters(secureRandom, strength)

            gen.Init(keyGenParam)

            Dim kp As Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair = gen.GenerateKeyPair()

            Return StringifyAsymmetricKey(kp.Private)
        End Function ' GenerateRsaKeyPair 


        Public Shared Sub WritePrivatePublic(keyPair As Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)
            Dim privateKey As String = Nothing
            Dim publicKey As String = Nothing
            
            ' id_rsa
            Using textWriter As System.IO.TextWriter = New System.IO.StringWriter()
                Dim pemWriter As New Org.BouncyCastle.OpenSsl.PemWriter(textWriter)
                pemWriter.WriteObject(keyPair.[Private])
                pemWriter.Writer.Flush()

                privateKey = textWriter.ToString()
            End Using ' textWriter 

            ' id_rsa.pub
            Using textWriter As System.IO.TextWriter = New System.IO.StringWriter()
                Dim pemWriter As New Org.BouncyCastle.OpenSsl.PemWriter(textWriter)
                pemWriter.WriteObject(keyPair.[Public])
                pemWriter.Writer.Flush()

                publicKey = textWriter.ToString()
            End Using ' textWriter

            System.Console.WriteLine(privateKey)
            System.Console.WriteLine(publicKey)
        End Sub ' WritePrivatePublic


        Public Shared Function StringifyAsymmetricKey(privateOrPublicKey As Org.BouncyCastle.Crypto.AsymmetricKeyParameter) As String
            Dim key As String = Nothing

            Using textWriter As System.IO.TextWriter = New System.IO.StringWriter()
                Dim pemWriter As New Org.BouncyCastle.OpenSsl.PemWriter(textWriter)
                pemWriter.WriteObject(privateOrPublicKey)
                pemWriter.Writer.Flush()

                key = textWriter.ToString()
            End Using ' textWriter 

            Return key
        End Function


        Public Shared Function ReadPrivateKey(privateKey As String) As Org.BouncyCastle.Crypto.AsymmetricKeyParameter
            Dim keyPair As Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair = Nothing

            Using reader As System.IO.TextReader = New System.IO.StringReader(privateKey)
                Dim pemReader As New Org.BouncyCastle.OpenSsl.PemReader(reader)

                Dim obj As Object = pemReader.ReadObject()

                If TypeOf obj Is Org.BouncyCastle.Crypto.AsymmetricKeyParameter Then
                    Throw New System.ArgumentException("The given privateKey is a public key, not a privateKey...", "privateKey")
                End If

                If Not (TypeOf obj Is Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair) Then
                    Throw New System.ArgumentException("The given privateKey is not a valid assymetric key.", "privateKey")
                End If

                keyPair = DirectCast(obj, Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair)
            End Using ' reader 

            ' Org.BouncyCastle.Crypto.AsymmetricKeyParameter priv = keyPair.Private;
            ' Org.BouncyCastle.Crypto.AsymmetricKeyParameter pub = keyPair.Public;

            ' Note: 
            ' cipher.Init(False, key)
            ' !!!

            Return keyPair.Private
        End Function ' ReadPrivateKey


        Public Function ReadPublicKeyFile(pemFilename As String) As Org.BouncyCastle.Crypto.AsymmetricKeyParameter
            Dim keyParameter As Org.BouncyCastle.Crypto.AsymmetricKeyParameter = Nothing

            Using streamReader As System.IO.StreamReader = System.IO.File.OpenText(pemFilename)
                Dim pemReader As New Org.BouncyCastle.OpenSsl.PemReader(streamReader)
                keyParameter = DirectCast(pemReader.ReadObject(), Org.BouncyCastle.Crypto.AsymmetricKeyParameter)
            End Using ' fileStream 

            Return keyParameter
        End Function ' ReadPublicKey 


    End Class


End Namespace ' BouncyJWT 
