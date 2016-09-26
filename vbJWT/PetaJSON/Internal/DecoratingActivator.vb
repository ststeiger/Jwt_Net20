Imports System

Namespace JWT.PetaJson.Internal
	Public Module DecoratingActivator
		Public Function CreateInstance(t As Type) As Object
			Dim result As Object
			Try
				result = Activator.CreateInstance(t)
			Catch x As Exception
				Throw New InvalidOperationException(String.Format("Failed to create instance of type '{0}'", t.FullName), x)
			End Try
			Return result
		End Function
	End Module
End Namespace
