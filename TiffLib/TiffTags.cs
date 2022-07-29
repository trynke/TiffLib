namespace TiffLib
{
    /// <summary>
    /// TIFF tag definitions. Only those that are necessary for spliting and merging multipage TIFFs.
    /// </summary>
    public enum TiffTag
    {
        /// <summary>
        /// Tag placeholder
        /// </summary>
        IGNORE = 0,

        /// <summary>
        /// Data compression technique.
        /// </summary>
        COMPRESSION = 259,

        /// <summary>
        /// Offsets to data strips.
        /// </summary>
        STRIPOFFSETS = 273,

        /// <summary>
        /// Bytes counts for strips.
        /// </summary>
        STRIPBYTECOUNTS = 279,

        /// <summary>
        /// Offsets to data tiles.
        /// </summary>
        TILEOFFSETS = 324,

        /// <summary>
        /// Byte counts for tiles.
        /// </summary>
        TILEBYTECOUNTS = 325,

        /// <summary>
        /// Pointer to SOI marker.
        /// </summary>
        JPEGIFOFFSET = 513,

        /// <summary>
        /// JFIF stream length
        /// </summary>
        JPEGIFBYTECOUNT = 514,

        /// <summary>
        /// Q matrice offsets.
        /// </summary>
        JPEGQTABLES = 519,

        /// <summary>
        /// DCT table offsets.
        /// </summary>
        JPEGDCTABLES = 520,

        /// <summary>
        /// AC coefficient offsets.
        /// </summary>
        JPEGACTABLES = 521,
    }
}