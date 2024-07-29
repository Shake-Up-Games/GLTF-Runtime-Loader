using System.Reflection;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    internal static class GLTFHelpers
    {

        static MethodInfo getValueGeneric = typeof(JsonNode).GetMethod(nameof(JsonNode.GetValue), BindingFlags.Public | BindingFlags.Instance, Array.Empty<Type>())!;
        static Dictionary<Type, MethodInfo> getValueMethods =
            new(from t in
                    new Type[]{
                        typeof(sbyte),
                        typeof(byte),
                        typeof(short),
                        typeof(ushort),
                        typeof(int),
                        typeof(uint),
                        typeof(float)
                    }
                select new KeyValuePair<Type, MethodInfo>(t, getValueGeneric.MakeGenericMethod(t))
                );

        /// <inheritdoc cref="GetDataFromArray(JsonArray, AccessorComponentType)"/>
        internal static T[] GetDataFromArray<T>(JsonArray array)
        {
            AccessorComponentType typeValue = (from f in typeof(AccessorComponentType).GetFields(BindingFlags.Public | BindingFlags.Static)
                                               let type = f.GetCustomAttribute<ComponentTypeAttribute>()!.Type
                                               where type == typeof(T)
                                               select (AccessorComponentType)f.GetValue(null)!
                                              ).Single();
            return (T[])GetDataFromArray(array, typeValue);
        }

        /// <summary>
        /// Gets an array of data from a containing array, in the format indicated by <paramref name="type"/>.
        /// </summary>
        internal static Array GetDataFromArray(JsonArray container, AccessorComponentType type)
        {
            ArgumentNullException.ThrowIfNull(container, nameof(container));
            FieldInfo typeMember = (from f in typeof(AccessorComponentType).GetFields()
                                    where f.Name == Enum.GetName(type)
                                    select f)!.Single();
            var runtimeType = typeMember.GetCustomAttribute<ComponentTypeAttribute>()!.Type;

            Array r = Array.CreateInstance(runtimeType, container.Count);
            for (int i = 0; i < container.Count; i++)
            {
                var node = container[i];
                var value = getValueMethods[runtimeType].Invoke(node, Array.Empty<object>());
                r.SetValue(value, i);
            }
            return r;
        }

        internal static Nullable<int> ExtractInt(JsonNode source, string property)
        {
            var propertyNode = source[property];
            if (propertyNode == null)
                return null;
            else
                return propertyNode.GetValue<int>();
        }

        internal static string PluralS(int count)
        {
            if (count == 1)
                return string.Empty;
            else
                return "s";
        }
    }
}