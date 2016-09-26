Imports System
Imports System.Reflection

Namespace JWT.PetaJson
    ' <Obfuscation(Exclude = True, ApplyToMembers = True)>
    Public Interface IJsonLoadField
        Function OnJsonField(r As IJsonReader, key As String) As Boolean
    End Interface
End Namespace
