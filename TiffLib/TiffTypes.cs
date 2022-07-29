namespace TiffLib
{
    /// <summary>
    /// Tag data type.
    /// </summary>
    public enum TiffType : short
    {
        /// <summary>
        /// 8-bit unsigned integer.
        /// </summary>
        BYTE = 1,

        /// <summary>
        /// 8-bit bytes with last byte <c>null</c>.
        /// </summary>
        ASCII = 2,

        /// <summary>
        /// 16-bit unsigned integer.
        /// </summary>
        SHORT = 3,

        /// <summary>
        /// 32-bit unsigned integer. 
        /// </summary>
        LONG = 4,

        /// <summary>
        /// 64-bit unsigned fraction. 
        /// </summary>
        RATIONAL = 5,

        /// <summary>
        /// 8-bit signed integer.
        /// </summary>
        SBYTE = 6,

        /// <summary>
        /// 8-bit untyped data.
        /// </summary>
        UNDEFINED = 7,

        /// <summary>
        /// 16-bit signed integer.
        /// </summary>
        SSHORT = 8,

        /// <summary>
        /// 32-bit signed integer.
        /// </summary>
        SLONG = 9,

        /// <summary>
        /// 64-bit signed fraction.
        /// </summary>
        SRATIONAL = 10,

        /// <summary>
        /// 32-bit IEEE floating point.
        /// </summary>
        FLOAT = 11,

        /// <summary>
        /// 64-bit IEEE floating point.
        /// </summary>
        DOUBLE = 12,
    }
}
