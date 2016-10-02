
namespace JSON
{


    public class JsonError : System.Exception
    {
        public JsonError(string message)
            : base(message)
        { } // End Constructor 
    } // End Class JsonError 


    public class Helper
    {

        public static void Test()
        {
            string foobar = "äöüПривет \r\nм\"ир你好，世界\u0005";
            foobar = DecodeJsonString(EscapeString(foobar));
            System.Console.WriteLine(foobar);


            //string jsonString = JSON.Helper.JsonEncodeString("äöüПривет мир你好，世界\u0005");
            //string jsonString = JSON.Helper.JsonEncodeString("\u0005");
            //string jsonString = JSON.Helper.JavaScriptStringEncode("\u0005", false);
            //string jsonString = JSON.Helper.JavaScriptStringEncode("äöüПривет \r\nм\"ир你好，世界\u0005", false);
            //string jsonString = JSON.Helper.EscapeString("äöüПривет \r\nм\"ир你好，世界\u0005");


            string jsonString = EscapeString(@"äöüПривет 
м""ир你好，世界\u0005");


            string foo = DecodeJsonString(jsonString);


            System.Console.WriteLine(jsonString);
            System.Console.WriteLine(foo);
            string jsonString2 = JavaScriptStringEncode(foo, false);
            System.Console.WriteLine(jsonString2);
        } // End Sub Test 


        public static string DecodeJsonString(string text)
        {
            string retVal = null;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            using(System.IO.TextReader rdr = new System.IO.StringReader(text + "\""))
            {
                retVal = (new JsonStringDecoder(rdr, sb)).ReadStringLiteral();
                sb.Length = 0;
                sb = null;
            } // End Using rdr 

            return retVal;
        } // End Function DecodeJsonString 


        // https://github.com/mono/mono/blob/master/mcs/class/System.Json/System.Json/JsonObject.cs
        private class JsonStringDecoder
        {

            private System.IO.TextReader r;
            private System.Text.StringBuilder sb;


            public JsonStringDecoder(System.IO.TextReader textReader, System.Text.StringBuilder builder)
            {
                this.r = textReader;
                this.sb = builder;
            } // End Constructor 


            int peek;
            public bool has_peek;
            public bool prev_lf;
            private int PeekChar()
            {
                if (!has_peek)
                {
                    peek = r.Read();
                    has_peek = true;
                } // End if (!has_peek) 

                return peek;
            } // End Function PeekChar 


            private int ReadChar()
            {
                int v = has_peek ? peek : r.Read();

                has_peek = false;

                if (prev_lf)
                {
                    prev_lf = false;
                }

                if (v == '\n')
                    prev_lf = true;

                return v;
            } // End Function ReadChar 


            public string ReadStringLiteral()
            {
                // if (PeekChar() != '"')
                //     throw new JsonError("Invalid JSON string literal format");

                if (PeekChar() == '"')
                    ReadChar();

                sb.Length = 0;
                while (true)
                {
                    int c = ReadChar();
                    //if (c < 0) throw new JsonError("JSON string is not closed");
                    if (c < 0)
                        return sb.ToString();

                    if (c == '"')
                        return sb.ToString();
                    else if (c != '\\')
                    {
                        sb.Append((char)c);
                        continue;
                    }

                    // escaped expression
                    c = ReadChar();
                    if (c < 0)
                        throw new JsonError("Invalid JSON string literal; incomplete escape sequence");

                    switch (c)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            sb.Append((char)c);
                            break;
                        case 'b':
                            sb.Append('\x8');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'u':
                            ushort cp = 0;
                            for (int i = 0; i < 4; i++)
                            {
                                cp <<= 4;
                                if ((c = ReadChar()) < 0)
                                    throw new JsonError("Incomplete unicode character escape literal");
                                if ('0' <= c && c <= '9')
                                    cp += (ushort)(c - '0');
                                if ('A' <= c && c <= 'F')
                                    cp += (ushort)(c - 'A' + 10);
                                if ('a' <= c && c <= 'f')
                                    cp += (ushort)(c - 'a' + 10);
                            }
                            sb.Append((char)cp);
                            break;
                        default:
                            throw new JsonError("Invalid JSON string literal; unexpected escape character");
                    } // End switch (c)

                } // Whend 

            } // End Function ReadStringLiteral 


        } // End Class  JsonStringDecoder





