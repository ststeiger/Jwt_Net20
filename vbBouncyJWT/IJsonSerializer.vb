
Namespace BouncyJWT


    ''' <summary>
    ''' Provides JSON Serialize and Deserialize.  Allows custom serializers used.
    ''' </summary>
    Public Interface IJsonSerializer
        ''' <summary>
        ''' Serialize an object to JSON string
        ''' </summary>
        ''' <param name="obj">object</param>
        ''' <returns>JSON string</returns>
        Function Serialize(obj As Object) As String

        ''' <summary>
        ''' Deserialize a JSON string to typed object.
        ''' </summary>
        ''' <typeparam name="T">type of object</typeparam>
        ''' <param name="json">JSON string</param>
        ''' <returns>typed object</returns>
        Function Deserialize(Of T)(json As String) As T
    End Interface


End Namespace

