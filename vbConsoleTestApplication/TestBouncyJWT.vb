
Public Class TestBouncyJWT


    Public Shared Sub Test()
        Dim pubKey As String = "-----BEGIN PUBLIC KEY-----" & vbCr & vbLf & "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCYk9VtkOHIRNCquRqCl9bbFsGw" & vbCr & vbLf & "HdJJPIoZENwcHYBgnqVoEa5SJs8ddkNNna6M+Gln2n4S/G7Mu+Cz0cQg06Ru8919" & vbCr & vbLf & "hYWGWdyVumAGgJwMEKAzUj9651Y6AAOcAM0qX/f0DrLlUAZFy+64L8kVjuCyFdti" & vbCr & vbLf & "5d3yaGnFM+Xw/4fcLwIDAQAB" & vbCr & vbLf & "-----END PUBLIC KEY-----" & vbCr & vbLf
        Dim privKey As String = "-----BEGIN RSA PRIVATE KEY-----" & vbCr & vbLf & "MIICXQIBAAKBgQCYk9VtkOHIRNCquRqCl9bbFsGwHdJJPIoZENwcHYBgnqVoEa5S" & vbCr & vbLf & "Js8ddkNNna6M+Gln2n4S/G7Mu+Cz0cQg06Ru8919hYWGWdyVumAGgJwMEKAzUj96" & vbCr & vbLf & "51Y6AAOcAM0qX/f0DrLlUAZFy+64L8kVjuCyFdti5d3yaGnFM+Xw/4fcLwIDAQAB" & vbCr & vbLf & "AoGAGyDZ51/FzUpzAY/k50hhEtZSfOJog84ITdmiETurmkJK7ZyLLp8o3zeqUtAQ" & vbCr & vbLf & "+46liyodlXmdp7hWBRLseNu4lh1gQGYj4/fH2BT75/zFngaTdz7pANKq6Y5IOHg0" & vbCr & vbLf & "C1UatzmuSmDGk/l7g1gQyWo8dcwjrzvsGWBAFZ4QHy2OsE0CQQDNStOX0USyfgrZ" & vbCr & vbLf & "AkKOfs3paaxVB/SZTBaorcqo8nBX1Fx/rdpBTIezHuZQchF/BGpHLS7/yyve+jg/" & vbCr & vbLf & "dspR7XZdAkEAvkO10QFsDR1GJwVcUpG1LguznKqS7v6FscnpBFvfsf7UaqNHCGvY" & vbCr & vbLf & "Feau1EwekVRl77ZKUPhDQt7XFniBO40b+wJBALZnQ7Xi1H0bjJvgbC6b8Gzx3ZL3" & vbCr & vbLf & "rJcAiil5sVWHg9Yl88HmQMRAMVovnEfh8jW/QIbZWKciaGqIPK326DD/ImkCQQCC" & vbCr & vbLf & "k1OHQfOWuH15sCshG5B9Lliw7ztxu8mjL0+0xxypOpsrKC1KsUCWHz/iwO7FjGd8" & vbCr & vbLf & "8Nzl3svCa86vRDpk1T3bAkBWjvKigxbkpYPbayKwjeWTiS3YIg63N2WUaetFBAD2" & vbCr & vbLf & "Yrv+Utm12zi99pZNA5WCqO/UhN9poJdWaYqYYImYhH8N" & vbCr & vbLf & "-----END RSA PRIVATE KEY-----" & vbCr & vbLf


        Dim key As New BouncyJWT.JwtKey() With {.PemPrivateKey = privKey}
        Dim arbitraryKey As New BouncyJWT.JwtKey() With {.PemPrivateKey = BouncyJWT.Crypto.GenerateRandomRsaPrivateKey(1024)}

        Dim token As String = BouncyJWT.JsonWebToken.Encode(New User(), key, BouncyJWT.JwtHashAlgorithm.RS256)

        System.Console.WriteLine(token)
        Dim thisUser As User = BouncyJWT.JsonWebToken.DecodeToObject(Of User)(token, key, True)
        Dim wrongUser As User = BouncyJWT.JsonWebToken.DecodeToObject(Of User)(token, arbitraryKey, True)


        System.Console.WriteLine(thisUser)
        System.Console.WriteLine(wrongUser)


        System.Console.WriteLine(System.Environment.NewLine)
        System.Console.WriteLine(" --- Press any key to continue --- ")
        System.Console.ReadKey()
    End Sub


End Class
