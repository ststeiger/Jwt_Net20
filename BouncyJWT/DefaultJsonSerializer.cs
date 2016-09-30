
namespace BouncyJWT
{


    /// <summary>
    /// JSON Serializer using JavaScriptSerializer
    /// </summary>
    public class DefaultJsonSerializer : IJsonSerializer
    {
        
        // private readonly System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();

        /// <summary>
        /// Serialize an object to JSON string
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>JSON string</returns>
        public string Serialize(object obj)
        {
            // return serializer.Serialize(obj);
            // return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            return PetaJson.Json.Format(obj, PetaJson.JsonOptions.DontWriteWhitespace);
        }


        /// <summary>
        /// Deserialize a JSON string to typed object.
        /// </summary>
        /// <typeparam name="T">type of object</typeparam>
        /// <param name="json">JSON string</param>
        /// <returns>typed object</returns>
        public T Deserialize<T>(string json)
        {
            // return serializer.Deserialize<T>(json);
            // return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            return PetaJson.Json.Parse<T>(json, PetaJson.JsonOptions.DontWriteWhitespace);
        }


    }


}
