namespace TiffLib
{
    public class TiffFile
    {
        public string path;
        public byte[] header = new byte[8];

        public bool isLastIFD = false;
        public long nextIFDPosition = 0;

        public long imageDataTagPosition = 0;
        public byte[] imageDataOffsets = Array.Empty<byte>();
        public byte[] imageDataByteCounts = Array.Empty<byte>();

        public bool isJPEG = false;
        
        // Для JPEG:
        
        public long JPEGIFTagPosition = 0;
        public byte[] JPEGIFOffset = Array.Empty<byte>();
        public byte[] JPEGIFByteCount = Array.Empty<byte>();

        public byte[] JPEGQTablesOffsets = Array.Empty<byte>();
        public byte[] JPEGDCTablesOffsets = Array.Empty<byte>();
        public byte[] JPEGACTablesOffsets = Array.Empty<byte>();

        public long JPEGQTablesTagPosition = 0;
        public long JPEGDCTablesTagPosition = 0;
        public long JPEGACTablesTagPosition = 0;

        
        public TiffFile(string path)
        {
            this.path = path;
        }


        // считываем заголовок (8 байтов), запоминаем его и возвращаем позицию первой ifd
        // считаем что везде используется little-endian (intel)
        internal long GetHeader()  
        {
            header = TiffOperations.ReadHeader(path);

            long IFDPosition = TiffOperations.GetFirstIFDPosition(header);

            header[4] = 0x08;
            for (int i = 5; i < 7; i++) // преобразуем заголовок, так как поменяется расположение первой ifd
            {
                header[i] = 0x00;
            }

            return IFDPosition;
        }
        

        internal void CheckTags(TiffTag tag, long tagPosition, byte[] data)
        {

            if (tag == TiffTag.STRIPOFFSETS || tag == TiffTag.TILEOFFSETS)
            {
                imageDataTagPosition = tagPosition;
                imageDataOffsets = data;
            }

            else if (tag == TiffTag.STRIPBYTECOUNTS || tag == TiffTag.TILEBYTECOUNTS)
                imageDataByteCounts = data;

            if (isJPEG)
            {
                if (tag == TiffTag.JPEGQTABLES)
                {
                    JPEGQTablesOffsets = data;
                    JPEGQTablesTagPosition = tagPosition;
                }

                else if (tag == TiffTag.JPEGDCTABLES)
                {
                    JPEGDCTablesOffsets = data;
                    JPEGDCTablesTagPosition = tagPosition;
                }

                else if (tag == TiffTag.JPEGACTABLES)
                {
                    JPEGACTablesOffsets = data;
                    JPEGACTablesTagPosition = tagPosition;
                }

                else if (tag == TiffTag.JPEGIFOFFSET)
                {
                    JPEGIFOffset = data;
                    JPEGIFTagPosition = tagPosition;
                }

                else if (tag == TiffTag.JPEGIFBYTECOUNT)
                    JPEGIFByteCount = data;

            }
        }


