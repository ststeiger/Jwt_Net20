
namespace ConsoleTestApplication
{
    

    public class User
    {
        public int Id = 123;
        public string Name = "Test";
        public string Language = "de-CH";
        public string Bla = "Test\r\n123\u0005äöüÄÖÜñõ"; 
    } // End Class User 


    static class Program
    {


        // http://stackoverflow.com/questions/1668353/how-can-i-generate-a-cryptographically-secure-pseudorandom-number-in-c
        public static byte[] GenerateRandomKey(int byteCount)
        {
            //using (System.Security.Cryptography.RandomNumberGenerator rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            System.Security.Cryptography.RandomNumberGenerator rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            {
                byte[] tokenData = new byte[byteCount];
                rng.GetBytes(tokenData);
                rng = null;
                return tokenData;
            } // End Using rng 

        } // End Function GenerateRandomKey 



        public static void Test()
        {
            string JSON = JWT.PetaJson.Json.Format(new User(), JWT.PetaJson.JsonOptions.DontWriteWhitespace);
            System.Console.WriteLine(JSON);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            using (System.IO.TextWriter tw = new System.IO.StringWriter(sb))
            {
                JWT.PetaJson.Json.Write(tw, new User(), JWT.PetaJson.JsonOptions.DontWriteWhitespace);
            }
            System.Console.WriteLine(sb);
        }


        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [System.STAThread]
        static void Main()
        {
            #if (false)
            {
                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
                System.Windows.Forms.Application.Run(new Form1());
            }
            #endif 

            
            Test();


            byte[] key = GenerateRandomKey(128);
            string token = System.Convert.ToBase64String(key);
            byte[] key2 = System.Convert.FromBase64String(token);
            byte[] key3 = System.Text.Encoding.UTF8.GetBytes(token);


            // string jwtToken = JWT.JsonWebToken.Encode(new User(), "I am a unicode capable password", JWT.JwtHashAlgorithm.HS512);
            // string jwtToken = JWT.JsonWebToken.Encode(new User(), token, JWT.JwtHashAlgorithm.HS512);
            // string jwtToken = JWT.JsonWebToken.Encode(new User(), key, JWT.JwtHashAlgorithm.HS512);

            // JWT.RSA.PEM.ExportEcdsaKey();


            string jwtToken = JWT.JsonWebToken.Encode(
                new System.Collections.Generic.Dictionary<string, object>()
                {
                    { "key1", "value1" },
                    { "key2", "value2" }
                }
                //, new User(), key, JWT.JwtHashAlgorithm.HS512
                , new User(), "hello"
                // , JWT.JwtHashAlgorithm.HS256
                // , JWT.JwtHashAlgorithm.RS256
                , JWT.JwtHashAlgorithm.ES256
            );
            System.Console.WriteLine(jwtToken);

            // string jwtTokenRandKey = JWT.JsonWebToken.Encode(null, GenerateRandomKey(), JWT.JwtHashAlgorithm.HS512);
            // System.Console.WriteLine(jwtTokenRandKey);

            string dir = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location
            );

            dir = System.IO.Path.Combine(dir, "../..");
            dir = System.IO.Path.GetFullPath(dir);
            System.Console.WriteLine(dir);

            // using (System.Security.Cryptography.RSACryptoServiceProvider csp = new System.Security.Cryptography.RSACryptoServiceProvider())
            using (System.Security.Cryptography.RSACryptoServiceProvider csp = JWT.RSA.PEM.CreateRsaProvider())
            {
                string privateKey = JWT.RSA.PEM.ExportPrivateKey(csp);
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "Private.txt"), privateKey, System.Text.Encoding.UTF8);
                string publicKey = JWT.RSA.PEM.ExportPublicKey(csp);
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "Public.txt"), publicKey, System.Text.Encoding.UTF8);
            } // End Using csp 

            // object decodedJWTobject = JWT.JsonWebToken.DecodeToObject(jwtToken, key, true);
            User decodedJWTobject = JWT.JsonWebToken.DecodeToObject<User>(jwtToken, key, true);
            System.Console.WriteLine(decodedJWTobject);


            string decodedJWT = JWT.JsonWebToken.Decode(jwtToken, key, true);
            // JWT.JsonWebToken.Decode(jwtToken, key3, true);
            // JWT.JsonWebToken.Decode(jwtToken, token, true);
            // JWT.JsonWebToken.Decode(jwtTokenRandKey, key, true);
            System.Console.WriteLine(decodedJWT);

            System.Console.WriteLine(System.Environment.NewLine);
            System.Console.WriteLine(" --- Press any key to continue --- ");
            System.Console.ReadKey();
        } // End Sub Main 


    } // End Class Program 


} // End Namespace ConsoleTestApplication 
