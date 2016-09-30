
Imports System
Imports System.IO
Imports System.Text
Imports System.Collections
Imports System.Collections.Generic

Imports System.Diagnostics
Imports System.Globalization

Imports System.Reflection
Imports System.Reflection.Emit


Namespace BouncyJWT.PetaJson





    Public Delegate Function ReadCallback_t(Of Out TResult)() As TResult

    Public Delegate Function ReadCallback_t(Of In T, Out TResult)(arg As T) As TResult

    Public Delegate Function ReadCallback_t(Of In T1, In T2, Out TResult)(arg1 As T1, arg2 As T2) As TResult

    Public Delegate Function ReadCallback_t(Of In T1, In T2, In T3, Out TResult)(arg1 As T1, arg2 As T2, arg3 As T3) As TResult





    Public Delegate Sub WriteCallback_t()

    Public Delegate Sub WriteCallback_t(Of In T)(obj As T)

    Public Delegate Sub WriteCallback_t(Of In T1, In T2)(arg1 As T1, arg2 As T2)






    ' <Obfuscation(Exclude = True, ApplyToMembers = True)>
    Public Interface IJsonLoaded
        Sub OnJsonLoaded(r As IJsonReader)
    End Interface






    ' <Obfuscation(Exclude = True, ApplyToMembers = True)>
    Public Interface IJsonLoadField
        Function OnJsonField(r As IJsonReader, key As String) As Boolean
    End Interface






    ' <Obfuscation(Exclude = True, ApplyToMembers = True)>
    Public Interface IJsonLoading
        Sub OnJsonLoading(r As IJsonReader)
    End Interface






    ' <Obfuscation(Exclude = True, ApplyToMembers = True)>
    Public Interface IJsonReader
        Function Parse(type As Type) As Object

        Function Parse(Of T)() As T

        Sub ParseInto(into As Object)

        Function ReadLiteral(converter As ReadCallback_t(Of Object, Object)) As Object

        Sub ParseDictionary(callback As WriteCallback_t(Of String))

        Sub ParseArray(callback As WriteCallback_t)

        Function GetLiteralKind() As LiteralKind

        Function GetLiteralString() As String

        Sub NextToken()
    End Interface








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






    ' <Obfuscation(Exclude = True, ApplyToMembers = True)>
    Public Interface IJsonWriting
        Sub OnJsonWriting(w As IJsonWriter)
    End Interface






    ' <Obfuscation(Exclude = True, ApplyToMembers = True)>
    Public Interface IJsonWritten
        Sub OnJsonWritten(w As IJsonWriter)
    End Interface





    <AttributeUsage(AttributeTargets.[Class] Or AttributeTargets.Struct Or AttributeTargets.[Property] Or AttributeTargets.Field)>
    Public Class JsonAttribute
        Inherits Attribute

        Private _key As String

        Public ReadOnly Property Key() As String
            Get
                Return Me._key
            End Get
        End Property

        Public Property KeepInstance() As Boolean

        Public Property Deprecated() As Boolean

        Public Sub New()
            Me._key = Nothing
        End Sub

        Public Sub New(key As String)
            Me._key = key
        End Sub
    End Class





    <AttributeUsage(AttributeTargets.[Property] Or AttributeTargets.Field)>
    Public Class JsonExcludeAttribute
        Inherits Attribute

    End Class





    Public Structure JsonLineOffset
        Public Line As Integer

        Public Offset As Integer

        Public Overrides Function ToString() As String
            Return String.Format("line {0}, character {1}", Me.Line + 1, Me.Offset + 1)
        End Function
    End Structure





    <Flags()>
    Public Enum JsonOptions
        None = 0
        WriteWhitespace = 1
        DontWriteWhitespace = 2
        StrictParser = 4
        NonStrictParser = 8
    End Enum





    Public Class JsonParseException
        Inherits Exception

        Public Position As JsonLineOffset

        Public Context As String

        Public Sub New(inner As Exception, context As String, position As JsonLineOffset)
            MyBase.New(String.Format("JSON parse error at {0}{1} - {2}", position, If(String.IsNullOrEmpty(context), "", String.Format(", context {0}", context)), inner.Message), inner)
            Me.Position = position
            Me.Context = context
        End Sub
    End Class





    <AttributeUsage(AttributeTargets.[Enum])>
    Public Class JsonUnknownAttribute
        Inherits Attribute

        Public Property UnknownValue() As Object

        Public Sub New(unknownValue As Object)
            Me.UnknownValue = unknownValue
        End Sub
    End Class





    Public Enum LiteralKind
        None
        [String]
        Null
        [True]
        [False]
        SignedInteger
        UnsignedInteger
        FloatingPoint
    End Enum








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

        Public Sub Write(w As TextWriter, o As Object, Optional options As JsonOptions = JsonOptions.None)
            Dim writer As Writer = New Writer(w, Json.ResolveOptions(options))
            writer.WriteValue(o)
        End Sub

        Public Sub WriteFile(filename As String, o As Object, Optional options As JsonOptions = JsonOptions.None)
            Using w As StreamWriter = New StreamWriter(filename)
                Json.Write(w, o, options)
            End Using
        End Sub

        Public Function Format(o As Object, Optional options As JsonOptions = JsonOptions.None) As String
            Dim sw As StringWriter = New StringWriter()
            Dim writer As Writer = New Writer(sw, Json.ResolveOptions(options))
            writer.WriteValue(o)
            Return sw.ToString()
        End Function

        Public Function Parse(r As TextReader, type As Type, Optional options As JsonOptions = JsonOptions.None) As Object
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

        Public Function Parse(Of T)(r As TextReader, Optional options As JsonOptions = JsonOptions.None) As T
            Return CType((CObj(Json.Parse(r, GetType(T), options))), T)
        End Function

        Public Sub ParseInto(r As TextReader, into As Object, Optional options As JsonOptions = JsonOptions.None)
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

        Public Function ParseFile(filename As String, type As Type, Optional options As JsonOptions = JsonOptions.None) As Object
            Dim result As Object
            Using r As StreamReader = New StreamReader(filename)
                result = Json.Parse(r, type, options)
            End Using
            Return result
        End Function

        Public Function ParseFile(Of T)(filename As String, Optional options As JsonOptions = JsonOptions.None) As T
            Dim result As T
            Using r As StreamReader = New StreamReader(filename)
                result = Json.Parse(Of T)(r, options)
            End Using
            Return result
        End Function

        Public Sub ParseFileInto(filename As String, into As Object, Optional options As JsonOptions = JsonOptions.None)
            Using r As StreamReader = New StreamReader(filename)
                Json.ParseInto(r, into, options)
            End Using
        End Sub

        Public Function Parse(data As String, type As Type, Optional options As JsonOptions = JsonOptions.None) As Object
            Return Json.Parse(New StringReader(data), type, options)
        End Function

        Public Function Parse(Of T)(data As String, Optional options As JsonOptions = JsonOptions.None) As T
            Return Json.Parse(Of T)(New StringReader(data), options)
        End Function

        Public Sub ParseInto(data As String, into As Object, Optional options As JsonOptions = JsonOptions.None)
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
            Dim parts As String() = Path.Split(New Char() {"."c})
            Dim result As Boolean
            For i As Integer = 0 To parts.Length - 1 - 1
                Dim val As Object = Nothing
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
                                           Dim val As Object = Nothing
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
        Public Function GetObjectAtPath(Of T As {Class, New})(This As IDictionary(Of String, Object), Path As String) As T
            Dim retVal As T = Nothing
            This.WalkPath(Path, True, Function(dict As IDictionary(Of String, Object), key As String)
                                          Dim val As Object = Nothing
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
        Public Function GetPath(Of T)(This As IDictionary(Of String, Object), Path As String, Optional def As T = Nothing) As T
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
            If (options And (JsonOptions.WriteWhitespace Or JsonOptions.DontWriteWhitespace)) <> JsonOptions.None Then
                resolved = resolved Or (options And (JsonOptions.WriteWhitespace Or JsonOptions.DontWriteWhitespace))
            Else
                resolved = resolved Or (If(Json.WriteWhitespaceDefault, JsonOptions.WriteWhitespace, JsonOptions.DontWriteWhitespace))
            End If
            If (options And (JsonOptions.StrictParser Or JsonOptions.NonStrictParser)) <> JsonOptions.None Then
                resolved = resolved Or (options And (JsonOptions.StrictParser Or JsonOptions.NonStrictParser))
            Else
                resolved = resolved Or (If(Json.StrictParserDefault, JsonOptions.StrictParser, JsonOptions.NonStrictParser))
            End If
            Return resolved
        End Function
    End Module












    Public Class Writer
        Implements IJsonWriter

        Public Shared _formatterResolver As ReadCallback_t(Of Type, WriteCallback_t(Of IJsonWriter, Object))

        Public Shared _formatters As Dictionary(Of Type, WriteCallback_t(Of IJsonWriter, Object))

        Private _writer As TextWriter

        Private IndentLevel As Integer

        Private _atStartOfLine As Boolean

        Private _needElementSeparator As Boolean = False

        Private _options As JsonOptions

        Private _currentBlockKind As Char = vbNullChar

        Shared Sub New()
            Writer._formatters = New Dictionary(Of Type, WriteCallback_t(Of IJsonWriter, Object))()
            Writer._formatterResolver = AddressOf Writer.ResolveFormatter
            Writer._formatters.Add(GetType(String), Sub(w As IJsonWriter, o As Object)
                                                        w.WriteStringLiteral(CStr(o))
                                                    End Sub)
            Writer._formatters.Add(GetType(Char), Sub(w As IJsonWriter, o As Object)
                                                      w.WriteStringLiteral((CChar(o)).ToString())
                                                  End Sub)
            Writer._formatters.Add(GetType(Boolean), Sub(w As IJsonWriter, o As Object)
                                                         w.WriteRaw(If((CBool(o)), "true", "false"))
                                                     End Sub)
            Dim convertWriter As WriteCallback_t(Of IJsonWriter, Object) = Sub(w As IJsonWriter, o As Object)
                                                                               w.WriteRaw(CStr(Convert.ChangeType(o, GetType(String), CultureInfo.InvariantCulture)))
                                                                           End Sub
            Writer._formatters.Add(GetType(Integer), convertWriter)
            Writer._formatters.Add(GetType(UInteger), convertWriter)
            Writer._formatters.Add(GetType(Long), convertWriter)
            Writer._formatters.Add(GetType(ULong), convertWriter)
            Writer._formatters.Add(GetType(Short), convertWriter)
            Writer._formatters.Add(GetType(UShort), convertWriter)
            Writer._formatters.Add(GetType(Decimal), convertWriter)
            Writer._formatters.Add(GetType(Byte), convertWriter)
            Writer._formatters.Add(GetType(SByte), convertWriter)
            Writer._formatters.Add(GetType(DateTime), Sub(w As IJsonWriter, o As Object)
                                                          convertWriter(w, Utils.ToUnixMilliseconds(CType(o, DateTime)))
                                                      End Sub)
            Writer._formatters.Add(GetType(Single), Sub(w As IJsonWriter, o As Object)
                                                        w.WriteRaw((CSng(o)).ToString("R", CultureInfo.InvariantCulture))
                                                    End Sub)
            Writer._formatters.Add(GetType(Double), Sub(w As IJsonWriter, o As Object)
                                                        w.WriteRaw((CDec(o)).ToString("R", CultureInfo.InvariantCulture))
                                                    End Sub)
            Writer._formatters.Add(GetType(Byte()), Sub(w As IJsonWriter, o As Object)
                                                        w.WriteRaw("""")
                                                        w.WriteRaw(Convert.ToBase64String(CType(o, Byte())))
                                                        w.WriteRaw("""")
                                                    End Sub)
        End Sub

        Private Shared Function ResolveFormatter(type As Type) As WriteCallback_t(Of IJsonWriter, Object)
            Dim formatJson As MethodInfo = ReflectionInfo.FindFormatJson(type)
            Dim result As WriteCallback_t(Of IJsonWriter, Object)
            If formatJson IsNot Nothing Then
                If formatJson.ReturnType Is GetType(Void) Then
                    result = Sub(w As IJsonWriter, obj As Object)
                                 formatJson.Invoke(obj, New Object() {w})
                             End Sub
                    Return result
                End If
                If formatJson.ReturnType Is GetType(String) Then
                    result = Sub(w As IJsonWriter, obj As Object)
                                 w.WriteStringLiteral(CStr(formatJson.Invoke(obj, New Object(-1) {})))
                             End Sub
                    Return result
                End If
            End If
            Dim ri As ReflectionInfo = ReflectionInfo.GetReflectionInfo(type)
            If ri IsNot Nothing Then
                result = AddressOf ri.Write
            Else
                result = Nothing
            End If
            Return result
        End Function

        Public Sub New(w As TextWriter, options As JsonOptions)
            Me._writer = w
            Me._atStartOfLine = True
            Me._needElementSeparator = False
            Me._options = options
        End Sub

        Public Sub NextLine()
            If Not Me._atStartOfLine Then
                If (Me._options And JsonOptions.WriteWhitespace) <> JsonOptions.None Then
                    Me.WriteRaw(vbLf)
                    Me.WriteRaw(New String(vbTab, Me.IndentLevel))
                End If
                Me._atStartOfLine = True
            End If
        End Sub

        Private Sub NextElement()
            If Me._needElementSeparator Then
                Me.WriteRaw(",")
                Me.NextLine()
            Else
                Me.NextLine()
                Me.IndentLevel += 1
                Me.WriteRaw(Me._currentBlockKind.ToString())
                Me.NextLine()
            End If
            Me._needElementSeparator = True
        End Sub

        Public Sub WriteElement() Implements IJsonWriter.WriteElement
            If Me._currentBlockKind <> "["c Then
                Throw New InvalidOperationException("Attempt to write array element when not in array block")
            End If
            Me.NextElement()
        End Sub

        Public Sub WriteKey(key As String) Implements IJsonWriter.WriteKey
            If Me._currentBlockKind <> "{"c Then
                Throw New InvalidOperationException("Attempt to write dictionary element when not in dictionary block")
            End If
            Me.NextElement()
            Me.WriteStringLiteral(key)
            Me.WriteRaw(If(((Me._options And JsonOptions.WriteWhitespace) <> JsonOptions.None), ": ", ":"))
        End Sub

        Public Sub WriteKeyNoEscaping(key As String) Implements IJsonWriter.WriteKeyNoEscaping
            If Me._currentBlockKind <> "{"c Then
                Throw New InvalidOperationException("Attempt to write dictionary element when not in dictionary block")
            End If
            Me.NextElement()
            Me.WriteRaw("""")
            Me.WriteRaw(key)
            Me.WriteRaw("""")
            Me.WriteRaw(If(((Me._options And JsonOptions.WriteWhitespace) <> JsonOptions.None), ": ", ":"))
        End Sub

        Public Sub WriteRaw(str As String) Implements IJsonWriter.WriteRaw
            Me._atStartOfLine = False
            Me._writer.Write(str)
        End Sub

        Private Shared Function IndexOfEscapeableChar(str As String, pos As Integer) As Integer
            Dim length As Integer = str.Length
            Dim result As Integer
            While pos < length
                Dim ch As Char = str(pos)
                If ch = "\"c OrElse ch = "/"c OrElse ch = """"c OrElse (ch >= vbNullChar AndAlso ch <= ""c) OrElse (ch >= ""c AndAlso ch <= ""c) OrElse ch = ChrW(8232) OrElse ch = ChrW(8233) Then
                    result = pos
                    Return result
                End If
                pos += 1
            End While
            result = -1
            Return result
        End Function

        Public Sub WriteStringLiteral(str As String) Implements IJsonWriter.WriteStringLiteral
            Me._atStartOfLine = False
            If str = Nothing Then
                Me._writer.Write("null")
            Else
                Me._writer.Write("""")
                Dim pos As Integer = 0
                While True
                    Dim expr_17B As Integer = Writer.IndexOfEscapeableChar(str, pos)
                    Dim escapePos As Integer = expr_17B
                    If expr_17B < 0 Then
                        Exit While
                    End If
                    If escapePos > pos Then
                        Me._writer.Write(str.Substring(pos, escapePos - pos))
                    End If
                    Dim c As Char = str(escapePos)
                    If c <= """"c Then
                        Select Case c
                            Case vbBack
                                Me._writer.Write("\b")
                            Case vbTab
                                Me._writer.Write("\t")
                            Case vbLf
                                Me._writer.Write("\n")
                            Case vbVerticalTab
                                GoTo IL_14B
                            Case vbFormFeed
                                Me._writer.Write("\f")
                            Case vbCr
                                Me._writer.Write("\r")
                            Case Else
                                If c <> """"c Then
                                    GoTo IL_14B
                                End If
                                Me._writer.Write("\""")
                        End Select
                    ElseIf c <> "/"c Then
                        If c <> "\"c Then
                            GoTo IL_14B
                        End If
                        Me._writer.Write("\\")
                    Else
                        Me._writer.Write("\/")
                    End If
IL_16F:
                    pos = escapePos + 1
                    Continue While
IL_14B:
                    Me._writer.Write(String.Format("\u{0:x4}", AscW(str(escapePos))))
                    GoTo IL_16F
                End While
                If str.Length > pos Then
                    Me._writer.Write(str.Substring(pos))
                End If
                Me._writer.Write("""")
            End If
        End Sub

        Private Sub WriteBlock(open As String, close As String, callback As WriteCallback_t)
            Dim prevBlockKind As Char = Me._currentBlockKind
            Me._currentBlockKind = open(0)
            Dim didNeedElementSeparator As Boolean = Me._needElementSeparator
            Me._needElementSeparator = False
            callback()
            If Me._needElementSeparator Then
                Me.IndentLevel -= 1
                Me.NextLine()
            Else
                Me.WriteRaw(open)
            End If
            Me.WriteRaw(close)
            Me._needElementSeparator = didNeedElementSeparator
            Me._currentBlockKind = prevBlockKind
        End Sub

        Public Sub WriteArray(callback As WriteCallback_t) Implements IJsonWriter.WriteArray
            Me.WriteBlock("[", "]", callback)
        End Sub

        Public Sub WriteDictionary(callback As WriteCallback_t) Implements IJsonWriter.WriteDictionary
            Me.WriteBlock("{", "}", callback)
        End Sub

        Public Sub WriteValue(value As Object) Implements IJsonWriter.WriteValue
            Me._atStartOfLine = False
            If value Is Nothing Then
                Me._writer.Write("null")
            Else
                Dim type As Type = value.[GetType]()
                Dim typeUnderlying As Type = Nullable.GetUnderlyingType(type)
                If typeUnderlying IsNot Nothing Then
                    type = typeUnderlying
                End If
                Dim typeWriter As WriteCallback_t(Of IJsonWriter, Object) = Nothing
                If Writer._formatters.TryGetValue(type, typeWriter) Then
                    typeWriter(Me, value)
                ElseIf type.IsEnum Then
                    If PetaJson.Enumerable.Any(Of Object)(type.GetCustomAttributes(GetType(FlagsAttribute), False)) Then
                        Me.WriteRaw(Convert.ToUInt32(value).ToString(CultureInfo.InvariantCulture))
                    Else
                        Me.WriteStringLiteral(value.ToString())
                    End If
                Else
                    Dim d As IDictionary = TryCast(value, IDictionary)
                    If d IsNot Nothing Then
                        Me.WriteDictionary(Sub()
                                               For Each key As Object In d.Keys
                                                   Me.WriteKey(key.ToString())
                                                   Me.WriteValue(d(key))
                                               Next
                                           End Sub)
                    Else
                        Dim dso As IDictionary(Of String, Object) = TryCast(value, IDictionary(Of String, Object))
                        If dso IsNot Nothing Then
                            Me.WriteDictionary(Sub()
                                                   For Each key As String In dso.Keys
                                                       Me.WriteKey(key.ToString())
                                                       Me.WriteValue(dso(key))
                                                   Next
                                               End Sub)
                        Else
                            Dim e As IEnumerable = TryCast(value, IEnumerable)
                            If e IsNot Nothing Then
                                Me.WriteArray(Sub()
                                                  For Each i As Object In e
                                                      Me.WriteElement()
                                                      Me.WriteValue(i)
                                                  Next
                                              End Sub)
                            Else
                                Dim formatter As WriteCallback_t(Of IJsonWriter, Object) = Writer._formatterResolver(type)
                                If formatter Is Nothing Then
                                    Throw New InvalidDataException(String.Format("Don't know how to write '{0}' to json", value.[GetType]()))
                                End If
                                Writer._formatters(type) = formatter
                                formatter(Me, value)
                            End If
                        End If
                    End If
                End If
            End If
        End Sub
    End Class








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
                If genDef Is GetType(IDictionary(Of ,)) AndAlso tItf.GetGenericArguments()(0) Is GetType(String) Then
                    result = GetType(Dictionary(Of ,)).MakeGenericType(tItf.GetGenericArguments())
                    Return result
                End If
            End If
            If tItf Is GetType(IEnumerable) Then
                result = GetType(List(Of Object))
            ElseIf tItf Is GetType(IDictionary) Then
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









    Public Class Tokenizer
        Private Structure ReaderState
            Private _currentCharPos As JsonLineOffset

            Private _currentTokenPos As JsonLineOffset

            Private _currentChar As Char

            Private _currentToken As Token

            Private _literalKind As LiteralKind

            Private _string As String

            Private _rewindBufferPos As Integer

            Public Sub New(tokenizer As Tokenizer)
                Me._currentCharPos = tokenizer._currentCharPos
                Me._currentChar = tokenizer._currentChar
                Me._string = tokenizer.[String]
                Me._literalKind = tokenizer.LiteralKind
                Me._rewindBufferPos = tokenizer._rewindBufferPos
                Me._currentTokenPos = tokenizer.CurrentTokenPosition
                Me._currentToken = tokenizer.CurrentToken
            End Sub

            Public Sub Apply(tokenizer As Tokenizer)
                tokenizer._currentCharPos = Me._currentCharPos
                tokenizer._currentChar = Me._currentChar
                tokenizer._rewindBufferPos = Me._rewindBufferPos
                tokenizer.CurrentToken = Me._currentToken
                tokenizer.CurrentTokenPosition = Me._currentTokenPos
                tokenizer.[String] = Me._string
                tokenizer.LiteralKind = Me._literalKind
            End Sub
        End Structure

        Private _options As JsonOptions

        Private _sb As StringBuilder = New StringBuilder()

        Private _underlying As TextReader

        Private _buf As Char() = New Char(4095) {}

        Private _pos As Integer

        Private _bufUsed As Integer

        Private _rewindBuffer As StringBuilder

        Private _rewindBufferPos As Integer

        Private _currentCharPos As JsonLineOffset

        Private _currentChar As Char

        Private _bookmarks As Stack(Of Tokenizer.ReaderState) = New Stack(Of Tokenizer.ReaderState)()

        Public CurrentTokenPosition As JsonLineOffset

        Public CurrentToken As Token

        Public LiteralKind As LiteralKind

        Public [String] As String

        Public ReadOnly Property LiteralValue() As Object
            Get
                If Me.CurrentToken <> Token.Literal Then
                    Throw New InvalidOperationException("token is not a literal")
                End If
                Dim result As Object
                Select Case Me.LiteralKind
                    Case LiteralKind.[String]
                        result = Me.[String]
                    Case LiteralKind.Null
                        result = Nothing
                    Case LiteralKind.[True]
                        result = True
                    Case LiteralKind.[False]
                        result = False
                    Case LiteralKind.SignedInteger
                        result = Long.Parse(Me.[String], CultureInfo.InvariantCulture)
                    Case LiteralKind.UnsignedInteger
                        result = ULong.Parse(Me.[String], CultureInfo.InvariantCulture)
                    Case LiteralKind.FloatingPoint
                        result = Double.Parse(Me.[String], CultureInfo.InvariantCulture)
                    Case Else
                        result = Nothing
                End Select
                Return result
            End Get
        End Property

        Public ReadOnly Property LiteralType() As Type
            Get
                If Me.CurrentToken <> Token.Literal Then
                    Throw New InvalidOperationException("token is not a literal")
                End If
                Dim result As Type
                Select Case Me.LiteralKind
                    Case LiteralKind.[String]
                        result = GetType(String)
                    Case LiteralKind.Null
                        result = GetType(Object)
                    Case LiteralKind.[True]
                        result = GetType(Boolean)
                    Case LiteralKind.[False]
                        result = GetType(Boolean)
                    Case LiteralKind.SignedInteger
                        result = GetType(Long)
                    Case LiteralKind.UnsignedInteger
                        result = GetType(ULong)
                    Case LiteralKind.FloatingPoint
                        result = GetType(Double)
                    Case Else
                        result = Nothing
                End Select
                Return result
            End Get
        End Property

        Public Sub New(r As TextReader, options As JsonOptions)
            Me._underlying = r
            Me._options = options
            Me.FillBuffer()
            Me.NextChar()
            Me.NextToken()
        End Sub

        Public Sub CreateBookmark()
            Me._bookmarks.Push(New Tokenizer.ReaderState(Me))
            If Me._rewindBuffer Is Nothing Then
                Me._rewindBuffer = New StringBuilder()
                Me._rewindBufferPos = 0
            End If
        End Sub

        Public Sub DiscardBookmark()
            Me._bookmarks.Pop()
            If Me._bookmarks.Count = 0 Then
                Me._rewindBuffer = Nothing
                Me._rewindBufferPos = 0
            End If
        End Sub

        Public Sub RewindToBookmark()
            Me._bookmarks.Pop().Apply(Me)
        End Sub

        Private Sub FillBuffer()
            Me._bufUsed = Me._underlying.Read(Me._buf, 0, Me._buf.Length)
            Me._pos = 0
        End Sub

        Public Function NextChar() As Char
            Dim result As Char
            If Me._rewindBuffer Is Nothing Then
                Dim c As Char
                If Me._pos >= Me._bufUsed Then
                    If Me._bufUsed > 0 Then
                        Me.FillBuffer()
                    End If
                    If Me._bufUsed = 0 Then
                        Dim expr_54 As Integer = 0
                        c = ChrW(expr_54)
                        Me._currentChar = ChrW(expr_54)
                        result = c
                        Return result
                    End If
                End If
                Me._currentCharPos.Offset = Me._currentCharPos.Offset + 1
                Dim arg_8E_0 As Char() = Me._buf
                Dim expr_84 As Integer = Me._pos
                Dim num As Integer = expr_84
                Me._pos = expr_84 + 1
                Dim expr_8F As Char = arg_8E_0(num)
                c = expr_8F
                Me._currentChar = expr_8F
                result = c
            ElseIf Me._rewindBufferPos < Me._rewindBuffer.Length Then
                Me._currentCharPos.Offset = Me._currentCharPos.Offset + 1
                Dim arg_E3_0 As StringBuilder = Me._rewindBuffer
                Dim expr_D9 As Integer = Me._rewindBufferPos
                Dim num As Integer = expr_D9
                Me._rewindBufferPos = expr_D9 + 1
                Dim expr_E8 As Char = arg_E3_0(num)
                Dim c As Char = expr_E8
                Me._currentChar = expr_E8
                result = c
            Else
                If Me._pos >= Me._bufUsed AndAlso Me._bufUsed > 0 Then
                    Me.FillBuffer()
                End If
                Dim arg_145_1 As Char
                If Me._bufUsed <> 0 Then
                    Dim arg_140_0 As Char() = Me._buf
                    Dim expr_136 As Integer = Me._pos
                    Dim num As Integer = expr_136
                    Me._pos = expr_136 + 1
                    arg_145_1 = arg_140_0(num)
                Else
                    arg_145_1 = CChar(Constants.vbNullChar)
                End If
                Me._currentChar = arg_145_1
                Me._rewindBuffer.Append(Me._currentChar)
                Me._rewindBufferPos += 1
                Me._currentCharPos.Offset = Me._currentCharPos.Offset + 1
                result = Me._currentChar
            End If
            Return result
        End Function

        Public Sub NextToken()
            Dim c As Char
            While True
                While True
                    If Me._currentChar = vbCr Then
                        If Me.NextChar() = vbLf Then
                            Me.NextChar()
                        End If
                        Me._currentCharPos.Line = Me._currentCharPos.Line + 1
                        Me._currentCharPos.Offset = 0
                    ElseIf Me._currentChar = vbLf Then
                        If Me.NextChar() = vbCr Then
                            Me.NextChar()
                        End If
                        Me._currentCharPos.Line = Me._currentCharPos.Line + 1
                        Me._currentCharPos.Offset = 0
                    ElseIf Me._currentChar = " "c Then
                        Me.NextChar()
                    Else
                        If Me._currentChar <> vbTab Then
                            Exit While
                        End If
                        Me.NextChar()
                    End If
                End While
                Me.CurrentTokenPosition = Me._currentCharPos
                c = Me._currentChar
                If c <= ","c Then
                    Exit While
                End If
                If c > "="c Then
                    GoTo IL_173
                End If
                If c <> "/"c Then
                    GoTo Block_14
                End If
                If (Me._options And JsonOptions.StrictParser) <> JsonOptions.None Then
                    GoTo Block_18
                End If
                Me.NextChar()
                c = Me._currentChar
                If c <> "*"c Then
                    If c <> "/"c Then
                        GoTo Block_20
                    End If
                    Me.NextChar()
                    While Me._currentChar <> vbNullChar AndAlso Me._currentChar <> vbCr AndAlso Me._currentChar <> vbLf
                        Me.NextChar()
                    End While
                Else
                    Dim endFound As Boolean = False
                    While Not endFound AndAlso Me._currentChar <> vbNullChar
                        If Me._currentChar = "*"c Then
                            Me.NextChar()
                            If Me._currentChar = "/"c Then
                                endFound = True
                            End If
                        End If
                        Me.NextChar()
                    End While
                End If
            End While
            If c <= """"c Then
                If c = vbNullChar Then
                    Me.CurrentToken = Token.EOF
                    Return
                End If
                If c <> """"c Then
                    GoTo IL_558
                End If
            ElseIf c <> "'"c Then
                If c <> ","c Then
                    GoTo IL_558
                End If
                Me.CurrentToken = Token.Comma
                Me.NextChar()
                Return
            End If
            Me._sb.Length = 0
            Dim quoteKind As Char = Me._currentChar
            Me.NextChar()
            While Me._currentChar <> vbNullChar
                If Me._currentChar = "\"c Then
                    Me.NextChar()
                    Dim escape As Char = Me._currentChar
                    c = escape
                    If c <= "\"c Then
                        If c <> """"c Then
                            If c <> "/"c Then
                                If c <> "\"c Then
                                    GoTo IL_41A
                                End If
                                Me._sb.Append("\"c)
                            Else
                                Me._sb.Append("/"c)
                            End If
                        Else
                            Me._sb.Append(""""c)
                        End If
                    ElseIf c <= "f"c Then
                        If c <> "b"c Then
                            If c <> "f"c Then
                                GoTo IL_41A
                            End If
                            Me._sb.Append(vbFormFeed)
                        Else
                            Me._sb.Append(vbBack)
                        End If
                    ElseIf c <> "n"c Then
                        Select Case c
                            Case "r"c
                                Me._sb.Append(vbCr)
                            Case "s"c
                                GoTo IL_41A
                            Case "t"c
                                Me._sb.Append(vbTab)
                            Case "u"c
                                Dim sbHex As StringBuilder = New StringBuilder()
                                For i As Integer = 0 To 4 - 1
                                    Me.NextChar()
                                    sbHex.Append(Me._currentChar)
                                Next
                                Me._sb.Append(ChrW(Convert.ToUInt16(sbHex.ToString(), 16)))
                            Case Else
                                GoTo IL_41A
                        End Select
                    Else
                        Me._sb.Append(vbLf)
                    End If
                    GoTo IL_48A
IL_41A:
                    Throw New InvalidDataException(String.Format("Invalid escape sequence in string literal: '\{0}'", Me._currentChar))
                End If
                If Me._currentChar = quoteKind Then
                    Me.[String] = Me._sb.ToString()
                    Me.CurrentToken = Token.Literal
                    Me.LiteralKind = LiteralKind.[String]
                    Me.NextChar()
                    Return
                End If
                Me._sb.Append(Me._currentChar)
IL_48A:
                Me.NextChar()
            End While
            Throw New InvalidDataException("syntax error, unterminated string literal")
Block_14:
            Select Case c
                Case ":"c
                    Me.CurrentToken = Token.Colon
                    Me.NextChar()
                    Return
                Case ";"c
                    Me.CurrentToken = Token.SemiColon
                    Me.NextChar()
                    Return
                Case "<"c
                    GoTo IL_558
                Case "="c
                    Me.CurrentToken = Token.Equal
                    Me.NextChar()
                    Return
                Case Else
                    GoTo IL_558
            End Select
IL_173:
            Select Case c
                Case "["c
                    Me.CurrentToken = Token.OpenSquare
                    Me.NextChar()
                    Return
                Case "\"c
                    GoTo IL_558
                Case "]"c
                    Me.CurrentToken = Token.CloseSquare
                    Me.NextChar()
                    Return
                Case Else
                    Select Case c
                        Case "{"c
                            Me.CurrentToken = Token.OpenBrace
                            Me.NextChar()
                            Return
                        Case "|"c
                            GoTo IL_558
                        Case "}"c
                            Me.CurrentToken = Token.CloseBrace
                            Me.NextChar()
                            Return
                        Case Else
                            GoTo IL_558
                    End Select
            End Select
Block_18:
            Throw New InvalidDataException(String.Format("syntax error, unexpected character '{0}'", Me._currentChar))
Block_20:
            Throw New InvalidDataException("syntax error, unexpected character after slash")
IL_558:
            If Char.IsDigit(Me._currentChar) OrElse Me._currentChar = "-"c Then
                Me.TokenizeNumber()
            Else
                If Not Char.IsLetter(Me._currentChar) AndAlso Me._currentChar <> "_"c AndAlso Me._currentChar <> "$"c Then
                    Throw New InvalidDataException(String.Format("syntax error, unexpected character '{0}'", Me._currentChar))
                End If
                Me._sb.Length = 0
                While Char.IsLetterOrDigit(Me._currentChar) OrElse Me._currentChar = "_"c OrElse Me._currentChar = "$"c
                    Me._sb.Append(Me._currentChar)
                    Me.NextChar()
                End While
                Me.[String] = Me._sb.ToString()
                Dim [string] As String = Me.[String]
                If [string] IsNot Nothing Then
                    If [string] = "true" Then
                        Me.LiteralKind = LiteralKind.[True]
                        Me.CurrentToken = Token.Literal
                        Return
                    End If
                    If [string] = "false" Then
                        Me.LiteralKind = LiteralKind.[False]
                        Me.CurrentToken = Token.Literal
                        Return
                    End If
                    If [string] = "null" Then
                        Me.LiteralKind = LiteralKind.Null
                        Me.CurrentToken = Token.Literal
                        Return
                    End If
                End If
                Me.CurrentToken = Token.Identifier
            End If
        End Sub

        Private Sub TokenizeNumber()
            Me._sb.Length = 0
            Dim signed As Boolean = False
            If Me._currentChar = "-"c Then
                signed = True
                Me._sb.Append(Me._currentChar)
                Me.NextChar()
            End If
            Dim hex As Boolean = False
            If Me._currentChar = "0"c AndAlso (Me._options And JsonOptions.StrictParser) = JsonOptions.None Then
                Me._sb.Append(Me._currentChar)
                Me.NextChar()
                If Me._currentChar = "x"c OrElse Me._currentChar = "X"c Then
                    Me._sb.Append(Me._currentChar)
                    Me.NextChar()
                    hex = True
                End If
            End If
            Dim cont As Boolean = True
            Dim fp As Boolean = False
            While cont
                Dim currentChar As Char = Me._currentChar
                Select Case currentChar
                    Case "."c
                        If hex Then
                            cont = False
                        Else
                            fp = True
                            Me._sb.Append(Me._currentChar)
                            Me.NextChar()
                        End If
                    Case "/"c, ":"c, ";"c, "<"c, "="c, ">"c, "?"c, "@"c
                        GoTo IL_23E
                    Case "0"c, "1"c, "2"c, "3"c, "4"c, "5"c, "6"c, "7"c, "8"c, "9"c
                        Me._sb.Append(Me._currentChar)
                        Me.NextChar()
                    Case "A"c, "B"c, "C"c, "D"c, "F"c
                        GoTo IL_182
                    Case "E"c
                        GoTo IL_1DC
                    Case Else
                        Select Case currentChar
                            Case "a"c, "b"c, "c"c, "d"c, "f"c
                                GoTo IL_182
                            Case "e"c
                                GoTo IL_1DC
                            Case Else
                                GoTo IL_23E
                        End Select
                End Select
                Continue While
IL_182:
                If Not hex Then
                    cont = False
                Else
                    Me._sb.Append(Me._currentChar)
                    Me.NextChar()
                End If
                Continue While
IL_1DC:
                If Not hex Then
                    fp = True
                    Me._sb.Append(Me._currentChar)
                    Me.NextChar()
                    If Me._currentChar = "+"c OrElse Me._currentChar = "-"c Then
                        Me._sb.Append(Me._currentChar)
                        Me.NextChar()
                    End If
                End If
                Continue While
IL_23E:
                cont = False
            End While
            If Char.IsLetter(Me._currentChar) Then
                Throw New InvalidDataException(String.Format("syntax error, invalid character following number '{0}'", Me._sb.ToString()))
            End If
            Me.[String] = Me._sb.ToString()
            Me.CurrentToken = Token.Literal
            If fp Then
                Me.LiteralKind = LiteralKind.FloatingPoint
            ElseIf signed Then
                Me.LiteralKind = LiteralKind.SignedInteger
            Else
                Me.LiteralKind = LiteralKind.UnsignedInteger
            End If
        End Sub

        Public Sub Check(tokenRequired As Token)
            If tokenRequired <> Me.CurrentToken Then
                Throw New InvalidDataException(String.Format("syntax error, expected {0} found {1}", tokenRequired, Me.CurrentToken))
            End If
        End Sub

        Public Sub Skip(tokenRequired As Token)
            Me.Check(tokenRequired)
            Me.NextToken()
        End Sub

        Public Function SkipIf(tokenRequired As Token) As Boolean
            Dim result As Boolean
            If tokenRequired = Me.CurrentToken Then
                Me.NextToken()
                result = True
            Else
                result = False
            End If
            Return result
        End Function
    End Class






    ' <Obfuscation(Exclude = True, ApplyToMembers = True)>
    Public Enum Token
        EOF
        Identifier
        Literal
        OpenBrace
        CloseBrace
        OpenSquare
        CloseSquare
        Equal
        Colon
        SemiColon
        Comma
    End Enum






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










    Public Class ReflectionInfo
        Public Members As List(Of JsonMemberInfo)

        Private Shared _cache As ThreadSafeCache(Of Type, ReflectionInfo) = New ThreadSafeCache(Of Type, ReflectionInfo)()

        Private _lastFoundIndex As Integer = 0

        Public Shared Function FindFormatJson(type As Type) As MethodInfo
            Dim result As MethodInfo
            If type.IsValueType Then
                Dim formatJson As MethodInfo = type.GetMethod("FormatJson", BindingFlags.Instance Or BindingFlags.[Public] Or BindingFlags.NonPublic, Nothing, New Type() {GetType(IJsonWriter)}, Nothing)
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
            Dim parseJson As MethodInfo = type.GetMethod("ParseJson", BindingFlags.[Static] Or BindingFlags.[Public] Or BindingFlags.NonPublic, Nothing, New Type() {GetType(IJsonReader)}, Nothing)
            Dim result As MethodInfo
            If parseJson IsNot Nothing AndAlso parseJson.ReturnType Is type Then
                result = parseJson
            Else
                parseJson = type.GetMethod("ParseJson", BindingFlags.[Static] Or BindingFlags.[Public] Or BindingFlags.NonPublic, Nothing, New Type() {GetType(String)}, Nothing)
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
                Dim jmi As JsonMemberInfo = Nothing
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

                                                         Dim typeMarked As Boolean = PetaJson.Enumerable.Any(Of JsonAttribute)(PetaJson.Enumerable.OfType(Of JsonAttribute)(type.GetCustomAttributes(GetType(JsonAttribute), True)))

                                                         'Dim anyFieldsMarked As Boolean = allMembers.Any(Function(x As MemberInfo) x.GetCustomAttributes(GetType(JsonAttribute), False).OfType(Of JsonAttribute)().Any(Of JsonAttribute)())
                                                         Dim anyFieldsMarked As Boolean = allMembers.Any(Function(x As MemberInfo) PetaJson.Enumerable.Any(Of JsonAttribute)(PetaJson.Enumerable.OfType(Of JsonAttribute)(x.GetCustomAttributes(GetType(JsonAttribute), False))))


                                                         Dim serializeAllPublics As Boolean = typeMarked OrElse Not anyFieldsMarked
                                                         Return ReflectionInfo.CreateReflectionInfo(type, Function(mi As MemberInfo)
                                                                                                              Dim result As JsonMemberInfo

                                                                                                              If PetaJson.Enumerable.Any(Of Object)(mi.GetCustomAttributes(GetType(JsonExcludeAttribute), False)) Then
                                                                                                                  result = Nothing
                                                                                                              Else
                                                                                                                  Dim attr As JsonAttribute = PetaJson.Enumerable.FirstOrDefault(Of JsonAttribute)(PetaJson.Enumerable.OfType(Of JsonAttribute)(mi.GetCustomAttributes(GetType(JsonAttribute), False)))

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



            If Not PetaJson.Enumerable.Any(Of JsonMemberInfo)(members) Then
                result = Nothing
            Else
                result = New ReflectionInfo() With {.Members = members}
            End If
            Return result
        End Function
    End Class












    Public Class Reader
        Implements IJsonReader

        Private _tokenizer As Tokenizer

        Private _options As JsonOptions

        Private _contextStack As List(Of String) = New List(Of String)()

        Public Shared _intoParserResolver As ReadCallback_t(Of Type, WriteCallback_t(Of IJsonReader, Object))

        Public Shared _parserResolver As ReadCallback_t(Of Type, ReadCallback_t(Of IJsonReader, Type, Object))

        Public Shared _parsers As ThreadSafeCache(Of Type, ReadCallback_t(Of IJsonReader, Type, Object))

        Public Shared _intoParsers As ThreadSafeCache(Of Type, WriteCallback_t(Of IJsonReader, Object))

        Public Shared _typeFactories As ThreadSafeCache(Of Type, ReadCallback_t(Of IJsonReader, String, Object))

        Public ReadOnly Property Context() As String
            Get
                Return String.Join(".", Me._contextStack.ToArray())
            End Get
        End Property

        Public ReadOnly Property CurrentTokenPosition() As JsonLineOffset
            Get
                Return Me._tokenizer.CurrentTokenPosition
            End Get
        End Property

        Shared Sub New()
            Reader._parsers = New ThreadSafeCache(Of Type, ReadCallback_t(Of IJsonReader, Type, Object))()
            Reader._intoParsers = New ThreadSafeCache(Of Type, WriteCallback_t(Of IJsonReader, Object))()
            Reader._typeFactories = New ThreadSafeCache(Of Type, ReadCallback_t(Of IJsonReader, String, Object))()
            Reader._parserResolver = AddressOf Reader.ResolveParser
            Reader._intoParserResolver = AddressOf Reader.ResolveIntoParser
            Dim simpleConverter As ReadCallback_t(Of IJsonReader, Type, Object) = Function(reader As IJsonReader, type As Type) reader.ReadLiteral(Function(literal As Object) Convert.ChangeType(literal, type, CultureInfo.InvariantCulture))
            Dim numberConverter As ReadCallback_t(Of IJsonReader, Type, Object) = Function(reader As IJsonReader, type As Type)
                                                                                      Select Case reader.GetLiteralKind()
                                                                                          Case LiteralKind.SignedInteger, LiteralKind.UnsignedInteger, LiteralKind.FloatingPoint
                                                                                              Dim val As Object = Convert.ChangeType(reader.GetLiteralString(), type, CultureInfo.InvariantCulture)
                                                                                              reader.NextToken()
                                                                                              Return val
                                                                                          Case Else
                                                                                              Throw New InvalidDataException("expected a numeric literal")
                                                                                      End Select
                                                                                  End Function
            Reader._parsers.[Set](GetType(String), simpleConverter)
            Reader._parsers.[Set](GetType(Char), simpleConverter)
            Reader._parsers.[Set](GetType(Boolean), simpleConverter)
            Reader._parsers.[Set](GetType(Byte), numberConverter)
            Reader._parsers.[Set](GetType(SByte), numberConverter)
            Reader._parsers.[Set](GetType(Short), numberConverter)
            Reader._parsers.[Set](GetType(UShort), numberConverter)
            Reader._parsers.[Set](GetType(Integer), numberConverter)
            Reader._parsers.[Set](GetType(UInteger), numberConverter)
            Reader._parsers.[Set](GetType(Long), numberConverter)
            Reader._parsers.[Set](GetType(ULong), numberConverter)
            Reader._parsers.[Set](GetType(Decimal), numberConverter)
            Reader._parsers.[Set](GetType(Single), numberConverter)
            Reader._parsers.[Set](GetType(Double), numberConverter)
            Reader._parsers.[Set](GetType(DateTime), Function(reader As IJsonReader, type As Type) reader.ReadLiteral(Function(literal As Object) Utils.FromUnixMilliseconds(CLng(Convert.ChangeType(literal, GetType(Long), CultureInfo.InvariantCulture)))))
            Reader._parsers.[Set](GetType(Byte()), Function(reader As IJsonReader, type As Type) reader.ReadLiteral(Function(literal As Object) Convert.FromBase64String(CStr(Convert.ChangeType(literal, GetType(String), CultureInfo.InvariantCulture)))))
        End Sub

        Public Sub New(r As TextReader, options As JsonOptions)
            Me._tokenizer = New Tokenizer(r, options)
            Me._options = options
        End Sub

        Private Shared Function ResolveIntoParser(type As Type) As WriteCallback_t(Of IJsonReader, Object)
            Dim ri As ReflectionInfo = ReflectionInfo.GetReflectionInfo(type)
            Dim result As WriteCallback_t(Of IJsonReader, Object)
            If ri IsNot Nothing Then
                result = AddressOf ri.ParseInto
            Else
                result = Nothing
            End If
            Return result
        End Function

        Private Shared Function ResolveParser(type As Type) As ReadCallback_t(Of IJsonReader, Type, Object)
            Dim parseJson As MethodInfo = ReflectionInfo.FindParseJson(type)
            Dim result As ReadCallback_t(Of IJsonReader, Type, Object)
            If parseJson IsNot Nothing Then
                If parseJson.GetParameters()(0).ParameterType Is GetType(IJsonReader) Then
                    result = (Function(r As IJsonReader, t As Type) parseJson.Invoke(Nothing, New Object() {r}))
                Else
                    result = Function(r As IJsonReader, t As Type)
                                 If r.GetLiteralKind() = LiteralKind.[String] Then
                                     Dim o As Object = parseJson.Invoke(Nothing, New Object() {r.GetLiteralString()})
                                     r.NextToken()
                                     Return o
                                 End If
                                 Throw New InvalidDataException(String.Format("Expected string literal for type {0}", type.FullName))
                             End Function
                End If
            Else
                result = Function(r As IJsonReader, t As Type)
                             Dim into As Object = DecoratingActivator.CreateInstance(type)
                             r.ParseInto(into)
                             Return into
                         End Function
            End If
            Return result
        End Function

        Public Function ReadLiteral(converter As ReadCallback_t(Of Object, Object)) As Object Implements IJsonReader.ReadLiteral
            Me._tokenizer.Check(Token.Literal)
            Dim retv As Object = converter(Me._tokenizer.LiteralValue)
            Me._tokenizer.NextToken()
            Return retv
        End Function

        Public Sub CheckEOF()
            Me._tokenizer.Check(Token.EOF)
        End Sub

        Public Function Parse(type As Type) As Object Implements IJsonReader.Parse
            Dim result As Object
            If Me._tokenizer.CurrentToken = Token.Literal AndAlso Me._tokenizer.LiteralKind = LiteralKind.Null Then
                Me._tokenizer.NextToken()
                result = Nothing
            Else
                Dim typeUnderlying As Type = Nullable.GetUnderlyingType(type)
                If typeUnderlying IsNot Nothing Then
                    type = typeUnderlying
                End If
                Dim factory As ReadCallback_t(Of IJsonReader, String, Object) = Nothing
                Dim parser As ReadCallback_t(Of IJsonReader, Type, Object) = Nothing
                Dim intoParser As WriteCallback_t(Of IJsonReader, Object) = Nothing
                If Reader._parsers.TryGetValue(type, parser) Then
                    result = parser(Me, type)
                ElseIf Reader._typeFactories.TryGetValue(type, factory) Then
                    Dim into As Object = factory(Me, Nothing)
                    If into Is Nothing Then
                        Me._tokenizer.CreateBookmark()
                        Me._tokenizer.Skip(Token.OpenBrace)
                        Me.ParseDictionaryKeys(Function(key As String)
                                                   into = factory(Me, key)
                                                   Return into Is Nothing
                                               End Function)
                        Me._tokenizer.RewindToBookmark()
                        If into Is Nothing Then
                            Throw New InvalidOperationException("Factory didn't create object instance (probably due to a missing key in the Json)")
                        End If
                    End If
                    Me.ParseInto(into)
                    result = into
                ElseIf Reader._intoParsers.TryGetValue(type, intoParser) Then
                    Dim into2 As Object = DecoratingActivator.CreateInstance(type)
                    Me.ParseInto(into2)
                    result = into2
                ElseIf type.IsEnum Then

                    If PetaJson.Enumerable.Any(Of Object)(type.GetCustomAttributes(GetType(FlagsAttribute), False)) Then
                        result = Me.ReadLiteral(Function(literal As Object)
                                                    Dim result2 As Object
                                                    Try
                                                        result2 = [Enum].Parse(type, CStr(literal))
                                                    Catch ex_16 As System.Exception
                                                        result2 = [Enum].ToObject(type, literal)
                                                    End Try
                                                    Return result2
                                                End Function)
                    Else
                        result = Me.ReadLiteral(Function(literal As Object)
                                                    Dim result2 As Object
                                                    Try
                                                        result2 = [Enum].Parse(type, CStr(literal))
                                                    Catch ex_16 As Exception
                                                        Dim attr As Object = PetaJson.Enumerable.FirstOrDefault(Of Object)(type.GetCustomAttributes(GetType(JsonUnknownAttribute), False))

                                                        If attr Is Nothing Then
                                                            Throw
                                                        End If
                                                        result2 = (CType(attr, JsonUnknownAttribute)).UnknownValue
                                                    End Try
                                                    Return result2
                                                End Function)
                    End If
                ElseIf type.IsArray AndAlso type.GetArrayRank() = 1 Then
                    Dim listType As Type = GetType(List(Of )).MakeGenericType(New Type() {type.GetElementType()})
                    Dim list As Object = DecoratingActivator.CreateInstance(listType)
                    Me.ParseInto(list)
                    result = listType.GetMethod("ToArray").Invoke(list, Nothing)
                Else
                    If type.IsInterface Then
                        type = Utils.ResolveInterfaceToClass(type)
                    End If
                    If Me._tokenizer.CurrentToken = Token.OpenBrace AndAlso type.IsAssignableFrom(GetType(IDictionary(Of String, Object))) Then
                        Dim container As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
                        Me.ParseDictionary(Sub(key As String)
                                               container(key) = Me.Parse(GetType(Object))
                                           End Sub)
                        result = container
                    ElseIf Me._tokenizer.CurrentToken = Token.OpenSquare AndAlso type.IsAssignableFrom(GetType(List(Of Object))) Then
                        Dim container As List(Of Object) = New List(Of Object)()
                        Me.ParseArray(Sub()
                                          container.Add(Me.Parse(GetType(Object)))
                                      End Sub)
                        result = container
                    ElseIf Me._tokenizer.CurrentToken = Token.Literal AndAlso type.IsAssignableFrom(Me._tokenizer.LiteralType) Then
                        Dim lit As Object = Me._tokenizer.LiteralValue
                        Me._tokenizer.NextToken()
                        result = lit
                    Else
                        If type.IsValueType Then
                            Dim tp As ReadCallback_t(Of IJsonReader, Type, Object) = Reader._parsers.[Get](type, Function() Reader._parserResolver(type))
                            If tp IsNot Nothing Then
                                result = tp(Me, type)
                                Return result
                            End If
                        End If
                        If Not type.IsClass OrElse Not (type IsNot GetType(Object)) Then
                            Throw New InvalidDataException(String.Format("syntax error, unexpected token {0}", Me._tokenizer.CurrentToken))
                        End If
                        Dim into2 As Object = DecoratingActivator.CreateInstance(type)
                        Me.ParseInto(into2)
                        result = into2
                    End If
                End If
            End If
            Return result
        End Function

        Public Sub ParseInto(into As Object) Implements IJsonReader.ParseInto
            If into IsNot Nothing Then
                If Me._tokenizer.CurrentToken = Token.Literal AndAlso Me._tokenizer.LiteralKind = LiteralKind.Null Then
                    Throw New InvalidOperationException("can't parse null into existing instance")
                End If
                Dim type As Type = into.[GetType]()
                Dim parseInto As WriteCallback_t(Of IJsonReader, Object) = Nothing
                If Reader._intoParsers.TryGetValue(type, parseInto) Then
                    parseInto(Me, into)
                Else
                    Dim dictType As Type = Utils.FindGenericInterface(type, GetType(IDictionary(Of ,)))
                    If dictType IsNot Nothing Then
                        Dim typeKey As Type = dictType.GetGenericArguments()(0)
                        Dim typeValue As Type = dictType.GetGenericArguments()(1)
                        Dim dict As IDictionary = CType(into, IDictionary)
                        dict.Clear()
                        Me.ParseDictionary(Sub(key As String)
                                               dict.Add(Convert.ChangeType(key, typeKey), Me.Parse(typeValue))
                                           End Sub)
                    Else
                        Dim listType As Type = Utils.FindGenericInterface(type, GetType(IList(Of )))
                        If listType IsNot Nothing Then
                            Dim typeElement As Type = listType.GetGenericArguments()(0)
                            Dim list As IList = CType(into, IList)
                            list.Clear()
                            Me.ParseArray(Sub()
                                              list.Add(Me.Parse(typeElement))
                                          End Sub)
                        Else
                            Dim objDict As IDictionary = TryCast(into, IDictionary)
                            If objDict IsNot Nothing Then
                                objDict.Clear()
                                Me.ParseDictionary(Sub(key As String)
                                                       objDict(key) = Me.Parse(GetType(Object))
                                                   End Sub)
                            Else
                                Dim objList As IList = TryCast(into, IList)
                                If objList IsNot Nothing Then
                                    objList.Clear()
                                    Me.ParseArray(Sub()
                                                      objList.Add(Me.Parse(GetType(Object)))
                                                  End Sub)
                                Else
                                    Dim intoParser As WriteCallback_t(Of IJsonReader, Object) = Reader._intoParsers.[Get](type, Function() Reader._intoParserResolver(type))
                                    If intoParser Is Nothing Then
                                        Throw New InvalidOperationException(String.Format("Don't know how to parse into type '{0}'", type.FullName))
                                    End If
                                    intoParser(Me, into)
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        End Sub

        Public Function Parse(Of T)() As T Implements IJsonReader.Parse
            Return CType((CObj(Me.Parse(GetType(T)))), T)
        End Function

        Public Function GetLiteralKind() As LiteralKind Implements IJsonReader.GetLiteralKind
            Return Me._tokenizer.LiteralKind
        End Function

        Public Function GetLiteralString() As String Implements IJsonReader.GetLiteralString
            Return Me._tokenizer.[String]
        End Function

        Public Sub NextToken() Implements IJsonReader.NextToken
            Me._tokenizer.NextToken()
        End Sub

        Public Sub ParseDictionary(callback As WriteCallback_t(Of String)) Implements IJsonReader.ParseDictionary
            Me._tokenizer.Skip(Token.OpenBrace)
            Me.ParseDictionaryKeys(Function(key As String)
                                       callback(key)
                                       Return True
                                   End Function)
            Me._tokenizer.Skip(Token.CloseBrace)
        End Sub

        Private Sub ParseDictionaryKeys(callback As ReadCallback_t(Of String, Boolean))
            While Me._tokenizer.CurrentToken <> Token.CloseBrace
                Dim key As String
                If Me._tokenizer.CurrentToken = Token.Identifier AndAlso (Me._options And JsonOptions.StrictParser) = JsonOptions.None Then
                    key = Me._tokenizer.[String]
                Else
                    If Me._tokenizer.CurrentToken <> Token.Literal OrElse Me._tokenizer.LiteralKind <> LiteralKind.[String] Then
                        Throw New InvalidDataException("syntax error, expected string literal or identifier")
                    End If
                    key = CStr(Me._tokenizer.LiteralValue)
                End If
                Me._tokenizer.NextToken()
                Me._tokenizer.Skip(Token.Colon)
                Dim pos As JsonLineOffset = Me._tokenizer.CurrentTokenPosition
                Me._contextStack.Add(key)
                Dim doDefaultProcessing As Boolean = callback(key)
                Me._contextStack.RemoveAt(Me._contextStack.Count - 1)
                If Not doDefaultProcessing Then
                    Exit While
                End If
                If pos.Line = Me._tokenizer.CurrentTokenPosition.Line AndAlso pos.Offset = Me._tokenizer.CurrentTokenPosition.Offset Then
                    Me.Parse(GetType(Object))
                End If
                If Not Me._tokenizer.SkipIf(Token.Comma) Then
                    Exit While
                End If
                If (Me._options And JsonOptions.StrictParser) <> JsonOptions.None AndAlso Me._tokenizer.CurrentToken = Token.CloseBrace Then
                    Throw New InvalidDataException("Trailing commas not allowed in strict mode")
                End If
            End While
        End Sub

        Public Sub ParseArray(callback As WriteCallback_t) Implements IJsonReader.ParseArray
            Me._tokenizer.Skip(Token.OpenSquare)
            Dim index As Integer = 0
            While Me._tokenizer.CurrentToken <> Token.CloseSquare
                Me._contextStack.Add(String.Format("[{0}]", index))
                callback()
                Me._contextStack.RemoveAt(Me._contextStack.Count - 1)
                If Not Me._tokenizer.SkipIf(Token.Comma) Then
                    Exit While
                End If
                If (Me._options And JsonOptions.StrictParser) <> JsonOptions.None AndAlso Me._tokenizer.CurrentToken = Token.CloseSquare Then
                    Throw New InvalidDataException("Trailing commas not allowed in strict mode")
                End If
            End While
            Me._tokenizer.Skip(Token.CloseSquare)
        End Sub
    End Class






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
                    Me.GetValue = AddressOf (CType(Me._mi, FieldInfo)).GetValue
                    Me.SetValue = AddressOf (CType(Me._mi, FieldInfo)).SetValue
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
                Dim method As DynamicMethod = New DynamicMethod("invoke_formatJson", Nothing, New Type() {GetType(IJsonWriter), GetType(Object)}, True)
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
                    Dim method As DynamicMethod = New DynamicMethod("dynamic_formatter", Nothing, New Type() {GetType(IJsonWriter), GetType(Object)}, True)
                    Dim il As ILGenerator = method.GetILGenerator()
                    Dim locTypedObj As LocalBuilder = il.DeclareLocal(type)
                    il.Emit(OpCodes.Ldarg_1)
                    il.Emit(If(type.IsValueType, OpCodes.Unbox_Any, OpCodes.Castclass), type)
                    il.Emit(OpCodes.Stloc, locTypedObj)
                    Dim locInvariant As LocalBuilder = il.DeclareLocal(GetType(IFormatProvider))
                    il.Emit(OpCodes.[Call], GetType(CultureInfo).GetProperty("InvariantCulture").GetGetMethod())
                    il.Emit(OpCodes.Stloc, locInvariant)
                    Dim toStringTypes As Type() = New Type() {GetType(Integer), GetType(UInteger), GetType(Long), GetType(ULong), GetType(Short), GetType(UShort), GetType(Decimal), GetType(Byte), GetType(SByte)}
                    Dim otherSupportedTypes As Type() = New Type() {GetType(Double), GetType(Single), GetType(String), GetType(Char)}
                    If GetType(IJsonWriting).IsAssignableFrom(type) Then
                        If type.IsValueType Then
                            il.Emit(OpCodes.Ldloca, locTypedObj)
                            il.Emit(OpCodes.Ldarg_0)
                            il.Emit(OpCodes.[Call], type.GetInterfaceMap(GetType(IJsonWriting)).TargetMethods(0))
                        Else
                            il.Emit(OpCodes.Ldloc, locTypedObj)
                            il.Emit(OpCodes.Castclass, GetType(IJsonWriting))
                            il.Emit(OpCodes.Ldarg_0)
                            il.Emit(OpCodes.Callvirt, GetType(IJsonWriting).GetMethod("OnJsonWriting", New Type() {GetType(IJsonWriter)}))
                        End If
                    End If
                    For Each i As JsonMemberInfo In ri.Members
                        If Not i.Deprecated Then
                            Dim pi As PropertyInfo = TryCast(i.Member, PropertyInfo)
                            If Not (pi IsNot Nothing) OrElse Not (pi.GetGetMethod(True) Is Nothing) Then
                                il.Emit(OpCodes.Ldarg_0)
                                il.Emit(OpCodes.Ldstr, i.JsonKey)
                                il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteKeyNoEscaping", New Type() {GetType(String)}))
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
                                    il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteRaw", New Type() {GetType(String)}))
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
                                    il.Emit(OpCodes.[Call], memberType.GetMethod("ToString", New Type() {GetType(IFormatProvider)}))
                                    il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteRaw", New Type() {GetType(String)}))
                                ElseIf memberType Is GetType(Single) OrElse memberType Is GetType(Double) Then
                                    il.Emit(OpCodes.Ldstr, "R")
                                    il.Emit(OpCodes.Ldloc, locInvariant)
                                    il.Emit(OpCodes.[Call], memberType.GetMethod("ToString", New Type() {GetType(String), GetType(IFormatProvider)}))
                                    il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteRaw", New Type() {GetType(String)}))
                                ElseIf memberType Is GetType(String) Then
                                    il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteStringLiteral", New Type() {GetType(String)}))
                                ElseIf memberType Is GetType(Char) Then
                                    il.Emit(OpCodes.[Call], memberType.GetMethod("ToString", New Type(-1) {}))
                                    il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteStringLiteral", New Type() {GetType(String)}))
                                ElseIf memberType Is GetType(Boolean) Then
                                    Dim lblTrue As Label = il.DefineLabel()
                                    Dim lblCont As Label = il.DefineLabel()
                                    il.Emit(OpCodes.Brtrue_S, lblTrue)
                                    il.Emit(OpCodes.Ldstr, "false")
                                    il.Emit(OpCodes.Br_S, lblCont)
                                    il.MarkLabel(lblTrue)
                                    il.Emit(OpCodes.Ldstr, "true")
                                    il.MarkLabel(lblCont)
                                    il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteRaw", New Type() {GetType(String)}))
                                Else
                                    If memberType.IsValueType Then
                                        il.Emit(OpCodes.Box, memberType)
                                    End If
                                    il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteValue", New Type() {GetType(Object)}))
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
                            il.Emit(OpCodes.Callvirt, GetType(IJsonWriting).GetMethod("OnJsonWritten", New Type() {GetType(IJsonWriter)}))
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
                    Dim method As DynamicMethod = New DynamicMethod("invoke_ParseJson", GetType(Object), New Type() {GetType(IJsonReader), GetType(Type)}, True)
                    Dim il As ILGenerator = method.GetILGenerator()
                    il.Emit(OpCodes.Ldarg_0)
                    il.Emit(OpCodes.[Call], parseJson)
                    il.Emit(OpCodes.Box, type)
                    il.Emit(OpCodes.Ret)
                    result = CType(method.CreateDelegate(GetType(ReadCallback_t(Of IJsonReader, Type, Object))), ReadCallback_t(Of IJsonReader, Type, Object))
                Else
                    Dim method As DynamicMethod = New DynamicMethod("invoke_ParseJson", GetType(Object), New Type() {GetType(String)}, True)
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
                    Dim boxType As Type = GetType(Emit.PseudoBox(Of )).MakeGenericType(New Type() {type})
                    For Each i As JsonMemberInfo In ri.Members
                        Dim pi As PropertyInfo = TryCast(i.Member, PropertyInfo)
                        Dim fi As FieldInfo = TryCast(i.Member, FieldInfo)
                        If Not (pi IsNot Nothing) OrElse Not (pi.GetSetMethod(True) Is Nothing) Then
                            Dim method As DynamicMethod = New DynamicMethod("dynamic_parser", Nothing, New Type() {GetType(IJsonReader), GetType(Object)}, True)
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
                Dim boxType As Type = GetType(Emit.PseudoBox(Of )).MakeGenericType(New Type() {type})
                Dim method As DynamicMethod = New DynamicMethod("dynamic_invoke_" + tItf.Name, Nothing, New Type() {GetType(Object), GetType(IJsonReader)}, True)
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
                Dim boxType As Type = GetType(Emit.PseudoBox(Of )).MakeGenericType(New Type() {type})
                Dim method As DynamicMethod = New DynamicMethod("dynamic_invoke_" + tItf.Name, GetType(Boolean), New Type() {GetType(Object), GetType(IJsonReader), GetType(String)}, True)
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
                    If Not (pi IsNot Nothing) OrElse Not (pi.GetSetMethod(True) Is Nothing) Then
                        If Not (pi IsNot Nothing) OrElse Not (pi.GetGetMethod(True) Is Nothing) OrElse Not i.KeepInstance Then
                            Dim method As DynamicMethod = New DynamicMethod("dynamic_parser", Nothing, New Type() {GetType(IJsonReader), GetType(Object)}, True)
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
                                il.Emit(OpCodes.Callvirt, GetType(IJsonReader).GetMethod("ParseInto", New Type() {GetType(Object)}))
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
            If Not (con Is Nothing) Then
                Dim method As DynamicMethod = New DynamicMethod("dynamic_factory", GetType(Object), New Type() {GetType(IJsonReader), GetType(WriteCallback_t(Of IJsonReader, Object))}, True)
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
                                                                         il.Emit(OpCodes.[Call], GetType(Emit).GetMethod(helperName, New Type() {GetType(IJsonReader)}))
                                                                         il.Emit(OpCodes.Ldarg_0)
                                                                         il.Emit(OpCodes.Callvirt, GetType(IJsonReader).GetMethod("NextToken", New Type(-1) {}))
                                                                     End Sub
            Dim numericTypes As Type() = New Type() {GetType(Integer), GetType(UInteger), GetType(Long), GetType(ULong), GetType(Short), GetType(UShort), GetType(Decimal), GetType(Byte), GetType(SByte), GetType(Double), GetType(Single)}
            If m.MemberType Is GetType(String) Then
                generateCallToHelper("GetLiteralString")
            ElseIf m.MemberType Is GetType(Boolean) Then
                generateCallToHelper("GetLiteralBool")
            ElseIf m.MemberType Is GetType(Char) Then
                generateCallToHelper("GetLiteralChar")
            ElseIf numericTypes.TypeArrayContains(m.MemberType) Then
                il.Emit(OpCodes.Ldarg_0)
                il.Emit(OpCodes.[Call], GetType(Emit).GetMethod("GetLiteralNumber", New Type() {GetType(IJsonReader)}))
                il.Emit(OpCodes.[Call], GetType(CultureInfo).GetProperty("InvariantCulture").GetGetMethod())
                il.Emit(OpCodes.[Call], m.MemberType.GetMethod("Parse", New Type() {GetType(String), GetType(IFormatProvider)}))
                il.Emit(OpCodes.Ldarg_0)
                il.Emit(OpCodes.Callvirt, GetType(IJsonReader).GetMethod("NextToken", New Type(-1) {}))
            Else
                il.Emit(OpCodes.Ldarg_0)
                il.Emit(OpCodes.Ldtoken, m.MemberType)
                il.Emit(OpCodes.[Call], GetType(Type).GetMethod("GetTypeFromHandle", New Type() {GetType(RuntimeTypeHandle)}))
                il.Emit(OpCodes.Callvirt, GetType(IJsonReader).GetMethod("Parse", New Type() {GetType(Type)}))
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