        // копируем информацию заголовка и ifd
        internal long CopyMetaData(long IFDPosition, string pathNew)
        {
            using (FileStream fsSource = new(path, FileMode.Open, FileAccess.Read))
            {
                using (FileStream fsNew = new(pathNew, FileMode.Create, FileAccess.Write))
                {
                    fsNew.Write(header, 0, header.Length); // записываем в файле tiff'овский заголовок

                    fsSource.Seek(IFDPosition, SeekOrigin.Begin); // перемещаемся на начало первой ifd для считывания

                    int readSize = 2;
                    byte[] numEntries = new byte[readSize]; // считываем количество полей (2 байта)
                    int bytesRead = 0;

                    while (readSize > 0)
                    {
                        int n = fsSource.Read(numEntries, bytesRead, readSize);
                        if (n == 0)
                            break;

                        bytesRead += n;
                        readSize -= n;
                    }

                    fsNew.Write(numEntries, 0, numEntries.Length); // записываем количество полей

                    Array.Reverse(numEntries);
                    int numEntriesInt = BytesOperations.GetInt(numEntries);

                    int dataWritePosition = header.Length + numEntries.Length + numEntriesInt * 12 + 4; // отсюда будем записывать данные (после директории)
                    byte[] fieldsDataArray = Array.Empty<byte>();

                    for (int i = 0; i < numEntriesInt; i++)
                    {
                        readSize = 12;
                        byte[] fieldDescriptor = new byte[readSize]; // считываем весь field descriptor (12 байтов), дальше поделим
                        bytesRead = 0;

                        while (readSize > 0)
                        {
                            int n = fsSource.Read(fieldDescriptor, bytesRead, readSize);
                            if (n == 0)
                                break;

                            bytesRead += n;
                            readSize -= n;
                        }

                        byte[] fieldTag = fieldDescriptor[0..2];
                        fsNew.Write(fieldTag, 0, fieldTag.Length); // записываем имя тега 

                        byte[] fieldType = fieldDescriptor[2..4];
                        fsNew.Write(fieldType, 0, fieldType.Length); // записываем тип тега

                        byte[] fieldLength = fieldDescriptor[4..8];
                        fsNew.Write(fieldLength, 0, fieldLength.Length); // записываем размер данных

                        byte[] fieldOffset = fieldDescriptor[8..12]; // читаем, но не записываем, т.к. offset будет другое значение

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
                  
                        if (lengthInBytes <= 4)
                        { 
                            fsNew.Write(fieldOffset); // если сами данные входят в поле offset, записываем их туда
                            CheckTags(tag, fsNew.Position - 4, fieldOffset);
                        }   

                        else
                        {
                            byte[] dataPositionBytes = BitConverter.GetBytes(dataWritePosition);

                            fsNew.Write(dataPositionBytes); // записываем позицию наших данных

                            long currentReadPosition = fsSource.Position; // запоминаем текущую позицию, чтобы вернуться

                            Array.Reverse(fieldOffset);
                            long fieldOffsetLong = BytesOperations.GetLong(fieldOffset);

                            fsSource.Seek(fieldOffsetLong, SeekOrigin.Begin);

                            readSize = lengthInBytes;
                            byte[] readData = new byte[readSize]; // считываем данные тега
                            bytesRead = 0;

                            while (readSize > 0)
                            {
                                int n = fsSource.Read(readData, bytesRead, readSize);
                                if (n == 0)
                                    break;

                                bytesRead += n;
                                readSize -= n;
                            }

                            CheckTags(tag, dataWritePosition, readData);

                            fieldsDataArray = fieldsDataArray.Concat(readData).ToArray();

                            dataWritePosition += lengthInBytes;  // обновляем позицию данных

                            fsSource.Seek(currentReadPosition, SeekOrigin.Begin);
                        }
                    }

                    readSize = 4;
                    byte[] nextIFDOffset = new byte[readSize];  
                    bytesRead = 0;

                    while (readSize > 0)
                    {
                        int n = fsSource.Read(nextIFDOffset, bytesRead, readSize);
                        if (n == 0)
                            break;

                        bytesRead += n;
                        readSize -= n;  
                    }

                    Array.Reverse(nextIFDOffset);
                    int nextIFDOffsetInt = BytesOperations.GetInt(nextIFDOffset);

                    if (nextIFDOffsetInt == 0)
                        isLastIFD = true;
                    else
                        nextIFDPosition = nextIFDOffsetInt;

                    byte[] lastIFD = { 0x00, 0x00, 0x00, 0x00 };
                    fsNew.Write(lastIFD);
                    fsNew.Write(fieldsDataArray);

                    return fsNew.Position;
                }
            }
        }


