

Namespace JWT


    ''' <summary>
    ''' JSON Serializer using JavaScriptSerializer
    ''' </summary>
    Public Class DefaultJsonSerializer
        Implements IJsonSerializer


        ' Private ReadOnly serializer As New System.Web.Script.Serialization.JavaScriptSerializer()

        ''' <summary>
        ''' Serialize an object to JSON string
        ''' </summary>
        ''' <param name="obj">object</param>
        ''' <returns>JSON string</returns>
        Function Serialize(obj As Object) As String Implements JWT.IJsonSerializer.Serialize
            ' Return Nothing
            ' Return serializer.Serialize(obj);
            ' Return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            Return PetaJson.Json.Format(obj, PetaJson.JsonOptions.DontWriteWhitespace)
        End Function


        ''' <summary>
        ''' Deserialize a JSON string to typed object.
        ''' </summary>
        ''' <typeparam name="T">type of object</typeparam>
        ''' <param name="json">JSON string</param>
        ''' <returns>typed object</returns>
        Function Deserialize(Of T)(json As String) As T Implements JWT.IJsonSerializer.Deserialize
            ' Return Nothing
            ' Return serializer.Deserialize<T>(json);
            ' Return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            Return PetaJson.Json.Parse(Of T)(json, PetaJson.JsonOptions.DontWriteWhitespace)
        End Function

    End Class


End Namespace
