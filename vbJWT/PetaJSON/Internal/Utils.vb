Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Reflection

Namespace JWT.PetaJson.Internal
	Friend Module Utils
		Public Function GetAllFieldsAndProperties(t As Type) As IEnumerable(Of MemberInfo)
			Dim result As IEnumerable(Of MemberInfo)
			If t Is Nothing Then
				Dim lsMemberInfo As List(Of MemberInfo) = New List(Of MemberInfo)()
				result = lsMemberInfo
			Else
				Dim flags As BindingFlags = BindingFlags.DeclaredOnly Or BindingFlags.Instance Or BindingFlags.[Public] Or BindingFlags.NonPublic
				Dim fieldsAndProps As List(Of MemberInfo) = New List(Of MemberInfo)()
				Dim members As MemberInfo() = t.GetMembers(flags)
				For i As Integer = 0 To members.Length - 1
					Dim x As MemberInfo = members(i)
					If TypeOf x Is FieldInfo OrElse TypeOf x Is PropertyInfo Then
						fieldsAndProps.Add(x)
					End If
				Next
				Dim baseFieldsAndProps As IEnumerable(Of MemberInfo) = Utils.GetAllFieldsAndProperties(t.BaseType)
				fieldsAndProps.AddRange(baseFieldsAndProps)
				result = fieldsAndProps
			End If
			Return result
		End Function

		Public Function FindGenericInterface(type As Type, tItf As Type) As Type
			Dim interfaces As Type() = type.GetInterfaces()
			Dim result As Type
			For i As Integer = 0 To interfaces.Length - 1
				Dim t As Type = interfaces(i)
				If t.IsGenericType AndAlso t.GetGenericTypeDefinition() Is tItf Then
					result = t
					Return result
				End If
			Next
			result = Nothing
			Return result
		End Function

		Public Function IsPublic(mi As MemberInfo) As Boolean
			Dim fi As FieldInfo = TryCast(mi, FieldInfo)
			Dim result As Boolean
			If fi IsNot Nothing Then
				result = fi.IsPublic
			Else
				Dim pi As PropertyInfo = TryCast(mi, PropertyInfo)
				If pi IsNot Nothing Then
					Dim gm As MethodInfo = pi.GetGetMethod(True)
					result = (gm IsNot Nothing AndAlso gm.IsPublic)
				Else
					result = False
				End If
			End If
			Return result
		End Function

		Public Function ResolveInterfaceToClass(tItf As Type) As Type
			Dim result As Type
			If tItf.IsGenericType Then
				Dim genDef As Type = tItf.GetGenericTypeDefinition()
				If genDef Is GetType(IList(Of )) Then
					result = GetType(List(Of )).MakeGenericType(tItf.GetGenericArguments())
					Return result
				End If
				If genDef Is GetType(IDictionary(Of , )) AndAlso tItf.GetGenericArguments()(0) Is GetType(String) Then
					result = GetType(Dictionary(Of , )).MakeGenericType(tItf.GetGenericArguments())
					Return result
				End If
			End If
			If tItf Is GetType(IEnumerable) Then
				result = GetType(List(Of Object))
			Else If tItf Is GetType(IDictionary) Then
				result = GetType(Dictionary(Of String, Object))
			Else
				result = tItf
			End If
			Return result
		End Function

		Public Function ToUnixMilliseconds(This As DateTime) As Long
			Return CLng(This.Subtract(New DateTime(1970, 1, 1)).TotalMilliseconds)
		End Function

		Public Function FromUnixMilliseconds(timeStamp As Long) As DateTime
			Return New DateTime(1970, 1, 1).AddMilliseconds(CDec(timeStamp))
		End Function
	End Module
End Namespace
