Imports System
Imports System.Collections.Generic
Imports System.Reflection


Imports XXXX.PetaJson

Namespace XXXX.PetaJson.Internal
	Public Class ReflectionInfo
		Public Members As List(Of JsonMemberInfo)

		Private Shared _cache As ThreadSafeCache(Of Type, ReflectionInfo) = New ThreadSafeCache(Of Type, ReflectionInfo)()

		Private _lastFoundIndex As Integer = 0

		Public Shared Function FindFormatJson(type As Type) As MethodInfo
			Dim result As MethodInfo
			If type.IsValueType Then
				Dim formatJson As MethodInfo = type.GetMethod("FormatJson", BindingFlags.Instance Or BindingFlags.[Public] Or BindingFlags.NonPublic, Nothing, New Type() { GetType(IJsonWriter) }, Nothing)
				If formatJson IsNot Nothing AndAlso formatJson.ReturnType Is GetType(Void) Then
					result = formatJson
					Return result
				End If
				formatJson = type.GetMethod("FormatJson", BindingFlags.Instance Or BindingFlags.[Public] Or BindingFlags.NonPublic, Nothing, New Type(-1) {}, Nothing)
				If formatJson IsNot Nothing AndAlso formatJson.ReturnType Is GetType(String) Then
					result = formatJson
					Return result
				End If
			End If
			result = Nothing
			Return result
		End Function

		Public Shared Function FindParseJson(type As Type) As MethodInfo
			Dim parseJson As MethodInfo = type.GetMethod("ParseJson", BindingFlags.[Static] Or BindingFlags.[Public] Or BindingFlags.NonPublic, Nothing, New Type() { GetType(IJsonReader) }, Nothing)
			Dim result As MethodInfo
			If parseJson IsNot Nothing AndAlso parseJson.ReturnType Is type Then
				result = parseJson
			Else
				parseJson = type.GetMethod("ParseJson", BindingFlags.[Static] Or BindingFlags.[Public] Or BindingFlags.NonPublic, Nothing, New Type() { GetType(String) }, Nothing)
				If parseJson IsNot Nothing AndAlso parseJson.ReturnType Is type Then
					result = parseJson
				Else
					result = Nothing
				End If
			End If
			Return result
		End Function

		Public Sub Write(w As IJsonWriter, val As Object)
			w.WriteDictionary(Sub()
				Dim writing As IJsonWriting = TryCast(val, IJsonWriting)
				If writing IsNot Nothing Then
					writing.OnJsonWriting(w)
				End If
				For Each jmi As JsonMemberInfo In Me.Members
					If Not jmi.Deprecated Then
						w.WriteKeyNoEscaping(jmi.JsonKey)
						w.WriteValue(jmi.GetValue(val))
					End If
				Next
				Dim written As IJsonWritten = TryCast(val, IJsonWritten)
				If written IsNot Nothing Then
					written.OnJsonWritten(w)
				End If
			End Sub)
		End Sub

		Public Sub ParseInto(r As IJsonReader, into As Object)
			Dim loading As IJsonLoading = TryCast(into, IJsonLoading)
			If loading IsNot Nothing Then
				loading.OnJsonLoading(r)
			End If
			r.ParseDictionary(Sub(key As String)
				Me.ParseFieldOrProperty(r, into, key)
			End Sub)
			Dim loaded As IJsonLoaded = TryCast(into, IJsonLoaded)
			If loaded IsNot Nothing Then
				loaded.OnJsonLoaded(r)
			End If
		End Sub

        Private Function FindMemberInfo(name As String, ByRef found As JsonMemberInfo) As Boolean
            Dim result As Boolean
            For i As Integer = 0 To Me.Members.Count - 1
                Dim index As Integer = (i + Me._lastFoundIndex) Mod Me.Members.Count
                Dim jmi As JsonMemberInfo = Me.Members(index)
                If jmi.JsonKey = name Then
                    Me._lastFoundIndex = index
                    found = jmi
                    result = True
                    Return result
                End If
            Next
            found = Nothing
            result = False
            Return result
        End Function

		Public Sub ParseFieldOrProperty(r As IJsonReader, into As Object, key As String)
			Dim lf As IJsonLoadField = TryCast(into, IJsonLoadField)
			If lf Is Nothing OrElse Not lf.OnJsonField(r, key) Then
				Dim jmi As JsonMemberInfo
				If Me.FindMemberInfo(key, jmi) Then
					If jmi.KeepInstance Then
						Dim subInto As Object = jmi.GetValue(into)
						If subInto IsNot Nothing Then
							r.ParseInto(subInto)
							Return
						End If
					End If
					Dim val As Object = r.Parse(jmi.MemberType)
					jmi.SetValue(into, val)
				End If
			End If
		End Sub

		Public Shared Function GetReflectionInfo(type As Type) As ReflectionInfo
            Return ReflectionInfo._cache.[Get](type, Function()
                                                         Dim allMembers As IEnumerable(Of MemberInfo) = Utils.GetAllFieldsAndProperties(type)

                                                         Dim typeMarked As Boolean = XXXX.PetaJson.Enumerable.Any(Of JsonAttribute)(XXXX.PetaJson.Enumerable.OfType(Of JsonAttribute)(type.GetCustomAttributes(GetType(JsonAttribute), True)))

                                                         'Dim anyFieldsMarked As Boolean = allMembers.Any(Function(x As MemberInfo) x.GetCustomAttributes(GetType(JsonAttribute), False).OfType(Of JsonAttribute)().Any(Of JsonAttribute)())
                                                         Dim anyFieldsMarked As Boolean = allMembers.Any(Function(x As MemberInfo) XXXX.PetaJson.Enumerable.Any(Of JsonAttribute)(XXXX.PetaJson.Enumerable.OfType(Of JsonAttribute)(x.GetCustomAttributes(GetType(JsonAttribute), False))))


                                                         Dim serializeAllPublics As Boolean = typeMarked OrElse Not anyFieldsMarked
                                                         Return ReflectionInfo.CreateReflectionInfo(type, Function(mi As MemberInfo)
                                                                                                              Dim result As JsonMemberInfo

                                                                                                              If XXXX.PetaJson.Enumerable.Any(Of Object)(mi.GetCustomAttributes(GetType(JsonExcludeAttribute), False)) Then
                                                                                                                  result = Nothing
                                                                                                              Else
                                                                                                                  Dim attr As JsonAttribute = XXXX.PetaJson.Enumerable.FirstOrDefault(Of JsonAttribute)(XXXX.PetaJson.Enumerable.OfType(Of JsonAttribute)(mi.GetCustomAttributes(GetType(JsonAttribute), False)))

                                                                                                                  If attr IsNot Nothing Then
                                                                                                                      result = New JsonMemberInfo() With {.Member = mi, .JsonKey = (If(attr.Key, (mi.Name.Substring(0, 1).ToLower() + mi.Name.Substring(1)))), .KeepInstance = attr.KeepInstance, .Deprecated = attr.Deprecated}
                                                                                                                  ElseIf serializeAllPublics AndAlso Utils.IsPublic(mi) Then
                                                                                                                      result = New JsonMemberInfo() With {.Member = mi, .JsonKey = mi.Name.Substring(0, 1).ToLower() + mi.Name.Substring(1)}
                                                                                                                  Else
                                                                                                                      result = Nothing
                                                                                                                  End If
                                                                                                              End If
                                                                                                              Return result
                                                                                                          End Function)
                                                     End Function)
		End Function

		Public Shared Function CreateReflectionInfo(type As Type, callback As ReadCallback_t(Of MemberInfo, JsonMemberInfo)) As ReflectionInfo
			Dim members As List(Of JsonMemberInfo) = New List(Of JsonMemberInfo)()
			For Each thisMember As MemberInfo In Utils.GetAllFieldsAndProperties(type)
				Dim mi As JsonMemberInfo = callback(thisMember)
				If mi IsNot Nothing Then
					members.Add(mi)
				End If
			Next
			Dim invalid As JsonMemberInfo = members.FirstOrDefault(Function(x As JsonMemberInfo) x.KeepInstance AndAlso x.MemberType.IsValueType)
			If invalid IsNot Nothing Then
				Throw New InvalidOperationException(String.Format("KeepInstance=true can only be applied to reference types ({0}.{1})", type.FullName, invalid.Member))
			End If
            Dim result As ReflectionInfo



            If Not XXXX.PetaJson.Enumerable.Any(Of JsonMemberInfo)(members) Then
                result = Nothing
            Else
                result = New ReflectionInfo() With {.Members = members}
            End If
			Return result
		End Function
	End Class
End Namespace