        internal void CopyImageData(long imageDataWritePosition, string pathNew)
        {
            using (FileStream fsSource = new(path, FileMode.Open, FileAccess.Read))
            {
                using (FileStream fsNew = new(pathNew, FileMode.Open, FileAccess.Write))
                {
                    if (!isJPEG)
                    {
                        fsNew.Seek(0, SeekOrigin.End); // записываем в конец файла

                        byte[] imageDataPositionBytes = Array.Empty<byte>();

                        for (int i = 0; i <= imageDataOffsets.Length - 4; i += 4)
                        {
                            byte[] dataOffset = imageDataOffsets[i..(i + 4)];
                            Array.Reverse(dataOffset);
                            int dataOffsetInt = BytesOperations.GetInt(dataOffset);
                            fsSource.Seek(dataOffsetInt, SeekOrigin.Begin);

                            byte[] newDataOffset = BitConverter.GetBytes((int)fsNew.Position);
                            imageDataPositionBytes = imageDataPositionBytes.Concat(newDataOffset).ToArray();

                            byte[] readSizeBytes = imageDataByteCounts[i..(i + 4)];
                            Array.Reverse(readSizeBytes);

                            int readSizeInt = BytesOperations.GetInt(readSizeBytes);

                            byte[] imageData = new byte[readSizeInt];
                            int bytesRead = 0;

                            while (readSizeInt > 0)
                            {
                                int n = fsSource.Read(imageData, bytesRead, readSizeInt);
                                if (n == 0)
                                    break;

                                bytesRead += n;
                                readSizeInt -= n;
                            }

                            fsNew.Write(imageData, 0, imageData.Length);
                        }

                        fsNew.Seek(imageDataTagPosition, SeekOrigin.Begin);
                        fsNew.Write(imageDataPositionBytes);
                    }

                    else if (isJPEG)
                    {
                        fsNew.Seek(imageDataTagPosition, SeekOrigin.Begin);

                        byte[] imageDataPositionBytes = BitConverter.GetBytes((int)imageDataWritePosition);
                        fsNew.Write(imageDataPositionBytes, 0, imageDataPositionBytes.Length);

                        fsNew.Seek(0, SeekOrigin.End);
                        long IFNewPosition = fsNew.Position;

                        Array.Reverse(JPEGIFOffset);
                        int JPEGIFOffsetInt = BytesOperations.GetInt(JPEGIFOffset);

                        Array.Reverse(JPEGIFByteCount);
                        int JPEGIFBytesCountInt = BytesOperations.GetInt(JPEGIFByteCount);

                        fsSource.Seek(JPEGIFOffsetInt, SeekOrigin.Begin);

                        int readSize = JPEGIFBytesCountInt;
                        byte[] readIF = new byte[readSize];
                        int bytesRead = 0;

                        while (readSize > 0)
                        {
                            int n = fsSource.Read(readIF, bytesRead, readSize);
                            if (n == 0)
                                break;

                            bytesRead += n;
                            readSize -= n;
                        }

                        fsNew.Write(readIF, 0, readIF.Length);

                        fsNew.Seek(JPEGIFTagPosition, SeekOrigin.Begin);

                        byte[] IFNewPositionBytes = BitConverter.GetBytes((int)IFNewPosition);
                        fsNew.Write(IFNewPositionBytes, 0, IFNewPositionBytes.Length);

                        // QTABLES

                        fsNew.Seek(0, SeekOrigin.End);

                        byte[] newJPEGQTablesOffsets = Array.Empty<byte>();

                        for (int i = 0; i <= JPEGQTablesOffsets.Length - 4; i += 4)
                        {
                            byte[] QTableOffset = JPEGQTablesOffsets[i..(i + 4)];

                            Array.Reverse(QTableOffset);
                            int QTableOffsetInt = BytesOperations.GetInt(QTableOffset);

                            fsSource.Seek(QTableOffsetInt, SeekOrigin.Begin);
                            
                            byte[] newQTableOffset = BitConverter.GetBytes((int)fsNew.Position);
                            newJPEGQTablesOffsets = newJPEGQTablesOffsets.Concat(newQTableOffset).ToArray();

                            readSize = 64; // размер каждой таблицы - 64 байта
                            byte[] QTable = new byte[readSize];
                            bytesRead = 0;
                            
                            while (readSize > 0)
                            {
                                int n = fsSource.Read(QTable, bytesRead, readSize);
                                if (n == 0)
                                    break;

                                bytesRead += n;
                                readSize -= n;
                            }

                            fsNew.Write(QTable, 0, QTable.Length);
                        }

                        fsNew.Seek(JPEGQTablesTagPosition, SeekOrigin.Begin);
                        fsNew.Write(newJPEGQTablesOffsets);

                        // DCTABLES

                        fsNew.Seek(0, SeekOrigin.End);

                        byte[] newJPEGDCTablesOffsets = Array.Empty<byte>();

                        for (int i = 0; i <= JPEGDCTablesOffsets.Length - 4; i += 4)
                        {
                            byte[] DCTableOffset = JPEGDCTablesOffsets[i..(i + 4)];

                            Array.Reverse(DCTableOffset);
                            int DCTableOffsetInt = BytesOperations.GetInt(DCTableOffset);

                            fsSource.Seek(DCTableOffsetInt, SeekOrigin.Begin);

                            byte[] newDCTableOffset = BitConverter.GetBytes((int)fsNew.Position);
                            newJPEGDCTablesOffsets = newJPEGDCTablesOffsets.Concat(newDCTableOffset).ToArray();

                            readSize = 33; // размер каждой таблицы - максимум 33 байта
                            byte[] DCTable = new byte[readSize];
                            bytesRead = 0;

                            while (readSize > 0)
                            {
                                int n = fsSource.Read(DCTable, bytesRead, readSize);
                                if (n == 0)
                                    break;

                                bytesRead += n;
                                readSize -= n;
                            }

                            fsNew.Write(DCTable, 0, DCTable.Length);
                        }

                        fsNew.Seek(JPEGDCTablesTagPosition, SeekOrigin.Begin);
                        fsNew.Write(newJPEGDCTablesOffsets);

                        // ACTABLES

                        fsNew.Seek(0, SeekOrigin.End);

                        byte[] newJPEGACTablesOffsets = Array.Empty<byte>();

                        for (int i = 0; i <= JPEGACTablesOffsets.Length - 4; i += 4)
                        {
                            byte[] ACTableOffset = JPEGACTablesOffsets[i..(i + 4)];

                            Array.Reverse(ACTableOffset);
                            int ACTableOffsetInt = BytesOperations.GetInt(ACTableOffset);

                            fsSource.Seek(ACTableOffsetInt, SeekOrigin.Begin);

                            byte[] newACTableOffset = BitConverter.GetBytes((int)fsNew.Position);
                            newJPEGACTablesOffsets = newJPEGACTablesOffsets.Concat(newACTableOffset).ToArray();

                            readSize = 272; // размер каждой таблицы - максимум 272 байта
                            byte[] ACTable = new byte[readSize];
                            bytesRead = 0;

                            while (readSize > 0)
                            {
                                int n = fsSource.Read(ACTable, bytesRead, readSize);
                                if (n == 0)
                                    break;

                                bytesRead += n;
                                readSize -= n;
                            }

                            fsNew.Write(ACTable, 0, ACTable.Length);
                        }

                        fsNew.Seek(JPEGACTablesTagPosition, SeekOrigin.Begin);
                        fsNew.Write(newJPEGACTablesOffsets);
                    }
                }
            }
        }


        public void Split(string pathNew)
        {
            nextIFDPosition = GetHeader();
            int imageCounter = 1;

            while (!isLastIFD) 
            {
                string path = pathNew + imageCounter + ".tif";
                long imageDataWritePosition = CopyMetaData(nextIFDPosition, path);
                CopyImageData(imageDataWritePosition, path);
                imageCounter++;
            }
        }
    }
}