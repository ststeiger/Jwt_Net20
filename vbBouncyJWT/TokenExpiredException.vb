
Namespace BouncyJWT


    Public Class TokenExpiredException
        Inherits System.Exception
        Public Sub New(message As String)
            MyBase.New(message)
        End Sub
    End Class


    Public Class TokenAlgorithmRefusedException
        Inherits System.Exception
        Public Sub New()
            Me.New("Acceptance of the specified algorithm is denied." & vbLf & "Reason: Braindead JWT spec represents an unacceptable security risk." & vbLf)
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub
    End Class


    Public Class UnknownTokenAlgorithmException
        Inherits System.Exception
        Public Sub New(message As String)
            MyBase.New(message)
        End Sub
    End Class


End Namespace
