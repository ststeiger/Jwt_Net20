
Namespace XXXX.PetaJson

    Public Delegate Function ReadCallback_t(Of Out TResult)() As TResult
    Public Delegate Function ReadCallback_t(Of In T, Out TResult)(arg As T) As TResult
    Public Delegate Function ReadCallback_t(Of In T1, In T2, Out TResult)(arg1 As T1, arg2 As T2) As TResult
    Public Delegate Function ReadCallback_t(Of In T1, In T2, In T3, Out TResult)(arg1 As T1, arg2 As T2, arg3 As T3) As TResult

End Namespace
