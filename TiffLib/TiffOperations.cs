namespace TiffLib
{
    internal class TiffOperations
    {
        internal static int GetDataLength(TiffType type, int fieldLength)
        {
            switch (type)
            {
                case TiffType.BYTE:
                case TiffType.ASCII:
                case TiffType.SBYTE:
                case TiffType.UNDEFINED:
                    return fieldLength;

                case TiffType.SHORT:
                case TiffType.SSHORT:
                    return fieldLength * 2;

                case TiffType.LONG:
                case TiffType.SLONG:
                case TiffType.FLOAT:
                    return fieldLength * 4;

                case TiffType.RATIONAL:
                case TiffType.SRATIONAL:
                case TiffType.DOUBLE:
                    return fieldLength * 8;

                default:
                    return 0;
            }
        }


        internal static byte[] ReadHeader(string sourceFile)
        {
            using (FileStream fsSource = new(sourceFile, FileMode.Open, FileAccess.Read))
            {
                int headerSize = 8;
                byte[] header = new byte[headerSize];
                int bytesRead = 0;

                while (headerSize > 0)
                {
                    int n = fsSource.Read(header, bytesRead, headerSize);
                    if (n == 0)
                        break;

                    bytesRead += n;
                    headerSize -= n;
                }

                return header;
            }
        }


        internal static long GetFirstIFDPosition(byte[] header)
        {
            byte[] last4 = header[4..8];
            Array.Reverse(last4);
            return BytesOperations.GetLong(last4);
        }
    }
}
