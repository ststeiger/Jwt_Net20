
'
' Enumerable.cs
'
' Authors:
'  Marek Safar (marek.safar@gmail.com)
'  Antonello Provenzano  <antonello@deveel.com>
'  Alejandro Serrano "Serras" (trupill@yahoo.es)
'  Jb Evain (jbevain@novell.com)
'
' Copyright (C) 2007 Novell, Inc (http://www.novell.com)
'
' Permission is hereby granted, free of charge, to any person obtaining
' a copy of this software and associated documentation files (the
' "Software"), to deal in the Software without restriction, including
' without limitation the rights to use, copy, modify, merge, publish,
' distribute, sublicense, and/or sell copies of the Software, and to
' permit persons to whom the Software is furnished to do so, subject to
' the following conditions:
'
' The above copyright notice and this permission notice shall be
' included in all copies or substantial portions of the Software.
'
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
' EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
' MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
' NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
' LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
' OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
' WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
'

' precious: http://www.hookedonlinq.com


Imports System.Collections.Generic


Namespace BouncyJWT.PetaJson


    Friend Module Enumerable



        Private Enum Fallback
            [Default]
            [Throw]
        End Enum


        '''''''''''''''''''''''''''''' Remove

        '' Func 
        'Public Delegate Function ReadCallback_t(Of Out TResult)() As TResult
        'Public Delegate Function ReadCallback_t(Of In T, Out TResult)(arg As T) As TResult
        'Public Delegate Function ReadCallback_t(Of In T1, In T2, Out TResult)(arg1 As T1, arg2 As T2) As TResult
        'Public Delegate Function ReadCallback_t(Of In T1, In T2, In T3, Out TResult)(arg1 As T1, arg2 As T2, arg3 As T3) As TResult


        '' Action 
        'Public Delegate Sub WriteCallback_t()
        'Public Delegate Sub WriteCallback_t(Of In T)(obj As T)
        'Public Delegate Sub WriteCallback_t(Of In T1, In T2)(arg1 As T1, arg2 As T2)

        '''''''''''''''''''''''''''''' Remove




        Private NotInheritable Class PredicateOf(Of T)
            Private Sub New()
            End Sub
            Public Shared ReadOnly Always As ReadCallback_t(Of T, Boolean) = Function(t) True
        End Class



#Region "All"

        <System.Runtime.CompilerServices.Extension> _
        Public Function All(Of TSource)(source As IEnumerable(Of TSource), predicate As ReadCallback_t(Of TSource, Boolean)) As Boolean
            Check.SourceAndPredicate(source, predicate)

            For Each element As TSource In source
                If Not predicate(element) Then
                    Return False
                End If
            Next

            Return True
        End Function

#End Region

#Region "Any"

        <System.Runtime.CompilerServices.Extension> _
        Public Function Any(Of TSource)(source As IEnumerable(Of TSource)) As Boolean
            Check.Source(source)

            Dim collection As ICollection(Of TSource) = TryCast(source, ICollection(Of TSource))
            If collection IsNot Nothing Then
                Return collection.Count > 0
            End If

            Using enumerator As Global.System.Collections.Generic.IEnumerator(Of TSource) = source.GetEnumerator()
                Return enumerator.MoveNext()
            End Using
        End Function

        <System.Runtime.CompilerServices.Extension> _
        Public Function Any(Of TSource)(source As IEnumerable(Of TSource), predicate As ReadCallback_t(Of TSource, Boolean)) As Boolean
            Check.SourceAndPredicate(source, predicate)

            For Each element As TSource In source
                If predicate(element) Then
                    Return True
                End If
            Next

            Return False
        End Function

#End Region



#Region "First"

        <System.Runtime.CompilerServices.Extension> _
        Private Function First(Of TSource)(source As IEnumerable(Of TSource), predicate As ReadCallback_t(Of TSource, Boolean), fallback__1 As Fallback) As TSource
            For Each element As TSource In source
                If predicate(element) Then
                    Return element
                End If
            Next

            If fallback__1 = Fallback.[Throw] Then
                Throw NoMatchingElement()
            End If

            Return Nothing
        End Function

        <System.Runtime.CompilerServices.Extension> _
        Public Function First(Of TSource)(source As IEnumerable(Of TSource)) As TSource
            Check.Source(source)

            Dim list As IList(Of TSource) = TryCast(source, IList(Of TSource))
            If list IsNot Nothing Then
                If list.Count <> 0 Then
                    Return list(0)
                End If
            Else
                Using enumerator As Global.System.Collections.Generic.IEnumerator(Of TSource) = source.GetEnumerator()
                    If enumerator.MoveNext() Then
                        Return enumerator.Current
                    End If
                End Using
            End If

            Throw EmptySequence()
        End Function

        <System.Runtime.CompilerServices.Extension> _
        Public Function First(Of TSource)(source As IEnumerable(Of TSource), predicate As ReadCallback_t(Of TSource, Boolean)) As TSource
            Check.SourceAndPredicate(source, predicate)


            Return First(source, predicate, Fallback.[Throw])
        End Function

#End Region

#Region "FirstOrDefault"

        <System.Runtime.CompilerServices.Extension> _
        Public Function FirstOrDefault(Of TSource)(source As IEnumerable(Of TSource)) As TSource
            Check.Source(source)

#If Not FULL_AOT_RUNTIME Then
            Return First(source, PredicateOf(Of TSource).Always, Fallback.[Default])
#Else
			' inline the code to reduce dependency o generic causing AOT errors on device (e.g. bug #3285)
			For Each element As TSource In source
				Return element
			Next

			Return Nothing
#End If
        End Function

        <System.Runtime.CompilerServices.Extension> _
        Public Function FirstOrDefault(Of TSource)(source As IEnumerable(Of TSource), predicate As ReadCallback_t(Of TSource, Boolean)) As TSource
            Check.SourceAndPredicate(source, predicate)

            Return First(source, predicate, Fallback.[Default])
        End Function

#End Region


#Region "OfType"

        <System.Runtime.CompilerServices.Extension> _
        Public Function OfType(Of TResult)(source As System.Collections.IEnumerable) As IEnumerable(Of TResult)
            Check.Source(source)

            Return CreateOfTypeIterator(Of TResult)(source)
        End Function

        Private Function CreateOfTypeIterator(Of TResult)(source As System.Collections.IEnumerable) As IEnumerable(Of TResult)
            Dim ls As New List(Of TResult)


            For Each element As Object In source
                If TypeOf element Is TResult Then
                    ls.Add(DirectCast(element, TResult))
                End If
            Next

            Return ls
        End Function

#End Region


#Region "Exception helpers"

        Private Function EmptySequence() As System.Exception
            Return New System.InvalidOperationException(("Sequence contains no elements"))
        End Function
        Private Function NoMatchingElement() As System.Exception
            Return New System.InvalidOperationException(("Sequence contains no matching element"))
        End Function
#End Region
    End Module
End Namespace
