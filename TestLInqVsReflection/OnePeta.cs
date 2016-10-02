
namespace TestLinqVsReflection
{


    internal class OnePeta
    {


        public static void AppendPath(ref string[] filez, string path)
        {
            for (int i = 0; i < filez.Length; ++i)
            {
                filez[i] = System.IO.Path.Combine(path, filez[i]);
                if (!System.IO.File.Exists(filez[i]))
                    System.Console.WriteLine("Error: " + filez[i]);
            } // Next i 

        } // End Sub AppendPath  


        public static string[] GetImports()
        {
            string[] imports = new string[]
                {
                      "Imports System.IO"
                    , "Imports System.Text"
                    , "Imports System.Collections.Generic"
                    , "Imports System.Collections"
                    , "Imports System.Diagnostics"
                    , "Imports System.Globalization"
                    , "Imports System.Reflection.Emit"
                    , "Imports System.Reflection"
                    , "Imports System"

                    , "Imports XXXX.PetaJson.Internal"
                    , "Imports XXXX.PetaJson"
                };

            return imports;
        } // End Function GetImports 


        public static void GetPathText(ref string[] filez)
        {

            string[] imports = GetImports();

            string[] namespaces = new string[]{
                 "Namespace XXXX.PetaJson.Internal"
                ,"Namespace XXXX.PetaJson"
                ,"End Namespace"
            };


            for (int i = 0; i < filez.Length; ++i)
            {
                if (!System.IO.File.Exists(filez[i]))
                    System.Console.WriteLine("Error: " + filez[i]);

                filez[i] = System.IO.File.ReadAllText(filez[i]);

                for (int j = 0; j < imports.Length; ++j)
                {
                    filez[i] = filez[i].Replace(imports[j], "");
                } // Next i 

                for (int j = 0; j < namespaces.Length; ++j)
                {
                    filez[i] = filez[i].Replace(namespaces[j], "");
                } // Next j 

                string cont = filez[i];
                // System.Console.WriteLine(cont);
            } // Next i 

        } // End Sub GetPathText 


        public static string ConcatFiles(params string[][] filez)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            string[] imports = GetImports();

            sb.AppendLine();
            for (int i = 0; i < imports.Length; ++i)
            {
                sb.AppendLine(imports[i]);
            } // Next i 

            sb.Replace("Imports XXXX.PetaJson.Internal", "");
            sb.Replace("Imports XXXX.PetaJson", "");

            sb.AppendLine(System.Environment.NewLine);
            sb.AppendLine("Namespace JWT.PetaJson");
            sb.AppendLine(System.Environment.NewLine);


            for (int i = 0; i < filez.Length; ++i)
            {
                for (int j = 0; j < filez[i].Length; ++j)
                {
                    sb.AppendLine(filez[i][j]);
                } // Next j 

            } // Next i 

            sb.AppendLine(System.Environment.NewLine);
            sb.AppendLine(System.Environment.NewLine);
            sb.AppendLine("End Namespace");
            sb.AppendLine(System.Environment.NewLine);

            sb.Replace("\r\n", "\n");
            sb.Replace("\n\n\n\n\n\n", "\n\n");
            sb.Replace("\n\n\n\n\n", "\n\n");
            sb.Replace("\n\n\n\n", "\n\n\n");

            return sb.ToString();
        } // End Function ConcatFiles  


        // http://www.slideshare.net/billkarwin/models-for-hierarchical-data
        // http://karwin.blogspot.ch/2010/03/rendering-trees-with-closure-tables.html
        // https://www.percona.com/blog/2011/02/14/moving-subtrees-in-closure-table/
        public static void MergeVbPetaJSON()
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            path = System.IO.Path.Combine(path, "../../../..");
            path = System.IO.Path.Combine(path, "vbPetaJSON");
            path = System.IO.Path.Combine(path, "PetaJSON");
            path = System.IO.Path.GetFullPath(path);


            string internalPath = System.IO.Path.Combine(path, "Internal");


            string[] filez1 = "ReadCallback_t.vb WriteCallback_t.vb".Split(' ');
            string[] filez2 = System.IO.Directory.GetFiles(path, "IJson*.vb");
            string[] filez3 = "JsonAttribute.vb JsonExcludeAttribute.vb JsonLineOffset.vb JsonOptions.vb JsonParseException.vb JsonUnknownAttribute.vb LiteralKind.vb Json.vb".Split(' ');
            AppendPath(ref filez1, path);
            AppendPath(ref filez2, path);
            AppendPath(ref filez3, path);

            GetPathText(ref filez1);
            GetPathText(ref filez2);
            GetPathText(ref filez3);


            // Namespace JWT.PetaJson.Internal
            string[] filez4 = "Writer.vb Utils.vb Tokenizer.vb Token.vb ThreadSafeCache.vb ReflectionInfo.vb Reader.vb JsonMemberInfo.vb Emit.vb DecoratingActivator.vb".Split(' ');
            AppendPath(ref filez4, internalPath);
            GetPathText(ref filez4);


            string myFile = ConcatFiles(filez1, filez2, filez3, filez4);

            string destPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            destPath = System.IO.Path.Combine(destPath, "../../..");
            destPath = System.IO.Path.Combine(destPath, "Peta.vb");
            destPath = System.IO.Path.GetFullPath(destPath);

            System.IO.File.WriteAllText(destPath, myFile, System.Text.Encoding.UTF8);
            System.Console.WriteLine("Finished");
        } // End Sub MergeVbPetaJSON  


    } // End Class OnePeta


} // End Namespace TestLinqVsReflection 
