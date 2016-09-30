
using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace BouncyCastleTest
{


    static class Program
    {


        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (false)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }


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
