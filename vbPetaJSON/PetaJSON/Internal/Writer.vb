Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Reflection

Namespace XXXX.PetaJson.Internal


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
                    If Enumerable.Any(Of Object)(type.GetCustomAttributes(GetType(FlagsAttribute), False)) Then
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
End Namespace
