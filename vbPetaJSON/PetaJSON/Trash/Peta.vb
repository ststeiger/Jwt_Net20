
' PetaJson v0.5 - A simple but flexible Json library in a single .cs file.
' 
' Copyright (C) 2014 Topten Software (contact@toptensoftware.com) All rights reserved.
' 
' Licensed under the Apache License, Version 2.0 (the "License"); you may not use this product 
' except in compliance with the License. You may obtain a copy of the License at
' 
' http://www.apache.org/licenses/LICENSE-2.0
' 
' Unless required by applicable law or agreed to in writing, software distributed under the 
' License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
' either express or implied. See the License for the specific language governing permissions 
' and limitations under the License.

' Define PETAJSON_NO_DYNAMIC to disable Expando support
' Define PETAJSON_NO_EMIT to disable Reflection.Emit
' Define PETAJSON_NO_DATACONTRACT to disable support for [DataContract]/[DataMember]


#Const PETAJSON_NO_DYNAMIC = True
#Const PETAJSON_NO_EMIT = True
#Const PETAJSON_NO_DATACONTRACT = True
' #Const WITH_RWLOCK = True


Imports System.Collections.Generic
Imports System.Text
Imports System.IO
Imports System.Reflection
Imports System.Globalization
Imports System.Collections
Imports System.Threading


#If Not PETAJSON_NO_DYNAMIC Then
Imports System.Dynamic
#End If

#If Not PETAJSON_NO_EMIT Then
Imports System.Reflection.Emit
#End If

#If Not PETAJSON_NO_DATACONTRACT Then
Imports System.Runtime.Serialization
#End If



Namespace JWT.PetaJson

    ' Func 
    Public Delegate Function ReadCallback_t(Of Out TResult)() As TResult
    Public Delegate Function ReadCallback_t(Of In T, Out TResult)(arg As T) As TResult
    Public Delegate Function ReadCallback_t(Of In T1, In T2, Out TResult)(arg1 As T1, arg2 As T2) As TResult
    Public Delegate Function ReadCallback_t(Of In T1, In T2, In T3, Out TResult)(arg1 As T1, arg2 As T2, arg3 As T3) As TResult


    ' Action 
    Public Delegate Sub WriteCallback_t()
    Public Delegate Sub WriteCallback_t(Of In T)(obj As T)
    Public Delegate Sub WriteCallback_t(Of In T1, In T2)(arg1 As T1, arg2 As T2)

    ' Pass to format/write/parse functions to override defaults
    <Flags> _
    Public Enum JsonOptions
        None = 0
        WriteWhitespace = &H1
        DontWriteWhitespace = &H2
        StrictParser = &H4
        NonStrictParser = &H8
    End Enum


    ' API
    Public Module Json

        Sub New()
            WriteWhitespaceDefault = True
            StrictParserDefault = False

#If Not PETAJSON_NO_EMIT Then
            Json.SetFormatterResolver(AddressOf Internal.Emit.MakeFormatter)
            Json.SetParserResolver(AddressOf Internal.Emit.MakeParser)
            Json.SetIntoParserResolver(AddressOf Internal.Emit.MakeIntoParser)
