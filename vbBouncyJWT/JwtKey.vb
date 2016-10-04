
Namespace BouncyJWT


    Public Class JwtKey

        Public MacKeyBytes As Byte()
        Public RsaPrivateKey As Org.BouncyCastle.Crypto.AsymmetricKeyParameter
        Public EcPrivateKey As Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters


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
            Me.RsaPrivateKey = rsaPrivateKey
        End Sub


        Public Sub New(ecPrivateKey As Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters)
            Me.EcPrivateKey = ecPrivateKey
        End Sub


        Public Property RSA As String
            Get
                Return Crypto.StringifyAsymmetricKey(Me.RsaPrivateKey)
            End Get
            Set(value As String)
                Me.RsaPrivateKey = Crypto.ReadPrivateKey(value)
            End Set
        End Property


    End Class


End Namespace
