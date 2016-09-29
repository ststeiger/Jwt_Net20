Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Text

Namespace XXXX.PetaJson.Internal
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
			Else If Me._rewindBufferPos < Me._rewindBuffer.Length Then
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
					Else If Me._currentChar = vbLf Then
						If Me.NextChar() = vbCr Then
							Me.NextChar()
						End If
						Me._currentCharPos.Line = Me._currentCharPos.Line + 1
						Me._currentCharPos.Offset = 0
					Else If Me._currentChar = " "c Then
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
				If(Me._options And JsonOptions.StrictParser) <> JsonOptions.None Then
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
			Else If c <> "'"c Then
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
					Else If c <= "f"c Then
						If c <> "b"c Then
							If c <> "f"c Then
								GoTo IL_41A
							End If
							Me._sb.Append(vbFormFeed)
						Else
							Me._sb.Append(vbBack)
						End If
					Else If c <> "n"c Then
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
			Else If signed Then
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
End Namespace
