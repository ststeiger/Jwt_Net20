Imports System
Imports System.Reflection

Namespace XXXX.PetaJson


    ' <Obfuscation(Exclude = True, ApplyToMembers = True)>
    Public Interface IJsonWriter
        Sub WriteStringLiteral(str As String)

        Sub WriteRaw(str As String)

        Sub WriteArray(callback As WriteCallback_t)

        Sub WriteDictionary(callback As WriteCallback_t)

        Sub WriteValue(value As Object)

        Sub WriteElement()

        Sub WriteKey(key As String)

        Sub WriteKeyNoEscaping(key As String)
    End Interface
End Namespace