        // https://github.com/mono/mono/blob/master/mcs/class/System.Json/System.Json/JsonValue.cs
        private static bool NeedEscape(string src, int i)
        {
            char c = src[i];
            return c < 32 || c == '"' || c == '\\'
                // Broken lead surrogate
                || (c >= '\uD800' && c <= '\uDBFF' &&
                    (i == src.Length - 1 || src[i + 1] < '\uDC00' || src[i + 1] > '\uDFFF'))
                // Broken tail surrogate
                || (c >= '\uDC00' && c <= '\uDFFF' &&
                    (i == 0 || src[i - 1] < '\uD800' || src[i - 1] > '\uDBFF'))
                // To produce valid JavaScript
                || c == '\u2028' || c == '\u2029'
                // Escape "</" for <script> tags
                || (c == '/' && i > 0 && src[i - 1] == '<');
        } // End Function NeedEscape 


        public static string EscapeString(string src)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            int start = 0;
            for (int i = 0; i < src.Length; i++)
                if (NeedEscape(src, i))
                {
                    sb.Append(src, start, i - start);
                    switch (src[i])
                    {
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        case '\"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '/': sb.Append("\\/"); break;
                        default:
                            sb.Append("\\u");
                            sb.Append(((int)src[i]).ToString("x04"));
                            break;
                    } // End switch (src[i]) 

                    start = i + 1;
                } // End if (NeedEscape(src, i)) 

            sb.Append(src, start, src.Length - start);
            return sb.ToString();
        } // End Function EscapeString 


        // https://github.com/mono/mono/blob/master/mcs/class/System.Json/System.Json/JsonValue.cs
        // https://github.com/mono/mono/blob/master/mcs/class/System.Web/System.Web/HttpUtility.cs
        public static string JavaScriptStringEncode(string value, bool addDoubleQuotes)
        {
            if (string.IsNullOrEmpty(value))
                return addDoubleQuotes ? "\"\"" : string.Empty;

            int len = value.Length;
            bool needEncode = false;
            char c;
            for (int i = 0; i < len; i++)
            {
                c = value[i];

                if (c >= 0 && c <= 31 || c == 34 || c == 39 || c == 60 || c == 62 || c == 92)
                {
                    needEncode = true;
                    break;
                } // End if (c >= 0 && c <= 31 || c == 34 || c == 39 || c == 60 || c == 62 || c == 92)

            } // Next i 

            if (!needEncode)
                return addDoubleQuotes ? "\"" + value + "\"" : value;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (addDoubleQuotes)
                sb.Append('"');

            for (int i = 0; i < len; i++)
            {
                c = value[i];
                if (c >= 0 && c <= 7 || c == 11 || c >= 14 && c <= 31 || c == 39 || c == 60 || c == 62)
                    sb.AppendFormat("\\u{0:x4}", (int)c);
                else switch ((int)c)
                    {
                        case 8:
                            sb.Append("\\b");
                            break;
                        case 9:
                            sb.Append("\\t");
                            break;
                        case 10:
                            sb.Append("\\n");
                            break;
                        case 12:
                            sb.Append("\\f");
                            break;
                        case 13:
                            sb.Append("\\r");
                            break;
                        case 34:
                            sb.Append("\\\"");
                            break;
                        case 92:
                            sb.Append("\\\\");
                            break;
                        default:
                            sb.Append(c);
                            break;
                } // End switch ((int)c)

            } // Next i 

            if (addDoubleQuotes)
                sb.Append('"');
            
            return sb.ToString();
        } // End Function JavaScriptStringEncode 


    } // End Class Helper


} // End Namespace JSON
