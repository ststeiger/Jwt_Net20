

Public Class User
    Public Id As Integer = 123
    Public Name As String = "Test"
    Public Language As String = "de-CH"
    Public Bla As String = "Test" & vbCr & vbLf & "123" & ChrW(5) & "äöüÄÖÜñõ"

    Private m_Message As String = "C'est un ""Teste"" éâÂçäöüÄÖÜ$£€"

    Public Property Message As String
        Get
            Return m_Message
        End Get
        Set(value As String)
            Me.m_Message = value
        End Set
    End Property


    Public ReadOnly Property AnotherMessage As String
        Get
            Return m_Message
        End Get
    End Property

End Class ' User 



Module Module1


    Public Function GetCookieValue() As String
        Dim authCookie As System.Web.HttpCookie = System.Web.HttpContext.Current.Request.Cookies("sqlAuthCookie")
        Return authCookie.Value
    End Function


    ' http://stackoverflow.com/questions/1668353/how-can-i-generate-a-cryptographically-secure-pseudorandom-number-in-c
    Public Function GenerateRandomKey(byteCount As Integer) As Byte()
        Dim tokenData As Byte() = New Byte(byteCount - 1) {}

        'using (System.Security.Cryptography.RandomNumberGenerator rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
        Dim rng As System.Security.Cryptography.RandomNumberGenerator = New System.Security.Cryptography.RNGCryptoServiceProvider()
        rng.GetBytes(tokenData)
        rng = Nothing

        Return tokenData
    End Function
    ' End Function GenerateRandomKey 


    Public Sub Test()
        Dim JSON As String = JWT.PetaJson.Json.Format(New User(), JWT.PetaJson.JsonOptions.DontWriteWhitespace)
        System.Console.WriteLine(JSON)

        Dim sb As New System.Text.StringBuilder()
        Using tw As System.IO.TextWriter = New System.IO.StringWriter(sb)
            JWT.PetaJson.Json.Write(tw, New User(), JWT.PetaJson.JsonOptions.DontWriteWhitespace)
        End Using
        System.Console.WriteLine(sb)

        Dim deserializedUser As User = JWT.PetaJson.Json.Parse(Of User)(JSON)
        System.Console.WriteLine(deserializedUser)
    End Sub


    ''' <summary>
    ''' Der Haupteinstiegspunkt für die Anwendung.
    ''' </summary>
    <System.STAThread> _
    Sub Main()
#If (False) Then
			If True Then
				System.Windows.Forms.Application.EnableVisualStyles()
				System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(False)
				System.Windows.Forms.Application.Run(New Form1())
			End If
#End If


        Test()


        Dim key As Byte() = GenerateRandomKey(128)
        Dim token As String = System.Convert.ToBase64String(key)
        Dim key2 As Byte() = System.Convert.FromBase64String(token)
        Dim key3 As Byte() = System.Text.Encoding.UTF8.GetBytes(token)


        ' string jwtToken = JWT.JsonWebToken.Encode(new User(), "I am a unicode capable password", JWT.JwtHashAlgorithm.HS512);
        ' string jwtToken = JWT.JsonWebToken.Encode(new User(), token, JWT.JwtHashAlgorithm.HS512);
        ' string jwtToken = JWT.JsonWebToken.Encode(new User(), key, JWT.JwtHashAlgorithm.HS512);

        ' JWT.RSA.PEM.ExportEcdsaKey();


        ', new User(), key, JWT.JwtHashAlgorithm.HS512
        ' , JWT.JwtHashAlgorithm.HS256
        ' , JWT.JwtHashAlgorithm.RS256
        Dim jwtToken As String = JWT.JsonWebToken.Encode(New System.Collections.Generic.Dictionary(Of String, Object)() From { _
            {"key1", "value1"}, _
            {"key2", "value2"} _
        }, New User(), "hello", JWT.JwtHashAlgorithm.HS256)

        System.Console.WriteLine(jwtToken)

        ' string jwtTokenRandKey = JWT.JsonWebToken.Encode(null, GenerateRandomKey(), JWT.JwtHashAlgorithm.HS512);
        ' System.Console.WriteLine(jwtTokenRandKey);

        'Dim dir As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)

        'dir = System.IO.Path.Combine(dir, "../..")
        'dir = System.IO.Path.GetFullPath(dir)
        'System.Console.WriteLine(dir)

        '' using (System.Security.Cryptography.RSACryptoServiceProvider csp = new System.Security.Cryptography.RSACryptoServiceProvider())
        'Using csp As System.Security.Cryptography.RSACryptoServiceProvider = JWT.RSA.PEM.CreateRsaProvider()
        '    Dim privateKey As String = JWT.RSA.PEM.ExportPrivateKey(csp)
        '    System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "Private.txt"), privateKey, System.Text.Encoding.UTF8)
        '    Dim publicKey As String = JWT.RSA.PEM.ExportPublicKey(csp)
        '    System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "Public.txt"), publicKey, System.Text.Encoding.UTF8)
        'End Using
        ' End Using csp 
        ' object decodedJWTobject = JWT.JsonWebToken.DecodeToObject(jwtToken, key, true);
        ' Dim decodedJWTobject As User = JWT.JsonWebToken.DecodeToObject(Of User)(jwtToken, key, True)

        Dim origuser As User = New User()
        Dim decodedJWTobject As User = JWT.JsonWebToken.DecodeToObject(Of User)(jwtToken, "hello", True)
        System.Console.WriteLine(decodedJWTobject)
        System.Console.WriteLine(origuser)



        Dim decodedJWT As String = JWT.JsonWebToken.Decode(jwtToken, key, True)
        ' JWT.JsonWebToken.Decode(jwtToken, key3, true);
        ' JWT.JsonWebToken.Decode(jwtToken, token, true);
        ' JWT.JsonWebToken.Decode(jwtTokenRandKey, key, true);
        System.Console.WriteLine(decodedJWT)

        System.Console.WriteLine(System.Environment.NewLine)
        System.Console.WriteLine(" --- Press any key to continue --- ")
        System.Console.ReadKey()
    End Sub ' Main 

End Module
