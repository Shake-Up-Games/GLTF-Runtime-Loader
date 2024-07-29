namespace GLTFRuntime
{
    [System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class ComponentTypeAttribute : Attribute
    {
        public ComponentTypeAttribute(int size, Type type)
        {
            Size = size;
            Type = type;
        }

        public int Size { get; }
        public Type Type { get; }
    }

#pragma warning disable CA1720 // Identifier contains type name

    /// <summary>
    /// Named values for the different componentType integer values listed in accessor JSON.
    /// </summary>
#pragma warning disable CA1008 // Enums should have zero value
    public enum AccessorComponentType : int
#pragma warning restore CA1008 // Enums should have zero value
    {

        /// <summary>
        /// signed byte
        /// </summary>        
        [ComponentType(sizeof(sbyte), typeof(sbyte))]
        SByte = 5120,

        /// <summary>
        /// unsigned byte
        /// </summary>
        [ComponentType(sizeof(byte), typeof(byte))]
        Byte = 5121,

        /// <summary>
        /// signed short
        /// </summary>
        [ComponentType(sizeof(short), typeof(short))]
        Short = 5122,

        /// <summary>
        /// unsigned short
        /// </summary>
        [ComponentType(sizeof(ushort), typeof(ushort))]
        UShort = 5123,

        /// <summary>
        /// signed int
        /// </summary>
        [ComponentType(sizeof(int), typeof(int))]
        Int = 5124,

        /// <summary>
        /// unsigned int
        /// </summary>
        [ComponentType(sizeof(uint), typeof(uint))]
        UInt = 5125,

        /// <summary>
        /// float
        /// </summary>
        [ComponentType(sizeof(float), typeof(float))]
        Float = 5126
    }
#pragma warning restore CA1720 // Identifier contains type name
}