Imports XXXX.PetaJson.Internal
Imports System
Imports System.Collections.Generic
Imports System.IO

Namespace XXXX.PetaJson
	Public Module Json
		Public Property WriteWhitespaceDefault() As Boolean

		Public Property StrictParserDefault() As Boolean

        Sub New()
            Json.WriteWhitespaceDefault = True
            Json.StrictParserDefault = False
            Json.SetFormatterResolver(AddressOf Emit.MakeFormatter)
            Json.SetParserResolver(AddressOf Emit.MakeParser)
            Json.SetIntoParserResolver(AddressOf Emit.MakeIntoParser)
        End Sub

		Public Sub Write(w As TextWriter, o As Object, Optional options As JsonOptions=JsonOptions.None)
			Dim writer As Writer = New Writer(w, Json.ResolveOptions(options))
			writer.WriteValue(o)
		End Sub

		Public Sub WriteFile(filename As String, o As Object, Optional options As JsonOptions=JsonOptions.None)
            Using w As StreamWriter = New StreamWriter(filename)
                Json.Write(w, o, options)
            End Using
		End Sub

		Public Function Format(o As Object, Optional options As JsonOptions=JsonOptions.None) As String
			Dim sw As StringWriter = New StringWriter()
			Dim writer As Writer = New Writer(sw, Json.ResolveOptions(options))
			writer.WriteValue(o)
			Return sw.ToString()
		End Function

		Public Function Parse(r As TextReader, type As Type, Optional options As JsonOptions=JsonOptions.None) As Object
			Dim reader As Reader = Nothing
			Dim result As Object
			Try
				reader = New Reader(r, Json.ResolveOptions(options))
				Dim retv As Object = reader.Parse(type)
				reader.CheckEOF()
				result = retv
			Catch x As Exception
				Dim loc As JsonLineOffset = If((reader Is Nothing), Nothing, reader.CurrentTokenPosition)
				Console.WriteLine("Exception thrown while parsing JSON at {0}, context:{1}" & vbLf & "{2}", loc, reader.Context, x.ToString())
				Throw New JsonParseException(x, reader.Context, loc)
			End Try
			Return result
		End Function

		Public Function Parse(Of T)(r As TextReader, Optional options As JsonOptions=JsonOptions.None) As T
			Return CType((CObj(Json.Parse(r, GetType(T), options))), T)
		End Function

		Public Sub ParseInto(r As TextReader, into As Object, Optional options As JsonOptions=JsonOptions.None)
			If into Is Nothing Then
				Throw New NullReferenceException()
			End If
			If into.[GetType]().IsValueType Then
				Throw New InvalidOperationException("Can't ParseInto a value type")
			End If
			Dim reader As Reader = Nothing
			Try
				reader = New Reader(r, Json.ResolveOptions(options))
				reader.ParseInto(into)
				reader.CheckEOF()
			Catch x As Exception
				Dim loc As JsonLineOffset = If((reader Is Nothing), Nothing, reader.CurrentTokenPosition)
				Console.WriteLine("Exception thrown while parsing JSON at {0}, context:{1}" & vbLf & "{2}", loc, reader.Context, x.ToString())
				Throw New JsonParseException(x, reader.Context, loc)
			End Try
		End Sub

		Public Function ParseFile(filename As String, type As Type, Optional options As JsonOptions=JsonOptions.None) As Object
			Dim result As Object
            Using r As StreamReader = New StreamReader(filename)
                result = Json.Parse(r, type, options)
            End Using
			Return result
		End Function

		Public Function ParseFile(Of T)(filename As String, Optional options As JsonOptions=JsonOptions.None) As T
			Dim result As T
            Using r As StreamReader = New StreamReader(filename)
                result = Json.Parse(Of T)(r, options)
            End Using
			Return result
		End Function

		Public Sub ParseFileInto(filename As String, into As Object, Optional options As JsonOptions=JsonOptions.None)
            Using r As StreamReader = New StreamReader(filename)
                Json.ParseInto(r, into, options)
            End Using
		End Sub

		Public Function Parse(data As String, type As Type, Optional options As JsonOptions=JsonOptions.None) As Object
			Return Json.Parse(New StringReader(data), type, options)
		End Function

		Public Function Parse(Of T)(data As String, Optional options As JsonOptions=JsonOptions.None) As T
			Return Json.Parse(Of T)(New StringReader(data), options)
		End Function

		Public Sub ParseInto(data As String, into As Object, Optional options As JsonOptions=JsonOptions.None)
			Json.ParseInto(New StringReader(data), into, options)
		End Sub

		Public Function Clone(Of T)(source As T) As T
			Return CType((CObj(Json.Reparse(source.[GetType](), source))), T)
		End Function

		Public Function Clone(source As Object) As Object
			Return Json.Reparse(source.[GetType](), source)
		End Function

		Public Sub CloneInto(dest As Object, source As Object)
			Json.ReparseInto(dest, source)
		End Sub

		Public Function Reparse(type As Type, source As Object) As Object
			Dim result As Object
			If source Is Nothing Then
				result = Nothing
			Else
				Dim ms As MemoryStream = New MemoryStream()
				Try
					Dim w As StreamWriter = New StreamWriter(ms)
					Json.Write(w, source, JsonOptions.None)
					w.Flush()
					ms.Seek(0L, SeekOrigin.Begin)
					Dim r As StreamReader = New StreamReader(ms)
					result = Json.Parse(r, type, JsonOptions.None)
				Finally
					ms.Dispose()
				End Try
			End If
			Return result
		End Function

		Public Function Reparse(Of T)(source As Object) As T
			Return CType((CObj(Json.Reparse(GetType(T), source))), T)
		End Function

		Public Sub ReparseInto(dest As Object, source As Object)
			Dim ms As MemoryStream = New MemoryStream()
			Try
				Dim w As StreamWriter = New StreamWriter(ms)
				Json.Write(w, source, JsonOptions.None)
				w.Flush()
				ms.Seek(0L, SeekOrigin.Begin)
				Dim r As StreamReader = New StreamReader(ms)
				Json.ParseInto(r, dest, JsonOptions.None)
			Finally
				ms.Dispose()
			End Try
		End Sub

		Public Sub RegisterFormatter(type As Type, formatter As WriteCallback_t(Of IJsonWriter, Object))
			Writer._formatters(type) = formatter
		End Sub

		Public Sub RegisterFormatter(Of T)(formatter As WriteCallback_t(Of IJsonWriter, T))
			Json.RegisterFormatter(GetType(T), Sub(w As IJsonWriter, o As Object)
				formatter(w, CType((CObj(o)), T))
			End Sub)
		End Sub

		Public Sub RegisterParser(type As Type, parser As ReadCallback_t(Of IJsonReader, Type, Object))
			Reader._parsers.[Set](type, parser)
		End Sub

		Public Sub RegisterParser(Of T)(parser As ReadCallback_t(Of IJsonReader, Type, T))
			Json.RegisterParser(GetType(T), Function(r As IJsonReader, tt As Type) parser(r, tt))
		End Sub

		Public Sub RegisterParser(type As Type, parser As ReadCallback_t(Of Object, Object))
			Json.RegisterParser(type, Function(r As IJsonReader, t As Type) r.ReadLiteral(parser))
		End Sub

		Public Sub RegisterParser(Of T)(parser As ReadCallback_t(Of Object, T))
			Json.RegisterParser(GetType(T), Function(literal As Object) parser(literal))
		End Sub

		Public Sub RegisterIntoParser(type As Type, parser As WriteCallback_t(Of IJsonReader, Object))
			Reader._intoParsers.[Set](type, parser)
		End Sub

		Public Sub RegisterIntoParser(Of T)(parser As WriteCallback_t(Of IJsonReader, Object))
			Json.RegisterIntoParser(GetType(T), parser)
		End Sub

		Public Sub RegisterTypeFactory(type As Type, factory As ReadCallback_t(Of IJsonReader, String, Object))
			Reader._typeFactories.[Set](type, factory)
		End Sub

		Public Sub SetFormatterResolver(resolver As ReadCallback_t(Of Type, WriteCallback_t(Of IJsonWriter, Object)))
			Writer._formatterResolver = resolver
		End Sub

		Public Sub SetParserResolver(resolver As ReadCallback_t(Of Type, ReadCallback_t(Of IJsonReader, Type, Object)))
			Reader._parserResolver = resolver
		End Sub

		Public Sub SetIntoParserResolver(resolver As ReadCallback_t(Of Type, WriteCallback_t(Of IJsonReader, Object)))
			Reader._intoParserResolver = resolver
		End Sub

		<System.Runtime.CompilerServices.ExtensionAttribute()>
		Public Function WalkPath(This As IDictionary(Of String, Object), Path As String, create As Boolean, leafCallback As ReadCallback_t(Of IDictionary(Of String, Object), String, Boolean)) As Boolean
			Dim parts As String() = Path.Split(New Char() { "."c })
			Dim result As Boolean
			For i As Integer = 0 To parts.Length - 1 - 1
				Dim val As Object
				If Not This.TryGetValue(parts(i), val) Then
					If Not create Then
						result = False
						Return result
					End If
					val = New Dictionary(Of String, Object)()
					This(parts(i)) = val
				End If
				This = CType(val, IDictionary(Of String, Object))
			Next
			result = leafCallback(This, parts(parts.Length - 1))
			Return result
		End Function

		<System.Runtime.CompilerServices.ExtensionAttribute()>
		Public Function PathExists(This As IDictionary(Of String, Object), Path As String) As Boolean
			Return This.WalkPath(Path, False, Function(dict As IDictionary(Of String, Object), key As String) dict.ContainsKey(key))
		End Function

		<System.Runtime.CompilerServices.ExtensionAttribute()>
		Public Function GetPath(This As IDictionary(Of String, Object), type As Type, Path As String, def As Object) As Object
            This.WalkPath(Path, False, Function(dict As IDictionary(Of String, Object), key As String)
                                                    Dim val As Object
                                                    If dict.TryGetValue(key, val) Then
                                                        If val Is Nothing Then
                                                            def = val
                                                        ElseIf type.IsAssignableFrom(val.[GetType]()) Then
                                                            def = val
                                                        Else
                                                            def = Json.Reparse(type, val)
                                                        End If
                                                    End If
                                                    Return True
                                                End Function)
			Return def
		End Function

		<System.Runtime.CompilerServices.ExtensionAttribute()>
		Public Function GetObjectAtPath(Of T As{Class, New})(This As IDictionary(Of String, Object), Path As String) As T
			Dim retVal As T = Nothing
            This.WalkPath(Path, True, Function(dict As IDictionary(Of String, Object), key As String)
                                                   Dim val As Object
                                                   dict.TryGetValue(key, val)
                                                   retVal = (TryCast(val, T))
                                                   If retVal Is Nothing Then
                                                       retVal = (If((val Is Nothing), Activator.CreateInstance(Of T)(), Json.Reparse(Of T)(val)))
                                                       dict(key) = retVal
                                                   End If
                                                   Return True
                                               End Function)
			Return retVal
		End Function

		<System.Runtime.CompilerServices.ExtensionAttribute()>
		Public Function GetPath(Of T)(This As IDictionary(Of String, Object), Path As String, Optional def As T=Nothing) As T
			Return CType((CObj(This.GetPath(GetType(T), Path, def))), T)
		End Function

		<System.Runtime.CompilerServices.ExtensionAttribute()>
		Public Sub SetPath(This As IDictionary(Of String, Object), Path As String, value As Object)
            This.WalkPath(Path, True, Function(dict As IDictionary(Of String, Object), key As String)
                                                   dict(key) = value
                                                   Return True
                                               End Function)
		End Sub

		Private Function ResolveOptions(options As JsonOptions) As JsonOptions
			Dim resolved As JsonOptions = JsonOptions.None
			If(options And (JsonOptions.WriteWhitespace Or JsonOptions.DontWriteWhitespace)) <> JsonOptions.None Then
				resolved = resolved Or (options And (JsonOptions.WriteWhitespace Or JsonOptions.DontWriteWhitespace))
			Else
				resolved = resolved Or (If(Json.WriteWhitespaceDefault, JsonOptions.WriteWhitespace, JsonOptions.DontWriteWhitespace))
			End If
			If(options And (JsonOptions.StrictParser Or JsonOptions.NonStrictParser)) <> JsonOptions.None Then
				resolved = resolved Or (options And (JsonOptions.StrictParser Or JsonOptions.NonStrictParser))
			Else
				resolved = resolved Or (If(Json.StrictParserDefault, JsonOptions.StrictParser, JsonOptions.NonStrictParser))
			End If
			Return resolved
		End Function
	End Module
End Namespace
