
namespace PetaJSON
{

    // https://github.com/toptensoftware/PetaJson
    class Testing
    {


        public static void Test()
        {

            string JSON = XXXX.PetaJson.Json.Format("object_to_serialize", XXXX.PetaJson.JsonOptions.DontWriteWhitespace);
            System.Console.WriteLine(JSON);


            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            using (System.IO.TextWriter tw = new System.IO.StringWriter(sb))
            {
                XXXX.PetaJson.Json.Write(tw, "object_to_serialize", XXXX.PetaJson.JsonOptions.DontWriteWhitespace);
            }
            
        }


    }


}
