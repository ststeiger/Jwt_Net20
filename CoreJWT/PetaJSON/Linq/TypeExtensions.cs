
namespace CoreJWT.PetaJson
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Defines extension methods that allow coding against <see cref="Type"/> without conditional compilation on versions of .NET framework.
    /// </summary>
    internal static class TypeExtensionsForDotNetCore
    {

        public static Type BaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }

        public static bool IsClass(this Type type)
        {
            return type.GetTypeInfo().IsClass;
        }

        public static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        public static bool IsValueType(this Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }

        public static bool IsInterface(this Type type)
        {
            return type.GetTypeInfo().IsInterface;
        }
        public static IEnumerable<Attribute> GetCustomAttributes(this Type type, Type attributeType)
        {
            return type.GetTypeInfo().GetCustomAttributes(attributeType);
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this Type type, Type attributeType, bool inherit)
        {
            return type.GetTypeInfo().GetCustomAttributes(attributeType, inherit);
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this Type type, bool inherit)
        {
            return type.GetTypeInfo().GetCustomAttributes(inherit);
        }


        // type.GetCustomAttributes(typeof(FlagsAttribute), false)

        public static MemberInfo[] GetMembers(this Type type, BindingFlags bindingAttr)
        {
            return type.GetTypeInfo().GetMembers(bindingAttr);
        }

        public static MethodInfo[] GetMethods(this Type type)
        {
            return type.GetTypeInfo().GetMethods();
        }

        public static MethodInfo GetMethod(this Type type, string name)
        {
            return type.GetTypeInfo().GetMethod(name);
        }

        public static MethodInfo GetMethod(this Type type, string name, BindingFlags bindingAttr)
        {
            return type.GetTypeInfo().GetMethod(name, bindingAttr);
        }

        public static MethodInfo GetMethod(this Type type, string name, Type[] types)
        {
            return type.GetTypeInfo().GetMethod(name, types);
        }


        // formatJson = type.GetMethod("FormatJson", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null);
        public static MethodInfo GetMethod(this Type type, string name, BindingFlags ignore1, string ignore2, Type[] types, string ignore3 )
        {
            return type.GetTypeInfo().GetMethod(name, types);
        }

        


        /// <summary>
        /// Returns a value that indicates whether the specified type can be assigned to the current type.
        /// </summary>
        /// <remarks>
        /// This method emulates the built-in method of the <see cref="Type"/> class which is not available on Windows Runtime.
        /// </remarks>
        public static bool IsAssignableFrom(this Type type, Type otherType)
        {
            return type.GetTypeInfo().IsAssignableFrom(otherType.GetTypeInfo());
        }

        /// <summary>
        /// Returns all the public properties of the specified type.
        /// </summary>
        /// <remarks>
        /// This method emulates the built-in method of the <see cref="Type"/> class which is not available on Windows Runtime.
        /// Note that, unlike the built-in <see cref="Type"/> method, this method does not return properties defined in any of the base types.
        /// However, this should be sufficient for our public types, which have to be sealed on Windows Runtime.
        /// </remarks>
        public static PropertyInfo[] GetProperties(this Type type)
        {
            var properties = new List<PropertyInfo>();
            properties.AddRange(type.GetTypeInfo().DeclaredProperties);
            var baseType = type.GetTypeInfo().BaseType;
            if (baseType != null)
            {
                properties.AddRange(baseType.GetProperties());
            }

            return properties.ToArray();
        }

        public static Type[] GetGenericArguments(this Type type)
        {
            return type.GetTypeInfo().GenericTypeArguments;
        }

        public static Type[] GetInterfaces(this Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces.ToArray();
        }

        public static bool IsAbstract(this Type type)
        {
            return type.GetTypeInfo().IsAbstract;
        }

        public static bool IsGenericType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

    }
}