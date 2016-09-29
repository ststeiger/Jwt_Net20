Imports System
Imports System.Collections.Generic

Namespace XXXX.PetaJson.Internal
	Public Class ThreadSafeCache(Of TKey, TValue)
		Private _map As Dictionary(Of TKey, TValue) = New Dictionary(Of TKey, TValue)()

		Public Function [Get](key As TKey, createIt As ReadCallback_t(Of TValue)) As TValue
			Dim result As TValue
			Try
				Dim val As TValue
				If Me._map.TryGetValue(key, val) Then
					result = val
					Return result
				End If
			Finally
			End Try
			Try
				Dim val As TValue
				If Not Me._map.TryGetValue(key, val) Then
					val = createIt()
					Me._map(key) = val
				End If
				result = val
			Finally
			End Try
			Return result
		End Function


        Public Function TryGetValue(key As TKey, ByRef val As TValue) As Boolean
            Dim result As Boolean
            Try
                result = Me._map.TryGetValue(key, val)
            Finally
            End Try
            Return result
        End Function

		Public Sub [Set](key As TKey, value As TValue)
			Try
				Me._map(key) = value
			Finally
			End Try
		End Sub
	End Class
End Namespace
