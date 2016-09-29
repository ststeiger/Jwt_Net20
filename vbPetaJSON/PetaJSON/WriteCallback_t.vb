Imports System

Namespace XXXX.PetaJson
	Public Delegate Sub WriteCallback_t()

	Public Delegate Sub WriteCallback_t(Of In T)(obj As T)

	Public Delegate Sub WriteCallback_t(Of In T1, In T2)(arg1 As T1, arg2 As T2)
End Namespace
