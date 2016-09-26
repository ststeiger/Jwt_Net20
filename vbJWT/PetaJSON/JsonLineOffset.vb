Imports System

Namespace JWT.PetaJson
	Public Structure JsonLineOffset
		Public Line As Integer

		Public Offset As Integer

		Public Overrides Function ToString() As String
			Return String.Format("line {0}, character {1}", Me.Line + 1, Me.Offset + 1)
		End Function
	End Structure
End Namespace
