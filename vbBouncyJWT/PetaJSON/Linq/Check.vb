
'
' Check.cs
'
' Author:
'   Jb Evain (jbevain@novell.com)
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


Namespace BouncyJWT.PetaJson

    Friend NotInheritable Class Check
        Private Sub New()
        End Sub

        Public Shared Sub Source(source__1 As Object)
            If source__1 Is Nothing Then
                Throw New Global.System.ArgumentNullException("source")
            End If
        End Sub

        Public Shared Sub Source1AndSource2(source1 As Object, source2 As Object)
            If source1 Is Nothing Then
                Throw New Global.System.ArgumentNullException("source1")
            End If
            If source2 Is Nothing Then
                Throw New Global.System.ArgumentNullException("source2")
            End If
        End Sub

        Public Shared Sub SourceAndFuncAndSelector(source As Object, func As Object, selector As Object)
            If source Is Nothing Then
                Throw New Global.System.ArgumentNullException("source")
            End If
            If func Is Nothing Then
                Throw New Global.System.ArgumentNullException("func")
            End If
            If selector Is Nothing Then
                Throw New Global.System.ArgumentNullException("selector")
            End If
        End Sub


        Public Shared Sub SourceAndFunc(source As Object, func As Object)
            If source Is Nothing Then
                Throw New Global.System.ArgumentNullException("source")
            End If
            If func Is Nothing Then
                Throw New Global.System.ArgumentNullException("func")
            End If
        End Sub

        Public Shared Sub SourceAndSelector(source As Object, selector As Object)
            If source Is Nothing Then
                Throw New Global.System.ArgumentNullException("source")
            End If
            If selector Is Nothing Then
                Throw New Global.System.ArgumentNullException("selector")
            End If
        End Sub

        Public Shared Sub SourceAndPredicate(source As Object, predicate As Object)
            If source Is Nothing Then
                Throw New Global.System.ArgumentNullException("source")
            End If
            If predicate Is Nothing Then
                Throw New Global.System.ArgumentNullException("predicate")
            End If
        End Sub

        Public Shared Sub FirstAndSecond(first As Object, second As Object)
            If first Is Nothing Then
                Throw New Global.System.ArgumentNullException("first")
            End If
            If second Is Nothing Then
                Throw New Global.System.ArgumentNullException("second")
            End If
        End Sub

        Public Shared Sub SourceAndKeySelector(source As Object, keySelector As Object)
            If source Is Nothing Then
                Throw New Global.System.ArgumentNullException("source")
            End If
            If keySelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("keySelector")
            End If
        End Sub

        Public Shared Sub SourceAndKeyElementSelectors(source As Object, keySelector As Object, elementSelector As Object)
            If source Is Nothing Then
                Throw New Global.System.ArgumentNullException("source")
            End If
            If keySelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("keySelector")
            End If
            If elementSelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("elementSelector")
            End If
        End Sub
        Public Shared Sub SourceAndKeyResultSelectors(source As Object, keySelector As Object, resultSelector As Object)
            If source Is Nothing Then
                Throw New Global.System.ArgumentNullException("source")
            End If
            If keySelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("keySelector")
            End If
            If resultSelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("resultSelector")
            End If
        End Sub

        Public Shared Sub SourceAndCollectionSelectorAndResultSelector(source As Object, collectionSelector As Object, resultSelector As Object)
            If source Is Nothing Then
                Throw New Global.System.ArgumentNullException("source")
            End If
            If collectionSelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("collectionSelector")
            End If
            If resultSelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("resultSelector")
            End If
        End Sub

        Public Shared Sub SourceAndCollectionSelectors(source As Object, collectionSelector As Object, selector As Object)
            If source Is Nothing Then
                Throw New Global.System.ArgumentNullException("source")
            End If
            If collectionSelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("collectionSelector")
            End If
            If selector Is Nothing Then
                Throw New Global.System.ArgumentNullException("selector")
            End If
        End Sub

        Public Shared Sub JoinSelectors(outer As Object, inner As Object, outerKeySelector As Object, innerKeySelector As Object, resultSelector As Object)
            If outer Is Nothing Then
                Throw New Global.System.ArgumentNullException("outer")
            End If
            If inner Is Nothing Then
                Throw New Global.System.ArgumentNullException("inner")
            End If
            If outerKeySelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("outerKeySelector")
            End If
            If innerKeySelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("innerKeySelector")
            End If
            If resultSelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("resultSelector")
            End If
        End Sub

        Public Shared Sub GroupBySelectors(source As Object, keySelector As Object, elementSelector As Object, resultSelector As Object)
            If source Is Nothing Then
                Throw New Global.System.ArgumentNullException("source")
            End If
            If keySelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("keySelector")
            End If
            If elementSelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("elementSelector")
            End If
            If resultSelector Is Nothing Then
                Throw New Global.System.ArgumentNullException("resultSelector")
            End If
        End Sub
    End Class
End Namespace
