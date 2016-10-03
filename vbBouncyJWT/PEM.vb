
Imports System.Security.Cryptography


' http://goboex.com/book-de.php?url=arbeitsplatz-der-zukunft-gestaltungsansatze-und-good-practice-beispiele&src=gdrive
Namespace BouncyJWT.RSA


    ' http://stackoverflow.com/questions/11506891/how-to-load-the-rsa-public-key-from-file-in-c-sharp
    Public Class PEM



        ''' <summary>
        ''' Export a certificate to a PEM format string
        ''' </summary>
        ''' <param name="cert">The certificate to export</param>
        ''' <returns>A PEM encoded string</returns>
        Public Shared Function ExportToPEM(cert As System.Security.Cryptography.X509Certificates.X509Certificate) As String
            Dim builder As New System.Text.StringBuilder()

            builder.AppendLine("-----BEGIN CERTIFICATE-----")
            builder.AppendLine(System.Convert.ToBase64String(cert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert), System.Base64FormattingOptions.InsertLineBreaks))
            builder.AppendLine("-----END CERTIFICATE-----")

            Return builder.ToString()
        End Function
        ' End Function ExportToPEM 
#If False Then
		' http://stackoverflow.com/questions/11244333/how-can-i-use-ecdsa-in-c-sharp-with-a-key-size-of-less-than-the-built-in-minimum
		Public Shared Sub ExportEcdsaKey()
			Dim publicKey As Byte() = Nothing
			Dim privateKey As Byte() = Nothing

			Using dsa As New ECDsaCng(256)

					' publicKey = dsa.Key.Export(CngKeyBlobFormat.EccPublicBlob);
					' privateKey = dsa.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
				dsa.HashAlgorithm = CngAlgorithm.Sha256
			End Using

			Dim k As CngKey = CngKey.Create(CngAlgorithm.ECDsaP256, "myECDH", New CngKeyCreationParameters() With { _
				Key .ExportPolicy = CngExportPolicies.AllowPlaintextExport, _
				Key .KeyCreationOptions = CngKeyCreationOptions.MachineKey, _
				Key .KeyUsage = CngKeyUsages.AllUsages, _
				Key .Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider, _
				Key .UIPolicy = New CngUIPolicy(CngUIProtectionLevels.None) _
			})




			System.Console.WriteLine(publicKey)
			System.Console.WriteLine(privateKey)
		End Sub


		Public Shared Function CreateEcdProvider() As System.Security.Cryptography.ECDsaCng
			Dim publicKey As Byte() = New Byte() {21, 5, 8, 12, 207}

			Return New ECDsaCng(CngKey.Import(publicKey, CngKeyBlobFormat.EccPublicBlob))
		End Function
		' End Function CreateEcdProvider 
#End If

        Public Shared Function CreateRsaProvider() As System.Security.Cryptography.RSACryptoServiceProvider
            Dim publicPrivateKeyXML As String = "<RSAKeyValue><Modulus>jwC1EyNgHkh3Q3J3ITmh6EkbsTSKJuuCYsg9UsaYA9+Trwlp4v37VVc3b2jTsUHaEcG1IYGQbQBu/IIxiDlmDFiQPpN8UrhLz0ZQ9SzWRONSzRC97DR08epl4JtO86uAWYR9+iEnxIaeiRG6i32sjZYaiqwBuvNzli94Wtz7yQDNlH6FmdkMp0n9Hg8MslRXbRbINXhW/nJ4zOggmRLQfOzc2ZxyARgmXvpmxPxaaawtBj919VHFishyU0u7CbzpQ3J5dldV1d+FTICZN5AveF4qM4tzWMBZdCubdcKiMnIGNgBO/mUb/SbWwlu4OuXG8vOr5aqYIuaWUKBFEyK3pQ==</Modulus><Exponent>AQAB</Exponent><P>uSoRvVLPX8EdlsYbCcVTgjqpP0e/UsXeBRXTMjxaHLDYgvgSUADhAd5ECscsNdb5sLUy18xSd9pVLFRMr7lqfstiY1tJDyLrP+54TON4KUhFkV1ntf5R1bGzvxlY3po3vUz+2a8fLq4F9ennaT6/NKyn4d7WH+qmr6oSbrgGLgU=</P><Q>xbWTfXb9UIGLN/j/V+TVukMSP8GmSXj+tKnrr5XjdwB4yRyf6krw4ntIU/wnS6in5TRy5R3Wdy8ixGCB9zghQnjShhCKprD2nx4f74BzpL62Y6qI+lsH17AZAov0Olpe3MPGFo+F6UXqlRQPEAfh2Wxv6hiCcfucP7gEy5Tc9SE=</Q><DP>JneN7eX5POxSqFMJpPMAkUp8hK/0GE8Q+793+7S8B7/ZiwPcUhCMriWtvwt3rMu3XbWXFWvWKh4KmcX9lHgRnrvD+d4qBGH9u29gQKD1AqaIBVYBSLbH63waWnX6l2w0bjhDrZeLA9iVVmw8bgniESBZVDxGAaVu8YmEgMnsRr0=</DP><DQ>etVV/hRIS5VAdpUHp4bv1ppHIz9f3bQDoyES4fMg8FVltaVIIVtQD5YCmNNHYrU1Iq0UWQ7RqRiq5BEFjh/cYh0IxuxOCERX5QHlW3qV3pvyWzefhNO7qqCo2TE0mnB9EXG8h1XCH+0lUlu1BAOxqNC7M1jo6oIlUF029XjWUqE=</DQ><InverseQ>aZwOBDDsp0tZSs2p418syfQwxyWUJ/kdu4D08x+LtI0Fd31LzHbD1Ogs6aBlAFf8wPFE4mNsHDrkjujLKoEmnwt8SEMSIXKz2EuDG4E7wKgTT3w0Dy3Vydo4Zh6kGJF+bQ2DP3LhwoHZBt06CPeFvsUBOnM/nKn9ICJeKHwyfkQ=</InverseQ><D>Cgxtgj9ESUx5SPLZqSrbaYRS9FRHTZh99y1alcmhCUtOyDLGpIM0A9lW6ra4gruoXwK4FMx9wWhm5B/NQDpxpSDHXgZPavaf/nF9Tdp34gEBTVSbATvnEyVlQZpYOr93Nj3Hpmm/BCHGMRve+l9QiTseJAFrNl+rZHHIfhtfe/kTZu+Klhelmb1JEgtqRe7Ve91JGoDka4L9GP0oIqD9nnxmI1gmpaUeuDKfpbzfeoQCU1RVUDyZ5MirRbTvUmCIjs3Hed2pmDTigkTJ2mYHd4gESXXyCYVa8Qgw5mOOoHCd5viG3hLYQGzMIoGkzKr+5vF63kxtkMRFKNIra3vbQQ==</D></RSAKeyValue>"
            ' string publicOnlyKeyXML = "<RSAKeyValue><Modulus>jwC1EyNgHkh3Q3J3ITmh6EkbsTSKJuuCYsg9UsaYA9+Trwlp4v37VVc3b2jTsUHaEcG1IYGQbQBu/IIxiDlmDFiQPpN8UrhLz0ZQ9SzWRONSzRC97DR08epl4JtO86uAWYR9+iEnxIaeiRG6i32sjZYaiqwBuvNzli94Wtz7yQDNlH6FmdkMp0n9Hg8MslRXbRbINXhW/nJ4zOggmRLQfOzc2ZxyARgmXvpmxPxaaawtBj919VHFishyU0u7CbzpQ3J5dldV1d+FTICZN5AveF4qM4tzWMBZdCubdcKiMnIGNgBO/mUb/SbWwlu4OuXG8vOr5aqYIuaWUKBFEyK3pQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

            Dim rSACryptoServiceProvider As New System.Security.Cryptography.RSACryptoServiceProvider(New System.Security.Cryptography.CspParameters() With { _
                 .Flags = System.Security.Cryptography.CspProviderFlags.UseMachineKeyStore _
            })

            rSACryptoServiceProvider.FromXmlString(publicPrivateKeyXML)
            Return rSACryptoServiceProvider
        End Function
        ' End Function CreateRsaProvider 

        Public Shared Function ExportPrivateKey(csp As System.Security.Cryptography.RSACryptoServiceProvider) As String
            Dim retVal As String = Nothing
            Dim sb As New System.Text.StringBuilder()
            Using sw As New System.IO.StringWriter(sb)
                ExportPrivateKey(csp, sw)
                retVal = sb.ToString()
                sb.Length = 0
                sb = Nothing
            End Using

            Return retVal
        End Function


        Public Shared Function ExportPublicKey(csp As System.Security.Cryptography.RSACryptoServiceProvider) As String
            Dim retVal As String = Nothing
            Dim sb As New System.Text.StringBuilder()
            Using sw As New System.IO.StringWriter(sb)
                ExportPublicKey(csp, sw)
                retVal = sb.ToString()
                sb.Length = 0
                sb = Nothing
            End Using

            Return retVal
        End Function



        ' http://stackoverflow.com/questions/23734792/c-sharp-export-private-public-rsa-key-from-rsacryptoserviceprovider-to-pem-strin
        Public Shared Sub ExportPrivateKey(csp As RSACryptoServiceProvider, outputStream As System.IO.TextWriter)
            If csp.PublicOnly Then
                Throw New System.ArgumentException("CSP does not contain a private key", "csp")
            End If

            Dim parameters As RSAParameters = csp.ExportParameters(True)
            Using stream As New System.IO.MemoryStream()
                Using writer As New System.IO.BinaryWriter(stream)
                    writer.Write(CByte(&H30))
                    ' SEQUENCE
                    Using innerStream As New System.IO.MemoryStream()

                        Using innerWriter As New System.IO.BinaryWriter(innerStream)
                            EncodeIntegerBigEndian(innerWriter, New Byte() {&H0})
                            ' Version
                            EncodeIntegerBigEndian(innerWriter, parameters.Modulus)
                            EncodeIntegerBigEndian(innerWriter, parameters.Exponent)
                            EncodeIntegerBigEndian(innerWriter, parameters.D)
                            EncodeIntegerBigEndian(innerWriter, parameters.P)
                            EncodeIntegerBigEndian(innerWriter, parameters.Q)
                            EncodeIntegerBigEndian(innerWriter, parameters.DP)
                            EncodeIntegerBigEndian(innerWriter, parameters.DQ)
                            EncodeIntegerBigEndian(innerWriter, parameters.InverseQ)
                            Dim length As Integer = CInt(innerStream.Length)
                            EncodeLength(writer, length)
                            writer.Write(innerStream.GetBuffer(), 0, length)
                            ' End Using innerWriter 
                        End Using
                    End Using
                    ' End Using innerStream 

                    Dim base64 As Char() = System.Convert.ToBase64String(stream.GetBuffer(), 0, CInt(stream.Length)).ToCharArray()
                    ' From here on, stream is no longer needed 


                    outputStream.WriteLine("-----BEGIN RSA PRIVATE KEY-----")
                    ' Output as Base64 with lines chopped at 64 characters
                    For i As Integer = 0 To base64.Length - 1 Step 64
                        outputStream.WriteLine(base64, i, System.Math.Min(64, base64.Length - i))
                    Next
                    ' Next i 

                    outputStream.WriteLine("-----END RSA PRIVATE KEY-----")
                    ' End Using writer 
                End Using
            End Using
            ' End Using stream 
        End Sub
        ' End Sub ExportPrivateKey

        ' http://stackoverflow.com/questions/28406888/c-sharp-rsa-public-key-output-not-correct/28407693#28407693
        Private Shared Sub ExportPublicKey(csp As RSACryptoServiceProvider, outputStream As System.IO.TextWriter)
            Dim parameters As RSAParameters = csp.ExportParameters(False)
            Using stream As New System.IO.MemoryStream()
                Using writer As New System.IO.BinaryWriter(stream)
                    writer.Write(CByte(&H30))
                    ' SEQUENCE
                    Using innerStream As New System.IO.MemoryStream()
                        Dim innerWriter As New System.IO.BinaryWriter(innerStream)
                        innerWriter.Write(CByte(&H30))
                        ' SEQUENCE
                        EncodeLength(innerWriter, 13)
                        innerWriter.Write(CByte(&H6))
                        ' OBJECT IDENTIFIER
                        Dim rsaEncryptionOid As Byte() = New Byte() {&H2A, &H86, &H48, &H86, &HF7, &HD, _
                            &H1, &H1, &H1}
                        EncodeLength(innerWriter, rsaEncryptionOid.Length)
                        innerWriter.Write(rsaEncryptionOid)
                        innerWriter.Write(CByte(&H5))
                        ' NULL
                        EncodeLength(innerWriter, 0)
                        innerWriter.Write(CByte(&H3))
                        ' BIT STRING
                        Using bitStringStream As New System.IO.MemoryStream()
                            Using bitStringWriter As New System.IO.BinaryWriter(bitStringStream)
                                bitStringWriter.Write(CByte(&H0))
                                ' # of unused bits
                                bitStringWriter.Write(CByte(&H30))
                                ' SEQUENCE
                                Using paramsStream As New System.IO.MemoryStream()

                                    Using paramsWriter As New System.IO.BinaryWriter(paramsStream)
                                        EncodeIntegerBigEndian(paramsWriter, parameters.Modulus)
                                        ' Modulus
                                        EncodeIntegerBigEndian(paramsWriter, parameters.Exponent)
                                        ' Exponent
                                        Dim paramsLength As Integer = CInt(paramsStream.Length)
                                        EncodeLength(bitStringWriter, paramsLength)
                                        bitStringWriter.Write(paramsStream.GetBuffer(), 0, paramsLength)
                                        ' End Using paramsWriter 
                                    End Using
                                End Using
                                ' End Using paramsStream 
                                Dim bitStringLength As Integer = CInt(bitStringStream.Length)
                                EncodeLength(innerWriter, bitStringLength)
                                innerWriter.Write(bitStringStream.GetBuffer(), 0, bitStringLength)
                                ' End Using bitStringWriter 
                            End Using
                        End Using
                        ' End Using bitStringStream 
                        Dim length As Integer = CInt(innerStream.Length)
                        EncodeLength(writer, length)
                        writer.Write(innerStream.GetBuffer(), 0, length)
                    End Using
                    ' End Using innerStream 
                    Dim base64 As Char() = System.Convert.ToBase64String(stream.GetBuffer(), 0, CInt(stream.Length)).ToCharArray()
                    ' From here on, stream is no longer needed 

                    outputStream.WriteLine("-----BEGIN PUBLIC KEY-----")
                    For i As Integer = 0 To base64.Length - 1 Step 64
                        outputStream.WriteLine(base64, i, System.Math.Min(64, base64.Length - i))
                    Next
                    ' Next i 

                    outputStream.WriteLine("-----END PUBLIC KEY-----")
                    ' End Using writer
                End Using
            End Using
            ' End Using stream
        End Sub
        ' End Sub ExportPublicKey 

        Private Shared Sub EncodeLength(stream As System.IO.BinaryWriter, length As Integer)
            If length < 0 Then
                Throw New System.ArgumentOutOfRangeException("length", "Length must be non-negative")
            End If

            If length < &H80 Then
                ' Short form
                stream.Write(CByte(length))
            Else
                ' Long form
                Dim temp As Integer = length
                Dim bytesRequired As Integer = 0
                While temp > 0
                    temp >>= 8
                    bytesRequired += 1
                End While
                stream.Write(CByte(bytesRequired Or &H80))
                For i As Integer = bytesRequired - 1 To 0 Step -1
                    stream.Write(CByte(length >> (8 * i) And &HFF))
                Next
            End If

        End Sub
        ' End Sub EncodeLength 

        Private Shared Sub EncodeIntegerBigEndian(stream As System.IO.BinaryWriter, value As Byte())
            EncodeIntegerBigEndian(stream, value, True)
        End Sub
        ' End Sub EncodeIntegerBigEndian 

        Private Shared Sub EncodeIntegerBigEndian(stream As System.IO.BinaryWriter, value As Byte(), forceUnsigned As Boolean)
            stream.Write(CByte(&H2))
            ' INTEGER
            Dim prefixZeros As Integer = 0
            For i As Integer = 0 To value.Length - 1
                If value(i) <> 0 Then
                    Exit For
                End If
                prefixZeros += 1
            Next
            If value.Length - prefixZeros = 0 Then
                EncodeLength(stream, 1)
                stream.Write(CByte(0))
            Else
                If forceUnsigned AndAlso value(prefixZeros) > &H7F Then
                    ' Add a prefix zero to force unsigned if the MSB is 1
                    EncodeLength(stream, value.Length - prefixZeros + 1)
                    stream.Write(CByte(0))
                Else
                    EncodeLength(stream, value.Length - prefixZeros)
                End If
                For i As Integer = prefixZeros To value.Length - 1
                    stream.Write(value(i))
                Next
            End If
        End Sub
        ' End Sub EncodeIntegerBigEndian 

    End Class
    ' End Class PEM 

End Namespace
' End Namespace BouncyJWT.RSA.KeyManagement 
