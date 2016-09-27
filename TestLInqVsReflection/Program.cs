using System;

namespace TestLInqVsReflection
{
    class MainClass
    {

        private static System.Reflection.MethodInfo m_FlexibleChangeType;

        static MainClass()
        {
            m_FlexibleChangeType = typeof(MainClass).GetMethod("FlexibleChangeType", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        }


        private static object FlexibleChangeType(object objVal, System.Type t)
        {
            bool typeIsNullable = (t.IsGenericType && object.ReferenceEquals(t.GetGenericTypeDefinition(), typeof(System.Nullable<>)));
            bool typeCanBeAssignedNull = !t.IsValueType || typeIsNullable;

            if (objVal == null || object.ReferenceEquals(objVal, System.DBNull.Value))
            {
                if (typeCanBeAssignedNull)
                    return null;
                else
                    throw new System.ArgumentNullException("objVal ([DataSource] => SetProperty => FlexibleChangeType => you're trying to assign NULL to a type that NULL cannot be assigned to...)");
            } // End if (objVal == null || object.ReferenceEquals(objVal, System.DBNull.Value))

            // Get base-type
            System.Type tThisType = objVal.GetType();

            if (typeIsNullable)
            {
                t = System.Nullable.GetUnderlyingType(t);
            }


            if (object.ReferenceEquals(tThisType, t))
                return objVal;

            // Convert Guid => string
            if (object.ReferenceEquals(t, typeof(string)) && object.ReferenceEquals(tThisType, typeof(System.Guid)))
            {
                return objVal.ToString();
            }

            // Convert string => Guid 
            if (object.ReferenceEquals(t, typeof(System.Guid)) && object.ReferenceEquals(tThisType, typeof(string)))
            {
                return new System.Guid(objVal.ToString());
            }

            return System.Convert.ChangeType(objVal, t);
        } // End Function MyChangeType



        // https://stackoverflow.com/questions/321650/how-do-i-set-a-field-value-in-an-c-sharp-expression-tree
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static System.Action<T, object> GetSetter<T>(string fieldName)
        {
            // Class in which to set value
            System.Linq.Expressions.ParameterExpression targetExp = System.Linq.Expressions.Expression.Parameter(typeof(T), "target");

            // Object's type:
            System.Linq.Expressions.ParameterExpression valueExp = System.Linq.Expressions.Expression.Parameter(typeof(object), "value");


            // Expression.Property can be used here as well
            System.Linq.Expressions.MemberExpression memberExp =
                // System.Linq.Expressions.Expression.Field(targetExp, fieldName);
                // System.Linq.Expressions.Expression.Property(targetExp, fieldName);
                System.Linq.Expressions.Expression.PropertyOrField(targetExp, fieldName);


            // http://www.dotnet-tricks.com/Tutorial/linq/RJX7120714-Understanding-Expression-and-Expression-Trees.html
            System.Linq.Expressions.ConstantExpression targetType = System.Linq.Expressions.Expression.Constant(memberExp.Type);

            // http://stackoverflow.com/questions/913325/how-do-i-make-a-linq-expression-to-call-a-method
            System.Linq.Expressions.MethodCallExpression mce = System.Linq.Expressions.Expression.Call(m_FlexibleChangeType, valueExp, targetType);


            //System.Linq.Expressions.UnaryExpression conversionExp = System.Linq.Expressions.Expression.Convert(valueExp, memberExp.Type);
            System.Linq.Expressions.UnaryExpression conversionExp = System.Linq.Expressions.Expression.Convert(mce, memberExp.Type);


            System.Linq.Expressions.BinaryExpression assignExp =
                //System.Linq.Expressions.Expression.Assign(memberExp, valueExp); // Without conversion 
                System.Linq.Expressions.Expression.Assign(memberExp, conversionExp);

            //System.Action<TTarget, TValue> setter = System.Linq.Expressions.Expression
            System.Action<T, object> setter = System.Linq.Expressions.Expression
                .Lambda<System.Action<T, object>>(assignExp, targetExp, valueExp).Compile();

            return setter;
        }



        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static System.Func<T, object> GetGetter<T>(string fieldName)
        {
            System.Linq.Expressions.ParameterExpression p = System.Linq.Expressions.Expression.Parameter(typeof(T));
            System.Linq.Expressions.MemberExpression prop = System.Linq.Expressions.Expression.PropertyOrField(p, fieldName);
            System.Linq.Expressions.UnaryExpression con = System.Linq.Expressions.Expression.Convert(prop, typeof(object));
            System.Linq.Expressions.LambdaExpression exp = System.Linq.Expressions.Expression.Lambda(con, p);

            System.Func<T, object> getter = (System.Func<T, object>)exp.Compile();
            return getter;
        }


        public class cUser
        {
            public int? ID = 123;
            public string Name = "hello";

            private string m_Language = "en-US";
            public string Language
            {
                get{ return this.m_Language;}
                set{ this.m_Language = value;}
            }
        }


        public static void Main(string[] args)
        {
            int iRepeatCount = 10000;
            // Action<T, object>[] setters = new Action<T, object>[count];

            System.Type t = typeof(cUser);
            System.Reflection.FieldInfo[] fis = t.GetFields();
            System.Reflection.PropertyInfo[] pis = t.GetProperties();
            System.Reflection.MemberInfo[] mis = new System.Reflection.MemberInfo[fis.Length + pis.Length];
            System.Array.Copy(fis, mis, fis.Length);
            System.Array.Copy(pis, 0, mis, fis.Length, pis.Length);

            System.Func<cUser, object>[] linqGetters = new System.Func<cUser, object>[mis.Length];
            System.Action<cUser, object>[] linqSetters = new System.Action<cUser, object>[mis.Length];

            for (int i = 0; i < mis.Length; ++i)
            {
                linqGetters[i] = GetGetter<cUser>(mis[i].Name);
                linqSetters[i] = GetSetter<cUser>(mis[i].Name);
            } // Next i




            System.Diagnostics.Stopwatch swReflection = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch swLinq = new System.Diagnostics.Stopwatch();

            cUser reflectionUser = null;
            swReflection.Start();
            reflectionUser = new cUser();
            for (int j = 0; j < iRepeatCount; ++j)
            {
                for (int i = 0; i < fis.Length; ++i)
                {
                    System.Reflection.FieldInfo fi  = fis[i]; //= tThisType.GetField(strName, m_CaseSensitivity);
                    object obj = fi.GetValue(reflectionUser);
                    // System.Console.WriteLine(obj);
                    // fi.SetValue(reflectionUser, null);
                } // Next i
                for (int i = 0; i < pis.Length; ++i)
                {
                    System.Reflection.PropertyInfo pi  = pis[i]; //= tThisType.GetField(strName, m_CaseSensitivity);
                    // object obj = pi.GetValue(reflectionUser);
                    // System.Console.WriteLine(obj);
                    pi.SetValue(reflectionUser, null);
                } // Next i
            }
            swReflection.Stop();


            cUser linqUser = null;
            swLinq.Start();
            linqUser = new cUser();
            for (int j = 0; j < iRepeatCount; ++j)
            {
                for (int i = 0; i < linqGetters.Length; ++i)
                {
                    // object obj = linqGetters[i](linqUser);
                    // System.Console.WriteLine(obj);
                    linqSetters[i](linqUser, null);
                } // Next i
            }
            swLinq.Stop();

            // Linq getter: 12x faster
            // Linq setter:  2x faster

            System.Console.WriteLine(reflectionUser);
            System.Console.WriteLine(linqUser);
            System.Console.Write("Reflection:\t");
            System.Console.WriteLine(swReflection.Elapsed);
            System.Console.Write("Linq:\t\t");
            System.Console.WriteLine(swLinq.Elapsed);

            Console.WriteLine("Hello World!");
        }
    }
}
