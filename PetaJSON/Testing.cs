
namespace PetaJSON
{


    // https://github.com/toptensoftware/PetaJson
    class Testing
    {


        public class SomeObject
        {
            public int Id = 123;
            public string Name = "C'est un Tést\r\n123\"señor äöüÄÖÜ";
        }

        public static void Test()
        {
            string JSON = XXXX.PetaJson.Json.Format(new SomeObject(), XXXX.PetaJson.JsonOptions.DontWriteWhitespace);
            System.Console.WriteLine(JSON);


            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            using (System.IO.TextWriter tw = new System.IO.StringWriter(sb))
            {
                XXXX.PetaJson.Json.Write(tw, "object_to_serialize", XXXX.PetaJson.JsonOptions.DontWriteWhitespace);
            } // End Using tw 


            SomeObject obj = XXXX.PetaJson.Json.Parse<SomeObject>(JSON, XXXX.PetaJson.JsonOptions.DontWriteWhitespace);
            System.Console.WriteLine(obj);
        } // End Sub Test 


    } // End Class Testing


} // End Namespace PetaJSON 