#End If
        End Sub

        ' Pretty format default
        Public Property WriteWhitespaceDefault() As Boolean
            Get
                Return m_WriteWhitespaceDefault
            End Get
            Set(value As Boolean)
                m_WriteWhitespaceDefault = value
            End Set
        End Property
        Private m_WriteWhitespaceDefault As Boolean

        ' Strict parser
        Public Property StrictParserDefault() As Boolean
            Get
                Return m_StrictParserDefault
            End Get
            Set(value As Boolean)
                m_StrictParserDefault = value
            End Set
        End Property
        Private m_StrictParserDefault As Boolean

        ' Write an object to a text writer
        Public Sub Write(w As TextWriter, o As Object, Optional options As JsonOptions = JsonOptions.None)
            Dim writer As New Internal.Writer(w, ResolveOptions(options))
            writer.WriteValue(o)
        End Sub

        ' Write an object to a file
        Public Sub WriteFile(filename As String, o As Object, Optional options As JsonOptions = JsonOptions.None)
            Using w As New StreamWriter(filename)
                Write(w, o, options)
            End Using
        End Sub

        ' Format an object as a json string
        Public Function Format(o As Object, Optional options As JsonOptions = JsonOptions.None) As String
            Dim sw As New StringWriter()
            Dim writer As New Internal.Writer(sw, ResolveOptions(options))
            writer.WriteValue(o)
            Return sw.ToString()
        End Function

        ' Parse an object of specified type from a text reader
        Public Function Parse(r As TextReader, type As Type, Optional options As JsonOptions = JsonOptions.None) As Object
            Dim reader As Internal.Reader = Nothing
            Try
                reader = New Internal.Reader(r, ResolveOptions(options))
                Dim retv As Object = reader.Parse(type)
                reader.CheckEOF()
                Return retv
            Catch x As Exception
                Dim loc As JsonLineOffset = If(reader Is Nothing, New JsonLineOffset(), reader.CurrentTokenPosition)
                Console.WriteLine("Exception thrown while parsing JSON at {0}, context:{1}" & vbLf & "{2}", loc, reader.Context, x.ToString())
                Throw New JsonParseException(x, reader.Context, loc)
            End Try
        End Function

        ' Parse an object of specified type from a text reader
        Public Function Parse(Of T)(r As TextReader, Optional options As JsonOptions = JsonOptions.None) As T
            Return DirectCast(Parse(r, GetType(T), options), T)
        End Function

        ' Parse from text reader into an already instantied object
        Public Sub ParseInto(r As TextReader, into As [Object], Optional options As JsonOptions = JsonOptions.None)
            If into Is Nothing Then
                Throw New NullReferenceException()
            End If
            If into.[GetType]().IsValueType Then
                Throw New InvalidOperationException("Can't ParseInto a value type")
            End If

            Dim reader As Internal.Reader = Nothing
            Try
                reader = New Internal.Reader(r, ResolveOptions(options))
                reader.ParseInto(into)
                reader.CheckEOF()
            Catch x As Exception
                Dim loc As JsonLineOffset = If(reader Is Nothing, New JsonLineOffset(), reader.CurrentTokenPosition)
                Console.WriteLine("Exception thrown while parsing JSON at {0}, context:{1}" & vbLf & "{2}", loc, reader.Context, x.ToString())
                Throw New JsonParseException(x, reader.Context, loc)
            End Try
        End Sub

        ' Parse an object of specified type from a file
        Public Function ParseFile(filename As String, type As Type, Optional options As JsonOptions = JsonOptions.None) As Object
            Using r As New StreamReader(filename)
                Return Parse(r, type, options)
            End Using
        End Function

        ' Parse an object of specified type from a file
        Public Function ParseFile(Of T)(filename As String, Optional options As JsonOptions = JsonOptions.None) As T
            Using r As New StreamReader(filename)
                Return Parse(Of T)(r, options)
            End Using
        End Function

        ' Parse from file into an already instantied object
        Public Sub ParseFileInto(filename As String, into As [Object], Optional options As JsonOptions = JsonOptions.None)
            Using r As New StreamReader(filename)
                ParseInto(r, into, options)
            End Using
        End Sub

        ' Parse an object from a string
        Public Function Parse(data As String, type As Type, Optional options As JsonOptions = JsonOptions.None) As Object
            Return Parse(New StringReader(data), type, options)
        End Function

        ' Parse an object from a string
        Public Function Parse(Of T)(data As String, Optional options As JsonOptions = JsonOptions.None) As T
            Return DirectCast(Parse(Of T)(New StringReader(data), options), T)
        End Function

        ' Parse from string into an already instantiated object
        Public Sub ParseInto(data As String, into As [Object], Optional options As JsonOptions = JsonOptions.None)
            ParseInto(New StringReader(data), into, options)
        End Sub

        ' Create a clone of an object
        Public Function Clone(Of T)(source As T) As T
            Return DirectCast(Reparse(source.[GetType](), source), T)
        End Function

        ' Create a clone of an object (untyped)
        Public Function Clone(source As Object) As Object
            Return Reparse(source.[GetType](), source)
        End Function

        ' Clone an object into another instance
        Public Sub CloneInto(dest As Object, source As Object)
            ReparseInto(dest, source)
        End Sub

        ' Reparse an object by writing to a stream and re-reading (possibly
        ' as a different type).
        Public Function Reparse(type As Type, source As Object) As Object
            If source Is Nothing Then
                Return Nothing
            End If
            Dim ms As New MemoryStream()
            Try
                ' Write
                Dim w As New StreamWriter(ms)
                Json.Write(w, source)
                w.Flush()

                ' Read
                ms.Seek(0, SeekOrigin.Begin)
                Dim r As New StreamReader(ms)
                Return Json.Parse(r, type)
            Finally
                ms.Dispose()
            End Try
        End Function

        ' Typed version of above
        Public Function Reparse(Of T)(source As Object) As T
            Return DirectCast(Reparse(GetType(T), source), T)
        End Function

        ' Reparse one object into another object 
        Public Sub ReparseInto(dest As Object, source As Object)
            Dim ms As New MemoryStream()
            Try
                ' Write
                Dim w As New StreamWriter(ms)
                Json.Write(w, source)
                w.Flush()

                ' Read
                ms.Seek(0, SeekOrigin.Begin)
                Dim r As New StreamReader(ms)
                Json.ParseInto(r, dest)
            Finally
                ms.Dispose()
            End Try
        End Sub

        ' Register a callback that can format a value of a particular type into json
        Public Sub RegisterFormatter(type As Type, formatter As WriteCallback_t(Of IJsonWriter, Object))
            Internal.Writer._formatters(type) = formatter
        End Sub

        ' Typed version of above
        Public Sub RegisterFormatter(Of T)(formatter As WriteCallback_t(Of IJsonWriter, T))
            Json.RegisterFormatter(GetType(T), Sub(w As IJsonWriter, o As Object)
                                                      formatter(w, CType((CObj(o)), T))
                                                  End Sub)
        End Sub

        ' Register a parser for a specified type
        Public Sub RegisterParser(type As Type, parser As ReadCallback_t(Of IJsonReader, Type, Object))
            Internal.Reader._parsers.[Set](type, parser)
        End Sub

        ' Register a typed parser
        Public Sub RegisterParser(Of T)(parser As ReadCallback_t(Of IJsonReader, Type, T))
            RegisterParser(GetType(T), Function(r, tt) parser(r, tt))
        End Sub

        ' Simpler version for simple types
        Public Sub RegisterParser(type As Type, parser As ReadCallback_t(Of Object, Object))
            RegisterParser(type, Function(r, t) r.ReadLiteral(parser))
        End Sub

        ' Simpler and typesafe parser for simple types
        Public Sub RegisterParser(Of T)(parser As ReadCallback_t(Of Object, T))
            RegisterParser(GetType(T), Function(literal) parser(literal))
        End Sub

        ' Register an into parser
        Public Sub RegisterIntoParser(type As Type, parser As WriteCallback_t(Of IJsonReader, Object))
            Internal.Reader._intoParsers.[Set](type, parser)
        End Sub

        ' Register an into parser
        Public Sub RegisterIntoParser(Of T)(parser As WriteCallback_t(Of IJsonReader, Object))
            RegisterIntoParser(GetType(T), parser)
        End Sub

        ' Register a factory for instantiating objects (typically abstract classes)
        ' Callback will be invoked for each key in the dictionary until it returns an object
        ' instance and which point it will switch to serialization using reflection
        Public Sub RegisterTypeFactory(type As Type, factory As ReadCallback_t(Of IJsonReader, String, Object))
            Internal.Reader._typeFactories.[Set](type, factory)
        End Sub

        ' Register a callback to provide a formatter for a newly encountered type
        Public Sub SetFormatterResolver(resolver As ReadCallback_t(Of Type, WriteCallback_t(Of IJsonWriter, Object)))
            Internal.Writer._formatterResolver = resolver
        End Sub

        ' Register a callback to provide a parser for a newly encountered value type
        Public Sub SetParserResolver(resolver As ReadCallback_t(Of Type, ReadCallback_t(Of IJsonReader, Type, Object)))
            Internal.Reader._parserResolver = resolver
        End Sub

        ' Register a callback to provide a parser for a newly encountered reference type
        Public Sub SetIntoParserResolver(resolver As ReadCallback_t(Of Type, WriteCallback_t(Of IJsonReader, Object)))
            Internal.Reader._intoParserResolver = resolver
        End Sub

        <System.Runtime.CompilerServices.Extension> _
        Public Function WalkPath(This As IDictionary(Of String, Object), Path As String, create As Boolean, leafCallback As ReadCallback_t(Of IDictionary(Of String, Object), String, Boolean)) As Boolean
            ' Walk the path
            Dim parts As String() = Path.Split("."c)
            For i As Integer = 0 To parts.Length - 2
                Dim val As Object
                If Not This.TryGetValue(parts(i), val) Then
                    If Not create Then
                        Return False
                    End If

                    val = New Dictionary(Of String, Object)()
                    This(parts(i)) = val
                End If
                This = DirectCast(val, IDictionary(Of String, Object))
            Next

            ' Process the leaf
            Return leafCallback(This, parts(parts.Length - 1))
        End Function

        <System.Runtime.CompilerServices.Extension> _
        Public Function PathExists(This As IDictionary(Of String, Object), Path As String) As Boolean
            Return This.WalkPath(Path, False, Function(dict, key) dict.ContainsKey(key))
        End Function

        <System.Runtime.CompilerServices.Extension> _
        Public Function GetPath(This As IDictionary(Of String, Object), type As Type, Path As String, def As Object) As Object
            This.WalkPath(Path, False, Function(dict, key)
                                           Dim val As Object
                                           If dict.TryGetValue(key, val) Then
                                               If val Is Nothing Then
                                                   def = val
                                               ElseIf type.IsAssignableFrom(val.[GetType]()) Then
                                                   def = val
                                               Else
                                                   def = Reparse(type, val)
                                               End If
                                           End If
                                           Return True

                                       End Function)

            Return def
        End Function

        ' Ensure there's an object of type T at specified path
        <System.Runtime.CompilerServices.Extension> _
        Public Function GetObjectAtPath(Of T As {Class, New})(This As IDictionary(Of String, Object), Path As String) As T
            Dim retVal As T = Nothing
            This.WalkPath(Path, True, Function(dict, key)
                                          Dim val As Object
                                          dict.TryGetValue(key, val)
                                          retVal = TryCast(val, T)
                                          If retVal Is Nothing Then
                                              retVal = If(val Is Nothing, New T(), Reparse(Of T)(val))
                                              dict(key) = retVal
                                          End If
                                          Return True

                                      End Function)

            Return retVal
        End Function

        <System.Runtime.CompilerServices.Extension> _
        Public Function GetPath(Of T)(This As IDictionary(Of String, Object), Path As String, Optional def As T = Nothing) As T
            Return DirectCast(This.GetPath(GetType(T), Path, def), T)
        End Function

        <System.Runtime.CompilerServices.Extension> _
        Public Sub SetPath(This As IDictionary(Of String, Object), Path As String, value As Object)
            This.WalkPath(Path, True, Function(dict, key)
                                          dict(key) = value
                                          Return True

                                      End Function)
        End Sub

        ' Resolve passed options        
        Private Function ResolveOptions(options As JsonOptions) As JsonOptions
            Dim resolved As JsonOptions = JsonOptions.None

            If (options And (JsonOptions.WriteWhitespace Or JsonOptions.DontWriteWhitespace)) <> 0 Then
                resolved = resolved Or options And (JsonOptions.WriteWhitespace Or JsonOptions.DontWriteWhitespace)
            Else
                resolved = resolved Or If(WriteWhitespaceDefault, JsonOptions.WriteWhitespace, JsonOptions.DontWriteWhitespace)
            End If

            If (options And (JsonOptions.StrictParser Or JsonOptions.NonStrictParser)) <> 0 Then
                resolved = resolved Or options And (JsonOptions.StrictParser Or JsonOptions.NonStrictParser)
            Else
                resolved = resolved Or If(StrictParserDefault, JsonOptions.StrictParser, JsonOptions.NonStrictParser)
            End If

            Return resolved
        End Function
    End Module

    ' Called before loading via reflection
    <Obfuscation(Exclude:=True, ApplyToMembers:=True)> _
    Public Interface IJsonLoading
        Sub OnJsonLoading(r As IJsonReader)
    End Interface

    ' Called after loading via reflection
    <Obfuscation(Exclude:=True, ApplyToMembers:=True)> _
    Public Interface IJsonLoaded
        Sub OnJsonLoaded(r As IJsonReader)
    End Interface

    ' Called for each field while loading from reflection
    ' Return true if handled
    <Obfuscation(Exclude:=True, ApplyToMembers:=True)> _
    Public Interface IJsonLoadField
        Function OnJsonField(r As IJsonReader, key As String) As Boolean
    End Interface

    ' Called when about to write using reflection
    <Obfuscation(Exclude:=True, ApplyToMembers:=True)> _
    Public Interface IJsonWriting
        Sub OnJsonWriting(w As IJsonWriter)
    End Interface

    ' Called after written using reflection
    <Obfuscation(Exclude:=True, ApplyToMembers:=True)> _
    Public Interface IJsonWritten
        Sub OnJsonWritten(w As IJsonWriter)
    End Interface

    ' Describes the current literal in the json stream
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

    ' Passed to registered parsers
    <Obfuscation(Exclude:=True, ApplyToMembers:=True)> _
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

    ' Passed to registered formatters
    <Obfuscation(Exclude:=True, ApplyToMembers:=True)> _
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

    ' Exception thrown for any parse error
    Public Class JsonParseException
        Inherits Exception


        Public Sub New(inner As Exception, context__1 As String, position__2 As JsonLineOffset)
            MyBase.New(String.Format("JSON parse error at {0}{1} - {2}", position__2, If(String.IsNullOrEmpty(context__1), "", String.Format(", context {0}", context__1)), inner.Message), inner)
            Position = position__2
            Context = context__1
        End Sub


        Public Position As JsonLineOffset
        Public Context As String
    End Class

    ' Represents a line and character offset position in the source Json
    Public Structure JsonLineOffset
        Public Line As Integer
        Public Offset As Integer

        Public Overrides Function ToString() As String
            Return String.Format("line {0}, character {1}", Line + 1, Offset + 1)
        End Function

    End Structure

    ' Used to decorate fields and properties that should be serialized
    '
    ' - [Json] on class or struct causes all public fields and properties to be serialized
    ' - [Json] on a public or non-public field or property causes that member to be serialized
    ' - [JsonExclude] on a field or property causes that field to be not serialized
    ' - A class or struct with no [Json] attribute has all public fields/properties serialized
    ' - A class or struct with no [Json] attribute but a [Json] attribute on one or more members only serializes those members
    '
    ' Use [Json("keyname")] to explicitly specify the key to be used 
    ' [Json] without the keyname will be serialized using the name of the member with the first letter lowercased.
    '
    ' [Json(KeepInstance=true)] causes container/subobject types to be serialized into the existing member instance (if not null)
    '
    ' You can also use the system supplied DataContract and DataMember attributes.  They'll only be used if there
    ' are no PetaJson attributes on the class or it's members. You must specify DataContract on the type and
    ' DataMember on any fields/properties that require serialization.  There's no need for exclude attribute.
    ' When using DataMember, the name of the field or property is used as is - the first letter is left in upper case
    '
    <AttributeUsage(AttributeTargets.[Class] Or AttributeTargets.Struct Or AttributeTargets.[Property] Or AttributeTargets.Field)> _
    Public Class JsonAttribute
        Inherits Attribute


        Public Sub New()
            _key = Nothing
        End Sub

        Public Sub New(key As String)
            _key = key
        End Sub

        ' Key used to save this field/property
        Private _key As String
        Public ReadOnly Property Key() As String
            Get
                Return _key
            End Get
        End Property


        ' If true uses ParseInto to parse into the existing object instance
        ' If false, creates a new instance as assigns it to the property
        Public Property KeepInstance() As Boolean
            Get
                Return m_KeepInstance
            End Get
            Set(value As Boolean)
                m_KeepInstance = value
            End Set
        End Property


        Private m_KeepInstance As Boolean

        ' If true, the property will be loaded, but not saved
        ' Use to upgrade deprecated persisted settings, but not
        ' write them back out again
        Public Property Deprecated() As Boolean
            Get
                Return m_Deprecated
            End Get
            Set(value As Boolean)
                m_Deprecated = value
            End Set
        End Property

        Private m_Deprecated As Boolean
    End Class


    ' See comments for JsonAttribute above
    <AttributeUsage(AttributeTargets.[Property] Or AttributeTargets.Field)> _
    Public Class JsonExcludeAttribute
        Inherits Attribute
        Public Sub New()
        End Sub
    End Class


    ' Apply to enum values to specify which enum value to select
    ' if the supplied json value doesn't match any.
    ' If not found throws an exception
    ' eg, any unknown values in the json will be mapped to Fruit.unknown
    '
    '   [JsonUnknown(Fruit.unknown)]
    '   enum Fruit
    '   {
    '      unknown,
    '      Apple,
    '      Pear,
    '   }
    <AttributeUsage(AttributeTargets.[Enum])> _
    Public Class JsonUnknownAttribute
        Inherits Attribute


        Public Sub New(unknownValue__1 As Object)
            UnknownValue = unknownValue__1
        End Sub


        Public Property UnknownValue() As Object
            Get
                Return m_UnknownValue
            End Get
            Private Set(value As Object)
                m_UnknownValue = value
            End Set
        End Property


        Private m_UnknownValue As Object
    End Class


    Namespace Internal
        <Obfuscation(Exclude:=True, ApplyToMembers:=True)> _
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


        ' Helper to create instances but include the type name in the thrown exception
        Public Module DecoratingActivator


            Public Function CreateInstance(t As Type) As Object
                Try
                    Return Activator.CreateInstance(t)
                Catch x As Exception
                    Throw New InvalidOperationException(String.Format("Failed to create instance of type '{0}'", t.FullName), x)
                End Try
            End Function

        End Module


        Public Class Reader
            Implements IJsonReader

            Shared Sub New()
                ' Setup default resolvers
                _parserResolver = AddressOf ResolveParser
                _intoParserResolver = AddressOf ResolveIntoParser

                Dim simpleConverter As ReadCallback_t(Of IJsonReader, Type, Object) = Function(reader__1, type)
                                                                                          Return reader__1.ReadLiteral(Function(literal) Convert.ChangeType(literal, type, CultureInfo.InvariantCulture))

                                                                                      End Function

                Dim numberConverter As ReadCallback_t(Of IJsonReader, Type, Object) = Function(reader__1, type)
                                                                                          Select Case reader__1.GetLiteralKind()
                                                                                              Case LiteralKind.SignedInteger, LiteralKind.UnsignedInteger, LiteralKind.FloatingPoint
                                                                                                  Dim val As Object = Convert.ChangeType(reader__1.GetLiteralString(), type, CultureInfo.InvariantCulture)
                                                                                                  reader__1.NextToken()
                                                                                                  Return val
                                                                                          End Select
                                                                                          Throw New InvalidDataException("expected a numeric literal")

                                                                                      End Function

                ' Default type handlers
                _parsers.[Set](GetType(String), simpleConverter)
                _parsers.[Set](GetType(Char), simpleConverter)
                _parsers.[Set](GetType(Boolean), simpleConverter)
                _parsers.[Set](GetType(Byte), numberConverter)
                _parsers.[Set](GetType(SByte), numberConverter)
                _parsers.[Set](GetType(Short), numberConverter)
                _parsers.[Set](GetType(UShort), numberConverter)
                _parsers.[Set](GetType(Integer), numberConverter)
                _parsers.[Set](GetType(UInteger), numberConverter)
                _parsers.[Set](GetType(Long), numberConverter)
                _parsers.[Set](GetType(ULong), numberConverter)
                _parsers.[Set](GetType(Decimal), numberConverter)
                _parsers.[Set](GetType(Single), numberConverter)
                _parsers.[Set](GetType(Double), numberConverter)
                _parsers.[Set](GetType(DateTime), Function(reader__1, type)
                                                      Return reader__1.ReadLiteral(Function(literal) Utils.FromUnixMilliseconds(CLng(Convert.ChangeType(literal, GetType(Long), CultureInfo.InvariantCulture))))

                                                  End Function)
                _parsers.[Set](GetType(Byte()), Function(reader__1, type)
                                                    Return reader__1.ReadLiteral(Function(literal) Convert.FromBase64String(DirectCast(Convert.ChangeType(literal, GetType(String), CultureInfo.InvariantCulture), String)))

                                                End Function)
            End Sub

            Public Sub New(r As TextReader, options As JsonOptions)
                _tokenizer = New Tokenizer(r, options)
                _options = options
            End Sub

            Private _tokenizer As Tokenizer
            Private _options As JsonOptions
            Private _contextStack As New List(Of String)()

            Public ReadOnly Property Context() As String
                Get
                    Return String.Join(".", _contextStack.ToArray())
                End Get
            End Property

            Private Shared Function ResolveIntoParser(type As Type) As WriteCallback_t(Of IJsonReader, Object)
                Dim ri As ReflectionInfo = ReflectionInfo.GetReflectionInfo(type)
                If ri IsNot Nothing Then
                    Return AddressOf ri.ParseInto
                Else
                    Return Nothing
                End If
            End Function

            Private Shared Function ResolveParser(type As Type) As ReadCallback_t(Of IJsonReader, Type, Object)
                ' See if the Type has a static parser method - T ParseJson(IJsonReader)
                Dim parseJson As System.Reflection.MethodInfo = ReflectionInfo.FindParseJson(type)
                If parseJson IsNot Nothing Then
                    If parseJson.GetParameters()(0).ParameterType Is GetType(IJsonReader) Then
                        Return Function(r, t) parseJson.Invoke(Nothing, New [Object]() {r})
                    Else
                        Return Function(r, t)
                                   If r.GetLiteralKind() = LiteralKind.[String] Then
                                       Dim o As Object = parseJson.Invoke(Nothing, New [Object]() {r.GetLiteralString()})
                                       r.NextToken()
                                       Return o
                                   End If
                                   Throw New InvalidDataException(String.Format("Expected string literal for type {0}", type.FullName))

                               End Function
                    End If
                End If

                Return Function(r, t)

                           Dim into As Object = DecoratingActivator.CreateInstance(type)
                           r.ParseInto(into)
                           Return into

                       End Function
            End Function

            Public ReadOnly Property CurrentTokenPosition() As JsonLineOffset
                Get
                    Return _tokenizer.CurrentTokenPosition
                End Get
            End Property

            ' ReadLiteral is implemented with a converter callback so that any
            ' errors on converting to the target type are thrown before the tokenizer
            ' is advanced to the next token.  This ensures error location is reported 
            ' at the start of the literal, not the following token.
            Public Function ReadLiteral(converter As ReadCallback_t(Of Object, Object)) As Object Implements IJsonReader.ReadLiteral
                _tokenizer.Check(Token.Literal)
                Dim retv As Object = converter(_tokenizer.LiteralValue)
                _tokenizer.NextToken()
                Return retv
            End Function

            Public Sub CheckEOF()
                _tokenizer.Check(Token.EOF)
            End Sub

            Public Function Parse(type As Type) As Object Implements IJsonReader.Parse
                ' Null?
                If _tokenizer.CurrentToken = Token.Literal AndAlso _tokenizer.LiteralKind = LiteralKind.Null Then
                    _tokenizer.NextToken()
                    Return Nothing
                End If

                ' Handle nullable types
                Dim typeUnderlying As System.Type = Nullable.GetUnderlyingType(type)
                If typeUnderlying IsNot Nothing Then
                    type = typeUnderlying
                End If

                ' See if we have a reader
                Dim parser As ReadCallback_t(Of IJsonReader, Type, Object)
                If Reader._parsers.TryGetValue(type, parser) Then
                    Return parser(Me, type)
                End If

                ' See if we have factory
                Dim factory As ReadCallback_t(Of IJsonReader, String, Object)
                If Reader._typeFactories.TryGetValue(type, factory) Then
                    ' Try first without passing dictionary keys
                    Dim into As Object = factory(Me, Nothing)
                    If into Is Nothing Then
                        ' This is a awkward situation.  The factory requires a value from the dictionary
                        ' in order to create the target object (typically an abstract class with the class
                        ' kind recorded in the Json).  Since there's no guarantee of order in a json dictionary
                        ' we can't assume the required key is first.
                        ' So, create a bookmark on the tokenizer, read keys until the factory returns an
                        ' object instance and then rewind the tokenizer and continue

                        ' Create a bookmark so we can rewind
                        _tokenizer.CreateBookmark()

                        ' Skip the opening brace
                        _tokenizer.Skip(Token.OpenBrace)

                        ' First pass to work out type
                        ParseDictionaryKeys(Function(key)
                                                ' Try to instantiate the object
                                                into = factory(Me, key)
                                                Return into Is Nothing
                                            End Function)

                        ' Move back to start of the dictionary
                        _tokenizer.RewindToBookmark()

                        ' Quit if still didn't get an object from the factory
                        If into Is Nothing Then
                            Throw New InvalidOperationException("Factory didn't create object instance (probably due to a missing key in the Json)")
                        End If
                    End If

                    ' Second pass
                    ParseInto(into)

                    ' Done
                    Return into
                End If

                ' Do we already have an into parser?
                Dim intoParser As WriteCallback_t(Of IJsonReader, Object)
                If Reader._intoParsers.TryGetValue(type, intoParser) Then
                    Dim into As Object = DecoratingActivator.CreateInstance(type)
                    ParseInto(into)
                    Return into
                End If

                ' Enumerated type?
                If type.IsEnum Then
                    If type.GetCustomAttributes(GetType(FlagsAttribute), False).Any() Then
                        Return ReadLiteral(Function(literal)
                                               Try
                                                   Return [Enum].Parse(type, DirectCast(literal, String))
                                               Catch
                                                   Return [Enum].ToObject(type, literal)
                                               End Try

                                           End Function)
                    Else
                        Return ReadLiteral(Function(literal)

                                               Try
                                                   Return [Enum].Parse(type, DirectCast(literal, String))
                                               Catch generatedExceptionName As Exception
                                                   Dim attr As Object = type.GetCustomAttributes(GetType(JsonUnknownAttribute), False).FirstOrDefault()
                                                   If attr Is Nothing Then
                                                       Throw
                                                   End If

                                                   Return DirectCast(attr, JsonUnknownAttribute).UnknownValue

                                               End Try

                                           End Function)
                    End If
                End If

                ' Array?
                If type.IsArray AndAlso type.GetArrayRank() = 1 Then
                    ' First parse as a List<>
                    Dim listType As System.Type = GetType(List(Of )).MakeGenericType(type.GetElementType())
                    Dim list As Object = DecoratingActivator.CreateInstance(listType)
                    ParseInto(list)

                    Return listType.GetMethod("ToArray").Invoke(list, Nothing)
                End If

                ' Convert interfaces to concrete types
                If type.IsInterface Then
                    type = Utils.ResolveInterfaceToClass(type)
                End If

                ' Untyped dictionary?
                If _tokenizer.CurrentToken = Token.OpenBrace AndAlso (type.IsAssignableFrom(GetType(IDictionary(Of String, Object)))) Then
