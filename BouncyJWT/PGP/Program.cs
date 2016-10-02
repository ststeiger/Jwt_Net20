using System;
using System.Collections.Generic;
using System.Text;


namespace CSbouncyCastle
{


    class old
    {

        public static void Test()
        {
            CSbouncyCastle.cPGP.GenerateKey("Hello", "world", System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop));
            Console.WriteLine(" --- Press any key to continue ---");
            Console.ReadKey();
        }


    } // End class Program


} // End namespace CSbouncyCastle
