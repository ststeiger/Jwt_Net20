Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Reflection

Namespace XXXX.PetaJson.Internal


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

                    If XXXX.PetaJson.Enumerable.Any(Of Object)(type.GetCustomAttributes(GetType(FlagsAttribute), False)) Then
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
                                                        Dim attr As Object = XXXX.PetaJson.Enumerable.FirstOrDefault(Of Object)(type.GetCustomAttributes(GetType(JsonUnknownAttribute), False))

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
End Namespace
