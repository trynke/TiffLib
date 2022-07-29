namespace TiffLib
{
    public class TiffFilesList
    {
        public bool isJPEG = false;


        // копируем заголовок первого файла в списке, он будет основным
        internal static void CopyHeader(string sourceFile, string newPath)
        {
            using (FileStream fsNew = new(newPath, FileMode.Create, FileAccess.Write))
            {
                byte[] header = TiffOperations.ReadHeader(sourceFile);
                fsNew.Write(header, 0, header.Length);
            }
        }



        // просто копируем файл в конец нового файла
        internal static long CopyFile(string sourceFile, string newPath)
        {
            using (FileStream fsSource = new(sourceFile, FileMode.Open, FileAccess.Read))
            {
                using (FileStream fsNew = new(newPath, FileMode.Append, FileAccess.Write))
                {
                    long startPosition = fsNew.Position - 8; 
                    fsSource.Seek(8, SeekOrigin.Begin);

                    int readSize = (int)fsSource.Length - 8;
                    byte[] fileData = new byte[readSize];
                    int bytesRead = 0;

                    while (readSize > 0)
                    {
                        int n = fsSource.Read(fileData, bytesRead, readSize);
                        if (n == 0)
                            break;

                        bytesRead += n;
                        readSize -= n;
                    }

                    fsNew.Write(fileData, 0, fileData.Length);

                    return startPosition;
                }
            }
        }


        internal static byte[] GetNewOffsetBytes(int fieldOffset, int offsetAddition)
        {
            return BitConverter.GetBytes(fieldOffset + offsetAddition);
        }



        // меняем значения смещений в скопированном файле с учётом всех предыдущих скопированных
        internal void ChangeOffsets(int offsetAddition, int position, int IFDOffset, string newPath, bool isLastPage)
        {
            using (FileStream fsNew = new(newPath, FileMode.Open, FileAccess.ReadWrite))
            {
                fsNew.Seek(position, SeekOrigin.Begin);

                int readSize = 2;
                byte[] numEntries = new byte[readSize]; // считываем количество полей (2 байта)
                int bytesRead = 0;

                while (readSize > 0)
                {
                    int n = fsNew.Read(numEntries, bytesRead, readSize);
                    if (n == 0)
                        break;

                    bytesRead += n;
                    readSize -= n;
                }

                Array.Reverse(numEntries);
                int numEntriesInt = BytesOperations.GetInt(numEntries);
                Console.WriteLine(numEntriesInt);

                for (int i = 0; i < numEntriesInt; i++)
                {
                    readSize = 12;
                    byte[] fieldDescriptor = new byte[readSize]; // считываем весь field descriptor
                    bytesRead = 0;

                    while (readSize > 0)
                    {
                        int n = fsNew.Read(fieldDescriptor, bytesRead, readSize);
                        if (n == 0)
                            break;

                        bytesRead += n;
                        readSize -= n;
                    }

                    byte[] fieldTag = fieldDescriptor[0..2];     // имя тега
                    byte[] fieldType = fieldDescriptor[2..4];    // тип тега
                    byte[] fieldLength = fieldDescriptor[4..8];  // размер данных
                    byte[] fieldOffset = fieldDescriptor[8..12]; // сами данные или ссылка на них

                    long nextFieldPosition = fsNew.Position;

                    Array.Reverse(fieldTag);
                    int fieldTagInt = BytesOperations.GetInt(fieldTag);
                    var tag = (TiffTag)fieldTagInt;

                    Array.Reverse(fieldType);
                    int fieldTypeInt = BytesOperations.GetInt(fieldType);

                    Array.Reverse(fieldLength);
                    int fieldLengthInt = BytesOperations.GetInt(fieldLength);

                    Array.Reverse(fieldOffset);
                    int fieldOffsetInt = BytesOperations.GetInt(fieldOffset);
                    Array.Reverse(fieldOffset);

                    if (tag == TiffTag.COMPRESSION && fieldOffsetInt == 6)
                        isJPEG = true;

                    else if (tag == TiffTag.COMPRESSION && fieldOffsetInt != 6)
                        isJPEG = false;

                    var type = (TiffType)fieldTypeInt;
                    int lengthInBytes = TiffOperations.GetDataLength(type, fieldLengthInt);

                    fsNew.Seek(-4, SeekOrigin.Current);

                    if (tag == TiffTag.STRIPOFFSETS || tag == TiffTag.TILEOFFSETS)
                    {
                        fsNew.Write(GetNewOffsetBytes(fieldOffsetInt, offsetAddition));

                        if (lengthInBytes > 4)
                        {
                            long startPosition = fieldOffsetInt + offsetAddition;
                            fsNew.Seek(startPosition, SeekOrigin.Begin);

                            byte[] newDataOffsets = Array.Empty<byte>();

                            for (int j = 0; j < fieldLengthInt; j++)
                            {
                                readSize = 4;
                                bytesRead = 0;
                                byte[] dataOffset = new byte[readSize];

                                while (readSize > 0)
                                {
                                    int n = fsNew.Read(dataOffset, bytesRead, readSize);
                                    if (n == 0)
                                        break;

                                    bytesRead += n;
                                    readSize -= n;
                                }

                                Array.Reverse(dataOffset);

                                int tableOffsetInt = BytesOperations.GetInt(dataOffset);
                                int newTableOffset = tableOffsetInt + offsetAddition;

                                newDataOffsets = newDataOffsets.Concat(BitConverter.GetBytes(newTableOffset)).ToArray();
                            }
                            fsNew.Seek(startPosition, SeekOrigin.Begin);
                            fsNew.Write(newDataOffsets, 0, newDataOffsets.Length);
                        }
                    }
                        
                    else if (!isJPEG && lengthInBytes > 4)
                        fsNew.Write(GetNewOffsetBytes(fieldOffsetInt, offsetAddition)); 
                    
                    else if (isJPEG)
                    {
                        if (tag == TiffTag.JPEGIFOFFSET)
                            fsNew.Write(GetNewOffsetBytes(fieldOffsetInt, offsetAddition));

                        else if (tag == TiffTag.JPEGDCTABLES || tag == TiffTag.JPEGACTABLES || tag == TiffTag.JPEGQTABLES)
                        {
                            fsNew.Write(GetNewOffsetBytes(fieldOffsetInt, offsetAddition));

                            long startPosition = fieldOffsetInt + offsetAddition;
                            fsNew.Seek(startPosition, SeekOrigin.Begin);

                            byte[] newTableOffsets = Array.Empty<byte>();

                            for (int j = 0; j < fieldLengthInt; j++)
                            {
                                readSize = 4;
                                bytesRead = 0;
                                byte[] tableOffset = new byte[readSize]; 

                                while (readSize > 0)
                                {
                                    int n = fsNew.Read(tableOffset, bytesRead, readSize);
                                    if (n == 0)
                                        break;

                                    bytesRead += n;
                                    readSize -= n;
                                }

                                Array.Reverse(tableOffset);

                                int tableOffsetInt = BytesOperations.GetInt(tableOffset);
                                int newTableOffset = tableOffsetInt + offsetAddition;

                                newTableOffsets = newTableOffsets.Concat(BitConverter.GetBytes(newTableOffset)).ToArray();
                            }

                            fsNew.Seek(startPosition, SeekOrigin.Begin);
                            fsNew.Write(newTableOffsets, 0, newTableOffsets.Length);
                        }

                        else if (lengthInBytes > 4)
                            fsNew.Write(GetNewOffsetBytes(fieldOffsetInt, offsetAddition));
                    }

                    fsNew.Seek(nextFieldPosition, SeekOrigin.Begin);
                }

                readSize = 4;
                byte[] nextIFDOffset = new byte[readSize];
                bytesRead = 0;

                while (readSize > 0)
                {
                    int n = fsNew.Read(nextIFDOffset, bytesRead, readSize);
                    if (n == 0)
                        break;

                    bytesRead += n;
                    readSize -= n;
                }

                Array.Reverse(nextIFDOffset);
                int nextIFDOffsetInt = BytesOperations.GetInt(nextIFDOffset);

                fsNew.Seek(-4, SeekOrigin.Current);

                if (nextIFDOffsetInt != 0)
                {
                    fsNew.Write(GetNewOffsetBytes(nextIFDOffsetInt, offsetAddition));
                    int newOffset = nextIFDOffsetInt + offsetAddition;

                    fsNew.Close();
                    ChangeOffsets(offsetAddition, newOffset, IFDOffset, newPath, isLastPage);
                }

                else if (!isLastPage)
                    fsNew.Write(BitConverter.GetBytes(IFDOffset), 0, 4); 
            }
        }


        public void Merge(string[] files, string newPath)
        {
            string firstFile = files[0];
            CopyHeader(firstFile, newPath);

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                bool isLastPage;    

                if (i == files.Length - 1)
                    isLastPage = true;
                else
                    isLastPage = false;

                long offsetAddition = CopyFile(file, newPath);

                byte[] header = TiffOperations.ReadHeader(file);
                int position = (int)TiffOperations.GetFirstIFDPosition(header) + (int)offsetAddition;

                int nextIFDPosition = 0;

                if (!isLastPage) // получаем смещение IFD следующего файла, чтобы сразу записать в конец этого
                {
                    byte[] prevHeader = TiffOperations.ReadHeader(files[i + 1]);
                    nextIFDPosition = (int)TiffOperations.GetFirstIFDPosition(prevHeader);
                    
                    foreach (string prev_file in files[0..(i + 1)])
                    {
                        long length = new FileInfo(prev_file).Length;
                        nextIFDPosition += (int)length;
                    }

                    nextIFDPosition -= 8 * files[0..(i + 1)].Length;
                }
                
                if (files.Length > 1)
                    ChangeOffsets((int)offsetAddition, position, nextIFDPosition, newPath, isLastPage);
            }
        }


        public void MergeDirectory(string directoryPath, string newPath)
        {
            string[] files = Directory.GetFiles(directoryPath);
            Merge(files, newPath);
        }
    }
}