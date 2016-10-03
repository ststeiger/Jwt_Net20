
Imports System
Imports System.Collections.Generic


Namespace XXXX.PetaJson.Internal



    Public Class ThreadSafeCache(Of TKey, TValue)

        Private _map As New Dictionary(Of TKey, TValue)()
#If WITH_RWLOCK Then
	Private _lock As New ReaderWriterLockSlim()
#Else
        Private _lock As New Object()
#End If


        Public Sub New()
        End Sub

        Public Function [Get](key As TKey, createIt As ReadCallback_t(Of TValue)) As TValue
            ' Check if already exists
#If WITH_RWLOCK Then
		_lock.EnterReadLock()
#Else
            SyncLock _lock
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

#If Not WITH_RWLOCK Then
            End SyncLock
#End If


            ' Nope, take lock and try again
#If WITH_RWLOCK Then
		_lock.EnterWriteLock()
#Else
            SyncLock _lock
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

#If Not WITH_RWLOCK Then

            End SyncLock
#End If


        End Function




        Public Function TryGetValue(key As TKey, ByRef val As TValue) As Boolean
#If WITH_RWLOCK Then
		_lock.EnterReadLock()
#Else
            SyncLock _lock
#End If
                Try
                    Return _map.TryGetValue(key, val)
                Finally

#If WITH_RWLOCK Then
                    _lock.ExitReadLock()
#End If
                End Try

#If Not WITH_RWLOCK Then
            End SyncLock
#End If

        End Function




        Public Sub [Set](key As TKey, value As TValue)
#If WITH_RWLOCK Then
		_lock.EnterWriteLock()
#Else
            SyncLock _lock
#End If
                Try
                    _map(key) = value
                Finally

#If WITH_RWLOCK Then
                    _lock.ExitWriteLock()
#End If

                End Try

#If Not WITH_RWLOCK Then
            End SyncLock
#End If


        End Sub

    End Class


End Namespace
