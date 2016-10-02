
Imports System


Namespace XXXX.PetaJson


    <AttributeUsage(AttributeTargets.Enum)>
    Public Class JsonUnknownAttribute
        Inherits Attribute

        Public Property UnknownValue() As Object

        Public Sub New(unknownValue As Object)
            Me.UnknownValue = unknownValue
        End Sub
    End Class


End Namespace