#If Not PETAJSON_NO_DYNAMIC Then
                    Dim container As IDictionary(Of String, Object) = TryCast(New ExpandoObject(), IDictionary(Of String, Object))
#Else
                    Dim container As New Dictionary(Of String, Object)()
#End If
                    ParseDictionary(Sub(key)
                                        container(key) = Parse(GetType(Object))
                                    End Sub)

                    Return container
                End If

                ' Untyped list?
                If _tokenizer.CurrentToken = Token.OpenSquare AndAlso (type.IsAssignableFrom(GetType(List(Of Object)))) Then
                    Dim container As New List(Of Object)()
                    ParseArray(Sub()
                                   container.Add(Parse(GetType(Object)))
                               End Sub)
                    Return container
                End If

                ' Untyped literal?
                If _tokenizer.CurrentToken = Token.Literal AndAlso type.IsAssignableFrom(_tokenizer.LiteralType) Then
                    Dim lit As Object = _tokenizer.LiteralValue
                    _tokenizer.NextToken()
                    Return lit
                End If

                ' Call value type resolver
                If type.IsValueType Then
                    Dim tp As ReadCallback_t(Of IJsonReader, System.Type, Object) = _parsers.[Get](type, Function() _parserResolver(type))
                    If tp IsNot Nothing Then
                        Return tp(Me, type)
                    End If
                End If

                ' Call reference type resolver
                If type.IsClass AndAlso type IsNot GetType(Object) Then
                    Dim into As Object = DecoratingActivator.CreateInstance(type)
                    ParseInto(into)
                    Return into
                End If

                ' Give up
                Throw New InvalidDataException(String.Format("syntax error, unexpected token {0}", _tokenizer.CurrentToken))
            End Function

            ' Parse into an existing object instance
            Public Sub ParseInto(into As Object) Implements IJsonReader.ParseInto
                If into Is Nothing Then
                    Return
                End If

                If _tokenizer.CurrentToken = Token.Literal AndAlso _tokenizer.LiteralKind = LiteralKind.Null Then
                    'return;
                    Throw New InvalidOperationException("can't parse null into existing instance")
                End If

                Dim type As System.Type = into.[GetType]()

                ' Existing parse into handler?
                Dim parseInto__1 As WriteCallback_t(Of IJsonReader, Object)
                If _intoParsers.TryGetValue(type, parseInto__1) Then
                    parseInto__1(Me, into)
                    Return
                End If

                ' Generic dictionary?
                Dim dictType As System.Type = Utils.FindGenericInterface(type, GetType(IDictionary(Of ,)))
                If dictType IsNot Nothing Then
                    ' Get the key and value types
                    Dim typeKey As System.Type = dictType.GetGenericArguments()(0)
                    Dim typeValue As System.Type = dictType.GetGenericArguments()(1)

                    ' Parse it
                    Dim dict As IDictionary = DirectCast(into, IDictionary)
                    dict.Clear()
                    ParseDictionary(Sub(key)
                                        dict.Add(Convert.ChangeType(key, typeKey), Parse(typeValue))
                                    End Sub)

                    Return
                End If

                ' Generic list
                Dim listType As System.Type = Utils.FindGenericInterface(type, GetType(IList(Of )))
                If listType IsNot Nothing Then
                    ' Get element type
                    Dim typeElement As System.Type = listType.GetGenericArguments()(0)

                    ' Parse it
                    Dim list As IList = DirectCast(into, IList)
                    list.Clear()
                    ParseArray(Sub()
                                   list.Add(Parse(typeElement))
                               End Sub)

                    Return
                End If

                ' Untyped dictionary
                Dim objDict As IDictionary = TryCast(into, IDictionary)
                If objDict IsNot Nothing Then
                    objDict.Clear()
                    ParseDictionary(Sub(key)
                                        objDict(key) = Parse(GetType(Object))
                                    End Sub)
                    Return
                End If

                ' Untyped list
                Dim objList As IList = TryCast(into, IList)
                If objList IsNot Nothing Then
                    objList.Clear()
                    ParseArray(Sub()
                                   objList.Add(Parse(GetType(Object)))
                               End Sub)
                    Return
                End If

                ' Try to resolve a parser
                Dim intoParser As WriteCallback_t(Of IJsonReader, Object) = _intoParsers.[Get](type, Function() _intoParserResolver(type))
                If intoParser IsNot Nothing Then
                    intoParser(Me, into)
                    Return
                End If

                Throw New InvalidOperationException(String.Format("Don't know how to parse into type '{0}'", type.FullName))
            End Sub

            Public Function Parse(Of T)() As T Implements IJsonReader.Parse
                Return DirectCast(Parse(GetType(T)), T)
            End Function

            Public Function GetLiteralKind() As LiteralKind Implements IJsonReader.GetLiteralKind
                Return _tokenizer.LiteralKind
            End Function

            Public Function GetLiteralString() As String Implements IJsonReader.GetLiteralString
                Return _tokenizer.[String]
            End Function

            Public Sub NextToken() Implements IJsonReader.NextToken
                _tokenizer.NextToken()
            End Sub

            ' Parse a dictionary
            Public Sub ParseDictionary(callback As WriteCallback_t(Of String)) Implements IJsonReader.ParseDictionary
                _tokenizer.Skip(Token.OpenBrace)
                ParseDictionaryKeys(Function(key)
                                        callback(key)
                                        Return True

                                    End Function)
                _tokenizer.Skip(Token.CloseBrace)
            End Sub

            ' Parse dictionary keys, calling callback for each one.  Continues until end of input
            ' or when callback returns false
            Private Sub ParseDictionaryKeys(callback As ReadCallback_t(Of String, Boolean))
                ' End?
                While _tokenizer.CurrentToken <> Token.CloseBrace
                    ' Parse the key
                    Dim key As String = Nothing
                    If _tokenizer.CurrentToken = Token.Identifier AndAlso (_options And JsonOptions.StrictParser) = 0 Then
                        key = _tokenizer.[String]
                    ElseIf _tokenizer.CurrentToken = Token.Literal AndAlso _tokenizer.LiteralKind = LiteralKind.[String] Then
                        key = DirectCast(_tokenizer.LiteralValue, String)
                    Else
                        Throw New InvalidDataException("syntax error, expected string literal or identifier")
                    End If
                    _tokenizer.NextToken()
                    _tokenizer.Skip(Token.Colon)

                    ' Remember current position
                    Dim pos As JsonLineOffset = _tokenizer.CurrentTokenPosition

                    ' Call the callback, quit if cancelled
                    _contextStack.Add(key)
                    Dim doDefaultProcessing As Boolean = callback(key)
                    _contextStack.RemoveAt(_contextStack.Count - 1)
                    If Not doDefaultProcessing Then
                        Return
                    End If

                    ' If the callback didn't read anything from the tokenizer, then skip it ourself
                    If pos.Line = _tokenizer.CurrentTokenPosition.Line AndAlso pos.Offset = _tokenizer.CurrentTokenPosition.Offset Then
                        Parse(GetType(Object))
                    End If

                    ' Separating/trailing comma
                    If _tokenizer.SkipIf(Token.Comma) Then
                        If (_options And JsonOptions.StrictParser) <> 0 AndAlso _tokenizer.CurrentToken = Token.CloseBrace Then
                            Throw New InvalidDataException("Trailing commas not allowed in strict mode")
                        End If
                        Continue While
                    End If

                    ' End
                    Exit While
                End While
            End Sub

            ' Parse an array
            Public Sub ParseArray(callback As WriteCallback_t) Implements IJsonReader.ParseArray
                _tokenizer.Skip(Token.OpenSquare)

                Dim index As Integer = 0
                While _tokenizer.CurrentToken <> Token.CloseSquare
                    _contextStack.Add(String.Format("[{0}]", index))
                    callback()
                    _contextStack.RemoveAt(_contextStack.Count - 1)

                    If _tokenizer.SkipIf(Token.Comma) Then
                        If (_options And JsonOptions.StrictParser) <> 0 AndAlso _tokenizer.CurrentToken = Token.CloseSquare Then
                            Throw New InvalidDataException("Trailing commas not allowed in strict mode")
                        End If
                        Continue While
                    End If
                    Exit While
                End While

                _tokenizer.Skip(Token.CloseSquare)
            End Sub

            ' Yikes!
            Public Shared _intoParserResolver As ReadCallback_t(Of Type, WriteCallback_t(Of IJsonReader, Object))
            Public Shared _parserResolver As ReadCallback_t(Of Type, ReadCallback_t(Of IJsonReader, Type, Object))
            Public Shared _parsers As New ThreadSafeCache(Of Type, ReadCallback_t(Of IJsonReader, Type, Object))()
            Public Shared _intoParsers As New ThreadSafeCache(Of Type, WriteCallback_t(Of IJsonReader, Object))()
            Public Shared _typeFactories As New ThreadSafeCache(Of Type, ReadCallback_t(Of IJsonReader, String, Object))()

        End Class




        Public Class Writer
            Implements IJsonWriter


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





            Public Shared _formatterResolver As ReadCallback_t(Of Type, WriteCallback_t(Of IJsonWriter, Object))
            Public Shared _formatters As New Dictionary(Of Type, WriteCallback_t(Of IJsonWriter, Object))()

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
                _writer = w
                _atStartOfLine = True
                _needElementSeparator = False
                _options = options
            End Sub

            Private _writer As TextWriter
            Private IndentLevel As Integer
            Private _atStartOfLine As Boolean
            Private _needElementSeparator As Boolean = False
            Private _options As JsonOptions
            Private _currentBlockKind As Char = ControlChars.NullChar

            ' Move to the next line
            Public Sub NextLine()
                If _atStartOfLine Then
                    Return
                End If

                If (_options And JsonOptions.WriteWhitespace) <> 0 Then
                    WriteRaw(vbLf)
                    WriteRaw(New String(ControlChars.Tab, IndentLevel))
                End If
                _atStartOfLine = True
            End Sub

            ' Start the next element, writing separators and white space
            Private Sub NextElement()
                If _needElementSeparator Then
                    WriteRaw(",")
                    NextLine()
                Else
                    NextLine()
                    IndentLevel += 1
                    WriteRaw(_currentBlockKind.ToString())
                    NextLine()
                End If

                _needElementSeparator = True
            End Sub

            ' Write next array element
            Public Sub WriteElement() Implements IJsonWriter.WriteElement
                If _currentBlockKind <> "["c Then
                    Throw New InvalidOperationException("Attempt to write array element when not in array block")
                End If
                NextElement()
            End Sub

            ' Write next dictionary key
            Public Sub WriteKey(key As String) Implements IJsonWriter.WriteKey
                If _currentBlockKind <> "{"c Then
                    Throw New InvalidOperationException("Attempt to write dictionary element when not in dictionary block")
                End If
                NextElement()
                WriteStringLiteral(key)
                WriteRaw(If(((_options And JsonOptions.WriteWhitespace) <> 0), ": ", ":"))
            End Sub

            ' Write an already escaped dictionary key
            Public Sub WriteKeyNoEscaping(key As String) Implements IJsonWriter.WriteKeyNoEscaping
                If _currentBlockKind <> "{"c Then
                    Throw New InvalidOperationException("Attempt to write dictionary element when not in dictionary block")
                End If
                NextElement()
                WriteRaw("""")
                WriteRaw(key)
                WriteRaw("""")
                WriteRaw(If(((_options And JsonOptions.WriteWhitespace) <> 0), ": ", ":"))
            End Sub

            ' Write anything
            Public Sub WriteRaw(str As String) Implements IJsonWriter.WriteRaw
                _atStartOfLine = False
                _writer.Write(str)
            End Sub

            Private Shared Function IndexOfEscapeableChar(str As String, pos As Integer) As Integer
                Dim length As Integer = str.Length
                While pos < length
                    Dim ch As Char = str(pos)
                    If ch = "\"c OrElse ch = "/"c OrElse ch = """"c OrElse (AscW(ch) >= 0 AndAlso AscW(ch) <= &H1F) OrElse (AscW(ch) >= &H7F AndAlso AscW(ch) <= &H9F) OrElse AscW(ch) = &H2028 OrElse AscW(ch) = &H2029 Then
                        Return pos
                    End If
                    pos += 1
                End While

                Return -1
            End Function

            Public Sub WriteStringLiteral(str As String) Implements IJsonWriter.WriteStringLiteral
                _atStartOfLine = False
                If str Is Nothing Then
                    _writer.Write("null")
                    Return
                End If
                _writer.Write("""")

                Dim pos As Integer = 0
                Dim escapePos As Integer
                While (InlineAssignHelper(escapePos, IndexOfEscapeableChar(str, pos))) >= 0
                    If escapePos > pos Then
                        _writer.Write(str.Substring(pos, escapePos - pos))
                    End If

                    Select Case str(escapePos)
                        Case """"c
                            _writer.Write("\""")
                            Exit Select
                        Case "\"c
                            _writer.Write("\\")
                            Exit Select
                        Case "/"c
                            _writer.Write("\/")
                            Exit Select
                        Case ControlChars.Back
                            _writer.Write("\b")
                            Exit Select
                        Case ControlChars.FormFeed
                            _writer.Write("\f")
                            Exit Select
                        Case ControlChars.Lf
                            _writer.Write("\n")
                            Exit Select
                        Case ControlChars.Cr
                            _writer.Write("\r")
                            Exit Select
                        Case ControlChars.Tab
                            _writer.Write("\t")
                            Exit Select
                        Case Else
                            _writer.Write(String.Format("\u{0:x4}", AscW(str(escapePos))))
                            Exit Select
                    End Select

                    pos = escapePos + 1
                End While


                If str.Length > pos Then
                    _writer.Write(str.Substring(pos))
                End If
                _writer.Write("""")
            End Sub

            ' Write an array or dictionary block
            Private Sub WriteBlock(open As String, close As String, callback As WriteCallback_t)
                Dim prevBlockKind As Char = _currentBlockKind
                _currentBlockKind = open(0)

                Dim didNeedElementSeparator As Boolean = _needElementSeparator
                _needElementSeparator = False

                callback()

                If _needElementSeparator Then
                    IndentLevel -= 1
                    NextLine()
                Else
                    WriteRaw(open)
                End If
                WriteRaw(close)

                _needElementSeparator = didNeedElementSeparator
                _currentBlockKind = prevBlockKind
            End Sub

            ' Write an array
            Public Sub WriteArray(callback As WriteCallback_t) Implements IJsonWriter.WriteArray
                WriteBlock("[", "]", callback)
            End Sub

            ' Write a dictionary
            Public Sub WriteDictionary(callback As WriteCallback_t) Implements IJsonWriter.WriteDictionary
                WriteBlock("{", "}", callback)
            End Sub

            ' Write any value
            Public Sub WriteValue(value As Object) Implements IJsonWriter.WriteValue
                _atStartOfLine = False

                ' Special handling for null
                If value Is Nothing Then
                    _writer.Write("null")
                    Return
                End If

                Dim type As System.Type = value.GetType()

                ' Handle nullable types
                Dim typeUnderlying As System.Type = Nullable.GetUnderlyingType(type)
                If typeUnderlying IsNot Nothing Then
                    type = typeUnderlying
                End If

                ' Look up type writer
                Dim typeWriter As WriteCallback_t(Of IJsonWriter, Object)
                If _formatters.TryGetValue(type, typeWriter) Then
                    ' Write it
                    typeWriter(Me, value)
                    Return
                End If

                ' Enumerated type?
                If type.IsEnum Then
                    If type.GetCustomAttributes(GetType(FlagsAttribute), False).Any() Then
                        WriteRaw(Convert.ToUInt32(value).ToString(CultureInfo.InvariantCulture))
                    Else
                        WriteStringLiteral(value.ToString())
                    End If
                    Return
                End If

                ' Dictionary?
                Dim d As System.Collections.IDictionary = TryCast(value, System.Collections.IDictionary)
                If d IsNot Nothing Then
                    WriteDictionary(Sub()
                                        For Each key As Object In d.Keys
                                            WriteKey(key.ToString())
                                            WriteValue(d(key))
                                        Next

                                    End Sub)
                    Return
                End If

                ' Dictionary?
                Dim dso As IDictionary(Of String, Object) = TryCast(value, IDictionary(Of String, Object))
                If dso IsNot Nothing Then
                    WriteDictionary(Sub()
                                        For Each key As String In dso.Keys
                                            WriteKey(key.ToString())
                                            WriteValue(dso(key))
                                        Next

                                    End Sub)
                    Return
                End If

                ' Array?
                Dim e As System.Collections.IEnumerable = TryCast(value, System.Collections.IEnumerable)
                If e IsNot Nothing Then
                    WriteArray(Sub()
                                   For Each i As Object In e
                                       WriteElement()
                                       WriteValue(i)
                                   Next

                               End Sub)
                    Return
                End If

                ' Resolve a formatter
                Dim formatter As WriteCallback_t(Of IJsonWriter, Object) = _formatterResolver(type)
                If formatter IsNot Nothing Then
                    _formatters(type) = formatter
                    formatter(Me, value)
                    Return
                End If

                ' Give up
                Throw New InvalidDataException(String.Format("Don't know how to write '{0}' to json", value.[GetType]()))
            End Sub


            Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function

        End Class

        ' Information about a field or property found through reflection
        Public Class JsonMemberInfo
            ' The Json key for this member
            Public JsonKey As String

            ' True if should keep existing instance (reference types only)
            Public KeepInstance As Boolean

            ' True if deprecated
            Public Deprecated As Boolean



            ' Reflected member info
            Private _mi As MemberInfo
            Public Property Member() As MemberInfo
                Get
                    Return _mi
                End Get
                Set(value As MemberInfo)
                    ' Store it
                    _mi = value

                    ' Also create getters and setters
                    If TypeOf _mi Is PropertyInfo Then
                        GetValue = Function(obj) DirectCast(_mi, PropertyInfo).GetValue(obj, Nothing)
                        SetValue = Sub(obj, val) DirectCast(_mi, PropertyInfo).SetValue(obj, val, Nothing)
                    Else
                        GetValue = AddressOf DirectCast(_mi, FieldInfo).GetValue
                        SetValue = AddressOf DirectCast(_mi, FieldInfo).SetValue
                    End If
                End Set
            End Property

            ' Member type
            Public ReadOnly Property MemberType() As Type
                Get
                    If TypeOf Member Is PropertyInfo Then
                        Return DirectCast(Member, PropertyInfo).PropertyType
                    Else
                        Return DirectCast(Member, FieldInfo).FieldType
                    End If
                End Get
            End Property

            ' Get/set helpers
            Public SetValue As WriteCallback_t(Of Object, Object)
            Public GetValue As ReadCallback_t(Of Object, Object)
        End Class

        ' Stores reflection info about a type
        Public Class ReflectionInfo
            ' List of members to be serialized
            Public Members As List(Of JsonMemberInfo)

            ' Cache of these ReflectionInfos's
            Shared _cache As New ThreadSafeCache(Of Type, ReflectionInfo)()

            Public Shared Function FindFormatJson(type As Type) As MethodInfo
                If type.IsValueType Then
                    ' Try `void FormatJson(IJsonWriter)`
                    Dim formatJson As System.Reflection.MethodInfo = type.GetMethod("FormatJson", BindingFlags.[Public] Or BindingFlags.NonPublic Or BindingFlags.Instance, Nothing, New Type() {GetType(IJsonWriter)}, Nothing)
                    If formatJson IsNot Nothing AndAlso formatJson.ReturnType Is GetType(System.Void) Then
                        Return formatJson
                    End If

                    ' Try `string FormatJson()`
                    formatJson = type.GetMethod("FormatJson", BindingFlags.[Public] Or BindingFlags.NonPublic Or BindingFlags.Instance, Nothing, New Type() {}, Nothing)
                    If formatJson IsNot Nothing AndAlso formatJson.ReturnType Is GetType(String) Then
                        Return formatJson
                    End If
                End If
                Return Nothing
            End Function

            Public Shared Function FindParseJson(type As Type) As MethodInfo
                ' Try `T ParseJson(IJsonReader)`
                Dim parseJson As System.Reflection.MethodInfo = type.GetMethod("ParseJson", BindingFlags.[Public] Or BindingFlags.NonPublic Or BindingFlags.[Static], Nothing, New Type() {GetType(IJsonReader)}, Nothing)
                If parseJson IsNot Nothing AndAlso parseJson.ReturnType Is type Then
                    Return parseJson
                End If

                ' Try `T ParseJson(string)`
                parseJson = type.GetMethod("ParseJson", BindingFlags.[Public] Or BindingFlags.NonPublic Or BindingFlags.[Static], Nothing, New Type() {GetType(String)}, Nothing)
                If parseJson IsNot Nothing AndAlso parseJson.ReturnType Is type Then
                    Return parseJson
                End If

                Return Nothing
            End Function

            ' Write one of these types
            Public Sub Write(w As IJsonWriter, val As Object)
                w.WriteDictionary(Sub()
                                      Dim writing As IJsonWriting = TryCast(val, IJsonWriting)
                                      If writing IsNot Nothing Then
                                          writing.OnJsonWriting(w)
                                      End If

                                      For Each jmi As JsonMemberInfo In Members

                                          If Not jmi.Deprecated Then
                                              w.WriteKeyNoEscaping(jmi.JsonKey)
                                              w.WriteValue(jmi.GetValue(val))
                                              ' End if(!jmi.Deprecated)
                                          End If
                                      Next
                                      ' Next jmi 
                                      Dim written As IJsonWritten = TryCast(val, IJsonWritten)
                                      If written IsNot Nothing Then
                                          written.OnJsonWritten(w)
                                      End If

                                  End Sub)
            End Sub

            ' Read one of these types.
            ' NB: Although PetaJson.JsonParseInto only works on reference type, when using reflection
            '     it also works for value types so we use the one method for both
            Public Sub ParseInto(r As IJsonReader, into As Object)
                Dim loading As IJsonLoading = TryCast(into, IJsonLoading)
                If loading IsNot Nothing Then
                    loading.OnJsonLoading(r)
                End If

                r.ParseDictionary(Sub(key)
                                      ParseFieldOrProperty(r, into, key)
                                  End Sub)

                Dim loaded As IJsonLoaded = TryCast(into, IJsonLoaded)
                If loaded IsNot Nothing Then
                    loaded.OnJsonLoaded(r)
                End If
            End Sub

            ' The member info is stored in a list (as opposed to a dictionary) so that
            ' the json is written in the same order as the fields/properties are defined
            ' On loading, we assume the fields will be in the same order, but need to
            ' handle if they're not.  This function performs a linear search, but
            ' starts after the last found item as an optimization that should work
            ' most of the time.
            Private _lastFoundIndex As Integer = 0
            Private Function FindMemberInfo(name As String, ByRef found As JsonMemberInfo) As Boolean
                For i As Integer = 0 To Members.Count - 1
                    Dim index As Integer = (i + _lastFoundIndex) Mod Members.Count
                    Dim jmi As JsonMemberInfo = Members(index)
                    If jmi.JsonKey = name Then
                        _lastFoundIndex = index
                        found = jmi
                        Return True
                    End If
                Next
                found = Nothing
                Return False
            End Function

            ' Parse a value from IJsonReader into an object instance
            Public Sub ParseFieldOrProperty(r As IJsonReader, into As Object, key As String)
                ' IJsonLoadField
                Dim lf As IJsonLoadField = TryCast(into, IJsonLoadField)
                If lf IsNot Nothing AndAlso lf.OnJsonField(r, key) Then
                    Return
                End If

                ' Find member
                Dim jmi As JsonMemberInfo
                If FindMemberInfo(key, jmi) Then
                    ' Try to keep existing instance
                    If jmi.KeepInstance Then
                        Dim subInto As Object = jmi.GetValue(into)
                        If subInto IsNot Nothing Then
                            r.ParseInto(subInto)
                            Return
                        End If
                    End If

                    ' Parse and set
                    Dim val As Object = r.Parse(jmi.MemberType)
                    jmi.SetValue(into, val)
                    Return
                End If
            End Sub

            ' Get the reflection info for a specified type
            Public Shared Function GetReflectionInfo(type As Type) As ReflectionInfo
                ' Check cache
                Return _cache.[Get](type, Function()
                                              Dim allMembers As IEnumerable(Of MemberInfo) = Utils.GetAllFieldsAndProperties(type)

                                              ' Does type have a [Json] attribute
                                              Dim typeMarked As Boolean = type.GetCustomAttributes(GetType(JsonAttribute), True).OfType(Of JsonAttribute)().Any()

                                              ' Do any members have a [Json] attribute
                                              Dim anyFieldsMarked As Boolean = allMembers.Any(Function(x) x.GetCustomAttributes(GetType(JsonAttribute), False).OfType(Of JsonAttribute)().Any())

#If Not PETAJSON_NO_DATACONTRACT Then
				' Try with DataContract and friends
				If Not typeMarked AndAlso Not anyFieldsMarked AndAlso type.GetCustomAttributes(GetType(DataContractAttribute), True).OfType(Of DataContractAttribute)().Any() Then
					Dim ri As ReflectionInfo = CreateReflectionInfo(type, Function(mi) 
					' Get attributes
					Dim attr As Object() = mi.GetCustomAttributes(GetType(DataMemberAttribute), False).OfType(Of DataMemberAttribute)().FirstOrDefault()
					If attr IsNot Nothing Then
							' No lower case first letter if using DataContract/Member
						Return New JsonMemberInfo() With { _
							Key .Member = mi, _
							Key .JsonKey = If(attr.Name, mi.Name) _
						}
					End If

					Return Nothing

End Function)

					ri.Members.Sort(Function(a, b) [String].CompareOrdinal(a.JsonKey, b.JsonKey))
					' Match DataContractJsonSerializer
					Return ri
				End If
#End If
                                              If True Then
                                                  ' Should we serialize all public methods?
                                                  Dim serializeAllPublics As Boolean = typeMarked OrElse Not anyFieldsMarked

                                                  ' Build 
                                                  Dim ri As ReflectionInfo = CreateReflectionInfo(type, Function(mi)
                                                                                                            ' Explicitly excluded?
                                                                                                            If mi.GetCustomAttributes(GetType(JsonExcludeAttribute), False).Any() Then
                                                                                                                Return Nothing
                                                                                                            End If

                                                                                                            ' Get attributes
                                                                                                            Dim attr As JsonAttribute = mi.GetCustomAttributes(GetType(JsonAttribute), False).OfType(Of JsonAttribute)().FirstOrDefault()
                                                                                                            If attr IsNot Nothing Then
                                                                                                                Return New JsonMemberInfo() With { _
                                                                                                                     .Member = mi, _
                                                                                                                     .JsonKey = If(attr.Key, mi.Name.Substring(0, 1).ToLower() + mi.Name.Substring(1)), _
                                                                                                                     .KeepInstance = attr.KeepInstance, _
                                                                                                                     .Deprecated = attr.Deprecated _
                                                                                                                }
                                                                                                            End If

                                                                                                            ' Serialize all publics?
                                                                                                            If serializeAllPublics AndAlso Utils.IsPublic(mi) Then
                                                                                                                Return New JsonMemberInfo() With { _
                                                                                                                     .Member = mi, _
                                                                                                                     .JsonKey = mi.Name.Substring(0, 1).ToLower() + mi.Name.Substring(1) _
                                                                                                                }
                                                                                                            End If

                                                                                                            Return Nothing

                                                                                                        End Function)
                                                  Return ri
                                              End If

                                              Return Nothing
                                          End Function)
            End Function

            Public Shared Function CreateReflectionInfo(type As Type, callback As ReadCallback_t(Of MemberInfo, JsonMemberInfo)) As ReflectionInfo
                ' Work out properties and fields

                Dim members As New List(Of JsonMemberInfo)()
                For Each thisMember As MemberInfo In Utils.GetAllFieldsAndProperties(type)
                    Dim mi As JsonMemberInfo = callback(thisMember)
                    If mi IsNot Nothing Then
                        members.Add(mi)
                    End If
                Next


                ' Anything with KeepInstance must be a reference type
                Dim invalid As JsonMemberInfo = members.FirstOrDefault(Function(x) x.KeepInstance AndAlso x.MemberType.IsValueType)
                If invalid IsNot Nothing Then
                    Throw New InvalidOperationException(String.Format("KeepInstance=true can only be applied to reference types ({0}.{1})", type.FullName, invalid.Member))
                End If

                ' Must have some members
                If Not members.Any() Then
                    Return Nothing
                End If

                ' Create reflection info
                Return New ReflectionInfo() With { _
                     .Members = members _
                }
            End Function
        End Class

        Public Class ThreadSafeCache(Of TKey, TValue)

            Public Sub New()
            End Sub

            Public Function [Get](key As TKey, createIt As ReadCallback_t(Of TValue)) As TValue
                ' Check if already exists
#If WITH_RWLOCK Then
				_lock.EnterReadLock()
#End If

                Try
                    Dim val As TValue
                    If _map.TryGetValue(key, val) Then
                        Return val
                    End If
                Finally
#If WITH_RWLOCK Then
                    _lock.ExitReadLock()
#End If


                End Try

                ' Nope, take lock and try again
#If WITH_RWLOCK Then
				_lock.EnterWriteLock()
#End If

                Try
                    ' Check again before creating it
                    Dim val As TValue
                    If Not _map.TryGetValue(key, val) Then
                        ' Store the new one
                        val = createIt()
                        _map(key) = val
                    End If
                    Return val
                Finally
#If WITH_RWLOCK Then
                    _lock.ExitWriteLock()
#End If

                End Try
            End Function

            Public Function TryGetValue(key As TKey, ByRef val As TValue) As Boolean
#If WITH_RWLOCK Then
				_lock.EnterReadLock()
#End If
                Try
                    Return _map.TryGetValue(key, val)
                Finally
#If WITH_RWLOCK Then
                    _lock.ExitReadLock()
#End If
                End Try
            End Function

            Public Sub [Set](key As TKey, value As TValue)
#If WITH_RWLOCK Then
				_lock.EnterWriteLock()
#End If
                Try
                    _map(key) = value
                Finally
#If WITH_RWLOCK Then
                    _lock.ExitWriteLock()
#End If
                End Try
            End Sub

            Private _map As New Dictionary(Of TKey, TValue)()
#If WITH_RWLOCK Then
			Private _lock As New ReaderWriterLockSlim()
#End If
        End Class

        Friend NotInheritable Class Utils
            Private Sub New()
            End Sub
            ' Get all fields and properties of a type
            Public Shared Function GetAllFieldsAndProperties(t As Type) As IEnumerable(Of MemberInfo)
                If t Is Nothing Then
                    Dim lsMemberInfo As New List(Of MemberInfo)()
                    Return lsMemberInfo
                End If

                Dim flags As BindingFlags = BindingFlags.[Public] Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.DeclaredOnly

                Dim fieldsAndProps As New List(Of MemberInfo)()


                For Each x As MemberInfo In t.GetMembers(flags)
                    If TypeOf x Is FieldInfo OrElse TypeOf x Is PropertyInfo Then
                        fieldsAndProps.Add(x)
                    End If
                Next

                Dim baseFieldsAndProps As IEnumerable(Of MemberInfo) = GetAllFieldsAndProperties(t.BaseType)
                fieldsAndProps.AddRange(baseFieldsAndProps)

                Return fieldsAndProps
            End Function

            Public Shared Function FindGenericInterface(type As Type, tItf As Type) As Type
                For Each t As System.Type In type.GetInterfaces()
                    ' Is this a generic list?
                    If t.IsGenericType AndAlso t.GetGenericTypeDefinition() Is tItf Then
                        Return t
                    End If
                Next

                Return Nothing
            End Function

            Public Shared Function IsPublic(mi As MemberInfo) As Boolean
                ' Public field
                Dim fi As FieldInfo = TryCast(mi, FieldInfo)
                If fi IsNot Nothing Then
                    Return fi.IsPublic
                End If

                ' Public property
                ' (We only check the get method so we can work with anonymous types)
                Dim pi As PropertyInfo = TryCast(mi, PropertyInfo)
                If pi IsNot Nothing Then
                    Dim gm As MethodInfo = pi.GetGetMethod(True)
                    Return (gm IsNot Nothing AndAlso gm.IsPublic)
                End If

                Return False
            End Function

            Public Shared Function ResolveInterfaceToClass(tItf As Type) As Type
                ' Generic type
                If tItf.IsGenericType Then
                    Dim genDef As System.Type = tItf.GetGenericTypeDefinition()

                    ' IList<> -> List<>
                    If genDef Is GetType(IList(Of )) Then
                        Return GetType(List(Of )).MakeGenericType(tItf.GetGenericArguments())
                    End If

                    ' IDictionary<string,> -> Dictionary<string,>
                    If genDef Is GetType(IDictionary(Of ,)) AndAlso tItf.GetGenericArguments()(0) Is GetType(String) Then
                        Return GetType(Dictionary(Of ,)).MakeGenericType(tItf.GetGenericArguments())
                    End If
                End If

                ' IEnumerable -> List<object>
                If tItf Is GetType(IEnumerable) Then
                    Return GetType(List(Of Object))
                End If

                ' IDicitonary -> Dictionary<string,object>
                If tItf Is GetType(IDictionary) Then
                    Return GetType(Dictionary(Of String, Object))
                End If
                Return tItf
            End Function

            Public Shared Function ToUnixMilliseconds(This As DateTime) As Long
                Return CLng(This.Subtract(New DateTime(1970, 1, 1)).TotalMilliseconds)
            End Function

            Public Shared Function FromUnixMilliseconds(timeStamp As Long) As DateTime
                Return New DateTime(1970, 1, 1).AddMilliseconds(timeStamp)
            End Function

        End Class

        Public Class Tokenizer
            Public Sub New(r As TextReader, options As JsonOptions)
                _underlying = r
                _options = options
                FillBuffer()
                NextChar()
                NextToken()
            End Sub

            Private _options As JsonOptions
            Private _sb As New StringBuilder()
            Private _underlying As TextReader
            Private _buf As Char() = New Char(4095) {}
            Private _pos As Integer
            Private _bufUsed As Integer
            Private _rewindBuffer As StringBuilder
            Private _rewindBufferPos As Integer
            Private _currentCharPos As JsonLineOffset
            Private _currentChar As Char
            Private _bookmarks As New Stack(Of ReaderState)()

            Public CurrentTokenPosition As JsonLineOffset
            Public CurrentToken As Token
            Public LiteralKind As LiteralKind
            Public [String] As String

            Public ReadOnly Property LiteralValue() As Object
                Get
                    If CurrentToken <> Token.Literal Then
                        Throw New InvalidOperationException("token is not a literal")
                    End If
                    Select Case LiteralKind
                        Case LiteralKind.Null
                            Return Nothing
                        Case LiteralKind.[False]
                            Return False
                        Case LiteralKind.[True]
                            Return True
                        Case LiteralKind.[String]
                            Return [String]
                        Case LiteralKind.SignedInteger
                            Return Long.Parse([String], CultureInfo.InvariantCulture)
                        Case LiteralKind.UnsignedInteger
                            Return ULong.Parse([String], CultureInfo.InvariantCulture)
                        Case LiteralKind.FloatingPoint
                            Return Double.Parse([String], CultureInfo.InvariantCulture)
                    End Select
                    Return Nothing
                End Get
            End Property

            Public ReadOnly Property LiteralType() As Type
                Get
                    If CurrentToken <> Token.Literal Then
                        Throw New InvalidOperationException("token is not a literal")
                    End If
                    Select Case LiteralKind
                        Case LiteralKind.Null
                            Return GetType([Object])
                        Case LiteralKind.[False]
                            Return GetType([Boolean])
                        Case LiteralKind.[True]
                            Return GetType([Boolean])
                        Case LiteralKind.[String]
                            Return GetType(String)
                        Case LiteralKind.SignedInteger
                            Return GetType(Long)
                        Case LiteralKind.UnsignedInteger
                            Return GetType(ULong)
                        Case LiteralKind.FloatingPoint
                            Return GetType(Double)
                    End Select

                    Return Nothing
                End Get
            End Property

            ' This object represents the entire state of the reader and is used for rewind
            Private Structure ReaderState
                Public Sub New(tokenizer As Tokenizer)
                    _currentCharPos = tokenizer._currentCharPos
                    _currentChar = tokenizer._currentChar
                    _string = tokenizer.[String]
                    _literalKind = tokenizer.LiteralKind
                    _rewindBufferPos = tokenizer._rewindBufferPos
                    _currentTokenPos = tokenizer.CurrentTokenPosition
                    _currentToken = tokenizer.CurrentToken
                End Sub

                Public Sub Apply(tokenizer As Tokenizer)
                    tokenizer._currentCharPos = _currentCharPos
                    tokenizer._currentChar = _currentChar
                    tokenizer._rewindBufferPos = _rewindBufferPos
                    tokenizer.CurrentToken = _currentToken
                    tokenizer.CurrentTokenPosition = _currentTokenPos
                    tokenizer.[String] = _string
                    tokenizer.LiteralKind = _literalKind
                End Sub

                Private _currentCharPos As JsonLineOffset
                Private _currentTokenPos As JsonLineOffset
                Private _currentChar As Char
                Private _currentToken As Token
                Private _literalKind As LiteralKind
                Private _string As String
                Private _rewindBufferPos As Integer
            End Structure

            ' Create a rewind bookmark
            Public Sub CreateBookmark()
                _bookmarks.Push(New ReaderState(Me))
                If _rewindBuffer Is Nothing Then
                    _rewindBuffer = New StringBuilder()
                    _rewindBufferPos = 0
                End If
            End Sub

            ' Discard bookmark
            Public Sub DiscardBookmark()
                _bookmarks.Pop()
                If _bookmarks.Count = 0 Then
                    _rewindBuffer = Nothing
                    _rewindBufferPos = 0
                End If
            End Sub

            ' Rewind to a bookmark
            Public Sub RewindToBookmark()
                _bookmarks.Pop().Apply(Me)
            End Sub

            ' Fill buffer by reading from underlying TextReader
            Private Sub FillBuffer()
                _bufUsed = _underlying.Read(_buf, 0, _buf.Length)
                _pos = 0
            End Sub

            ' Get the next character from the input stream
            ' (this function could be extracted into a few different methods, but is mostly inlined
            '  for performance - yes it makes a difference)
            Public Function NextChar() As Char
                If _rewindBuffer Is Nothing Then
                    If _pos >= _bufUsed Then
                        If _bufUsed > 0 Then
                            FillBuffer()
                        End If
                        If _bufUsed = 0 Then
                            Return InlineAssignHelper(_currentChar, ControlChars.NullChar)
                        End If
                    End If

                    ' Next
                    _currentCharPos.Offset += 1
                    Return InlineAssignHelper(_currentChar, _buf(System.Math.Max(System.Threading.Interlocked.Increment(_pos), _pos - 1)))
                End If

                If _rewindBufferPos < _rewindBuffer.Length Then
                    _currentCharPos.Offset += 1
                    Return InlineAssignHelper(_currentChar, _rewindBuffer(System.Math.Max(System.Threading.Interlocked.Increment(_rewindBufferPos), _rewindBufferPos - 1)))
                Else
                    If _pos >= _bufUsed AndAlso _bufUsed > 0 Then
                        FillBuffer()
                    End If

                    _currentChar = If(_bufUsed = 0, ControlChars.NullChar, _buf(System.Math.Max(System.Threading.Interlocked.Increment(_pos), _pos - 1)))
                    _rewindBuffer.Append(_currentChar)
                    _rewindBufferPos += 1
                    _currentCharPos.Offset += 1
                    Return _currentChar
                End If
            End Function

            '' Read the next token from the input stream
            '' (Mostly inline for performance)
            'Public Sub NextToken()
            '    While True
            '        ' Skip whitespace and handle line numbers
            '        While True
            '            If _currentChar = ControlChars.Cr Then
            '                If NextChar() = ControlChars.Lf Then
            '                    NextChar()
            '                End If
            '                _currentCharPos.Line += 1
            '                _currentCharPos.Offset = 0
            '            ElseIf _currentChar = ControlChars.Lf Then
            '                If NextChar() = ControlChars.Cr Then
            '                    NextChar()
            '                End If
            '                _currentCharPos.Line += 1
            '                _currentCharPos.Offset = 0
            '            ElseIf _currentChar = " "c Then
            '                NextChar()
            '            ElseIf _currentChar = ControlChars.Tab Then
            '                NextChar()
            '            Else
            '                Exit While
            '            End If
            '        End While

            '        ' Remember position of token
            '        CurrentTokenPosition = _currentCharPos

            '        ' Handle common characters first
            '        Select Case _currentChar
            '            Case "/"c
            '                ' Comments not support in strict mode
            '                If (_options And JsonOptions.StrictParser) <> 0 Then
            '                    Throw New InvalidDataException(String.Format("syntax error, unexpected character '{0}'", _currentChar))
            '                End If

            '                ' Process comment
            '                NextChar()
            '                Select Case _currentChar
            '                    Case "/"c
            '                        NextChar()
            '                        While _currentChar <> ControlChars.NullChar AndAlso _currentChar <> ControlChars.Cr AndAlso _currentChar <> ControlChars.Lf
            '                            NextChar()
            '                        End While
            '                        Exit Select

            '                    Case "*"c
            '                        Dim endFound As Boolean = False
            '                        While Not endFound AndAlso _currentChar <> ControlChars.NullChar
            '                            If _currentChar = "*"c Then
            '                                NextChar()
            '                                If _currentChar = "/"c Then
            '                                    endFound = True
            '                                End If
            '                            End If
            '                            NextChar()
            '                        End While
            '                        Exit Select
            '                    Case Else

            '                        Throw New InvalidDataException("syntax error, unexpected character after slash")
            '                End Select
            '                Continue While

            '            Case """"c, "'"c
            '                If True Then
            '                    _sb.Length = 0
            '                    Dim quoteKind As Char = _currentChar
            '                    NextChar()
            '                    While _currentChar <> ControlChars.NullChar
            '                        If _currentChar = "\"c Then
            '                            NextChar()
            '                            Dim escape As Char = _currentChar
            '                            Select Case escape
            '                                Case """"c
            '                                    _sb.Append(""""c)
            '                                    Exit Select
            '                                Case "\"c
            '                                    _sb.Append("\"c)
            '                                    Exit Select
            '                                Case "/"c
            '                                    _sb.Append("/"c)
            '                                    Exit Select
            '                                Case "b"c
            '                                    _sb.Append(ControlChars.Back)
            '                                    Exit Select
            '                                Case "f"c
            '                                    _sb.Append(ControlChars.FormFeed)
            '                                    Exit Select
            '                                Case "n"c
            '                                    _sb.Append(ControlChars.Lf)
            '                                    Exit Select
            '                                Case "r"c
            '                                    _sb.Append(ControlChars.Cr)
            '                                    Exit Select
            '                                Case "t"c
            '                                    _sb.Append(ControlChars.Tab)
            '                                    Exit Select
            '                                Case "u"c
            '                                    Dim sbHex As System.Text.StringBuilder = New StringBuilder()
            '                                    For i As Integer = 0 To 3
            '                                        NextChar()
            '                                        sbHex.Append(_currentChar)
            '                                    Next
            '                                    _sb.Append(ChrW(Convert.ToUInt16(sbHex.ToString(), 16)))
            '                                    Exit Select
            '                                Case Else

            '                                    Throw New InvalidDataException(String.Format("Invalid escape sequence in string literal: '\{0}'", _currentChar))
            '                            End Select
            '                        ElseIf _currentChar = quoteKind Then
            '                            [String] = _sb.ToString()
            '                            CurrentToken = Token.Literal
            '                            LiteralKind = LiteralKind.[String]
            '                            NextChar()
            '                            Return
            '                        Else
            '                            _sb.Append(_currentChar)
            '                        End If

            '                        NextChar()
            '                    End While
            '                    Throw New InvalidDataException("syntax error, unterminated string literal")
            '                End If

            '            Case "{"c
            '                CurrentToken = Token.OpenBrace
            '                NextChar()
            '                Return
            '            Case "}"c
            '                CurrentToken = Token.CloseBrace
            '                NextChar()
            '                Return
            '            Case "["c
            '                CurrentToken = Token.OpenSquare
            '                NextChar()
            '                Return
            '            Case "]"c
            '                CurrentToken = Token.CloseSquare
            '                NextChar()
            '                Return
            '            Case "="c
            '                CurrentToken = Token.Equal
            '                NextChar()
            '                Return
            '            Case ":"c
            '                CurrentToken = Token.Colon
            '                NextChar()
            '                Return
            '            Case ";"c
            '                CurrentToken = Token.SemiColon
            '                NextChar()
            '                Return
            '            Case ","c
            '                CurrentToken = Token.Comma
            '                NextChar()
            '                Return
            '            Case ControlChars.NullChar
            '                CurrentToken = Token.EOF
            '                Return
            '        End Select

            '        ' Number?
            '        If Char.IsDigit(_currentChar) OrElse _currentChar = "-"c Then
            '            TokenizeNumber()
            '            Return
            '        End If

            '        ' Identifier?  (checked for after everything else as identifiers are actually quite rare in valid json)
            '        If [Char].IsLetter(_currentChar) OrElse _currentChar = "_"c OrElse _currentChar = "$"c Then
            '            ' Find end of identifier
            '            _sb.Length = 0
            '            While [Char].IsLetterOrDigit(_currentChar) OrElse _currentChar = "_"c OrElse _currentChar = "$"c
            '                _sb.Append(_currentChar)
            '                NextChar()
            '            End While
            '            [String] = _sb.ToString()

            '            ' Handle special identifiers
            '            Select Case [String]
            '                Case "true"
            '                    LiteralKind = LiteralKind.[True]
            '                    CurrentToken = Token.Literal
            '                    Return

            '                Case "false"
            '                    LiteralKind = LiteralKind.[False]
            '                    CurrentToken = Token.Literal
            '                    Return

            '                Case "null"
            '                    LiteralKind = LiteralKind.Null
            '                    CurrentToken = Token.Literal
            '                    Return
            '            End Select

            '            CurrentToken = Token.Identifier
            '            Return
            '        End If

            '        ' What the?
            '        Throw New InvalidDataException(String.Format("syntax error, unexpected character '{0}'", _currentChar))
            '    End While
            'End Sub



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





            ' Parse a sequence of characters that could make up a valid number
            ' For performance, we don't actually parse it into a number yet.  When using PetaJsonEmit we parse
            ' later, directly into a value type to avoid boxing
            Private Sub TokenizeNumber()
                _sb.Length = 0

                ' Leading negative sign
                Dim signed As Boolean = False
                If _currentChar = "-"c Then
                    signed = True
                    _sb.Append(_currentChar)
                    NextChar()
                End If

                ' Hex prefix?
                Dim hex As Boolean = False
                If _currentChar = "0"c AndAlso (_options And JsonOptions.StrictParser) = 0 Then
                    _sb.Append(_currentChar)
                    NextChar()
                    If _currentChar = "x"c OrElse _currentChar = "X"c Then
                        _sb.Append(_currentChar)
                        NextChar()
                        hex = True
                    End If
                End If

                ' Process characters, but vaguely figure out what type it is
                Dim cont As Boolean = True
                Dim fp As Boolean = False
                While cont
                    Select Case _currentChar
                        Case "0"c, "1"c, "2"c, "3"c, "4"c, "5"c, _
                            "6"c, "7"c, "8"c, "9"c
                            _sb.Append(_currentChar)
                            NextChar()
                            Exit Select

                        Case "A"c, "a"c, "B"c, "b"c, "C"c, "c"c, _
                            "D"c, "d"c, "F"c, "f"c
                            If Not hex Then
                                cont = False
                            Else
                                _sb.Append(_currentChar)
                                NextChar()
                            End If
                            Exit Select

                        Case "."c
                            If hex Then
                                cont = False
                            Else
                                fp = True
                                _sb.Append(_currentChar)
                                NextChar()
                            End If
                            Exit Select

                        Case "E"c, "e"c
                            If Not hex Then
                                fp = True
                                _sb.Append(_currentChar)
                                NextChar()
                                If _currentChar = "+"c OrElse _currentChar = "-"c Then
                                    _sb.Append(_currentChar)
                                    NextChar()
                                End If
                            End If
                            Exit Select
                        Case Else

                            cont = False
                            Exit Select
                    End Select
                End While

                If Char.IsLetter(_currentChar) Then
                    Throw New InvalidDataException(String.Format("syntax error, invalid character following number '{0}'", _sb.ToString()))
                End If

                ' Setup token
                [String] = _sb.ToString()
                CurrentToken = Token.Literal

                ' Setup literal kind
                If fp Then
                    LiteralKind = LiteralKind.FloatingPoint
                ElseIf signed Then
                    LiteralKind = LiteralKind.SignedInteger
                Else
                    LiteralKind = LiteralKind.UnsignedInteger
                End If
            End Sub

            ' Check the current token, throw exception if mismatch
            Public Sub Check(tokenRequired As Token)
                If tokenRequired <> CurrentToken Then
                    Throw New InvalidDataException(String.Format("syntax error, expected {0} found {1}", tokenRequired, CurrentToken))
                End If
            End Sub

            ' Skip token which must match
            Public Sub Skip(tokenRequired As Token)
                Check(tokenRequired)
                NextToken()
            End Sub

            ' Skip token if it matches
            Public Function SkipIf(tokenRequired As Token) As Boolean
                If tokenRequired = CurrentToken Then
                    NextToken()
                    Return True
                End If
                Return False
            End Function
            Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class

#If Not PETAJSON_NO_EMIT Then


        Module Emit

            <System.Runtime.CompilerServices.Extension> _
            Private Function TypeArrayContains(types As System.Type(), type As System.Type) As Boolean
                For i As Integer = 0 To types.Length - 1
                    If types(i) Is type Then
                        Return True
                    End If
                Next

                Return False
            End Function


            ' Generates a function that when passed an object of specified type, renders it to an IJsonReader
            Public Function MakeFormatter(type As Type) As WriteCallback_t(Of IJsonWriter, Object)
                Dim formatJson As System.Reflection.MethodInfo = ReflectionInfo.FindFormatJson(type)
                If formatJson IsNot Nothing Then
                    Dim method As System.Reflection.Emit.DynamicMethod = New DynamicMethod("invoke_formatJson", Nothing, New Type() {GetType(IJsonWriter), GetType([Object])}, True)
                    Dim il As System.Reflection.Emit.ILGenerator = method.GetILGenerator()
                    If formatJson.ReturnType Is GetType(String) Then
                        ' w.WriteStringLiteral(o.FormatJson())
                        il.Emit(OpCodes.Ldarg_0)
                        il.Emit(OpCodes.Ldarg_1)
                        il.Emit(OpCodes.Unbox, type)
                        il.Emit(OpCodes.[Call], formatJson)
                        il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteStringLiteral"))
                    Else
                        ' o.FormatJson(w);
                        il.Emit(OpCodes.Ldarg_1)
                        il.Emit(If(type.IsValueType, OpCodes.Unbox, OpCodes.Castclass), type)
                        il.Emit(OpCodes.Ldarg_0)
                        il.Emit(If(type.IsValueType, OpCodes.[Call], OpCodes.Callvirt), formatJson)
                    End If
                    il.Emit(OpCodes.Ret)
                    Return DirectCast(method.CreateDelegate(GetType(WriteCallback_t(Of IJsonWriter, Object))), WriteCallback_t(Of IJsonWriter, Object))
                Else
                    ' Get the reflection info for this type
                    Dim ri As ReflectionInfo = ReflectionInfo.GetReflectionInfo(type)
                    If ri Is Nothing Then
                        Return Nothing
                    End If

                    ' Create a dynamic method that can do the work
                    Dim method As New DynamicMethod("dynamic_formatter", Nothing, New Type() {GetType(IJsonWriter), GetType(Object)}, True)
                    Dim il As System.Reflection.Emit.ILGenerator = method.GetILGenerator()

                    ' Cast/unbox the target object and store in local variable
                    Dim locTypedObj As System.Reflection.Emit.LocalBuilder = il.DeclareLocal(type)
                    il.Emit(OpCodes.Ldarg_1)
                    il.Emit(If(type.IsValueType, OpCodes.Unbox_Any, OpCodes.Castclass), type)
                    il.Emit(OpCodes.Stloc, locTypedObj)

                    ' Get Invariant CultureInfo (since we'll probably be needing this)
                    Dim locInvariant As System.Reflection.Emit.LocalBuilder = il.DeclareLocal(GetType(IFormatProvider))
                    il.Emit(OpCodes.[Call], GetType(CultureInfo).GetProperty("InvariantCulture").GetGetMethod())
                    il.Emit(OpCodes.Stloc, locInvariant)

                    ' These are the types we'll call .ToString(Culture.InvariantCulture) on
                    Dim toStringTypes As System.Type() = New Type() {GetType(Integer), GetType(UInteger), GetType(Long), GetType(ULong), GetType(Short), GetType(UShort), _
                        GetType(Decimal), GetType(Byte), GetType(SByte)}

                    ' Theses types we also generate for
                    Dim otherSupportedTypes As System.Type() = New Type() {GetType(Double), GetType(Single), GetType(String), GetType(Char)}

                    ' Call IJsonWriting if implemented
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

                    ' Process all members
                    For Each m As JsonMemberInfo In ri.Members
                        ' Dont save deprecated properties
                        If m.Deprecated Then
                            Continue For
                        End If

                        ' Ignore write only properties
                        Dim pi As PropertyInfo = TryCast(m.Member, PropertyInfo)
                        If pi IsNot Nothing AndAlso pi.GetGetMethod(True) Is Nothing Then
                            Continue For
                        End If

                        ' Write the Json key
                        il.Emit(OpCodes.Ldarg_0)
                        il.Emit(OpCodes.Ldstr, m.JsonKey)
                        il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteKeyNoEscaping", New Type() {GetType(String)}))

                        ' Load the writer
                        il.Emit(OpCodes.Ldarg_0)

                        ' Get the member type
                        Dim memberType As System.Type = m.MemberType

                        ' Load the target object
                        If type.IsValueType Then
                            il.Emit(OpCodes.Ldloca, locTypedObj)
                        Else
                            il.Emit(OpCodes.Ldloc, locTypedObj)
                        End If

                        ' Work out if we need the value or it's address on the stack
                        Dim NeedValueAddress As Boolean = (memberType.IsValueType AndAlso (TypeArrayContains(toStringTypes, memberType) OrElse TypeArrayContains(otherSupportedTypes, memberType)))
                        If Nullable.GetUnderlyingType(memberType) IsNot Nothing Then
                            NeedValueAddress = True
                        End If

                        ' Property?
                        If pi IsNot Nothing Then
                            ' Call property's get method
                            If type.IsValueType Then
                                il.Emit(OpCodes.[Call], pi.GetGetMethod(True))
                            Else
                                il.Emit(OpCodes.Callvirt, pi.GetGetMethod(True))
                            End If

                            ' If we need the address then store in a local and take it's address
                            If NeedValueAddress Then
                                Dim locTemp As System.Reflection.Emit.LocalBuilder = il.DeclareLocal(memberType)
                                il.Emit(OpCodes.Stloc, locTemp)
                                il.Emit(OpCodes.Ldloca, locTemp)
                            End If
                        End If

                        ' Field?
                        Dim fi As FieldInfo = TryCast(m.Member, FieldInfo)
                        If fi IsNot Nothing Then
                            If NeedValueAddress Then
                                il.Emit(OpCodes.Ldflda, fi)
                            Else
                                il.Emit(OpCodes.Ldfld, fi)
                            End If
                        End If

                        Dim lblFinished As System.Nullable(Of Label) = Nothing

                        ' Is it a nullable type?
                        Dim typeUnderlying As System.Type = Nullable.GetUnderlyingType(memberType)
                        If typeUnderlying IsNot Nothing Then
                            ' Duplicate the address so we can call get_HasValue() and then get_Value()
                            il.Emit(OpCodes.Dup)

                            ' Define some labels
                            Dim lblHasValue As System.Reflection.Emit.Label = il.DefineLabel()
                            lblFinished = il.DefineLabel()

                            ' Call has_Value
                            il.Emit(OpCodes.[Call], memberType.GetProperty("HasValue").GetGetMethod())
                            il.Emit(OpCodes.Brtrue, lblHasValue)

                            ' No value, write "null:
                            il.Emit(OpCodes.Pop)
                            il.Emit(OpCodes.Ldstr, "null")
                            il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteRaw", New Type() {GetType(String)}))
                            il.Emit(OpCodes.Br_S, lblFinished.Value)

                            ' Get it's value
                            il.MarkLabel(lblHasValue)
                            il.Emit(OpCodes.[Call], memberType.GetProperty("Value").GetGetMethod())

                            ' Switch to the underlying type from here on
                            memberType = typeUnderlying
                            NeedValueAddress = (memberType.IsValueType AndAlso (TypeArrayContains(toStringTypes, memberType) OrElse TypeArrayContains(otherSupportedTypes, memberType)))

                            ' Work out again if we need the address of the value
                            If NeedValueAddress Then
                                Dim locTemp As System.Reflection.Emit.LocalBuilder = il.DeclareLocal(memberType)
                                il.Emit(OpCodes.Stloc, locTemp)
                                il.Emit(OpCodes.Ldloca, locTemp)
                            End If
                        End If

                        ' ToString()
                        If TypeArrayContains(toStringTypes, memberType) Then
                            ' Convert to string
                            il.Emit(OpCodes.Ldloc, locInvariant)
                            il.Emit(OpCodes.[Call], memberType.GetMethod("ToString", New Type() {GetType(IFormatProvider)}))
                            il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteRaw", New Type() {GetType(String)}))

                            ' ToString("R")
                        ElseIf memberType Is GetType(Single) OrElse memberType Is GetType(Double) Then
                            il.Emit(OpCodes.Ldstr, "R")
                            il.Emit(OpCodes.Ldloc, locInvariant)
                            il.Emit(OpCodes.[Call], memberType.GetMethod("ToString", New Type() {GetType(String), GetType(IFormatProvider)}))
                            il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteRaw", New Type() {GetType(String)}))

                            ' String?
                        ElseIf memberType Is GetType(String) Then
                            il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteStringLiteral", New Type() {GetType(String)}))

                            ' Char?
                        ElseIf memberType Is GetType(Char) Then
                            il.Emit(OpCodes.[Call], memberType.GetMethod("ToString", New Type() {}))
                            il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteStringLiteral", New Type() {GetType(String)}))

                            ' Bool?
                        ElseIf memberType Is GetType(Boolean) Then
                            Dim lblTrue As System.Reflection.Emit.Label = il.DefineLabel()
                            Dim lblCont As System.Reflection.Emit.Label = il.DefineLabel()
                            il.Emit(OpCodes.Brtrue_S, lblTrue)
                            il.Emit(OpCodes.Ldstr, "false")
                            il.Emit(OpCodes.Br_S, lblCont)
                            il.MarkLabel(lblTrue)
                            il.Emit(OpCodes.Ldstr, "true")
                            il.MarkLabel(lblCont)
                            il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteRaw", New Type() {GetType(String)}))
                        Else

                            ' NB: We don't support DateTime as it's format can be changed

                            ' Unsupported type, pass through
                            If memberType.IsValueType Then
                                il.Emit(OpCodes.Box, memberType)
                            End If
                            il.Emit(OpCodes.Callvirt, GetType(IJsonWriter).GetMethod("WriteValue", New Type() {GetType(Object)}))
                        End If

                        If lblFinished.HasValue Then
                            il.MarkLabel(lblFinished.Value)
                        End If
                    Next

                    ' Call IJsonWritten
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

                    ' Done!
                    il.Emit(OpCodes.Ret)
                    Dim impl As WriteCallback_t(Of IJsonWriter, Object) = DirectCast(method.CreateDelegate(GetType(WriteCallback_t(Of IJsonWriter, Object))), WriteCallback_t(Of IJsonWriter, Object))

                    ' Wrap it in a call to WriteDictionary
                    Return Function(w, obj)
                               w.WriteDictionary(Function()
                                                     impl(w, obj)

                                                 End Function)

                           End Function
                End If
            End Function

            ' Pseudo box lets us pass a value type by reference.  Used during 
            ' deserialization of value types.
            Private Interface IPseudoBox
                Function GetValue() As Object
            End Interface


            <Obfuscation(Exclude:=True, ApplyToMembers:=True)> _
            Private Class PseudoBox(Of T As Structure)
                Implements IPseudoBox
                Public value As T = Nothing
                Private Function IPseudoBox_GetValue() As Object Implements IPseudoBox.GetValue
                    Return value
                End Function
            End Class


            ' Make a parser for value types
            Public Function MakeParser(type__1 As Type) As ReadCallback_t(Of IJsonReader, Type, Object)
                System.Diagnostics.Debug.Assert(type__1.IsValueType)

                ' ParseJson method?
                Dim parseJson As System.Reflection.MethodInfo = ReflectionInfo.FindParseJson(type__1)
                If parseJson IsNot Nothing Then
                    If parseJson.GetParameters()(0).ParameterType Is GetType(IJsonReader) Then
                        Dim method As New DynamicMethod("invoke_ParseJson", GetType([Object]), New Type() {GetType(IJsonReader), GetType(Type)}, True)
                        Dim il As System.Reflection.Emit.ILGenerator = method.GetILGenerator()

                        il.Emit(OpCodes.Ldarg_0)
                        il.Emit(OpCodes.[Call], parseJson)
                        il.Emit(OpCodes.Box, type__1)
                        il.Emit(OpCodes.Ret)
                        Return DirectCast(method.CreateDelegate(GetType(ReadCallback_t(Of IJsonReader, Type, Object))), ReadCallback_t(Of IJsonReader, Type, Object))
                    Else
                        Dim method As New DynamicMethod("invoke_ParseJson", GetType(Object), New Type() {GetType(String)}, True)
                        Dim il As System.Reflection.Emit.ILGenerator = method.GetILGenerator()

                        il.Emit(OpCodes.Ldarg_0)
                        il.Emit(OpCodes.[Call], parseJson)
                        il.Emit(OpCodes.Box, type__1)
                        il.Emit(OpCodes.Ret)
                        Dim invoke As ReadCallback_t(Of String, Object) = DirectCast(method.CreateDelegate(GetType(ReadCallback_t(Of String, Object))), ReadCallback_t(Of String, Object))

                        Return Function(r, t)
                                   If r.GetLiteralKind() = LiteralKind.[String] Then
                                       Dim o As Object = invoke(r.GetLiteralString())
                                       r.NextToken()
                                       Return o
                                   End If
                                   Throw New InvalidDataException(String.Format("Expected string literal for type {0}", type__1.FullName))

                               End Function
                    End If
                Else
                    ' Get the reflection info for this type
                    Dim ri As ReflectionInfo = ReflectionInfo.GetReflectionInfo(type__1)
                    If ri Is Nothing Then
                        Return Nothing
                    End If

                    ' We'll create setters for each property/field
                    Dim setters As New Dictionary(Of String, WriteCallback_t(Of IJsonReader, Object))()

                    ' Store the value in a pseudo box until it's fully initialized
                    Dim boxType As System.Type = GetType(PseudoBox(Of )).MakeGenericType(type__1)

                    ' Process all members
                    For Each m As JsonMemberInfo In ri.Members
                        ' Ignore write only properties
                        Dim pi As PropertyInfo = TryCast(m.Member, PropertyInfo)
                        Dim fi As FieldInfo = TryCast(m.Member, FieldInfo)
                        If pi IsNot Nothing AndAlso pi.GetSetMethod(True) Is Nothing Then
                            Continue For
                        End If

                        ' Create a dynamic method that can do the work
                        Dim method As New DynamicMethod("dynamic_parser", Nothing, New Type() {GetType(IJsonReader), GetType(Object)}, True)
                        Dim il As System.Reflection.Emit.ILGenerator = method.GetILGenerator()

                        ' Load the target
                        il.Emit(OpCodes.Ldarg_1)
                        il.Emit(OpCodes.Castclass, boxType)
                        il.Emit(OpCodes.Ldflda, boxType.GetField("value"))

                        ' Get the value
                        GenerateGetJsonValue(m, il)

                        ' Assign it
                        If pi IsNot Nothing Then
                            il.Emit(OpCodes.[Call], pi.GetSetMethod(True))
                        End If
                        If fi IsNot Nothing Then
                            il.Emit(OpCodes.Stfld, fi)
                        End If

                        ' Done
                        il.Emit(OpCodes.Ret)

                        ' Store in the map of setters
                        setters.Add(m.JsonKey, DirectCast(method.CreateDelegate(GetType(WriteCallback_t(Of IJsonReader, Object))), WriteCallback_t(Of IJsonReader, Object)))
                    Next

                    ' Create helpers to invoke the interfaces (this is painful but avoids having to really box 
                    ' the value in order to call the interface).
                    Dim invokeLoading As WriteCallback_t(Of Object, IJsonReader) = MakeInterfaceCall(type__1, GetType(IJsonLoading))
                    Dim invokeLoaded As WriteCallback_t(Of Object, IJsonReader) = MakeInterfaceCall(type__1, GetType(IJsonLoaded))
                    Dim invokeField As ReadCallback_t(Of Object, IJsonReader, String, Boolean) = MakeLoadFieldCall(type__1)

                    ' Create the parser
                    Dim parser As ReadCallback_t(Of IJsonReader, Type, Object) = Function(reader, Type__2)
                                                                                     ' Create pseudobox (ie: new PseudoBox<Type>)
                                                                                     Dim box As Object = DecoratingActivator.CreateInstance(boxType)

                                                                                     ' Call IJsonLoading
                                                                                     ' RaiseEvent invokeLoading(box, reader)
                                                                                     If invokeLoading IsNot Nothing Then
                                                                                         invokeLoading(box, reader)
                                                                                     End If

                                                                                     ' Read the dictionary
                                                                                     reader.ParseDictionary(Function(key)
                                                                                                                ' Call IJsonLoadField
                                                                                                                If invokeField IsNot Nothing AndAlso invokeField(box, reader, key) Then
                                                                                                                    Return Nothing
                                                                                                                End If

                                                                                                                ' Get a setter and invoke it if found
                                                                                                                Dim setter As WriteCallback_t(Of IJsonReader, Object)
                                                                                                                If setters.TryGetValue(key, setter) Then
                                                                                                                    setter(reader, box)
                                                                                                                End If

                                                                                                                Return Nothing
                                                                                                            End Function)

                                                                                     ' IJsonLoaded
                                                                                     ' RaiseEvent invokeLoaded(box, reader)
                                                                                     If invokeLoaded IsNot Nothing Then
                                                                                         invokeLoaded(box, reader)
                                                                                     End If

                                                                                     ' Return the value
                                                                                     Return DirectCast(box, IPseudoBox).GetValue()
                                                                                 End Function

                    ' Done
                    Return parser
                End If
            End Function

            ' Helper to make the call to a PsuedoBox value's IJsonLoading or IJsonLoaded
            Private Function MakeInterfaceCall(type As Type, tItf As Type) As WriteCallback_t(Of Object, IJsonReader)
                ' Interface supported?
                If Not tItf.IsAssignableFrom(type) Then
                    Return Nothing
                End If

                ' Resolve the box type
                Dim boxType As System.Type = GetType(PseudoBox(Of )).MakeGenericType(type)

                ' Create method
                Dim method As New DynamicMethod("dynamic_invoke_" + tItf.Name, Nothing, New Type() {GetType(Object), GetType(IJsonReader)}, True)
                Dim il As System.Reflection.Emit.ILGenerator = method.GetILGenerator()

                ' Call interface method
                il.Emit(OpCodes.Ldarg_0)
                il.Emit(OpCodes.Castclass, boxType)
                il.Emit(OpCodes.Ldflda, boxType.GetField("value"))
                il.Emit(OpCodes.Ldarg_1)
                il.Emit(OpCodes.[Call], type.GetInterfaceMap(tItf).TargetMethods(0))
                il.Emit(OpCodes.Ret)

                ' Done
                Return DirectCast(method.CreateDelegate(GetType(WriteCallback_t(Of Object, IJsonReader))), WriteCallback_t(Of Object, IJsonReader))
            End Function


            ' Similar to above but for IJsonLoadField
            Private Function MakeLoadFieldCall(type As Type) As ReadCallback_t(Of Object, IJsonReader, String, Boolean)
                ' Interface supported?
                Dim tItf As System.Type = GetType(IJsonLoadField)
                If Not tItf.IsAssignableFrom(type) Then
                    Return Nothing
                End If

                ' Resolve the box type
                Dim boxType As System.Type = GetType(PseudoBox(Of )).MakeGenericType(type)

                ' Create method
                Dim method As New DynamicMethod("dynamic_invoke_" + tItf.Name, GetType(Boolean), New Type() {GetType(Object), GetType(IJsonReader), GetType(String)}, True)
                Dim il As System.Reflection.Emit.ILGenerator = method.GetILGenerator()

                ' Call interface method
                il.Emit(OpCodes.Ldarg_0)
                il.Emit(OpCodes.Castclass, boxType)
                il.Emit(OpCodes.Ldflda, boxType.GetField("value"))
                il.Emit(OpCodes.Ldarg_1)
                il.Emit(OpCodes.Ldarg_2)
                il.Emit(OpCodes.[Call], type.GetInterfaceMap(tItf).TargetMethods(0))
                il.Emit(OpCodes.Ret)

                ' Done
                Return DirectCast(method.CreateDelegate(GetType(ReadCallback_t(Of Object, IJsonReader, String, Boolean))), ReadCallback_t(Of Object, IJsonReader, String, Boolean))
            End Function

            ' Create an "into parser" that can parse from IJsonReader into a reference type (ie: a class)
            Public Function MakeIntoParser(type As Type) As WriteCallback_t(Of IJsonReader, Object)
                System.Diagnostics.Debug.Assert(Not type.IsValueType)

                ' Get the reflection info for this type
                Dim ri As ReflectionInfo = ReflectionInfo.GetReflectionInfo(type)
                If ri Is Nothing Then
                    Return Nothing
                End If

                ' We'll create setters for each property/field
                Dim setters As New Dictionary(Of String, WriteCallback_t(Of IJsonReader, Object))()

                ' Process all members
                For Each m As JsonMemberInfo In ri.Members
                    ' Ignore write only properties
                    Dim pi As PropertyInfo = TryCast(m.Member, PropertyInfo)
                    Dim fi As FieldInfo = TryCast(m.Member, FieldInfo)
                    If pi IsNot Nothing AndAlso pi.GetSetMethod(True) Is Nothing Then
                        Continue For
                    End If

                    ' Ignore read only properties that has KeepInstance attribute
                    If pi IsNot Nothing AndAlso pi.GetGetMethod(True) Is Nothing AndAlso m.KeepInstance Then
                        Continue For
                    End If

                    ' Create a dynamic method that can do the work
                    Dim method As New DynamicMethod("dynamic_parser", Nothing, New Type() {GetType(IJsonReader), GetType(Object)}, True)
                    Dim il As System.Reflection.Emit.ILGenerator = method.GetILGenerator()

                    ' Load the target
                    il.Emit(OpCodes.Ldarg_1)
                    il.Emit(OpCodes.Castclass, type)

                    ' Try to keep existing instance?
                    If m.KeepInstance Then
                        ' Get existing existing instance
                        il.Emit(OpCodes.Dup)
                        If pi IsNot Nothing Then
                            il.Emit(OpCodes.Callvirt, pi.GetGetMethod(True))
                        Else
                            il.Emit(OpCodes.Ldfld, fi)
                        End If

                        Dim existingInstance As System.Reflection.Emit.LocalBuilder = il.DeclareLocal(m.MemberType)
                        Dim lblExistingInstanceNull As System.Reflection.Emit.Label = il.DefineLabel()

                        ' Keep a copy of the existing instance in a locale
                        il.Emit(OpCodes.Dup)
                        il.Emit(OpCodes.Stloc, existingInstance)

                        ' Compare to null
                        il.Emit(OpCodes.Ldnull)
                        il.Emit(OpCodes.Ceq)
                        il.Emit(OpCodes.Brtrue_S, lblExistingInstanceNull)

                        il.Emit(OpCodes.Ldarg_0)
                        ' reader
                        il.Emit(OpCodes.Ldloc, existingInstance)
                        ' into
                        il.Emit(OpCodes.Callvirt, GetType(IJsonReader).GetMethod("ParseInto", New Type() {GetType([Object])}))

                        il.Emit(OpCodes.Pop)
                        ' Clean up target left on stack (1)
                        il.Emit(OpCodes.Ret)

                        il.MarkLabel(lblExistingInstanceNull)
                    End If

                    ' Get the value from IJsonReader
                    GenerateGetJsonValue(m, il)

                    ' Assign it
                    If pi IsNot Nothing Then
                        il.Emit(OpCodes.Callvirt, pi.GetSetMethod(True))
                    End If
                    If fi IsNot Nothing Then
                        il.Emit(OpCodes.Stfld, fi)
                    End If

                    ' Done
                    il.Emit(OpCodes.Ret)

                    ' Store the handler in map
                    setters.Add(m.JsonKey, DirectCast(method.CreateDelegate(GetType(WriteCallback_t(Of IJsonReader, Object))), WriteCallback_t(Of IJsonReader, Object)))
                Next


                ' Now create the parseInto delegate
                Dim parseInto As WriteCallback_t(Of IJsonReader, Object) = Function(reader, obj)
                                                                               ' Call IJsonLoading
                                                                               Dim loading As IJsonLoading = TryCast(obj, IJsonLoading)
                                                                               If loading IsNot Nothing Then
                                                                                   loading.OnJsonLoading(reader)
                                                                               End If

                                                                               ' Cache IJsonLoadField
                                                                               Dim lf As IJsonLoadField = TryCast(obj, IJsonLoadField)

                                                                               ' Read dictionary keys
                                                                               reader.ParseDictionary(Function(key)
                                                                                                          ' Call IJsonLoadField
                                                                                                          If lf IsNot Nothing AndAlso lf.OnJsonField(reader, key) Then
                                                                                                              Return Nothing
                                                                                                          End If

                                                                                                          ' Call setters
                                                                                                          Dim setter As WriteCallback_t(Of IJsonReader, Object)
                                                                                                          If setters.TryGetValue(key, setter) Then
                                                                                                              setter(reader, obj)
                                                                                                          End If

                                                                                                          Return Nothing
                                                                                                      End Function)

                                                                               ' Call IJsonLoaded
                                                                               Dim loaded As IJsonLoaded = TryCast(obj, IJsonLoaded)
                                                                               If loaded IsNot Nothing Then
                                                                                   loaded.OnJsonLoaded(reader)
                                                                               End If

                                                                               Return Nothing
                                                                           End Function

                ' Since we've created the ParseInto handler, we might as well register
                ' as a Parse handler too.
                RegisterIntoParser(type, parseInto)

                ' Done
                Return parseInto
            End Function


            ' Registers a ParseInto handler as Parse handler that instantiates the object
            ' and then parses into it.
            Private Sub RegisterIntoParser(type As Type, parseInto As WriteCallback_t(Of IJsonReader, Object))
                ' Check type has a parameterless constructor
                Dim con As System.Reflection.ConstructorInfo = type.GetConstructor(BindingFlags.Instance Or BindingFlags.[Public] Or BindingFlags.NonPublic, Nothing, New Type(-1) {}, Nothing)
                If con Is Nothing Then
                    Return
                End If

                ' Create a dynamic method that can do the work
                Dim method As New DynamicMethod("dynamic_factory", GetType(Object), New Type() {GetType(IJsonReader), GetType(WriteCallback_t(Of IJsonReader, Object))}, True)
                Dim il As System.Reflection.Emit.ILGenerator = method.GetILGenerator()

                ' Create the new object
                Dim locObj As System.Reflection.Emit.LocalBuilder = il.DeclareLocal(GetType(Object))
                il.Emit(OpCodes.Newobj, con)

                il.Emit(OpCodes.Dup)
                ' For return value
                il.Emit(OpCodes.Stloc, locObj)

                il.Emit(OpCodes.Ldarg_1)
                ' parseinto delegate
                il.Emit(OpCodes.Ldarg_0)
                ' IJsonReader
                il.Emit(OpCodes.Ldloc, locObj)
                ' new object instance
                il.Emit(OpCodes.Callvirt, GetType(WriteCallback_t(Of IJsonReader, Object)).GetMethod("Invoke"))
                il.Emit(OpCodes.Ret)

                Dim factory As ReadCallback_t(Of IJsonReader, WriteCallback_t(Of IJsonReader, Object), Object) = DirectCast(method.CreateDelegate(GetType(ReadCallback_t(Of IJsonReader, WriteCallback_t(Of IJsonReader, Object), Object))), ReadCallback_t(Of IJsonReader, WriteCallback_t(Of IJsonReader, Object), Object))

                Json.RegisterParser(type, Function(reader, type2)
                                              Return factory(reader, parseInto)

                                          End Function)
            End Sub

            ' Generate the MSIL to retrieve a value for a particular field or property from a IJsonReader
            Private Sub GenerateGetJsonValue(m As JsonMemberInfo, il As ILGenerator)
                Dim generateCallToHelper As WriteCallback_t(Of String) = Function(helperName)
                                                                             ' Call the helper
                                                                             il.Emit(OpCodes.Ldarg_0)
                                                                             il.Emit(OpCodes.[Call], GetType(Emit).GetMethod(helperName, New Type() {GetType(IJsonReader)}))

                                                                             ' Move to next token
                                                                             il.Emit(OpCodes.Ldarg_0)
                                                                             il.Emit(OpCodes.Callvirt, GetType(IJsonReader).GetMethod("NextToken", New Type() {}))

                                                                             Return Nothing
                                                                         End Function

                Dim numericTypes As Type() = New Type() {GetType(Integer), GetType(UInteger), GetType(Long), GetType(ULong), GetType(Short), GetType(UShort), _
                    GetType(Decimal), GetType(Byte), GetType(SByte), GetType(Double), GetType(Single)}

                If m.MemberType Is GetType(String) Then
                    generateCallToHelper("GetLiteralString")

                ElseIf m.MemberType Is GetType(Boolean) Then
                    generateCallToHelper("GetLiteralBool")

                ElseIf m.MemberType Is GetType(Char) Then
                    generateCallToHelper("GetLiteralChar")

                ElseIf TypeArrayContains(numericTypes, m.MemberType) Then
                    ' Get raw number string
                    il.Emit(OpCodes.Ldarg_0)
                    il.Emit(OpCodes.[Call], GetType(Emit).GetMethod("GetLiteralNumber", New Type() {GetType(IJsonReader)}))

                    ' Convert to a string
                    il.Emit(OpCodes.[Call], GetType(CultureInfo).GetProperty("InvariantCulture").GetGetMethod())
                    il.Emit(OpCodes.[Call], m.MemberType.GetMethod("Parse", New Type() {GetType(String), GetType(IFormatProvider)}))

                    ' 
                    il.Emit(OpCodes.Ldarg_0)
                    il.Emit(OpCodes.Callvirt, GetType(IJsonReader).GetMethod("NextToken", New Type() {}))
                Else

                    il.Emit(OpCodes.Ldarg_0)
                    il.Emit(OpCodes.Ldtoken, m.MemberType)
                    il.Emit(OpCodes.[Call], GetType(Type).GetMethod("GetTypeFromHandle", New Type() {GetType(RuntimeTypeHandle)}))
                    il.Emit(OpCodes.Callvirt, GetType(IJsonReader).GetMethod("Parse", New Type() {GetType(Type)}))
                    il.Emit(If(m.MemberType.IsValueType, OpCodes.Unbox_Any, OpCodes.Castclass), m.MemberType)
                End If
            End Sub

            ' Helper to fetch a literal bool from an IJsonReader
            <Obfuscation(Exclude:=True)> _
            Public Function GetLiteralBool(r As IJsonReader) As Boolean
                Select Case r.GetLiteralKind()
                    Case LiteralKind.[True]
                        Return True

                    Case LiteralKind.[False]
                        Return False
                    Case Else

                        Throw New InvalidDataException("expected a boolean value")
                End Select
            End Function

            ' Helper to fetch a literal character from an IJsonReader
            <Obfuscation(Exclude:=True)> _
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

            ' Helper to fetch a literal string from an IJsonReader
            <Obfuscation(Exclude:=True)> _
            Public Function GetLiteralString(r As IJsonReader) As String
                Select Case r.GetLiteralKind()
                    Case LiteralKind.Null
                        Return Nothing
                    Case LiteralKind.[String]
                        Return r.GetLiteralString()
                End Select
                Throw New InvalidDataException("expected a string literal")
            End Function

            ' Helper to fetch a literal number from an IJsonReader (returns the raw string)
            <Obfuscation(Exclude:=True)> _
            Public Function GetLiteralNumber(r As IJsonReader) As String
                Select Case r.GetLiteralKind()
                    Case LiteralKind.SignedInteger, LiteralKind.UnsignedInteger, LiteralKind.FloatingPoint
                        Return r.GetLiteralString()
                End Select
                Throw New InvalidDataException("expected a numeric literal")
            End Function
        End Module
#End If
    End Namespace
End Namespace
