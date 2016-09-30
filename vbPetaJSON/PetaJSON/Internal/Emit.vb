Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Reflection.Emit

Namespace XXXX.PetaJson.Internal
	Friend Module Emit
		Private Interface IPseudoBox
			Function GetValue() As Object
		End Interface

        ' <Obfuscation(Exclude = True, ApplyToMembers = True)>
        Private Class PseudoBox(Of T As Structure)
            Implements Emit.IPseudoBox

            Public value As T = Nothing

            Function GetValue() As Object Implements Emit.IPseudoBox.GetValue
                Return Me.value
            End Function
        End Class

		<System.Runtime.CompilerServices.ExtensionAttribute()>
		Private Function TypeArrayContains(types As Type(), type As Type) As Boolean
			Dim result As Boolean
			For i As Integer = 0 To types.Length - 1
				If types(i) Is type Then
					result = True
					Return result
				End If
			Next
			result = False
			Return result
		End Function

		Public Function MakeFormatter(type As Type) As WriteCallback_t(Of IJsonWriter, Object)
			Dim formatJson As MethodInfo = ReflectionInfo.FindFormatJson(type)
			Dim result As WriteCallback_t(Of IJsonWriter, Object)
			If formatJson IsNot Nothing Then
				Dim method As DynamicMethod = New DynamicMethod("invoke_formatJson", Nothing, New Type() { GetType(IJsonWriter), GetType(Object) }, True)
				Dim il As ILGenerator = method.GetILGenerator()
				If formatJson.ReturnType Is GetType(String) Then
					il.Emit(OpCodes.Ldarg_0)
					il.Emit(OpCodes.Ldarg_1)
					il.Emit(OpCodes.Unbox, type)
					il.Emit(OpCodes.[Call], formatJson)
					il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteStringLiteral"))
				Else
					il.Emit(OpCodes.Ldarg_1)
					il.Emit(If(type.IsValueType, OpCodes.Unbox, OpCodes.Castclass), type)
					il.Emit(OpCodes.Ldarg_0)
					il.Emit(If(type.IsValueType, OpCodes.[Call], OpCodes.Callvirt), formatJson)
				End If
				il.Emit(OpCodes.Ret)
				result = CType(method.CreateDelegate(GetType(WriteCallback_t(Of IJsonWriter, Object))), WriteCallback_t(Of IJsonWriter, Object))
			Else
				Dim ri As ReflectionInfo = ReflectionInfo.GetReflectionInfo(type)
				If ri Is Nothing Then
					result = Nothing
				Else
					Dim method As DynamicMethod = New DynamicMethod("dynamic_formatter", Nothing, New Type() { GetType(IJsonWriter), GetType(Object) }, True)
					Dim il As ILGenerator = method.GetILGenerator()
					Dim locTypedObj As LocalBuilder = il.DeclareLocal(type)
					il.Emit(OpCodes.Ldarg_1)
					il.Emit(If(type.IsValueType, OpCodes.Unbox_Any, OpCodes.Castclass), type)
					il.Emit(OpCodes.Stloc, locTypedObj)
					Dim locInvariant As LocalBuilder = il.DeclareLocal(GetType(IFormatProvider))
					il.Emit(OpCodes.[Call], GetType(CultureInfo).GetProperty("InvariantCulture").GetGetMethod())
					il.Emit(OpCodes.Stloc, locInvariant)
					Dim toStringTypes As Type() = New Type() { GetType(Integer), GetType(UInteger), GetType(Long), GetType(ULong), GetType(Short), GetType(UShort), GetType(Decimal), GetType(Byte), GetType(SByte) }
					Dim otherSupportedTypes As Type() = New Type() { GetType(Double), GetType(Single), GetType(String), GetType(Char) }
					If GetType(IJsonWriting).IsAssignableFrom(type) Then
						If type.IsValueType Then
							il.Emit(OpCodes.Ldloca, locTypedObj)
							il.Emit(OpCodes.Ldarg_0)
							il.Emit(OpCodes.[Call], type.GetInterfaceMap(GetType(IJsonWriting)).TargetMethods(0))
						Else
							il.Emit(OpCodes.Ldloc, locTypedObj)
							il.Emit(OpCodes.Castclass, GetType(IJsonWriting))
							il.Emit(OpCodes.Ldarg_0)
							il.Emit(OpCodes.Callvirt, GetType(IJsonWriting).GetMethod("OnJsonWriting", New Type() { GetType(IJsonWriter) }))
						End If
					End If
					For Each i As JsonMemberInfo In ri.Members
						If Not i.Deprecated Then
							Dim pi As PropertyInfo = TryCast(i.Member, PropertyInfo)
							If Not(pi IsNot Nothing) OrElse Not(pi.GetGetMethod(True) Is Nothing) Then
								il.Emit(OpCodes.Ldarg_0)
								il.Emit(OpCodes.Ldstr, i.JsonKey)
								il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteKeyNoEscaping", New Type() { GetType(String) }))
								il.Emit(OpCodes.Ldarg_0)
								Dim memberType As Type = i.MemberType
								If type.IsValueType Then
									il.Emit(OpCodes.Ldloca, locTypedObj)
								Else
									il.Emit(OpCodes.Ldloc, locTypedObj)
								End If
								Dim NeedValueAddress As Boolean = memberType.IsValueType AndAlso (toStringTypes.TypeArrayContains(memberType) OrElse otherSupportedTypes.TypeArrayContains(memberType))
								If Nullable.GetUnderlyingType(memberType) IsNot Nothing Then
									NeedValueAddress = True
								End If
								If pi IsNot Nothing Then
									If type.IsValueType Then
										il.Emit(OpCodes.[Call], pi.GetGetMethod(True))
									Else
										il.Emit(OpCodes.Callvirt, pi.GetGetMethod(True))
									End If
									If NeedValueAddress Then
										Dim locTemp As LocalBuilder = il.DeclareLocal(memberType)
										il.Emit(OpCodes.Stloc, locTemp)
										il.Emit(OpCodes.Ldloca, locTemp)
									End If
								End If
								Dim fi As FieldInfo = TryCast(i.Member, FieldInfo)
								If fi IsNot Nothing Then
									If NeedValueAddress Then
										il.Emit(OpCodes.Ldflda, fi)
									Else
										il.Emit(OpCodes.Ldfld, fi)
									End If
								End If
								Dim lblFinished As Label? = Nothing
								Dim typeUnderlying As Type = Nullable.GetUnderlyingType(memberType)
								If typeUnderlying IsNot Nothing Then
									il.Emit(OpCodes.Dup)
									Dim lblHasValue As Label = il.DefineLabel()
									lblFinished = New Label?(il.DefineLabel())
									il.Emit(OpCodes.[Call], memberType.GetProperty("HasValue").GetGetMethod())
									il.Emit(OpCodes.Brtrue, lblHasValue)
									il.Emit(OpCodes.Pop)
									il.Emit(OpCodes.Ldstr, "null")
									il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteRaw", New Type() { GetType(String) }))
									il.Emit(OpCodes.Br_S, lblFinished.Value)
									il.MarkLabel(lblHasValue)
									il.Emit(OpCodes.[Call], memberType.GetProperty("Value").GetGetMethod())
									memberType = typeUnderlying
									NeedValueAddress = (memberType.IsValueType AndAlso (toStringTypes.TypeArrayContains(memberType) OrElse otherSupportedTypes.TypeArrayContains(memberType)))
									If NeedValueAddress Then
										Dim locTemp As LocalBuilder = il.DeclareLocal(memberType)
										il.Emit(OpCodes.Stloc, locTemp)
										il.Emit(OpCodes.Ldloca, locTemp)
									End If
								End If
								If toStringTypes.TypeArrayContains(memberType) Then
									il.Emit(OpCodes.Ldloc, locInvariant)
									il.Emit(OpCodes.[Call], memberType.GetMethod("ToString", New Type() { GetType(IFormatProvider) }))
									il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteRaw", New Type() { GetType(String) }))
								Else If memberType Is GetType(Single) OrElse memberType Is GetType(Double) Then
									il.Emit(OpCodes.Ldstr, "R")
									il.Emit(OpCodes.Ldloc, locInvariant)
									il.Emit(OpCodes.[Call], memberType.GetMethod("ToString", New Type() { GetType(String), GetType(IFormatProvider) }))
									il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteRaw", New Type() { GetType(String) }))
								Else If memberType Is GetType(String) Then
									il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteStringLiteral", New Type() { GetType(String) }))
								Else If memberType Is GetType(Char) Then
									il.Emit(OpCodes.[Call], memberType.GetMethod("ToString", New Type(-1) {}))
									il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteStringLiteral", New Type() { GetType(String) }))
								Else If memberType Is GetType(Boolean) Then
									Dim lblTrue As Label = il.DefineLabel()
									Dim lblCont As Label = il.DefineLabel()
									il.Emit(OpCodes.Brtrue_S, lblTrue)
									il.Emit(OpCodes.Ldstr, "false")
									il.Emit(OpCodes.Br_S, lblCont)
									il.MarkLabel(lblTrue)
									il.Emit(OpCodes.Ldstr, "true")
									il.MarkLabel(lblCont)
									il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteRaw", New Type() { GetType(String) }))
								Else
									If memberType.IsValueType Then
										il.Emit(OpCodes.Box, memberType)
									End If
									il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteValue", New Type() { GetType(Object) }))
								End If
								If lblFinished.HasValue Then
									il.MarkLabel(lblFinished.Value)
								End If
							End If
						End If
					Next
					If GetType(IJsonWritten).IsAssignableFrom(type) Then
						If type.IsValueType Then
							il.Emit(OpCodes.Ldloca, locTypedObj)
							il.Emit(OpCodes.Ldarg_0)
							il.Emit(OpCodes.[Call], type.GetInterfaceMap(GetType(IJsonWritten)).TargetMethods(0))
						Else
							il.Emit(OpCodes.Ldloc, locTypedObj)
							il.Emit(OpCodes.Castclass, GetType(IJsonWriting))
							il.Emit(OpCodes.Ldarg_0)
							il.Emit(OpCodes.Callvirt, GetType(IJsonWriting).GetMethod("OnJsonWritten", New Type() { GetType(IJsonWriter) }))
						End If
					End If
					il.Emit(OpCodes.Ret)
					Dim impl As WriteCallback_t(Of IJsonWriter, Object) = CType(method.CreateDelegate(GetType(WriteCallback_t(Of IJsonWriter, Object))), WriteCallback_t(Of IJsonWriter, Object))
					result = Sub(w As IJsonWriter, obj As Object)
						w.WriteDictionary(Sub()
							impl(w, obj)
						End Sub)
					End Sub
				End If
			End If
			Return result
		End Function

		Public Function MakeParser(type As Type) As ReadCallback_t(Of IJsonReader, Type, Object)
			Debug.Assert(type.IsValueType)
			Dim parseJson As MethodInfo = ReflectionInfo.FindParseJson(type)
			Dim result As ReadCallback_t(Of IJsonReader, Type, Object)
			If parseJson IsNot Nothing Then
				If parseJson.GetParameters()(0).ParameterType Is GetType(IJsonReader) Then
					Dim method As DynamicMethod = New DynamicMethod("invoke_ParseJson", GetType(Object), New Type() { GetType(IJsonReader), GetType(Type) }, True)
					Dim il As ILGenerator = method.GetILGenerator()
					il.Emit(OpCodes.Ldarg_0)
					il.Emit(OpCodes.[Call], parseJson)
					il.Emit(OpCodes.Box, type)
					il.Emit(OpCodes.Ret)
					result = CType(method.CreateDelegate(GetType(ReadCallback_t(Of IJsonReader, Type, Object))), ReadCallback_t(Of IJsonReader, Type, Object))
				Else
					Dim method As DynamicMethod = New DynamicMethod("invoke_ParseJson", GetType(Object), New Type() { GetType(String) }, True)
					Dim il As ILGenerator = method.GetILGenerator()
					il.Emit(OpCodes.Ldarg_0)
					il.Emit(OpCodes.[Call], parseJson)
					il.Emit(OpCodes.Box, type)
					il.Emit(OpCodes.Ret)
					Dim invoke As ReadCallback_t(Of String, Object) = CType(method.CreateDelegate(GetType(ReadCallback_t(Of String, Object))), ReadCallback_t(Of String, Object))
                    result = Function(r As IJsonReader, t As Type)
                                                If r.GetLiteralKind() = LiteralKind.[String] Then
                                                    Dim o As Object = invoke(r.GetLiteralString())
                                                    r.NextToken()
                                                    Return o
                                                End If
                                                Throw New InvalidDataException(String.Format("Expected string literal for type {0}", type.FullName))
                                            End Function
				End If
			Else
				Dim ri As ReflectionInfo = ReflectionInfo.GetReflectionInfo(type)
				If ri Is Nothing Then
					result = Nothing
				Else
					Dim setters As Dictionary(Of String, WriteCallback_t(Of IJsonReader, Object)) = New Dictionary(Of String, WriteCallback_t(Of IJsonReader, Object))()
					Dim boxType As Type = GetType(Emit.PseudoBox(Of )).MakeGenericType(New Type() { type })
					For Each i As JsonMemberInfo In ri.Members
						Dim pi As PropertyInfo = TryCast(i.Member, PropertyInfo)
						Dim fi As FieldInfo = TryCast(i.Member, FieldInfo)
						If Not(pi IsNot Nothing) OrElse Not(pi.GetSetMethod(True) Is Nothing) Then
							Dim method As DynamicMethod = New DynamicMethod("dynamic_parser", Nothing, New Type() { GetType(IJsonReader), GetType(Object) }, True)
							Dim il As ILGenerator = method.GetILGenerator()
							il.Emit(OpCodes.Ldarg_1)
							il.Emit(OpCodes.Castclass, boxType)
							il.Emit(OpCodes.Ldflda, boxType.GetField("value"))
							Emit.GenerateGetJsonValue(i, il)
							If pi IsNot Nothing Then
								il.Emit(OpCodes.[Call], pi.GetSetMethod(True))
							End If
							If fi IsNot Nothing Then
								il.Emit(OpCodes.Stfld, fi)
							End If
							il.Emit(OpCodes.Ret)
							setters.Add(i.JsonKey, CType(method.CreateDelegate(GetType(WriteCallback_t(Of IJsonReader, Object))), WriteCallback_t(Of IJsonReader, Object)))
						End If
					Next
					Dim invokeLoading As WriteCallback_t(Of Object, IJsonReader) = Emit.MakeInterfaceCall(type, GetType(IJsonLoading))
					Dim invokeLoaded As WriteCallback_t(Of Object, IJsonReader) = Emit.MakeInterfaceCall(type, GetType(IJsonLoaded))
					Dim invokeField As ReadCallback_t(Of Object, IJsonReader, String, Boolean) = Emit.MakeLoadFieldCall(type)
                    Dim parser As ReadCallback_t(Of IJsonReader, Type, Object) = Function(reader As IJsonReader, ttype As Type)
                                                                                     Dim box As Object = DecoratingActivator.CreateInstance(boxType)
                                                                                     If invokeLoading IsNot Nothing Then
                                                                                         invokeLoading(box, reader)
                                                                                     End If
                                                                                     reader.ParseDictionary(Sub(key As String)
                                                                                                                If invokeField Is Nothing OrElse Not invokeField(box, reader, key) Then
                                                                                                                    Dim setter As WriteCallback_t(Of IJsonReader, Object) = Nothing
                                                                                                                    If setters.TryGetValue(key, setter) Then
                                                                                                                        setter(reader, box)
                                                                                                                    End If
                                                                                                                End If
                                                                                                            End Sub)
                                                                                     If invokeLoaded IsNot Nothing Then
                                                                                         invokeLoaded(box, reader)
                                                                                     End If
                                                                                     Return (CType(box, Emit.IPseudoBox)).GetValue()
                                                                                 End Function
					result = parser
				End If
			End If
			Return result
		End Function

		Private Function MakeInterfaceCall(type As Type, tItf As Type) As WriteCallback_t(Of Object, IJsonReader)
			Dim result As WriteCallback_t(Of Object, IJsonReader)
			If Not tItf.IsAssignableFrom(type) Then
				result = Nothing
			Else
				Dim boxType As Type = GetType(Emit.PseudoBox(Of )).MakeGenericType(New Type() { type })
				Dim method As DynamicMethod = New DynamicMethod("dynamic_invoke_" + tItf.Name, Nothing, New Type() { GetType(Object), GetType(IJsonReader) }, True)
				Dim il As ILGenerator = method.GetILGenerator()
				il.Emit(OpCodes.Ldarg_0)
				il.Emit(OpCodes.Castclass, boxType)
				il.Emit(OpCodes.Ldflda, boxType.GetField("value"))
				il.Emit(OpCodes.Ldarg_1)
				il.Emit(OpCodes.[Call], type.GetInterfaceMap(tItf).TargetMethods(0))
				il.Emit(OpCodes.Ret)
				result = CType(method.CreateDelegate(GetType(WriteCallback_t(Of Object, IJsonReader))), WriteCallback_t(Of Object, IJsonReader))
			End If
			Return result
		End Function

		Private Function MakeLoadFieldCall(type As Type) As ReadCallback_t(Of Object, IJsonReader, String, Boolean)
			Dim tItf As Type = GetType(IJsonLoadField)
			Dim result As ReadCallback_t(Of Object, IJsonReader, String, Boolean)
			If Not tItf.IsAssignableFrom(type) Then
				result = Nothing
			Else
				Dim boxType As Type = GetType(Emit.PseudoBox(Of )).MakeGenericType(New Type() { type })
				Dim method As DynamicMethod = New DynamicMethod("dynamic_invoke_" + tItf.Name, GetType(Boolean), New Type() { GetType(Object), GetType(IJsonReader), GetType(String) }, True)
				Dim il As ILGenerator = method.GetILGenerator()
				il.Emit(OpCodes.Ldarg_0)
				il.Emit(OpCodes.Castclass, boxType)
				il.Emit(OpCodes.Ldflda, boxType.GetField("value"))
				il.Emit(OpCodes.Ldarg_1)
				il.Emit(OpCodes.Ldarg_2)
				il.Emit(OpCodes.[Call], type.GetInterfaceMap(tItf).TargetMethods(0))
				il.Emit(OpCodes.Ret)
				result = CType(method.CreateDelegate(GetType(ReadCallback_t(Of Object, IJsonReader, String, Boolean))), ReadCallback_t(Of Object, IJsonReader, String, Boolean))
			End If
			Return result
		End Function

		Public Function MakeIntoParser(type As Type) As WriteCallback_t(Of IJsonReader, Object)
			Debug.Assert(Not type.IsValueType)
			Dim ri As ReflectionInfo = ReflectionInfo.GetReflectionInfo(type)
			Dim result As WriteCallback_t(Of IJsonReader, Object)
			If ri Is Nothing Then
				result = Nothing
			Else
				Dim setters As Dictionary(Of String, WriteCallback_t(Of IJsonReader, Object)) = New Dictionary(Of String, WriteCallback_t(Of IJsonReader, Object))()
				For Each i As JsonMemberInfo In ri.Members
					Dim pi As PropertyInfo = TryCast(i.Member, PropertyInfo)
					Dim fi As FieldInfo = TryCast(i.Member, FieldInfo)
					If Not(pi IsNot Nothing) OrElse Not(pi.GetSetMethod(True) Is Nothing) Then
						If Not(pi IsNot Nothing) OrElse Not(pi.GetGetMethod(True) Is Nothing) OrElse Not i.KeepInstance Then
							Dim method As DynamicMethod = New DynamicMethod("dynamic_parser", Nothing, New Type() { GetType(IJsonReader), GetType(Object) }, True)
							Dim il As ILGenerator = method.GetILGenerator()
							il.Emit(OpCodes.Ldarg_1)
							il.Emit(OpCodes.Castclass, type)
							If i.KeepInstance Then
								il.Emit(OpCodes.Dup)
								If pi IsNot Nothing Then
									il.Emit(OpCodes.Callvirt, pi.GetGetMethod(True))
								Else
									il.Emit(OpCodes.Ldfld, fi)
								End If
								Dim existingInstance As LocalBuilder = il.DeclareLocal(i.MemberType)
								Dim lblExistingInstanceNull As Label = il.DefineLabel()
								il.Emit(OpCodes.Dup)
								il.Emit(OpCodes.Stloc, existingInstance)
								il.Emit(OpCodes.Ldnull)
								il.Emit(OpCodes.Ceq)
								il.Emit(OpCodes.Brtrue_S, lblExistingInstanceNull)
								il.Emit(OpCodes.Ldarg_0)
								il.Emit(OpCodes.Ldloc, existingInstance)
								il.Emit(OpCodes.Callvirt, GetType(IJsonReader).GetMethod("ParseInto", New Type() { GetType(Object) }))
								il.Emit(OpCodes.Pop)
								il.Emit(OpCodes.Ret)
								il.MarkLabel(lblExistingInstanceNull)
							End If
							Emit.GenerateGetJsonValue(i, il)
							If pi IsNot Nothing Then
								il.Emit(OpCodes.Callvirt, pi.GetSetMethod(True))
							End If
							If fi IsNot Nothing Then
								il.Emit(OpCodes.Stfld, fi)
							End If
							il.Emit(OpCodes.Ret)
							setters.Add(i.JsonKey, CType(method.CreateDelegate(GetType(WriteCallback_t(Of IJsonReader, Object))), WriteCallback_t(Of IJsonReader, Object)))
						End If
					End If
				Next
                Dim parseInto As WriteCallback_t(Of IJsonReader, Object) = Sub(reader As IJsonReader, obj As Object)
                                                                                           Dim loading As IJsonLoading = TryCast(obj, IJsonLoading)
                                                                                           If loading IsNot Nothing Then
                                                                                               loading.OnJsonLoading(reader)
                                                                                           End If
                                                                                           Dim lf As IJsonLoadField = TryCast(obj, IJsonLoadField)
                                                                                           reader.ParseDictionary(Sub(key As String)
                                                                                                                      If lf Is Nothing OrElse Not lf.OnJsonField(reader, key) Then
                                                                                                                          Dim setter As WriteCallback_t(Of IJsonReader, Object) = Nothing
                                                                                                                          If setters.TryGetValue(key, setter) Then
                                                                                                                              setter(reader, obj)
                                                                                                                          End If
                                                                                                                      End If
                                                                                                                  End Sub)
                                                                                           Dim loaded As IJsonLoaded = TryCast(obj, IJsonLoaded)
                                                                                           If loaded IsNot Nothing Then
                                                                                               loaded.OnJsonLoaded(reader)
                                                                                           End If
                                                                                       End Sub
				Emit.RegisterIntoParser(type, parseInto)
				result = parseInto
			End If
			Return result
		End Function

		Private Sub RegisterIntoParser(type As Type, parseInto As WriteCallback_t(Of IJsonReader, Object))
			Dim con As ConstructorInfo = type.GetConstructor(BindingFlags.Instance Or BindingFlags.[Public] Or BindingFlags.NonPublic, Nothing, New Type(-1) {}, Nothing)
			If Not(con Is Nothing) Then
				Dim method As DynamicMethod = New DynamicMethod("dynamic_factory", GetType(Object), New Type() { GetType(IJsonReader), GetType(WriteCallback_t(Of IJsonReader, Object)) }, True)
				Dim il As ILGenerator = method.GetILGenerator()
				Dim locObj As LocalBuilder = il.DeclareLocal(GetType(Object))
				il.Emit(OpCodes.Newobj, con)
				il.Emit(OpCodes.Dup)
				il.Emit(OpCodes.Stloc, locObj)
				il.Emit(OpCodes.Ldarg_1)
				il.Emit(OpCodes.Ldarg_0)
				il.Emit(OpCodes.Ldloc, locObj)
				il.Emit(OpCodes.Callvirt, GetType(WriteCallback_t(Of IJsonReader, Object)).GetMethod("Invoke"))
				il.Emit(OpCodes.Ret)
				Dim factory As ReadCallback_t(Of IJsonReader, WriteCallback_t(Of IJsonReader, Object), Object) = CType(method.CreateDelegate(GetType(ReadCallback_t(Of IJsonReader, WriteCallback_t(Of IJsonReader, Object), Object))), ReadCallback_t(Of IJsonReader, WriteCallback_t(Of IJsonReader, Object), Object))
				Json.RegisterParser(type, Function(reader As IJsonReader, type2 As Type) factory(reader, parseInto))
			End If
		End Sub

		Private Sub GenerateGetJsonValue(m As JsonMemberInfo, il As ILGenerator)
			Dim generateCallToHelper As WriteCallback_t(Of String) = Sub(helperName As String)
				il.Emit(OpCodes.Ldarg_0)
				il.Emit(OpCodes.[Call], GetType(Emit).GetMethod(helperName, New Type() { GetType(IJsonReader) }))
				il.Emit(OpCodes.Ldarg_0)
				il.Emit(OpCodes.Callvirt, GetType(IJsonReader).GetMethod("NextToken", New Type(-1) {}))
			End Sub
			Dim numericTypes As Type() = New Type() { GetType(Integer), GetType(UInteger), GetType(Long), GetType(ULong), GetType(Short), GetType(UShort), GetType(Decimal), GetType(Byte), GetType(SByte), GetType(Double), GetType(Single) }
			If m.MemberType Is GetType(String) Then
				generateCallToHelper("GetLiteralString")
			Else If m.MemberType Is GetType(Boolean) Then
				generateCallToHelper("GetLiteralBool")
			Else If m.MemberType Is GetType(Char) Then
				generateCallToHelper("GetLiteralChar")
			Else If numericTypes.TypeArrayContains(m.MemberType) Then
				il.Emit(OpCodes.Ldarg_0)
				il.Emit(OpCodes.[Call], GetType(Emit).GetMethod("GetLiteralNumber", New Type() { GetType(IJsonReader) }))
				il.Emit(OpCodes.[Call], GetType(CultureInfo).GetProperty("InvariantCulture").GetGetMethod())
				il.Emit(OpCodes.[Call], m.MemberType.GetMethod("Parse", New Type() { GetType(String), GetType(IFormatProvider) }))
				il.Emit(OpCodes.Ldarg_0)
				il.Emit(OpCodes.Callvirt, GetType(IJsonReader).GetMethod("NextToken", New Type(-1) {}))
			Else
				il.Emit(OpCodes.Ldarg_0)
				il.Emit(OpCodes.Ldtoken, m.MemberType)
				il.Emit(OpCodes.[Call], GetType(Type).GetMethod("GetTypeFromHandle", New Type() { GetType(RuntimeTypeHandle) }))
				il.Emit(OpCodes.Callvirt, GetType(IJsonReader).GetMethod("Parse", New Type() { GetType(Type) }))
				il.Emit(If(m.MemberType.IsValueType, OpCodes.Unbox_Any, OpCodes.Castclass), m.MemberType)
			End If
		End Sub

        ' <Obfuscation(Exclude = True)>
        Public Function GetLiteralBool(r As IJsonReader) As Boolean
            Dim result As Boolean
            Select Case r.GetLiteralKind()
                Case LiteralKind.[True]
                    result = True
                Case LiteralKind.[False]
                    result = False
                Case Else
                    Throw New InvalidDataException("expected a boolean value")
            End Select
            Return result
        End Function

        ' <Obfuscation(Exclude = True)>
        Public Function GetLiteralChar(r As IJsonReader) As Char
            If r.GetLiteralKind() <> LiteralKind.[String] Then
                Throw New InvalidDataException("expected a single character string literal")
            End If
            Dim str As String = r.GetLiteralString()
            If str Is Nothing OrElse str.Length <> 1 Then
                Throw New InvalidDataException("expected a single character string literal")
            End If
            Return str(0)
        End Function

        ' <Obfuscation(Exclude = True)>
        Public Function GetLiteralString(r As IJsonReader) As String
            Dim result As String
            Select Case r.GetLiteralKind()
                Case LiteralKind.[String]
                    result = r.GetLiteralString()
                Case LiteralKind.Null
                    result = Nothing
                Case Else
                    Throw New InvalidDataException("expected a string literal")
            End Select
            Return result
        End Function

        ' <Obfuscation(Exclude = True)>
        Public Function GetLiteralNumber(r As IJsonReader) As String
            Select Case r.GetLiteralKind()
                Case LiteralKind.SignedInteger, LiteralKind.UnsignedInteger, LiteralKind.FloatingPoint
                    Return r.GetLiteralString()
                Case Else
                    Throw New InvalidDataException("expected a numeric literal")
            End Select
        End Function
	End Module
End Namespace
