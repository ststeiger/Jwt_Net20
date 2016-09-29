Imports System
Imports System.Reflection

Namespace XXXX.PetaJson.Internal
	Public Class JsonMemberInfo
		Public JsonKey As String

		Public KeepInstance As Boolean

		Public Deprecated As Boolean

		Private _mi As MemberInfo

		Public SetValue As WriteCallback_t(Of Object, Object)

		Public GetValue As ReadCallback_t(Of Object, Object)

		Public Property Member() As MemberInfo
			Get
				Return Me._mi
			End Get
			Set(value As MemberInfo)
				Me._mi = value
				If TypeOf Me._mi Is PropertyInfo Then
                    Me.GetValue = (Function(obj As Object) (CType(Me._mi, PropertyInfo)).GetValue(obj, Nothing))
                    Me.SetValue = Sub(obj, val) DirectCast(_mi, PropertyInfo).SetValue(obj, val, Nothing)
				Else
					Me.GetValue = AddressOf(CType(Me._mi, FieldInfo)).GetValue
					Me.SetValue = AddressOf(CType(Me._mi, FieldInfo)).SetValue
				End If
			End Set
		End Property

		Public ReadOnly Property MemberType() As Type
			Get
				Dim result As Type
				If TypeOf Me.Member Is PropertyInfo Then
					result = (CType(Me.Member, PropertyInfo)).PropertyType
				Else
					result = (CType(Me.Member, FieldInfo)).FieldType
				End If
				Return result
			End Get
		End Property
	End Class
End Namespace
