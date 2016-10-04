
namespace BouncyCastleTest
{

    public class User
    {
        public int Id = 123;
        public string Name = "Test";

        // [BouncyJWT.PetaJson.JsonExclude]
        public string Language = "de-CH";
        public string Bla = "Test\r\n123\u0005äöüÄÖÜñõ";
    } // End Class User 


    static class Program
    {

        public static string ReplaceDateTime(System.Text.RegularExpressions.Match ma)
        {
            string str = System.Text.RegularExpressions.Regex.Replace(ma.Value, @"\s*", "");
            str = str.ToLower();
            str = str.Replace("cast(0x", "");
            str = str.Replace("asdatetime)", "");
            str = "'" + HexDateTimeToDateTimeString(str) + "'";

            return str;
        }

        public static string ReplaceDateTime2(System.Text.RegularExpressions.Match ma)
        {
            string str = System.Text.RegularExpressions.Regex.Replace(ma.Value, @"\s*", "");
            str = str.ToLower();
            str = str.Replace("cast(0x", "");
            str = str.Replace("asdatetime2)", "");
            str = "'" + HexDateTimeToDateTimeString(str) + "'";

            return str;
        }

        // datetime: two integers, first day since jan 1900, 2nd number of tick since midnight
        // 1 tick = 1/300 of a second ==> 
        // x ticks * 1s/300 ticks = x ticks * 1s/300ticks *1000ms/s = x *1/300*1000 = x * 10/3 ms
        static string HexDateTimeToDateTimeString(string dateTimeHexString)
        {
            string datePartHexString = dateTimeHexString.Substring(0, 8);
            int datePartInt = System.Convert.ToInt32(datePartHexString, 16);
            System.DateTime dateTimeFinal = (new System.DateTime(1900, 1, 1)).AddDays(datePartInt);

            string timePartHexString = dateTimeHexString.Substring(8, 8);
            int timePartInt = System.Convert.ToInt32(timePartHexString, 16);
            double timePart = timePartInt * 10 / 3;
            dateTimeFinal = dateTimeFinal.AddMilliseconds(timePart);

            return dateTimeFinal.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff");
        }


        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [System.STAThread]
        static void Main()
        {
            if (false)
            {
                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
                System.Windows.Forms.Application.Run(new Form1());
            }

            // TestECDSA.Test();
            // TestRSA.Test();

            string fn =@"/root/Downloads/ChineseYears.sql";
            fn =@"/root/Downloads/AhnenDaten.sql";

            string sql = System.IO.File.ReadAllText(fn, System.Text.Encoding.UTF8);
            sql = System.Text.RegularExpressions.Regex.Replace(sql, @"CAST\s*\(\s*0x[a-f0-9]{16}\s*AS\s*datetime\s*\)"
                , new System.Text.RegularExpressions.MatchEvaluator(ReplaceDateTime)
            ,System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );


            sql = System.Text.RegularExpressions.Regex.Replace(sql, @"CAST\s*\(\s*0x[a-f0-9]{18}\s*AS\s*datetime2\s*\)"
                , new System.Text.RegularExpressions.MatchEvaluator(ReplaceDateTime2)
            ,System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
            


            // https://msdn.microsoft.com/en-us/library/ewy2t5e0(v=vs.110).aspx
            sql = System.Text.RegularExpressions.Regex.Replace(sql, @"N('[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}')"
                    , "$1"
                    ,System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            System.Console.WriteLine(sql);


            string pubKey = @"-----BEGIN PUBLIC KEY-----
MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCYk9VtkOHIRNCquRqCl9bbFsGw
HdJJPIoZENwcHYBgnqVoEa5SJs8ddkNNna6M+Gln2n4S/G7Mu+Cz0cQg06Ru8919
hYWGWdyVumAGgJwMEKAzUj9651Y6AAOcAM0qX/f0DrLlUAZFy+64L8kVjuCyFdti
5d3yaGnFM+Xw/4fcLwIDAQAB
-----END PUBLIC KEY-----
";

            string privKey = @"-----BEGIN RSA PRIVATE KEY-----
MIICXQIBAAKBgQCYk9VtkOHIRNCquRqCl9bbFsGwHdJJPIoZENwcHYBgnqVoEa5S
Js8ddkNNna6M+Gln2n4S/G7Mu+Cz0cQg06Ru8919hYWGWdyVumAGgJwMEKAzUj96
51Y6AAOcAM0qX/f0DrLlUAZFy+64L8kVjuCyFdti5d3yaGnFM+Xw/4fcLwIDAQAB
AoGAGyDZ51/FzUpzAY/k50hhEtZSfOJog84ITdmiETurmkJK7ZyLLp8o3zeqUtAQ
+46liyodlXmdp7hWBRLseNu4lh1gQGYj4/fH2BT75/zFngaTdz7pANKq6Y5IOHg0
C1UatzmuSmDGk/l7g1gQyWo8dcwjrzvsGWBAFZ4QHy2OsE0CQQDNStOX0USyfgrZ
AkKOfs3paaxVB/SZTBaorcqo8nBX1Fx/rdpBTIezHuZQchF/BGpHLS7/yyve+jg/
dspR7XZdAkEAvkO10QFsDR1GJwVcUpG1LguznKqS7v6FscnpBFvfsf7UaqNHCGvY
Feau1EwekVRl77ZKUPhDQt7XFniBO40b+wJBALZnQ7Xi1H0bjJvgbC6b8Gzx3ZL3
rJcAiil5sVWHg9Yl88HmQMRAMVovnEfh8jW/QIbZWKciaGqIPK326DD/ImkCQQCC
k1OHQfOWuH15sCshG5B9Lliw7ztxu8mjL0+0xxypOpsrKC1KsUCWHz/iwO7FjGd8
8Nzl3svCa86vRDpk1T3bAkBWjvKigxbkpYPbayKwjeWTiS3YIg63N2WUaetFBAD2
Yrv+Utm12zi99pZNA5WCqO/UhN9poJdWaYqYYImYhH8N
-----END RSA PRIVATE KEY-----
";



            Org.BouncyCastle.Crypto.AsymmetricKeyParameter rsaPrivate = TestRSA.ReadPrivateKey(privKey);
            BouncyJWT.JwtKey key = new BouncyJWT.JwtKey(rsaPrivate);


            Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair kpArbitrary = TestRSA.GenerateRsaKeyPair(1024);
            BouncyJWT.JwtKey arbitraryKey = new BouncyJWT.JwtKey(kpArbitrary.Private);


            string token = BouncyJWT.JsonWebToken.Encode(new User(), key, BouncyJWT.JwtHashAlgorithm.RS256);

            System.Console.WriteLine(token);
            User thisUser = BouncyJWT.JsonWebToken.DecodeToObject<User>(token, key, true);
            User wrongUser = BouncyJWT.JsonWebToken.DecodeToObject<User>(token, arbitraryKey, true);

            
            System.Console.WriteLine(thisUser);
            System.Console.WriteLine(wrongUser);


            byte[] ba1 = TestSha.SHA512("Test");
            byte[] ba2 = TestSha.Sha512Managed("Test");
            System.Console.WriteLine(ba1);
            System.Console.WriteLine(ba2);



            System.Console.WriteLine(System.Environment.NewLine);
            System.Console.WriteLine(" --- Press any key to continue --- ");
            System.Console.ReadKey();
        }


    }


}
