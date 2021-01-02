using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Atomech
{
    public static class ReflectionHelper
    {
        private static Assembly[] s_allAssemblies;

        /// <summary>
        /// Instantiate all types and cast them all to the target type
        /// </summary>
        /// <param name="types">Types to instantiate</param>
        /// <typeparam name="T">Type to cast to</typeparam>
        /// <returns>Set of instances</returns>
        /// <exception cref="Exception">Throws exception of one of the types is not castable to <typeparamref name="T"/></exception>
        public static IEnumerable<T> InstantiateAllAs<T>(this IEnumerable<Type> types)
        {
            var targetType = typeof(T);
            foreach (var type in types)
            {
                if (!targetType.IsAssignableFrom(type))
                    throw new Exception(string.Format("Type {0} is not assignable from {1}", targetType.Name, type.Name));
                yield return (T)Activator.CreateInstance(type);
            }
        }

        /// <summary>
        /// Instantiate all types and cast them all to the target type
        /// </summary>
        /// <param name="types">Types to instantiate</param>
        /// <param name="resultSet">List to populate</param>
        /// <typeparam name="T">Type to cast to</typeparam>
        /// <returns>Set of instances</returns>
        /// <exception cref="Exception">Throws exception of one of the types is not castable to <typeparamref name="T"/></exception>
        public static void InstantiateAllAs<T>(List<Type> types, List<T> resultSet)
        {
            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                resultSet.Add((T)Activator.CreateInstance(type));
            }
        }

        /// <summary>
        /// Searches all assemblies to find types that implement specific type T
        /// </summary>
        /// <typeparam name="T">Type to implement</typeparam>
        /// <returns></returns>
        public static IEnumerable<Type> GetTypesImplementing<T>()
        {
            return AllCustomAssemblies.SelectMany(a => a.GetTypes()).Where(t => typeof(T).IsAssignableFrom(t) && t != typeof(T));
        }

        /// <summary>
        /// Searches all assemblies to find types that implement specific type T
        /// </summary>
        /// <typeparam name="T">Type to implement</typeparam>
        /// <returns></returns>
        public static void GetTypesImplementing<T>(List<Type> resultTypes)
        {
            var asseblies = AllCustomAssemblies;
            for (var i = 0; i < asseblies.Length; i++)
            {
                var assembly = asseblies[i];
                var types = assembly.GetTypes();
                for (var j = 0; j < types.Length; j++)
                {
                    var type = types[j];
                    if (typeof(T).IsAssignableFrom(type) && type != typeof(T))
                    {
                        resultTypes.Add(type);
                    }
                }

            }
        }

        public const string AssemblyPrefix = "Assembly";

        /// <summary>
        /// Gets all loaded assemblies
        /// </summary>
        public static Assembly[] AllCustomAssemblies
        {
            get { return s_allAssemblies ?? (s_allAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(s => s.FullName.StartsWith(AssemblyPrefix, StringComparison.Ordinal)).ToArray()); }
        }

        /// <summary>
        /// Searches for public instance method with a specific name
        /// </summary>
        /// <param name="type">Target type to search through</param>
        /// <param name="methodName">Method name to search for</param>
        /// <returns></returns>
        public static MethodInfo GetPublicInstanceMethod(this Type type, string methodName)
        {
            return type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        /// <summary>
        /// Search type by non-full-qualified name in a target assembly
        /// </summary>
        /// <param name="assembly">Assembly to search in</param>
        /// <param name="typename">Shot type name to search for</param>
        /// <returns></returns>
        public static Type SearchType(this Assembly assembly, string typename)
        {
            return assembly.GetTypes().FirstOrDefault(t => t.Name == typename || t.FullName == typename);
        }



        /// <summary>
        /// Use Reflection to access ScriptAttributeUtility to find the
        /// PropertyDrawer type for a property type
        /// </summary>
        public static Type GetPropertyDrawerType(Type typeToDraw)
        {
            var scriptAttributeUtilityType = GetScriptAttributeUtilityType();

            var getDrawerTypeForTypeMethod =
                        scriptAttributeUtilityType.GetMethod(
                            "GetDrawerTypeForType",
                            BindingFlags.Static | BindingFlags.NonPublic, null,
                            new[] { typeof(Type) }, null);

            return (Type)getDrawerTypeForTypeMethod.Invoke(null, new[] { typeToDraw });
        }

        public static Type GetScriptAttributeUtilityType()
        {
            var asm = Array.Find(AppDomain.CurrentDomain.GetAssemblies(),
                                              (a) => a.GetName().Name == "UnityEditor");

            var types = asm.GetTypes();
            var type = Array.Find(types, (t) => t.Name == "ScriptAttributeUtility");

            return type;
        }

    }

}