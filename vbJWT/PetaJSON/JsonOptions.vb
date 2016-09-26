Imports System

Namespace JWT.PetaJson
	<Flags()>
	Public Enum JsonOptions
		None = 0
		WriteWhitespace = 1
		DontWriteWhitespace = 2
		StrictParser = 4
		NonStrictParser = 8
	End Enum
End Namespace
