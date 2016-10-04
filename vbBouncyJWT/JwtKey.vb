
Namespace BouncyJWT


    Public Class JwtKey

        Public MacKeyBytes As Byte()
        Public PrivateKey As Org.BouncyCastle.Crypto.AsymmetricKeyParameter



        Public Property MacKey() As String
            Get
                Return System.Text.Encoding.UTF8.GetString(Me.MacKeyBytes)
            End Get
            Set(value As String)
                Me.MacKeyBytes = System.Text.Encoding.UTF8.GetBytes(value)
            End Set
        End Property


        Public Sub New()
        End Sub


        Public Sub New(macKey As String)
            Me.MacKey = macKey
        End Sub


        Public Sub New(macKey As Byte())
            Me.MacKeyBytes = macKey
        End Sub


        Public Sub New(rsaPrivateKey As Org.BouncyCastle.Crypto.AsymmetricKeyParameter)
            Me.PrivateKey = rsaPrivateKey
        End Sub


        Public Property PemPrivateKey() As String
            Get
                Return Crypto.StringifyAsymmetricKey(Me.PrivateKey)
            End Get
            Set(value As String)
                Me.PrivateKey = Crypto.ReadPrivateKey(value)
            End Set
        End Property


    End Class


End Namespace
