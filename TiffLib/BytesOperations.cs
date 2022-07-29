namespace TiffLib
{
    internal class BytesOperations
    {
        internal static int GetInt(byte[] bytes) 
        {
            return int.Parse(Convert.ToHexString(bytes), System.Globalization.NumberStyles.HexNumber);
        }


        internal static long GetLong(byte[] bytes)
        {
            return long.Parse(Convert.ToHexString(bytes), System.Globalization.NumberStyles.HexNumber);
        }
    }
}
