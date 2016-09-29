Imports System

Namespace XXXX.PetaJson
	Public Class JsonParseException
		Inherits Exception

		Public Position As JsonLineOffset

		Public Context As String

		Public Sub New(inner As Exception, context As String, position As JsonLineOffset)
			MyBase.New(String.Format("JSON parse error at {0}{1} - {2}", position, If(String.IsNullOrEmpty(context), "", String.Format(", context {0}", context)), inner.Message), inner)
			Me.Position = position
			Me.Context = context
		End Sub
	End Class
End Namespace
