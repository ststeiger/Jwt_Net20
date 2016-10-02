
Namespace XXXX.PetaJson


    <System.AttributeUsage(System.AttributeTargets.Class Or System.AttributeTargets.Struct Or System.AttributeTargets.Property Or System.AttributeTargets.Field)>
    Public Class JsonAttribute
        Inherits System.Attribute


        Private _key As String


        Public ReadOnly Property Key() As String
            Get
                Return Me._key
            End Get
        End Property


        Public Property KeepInstance() As Boolean


        Public Property Deprecated() As Boolean


        Public Sub New()
            Me._key = Nothing
        End Sub


        Public Sub New(key As String)
            Me._key = key
        End Sub


    End Class


End Namespace
