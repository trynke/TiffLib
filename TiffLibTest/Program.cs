/* Библиотека будет дополняться.
 * Последнюю версию можно найти здесь:
 * https://github.com/trynke/TiffLib */

using TiffLib;


// Делим многостраничный tiff
TiffFile tiffFile = new("C:/Users/BachininaEO/Tiff/Doc1472.tif"); // расположение изначального файла
tiffFile.Split("C:/Users/BachininaEO/result/result"); // путь к разделённым файлам (названия будут result1.tif, result2.tif...)


// Соединяем все файлы в папке в один многостраничный
TiffFilesList tiffFiles = new();
tiffFiles.MergeDirectory("C:/Users/BachininaEO/result", "C:/Users/BachininaEO/merged1.tif");


// Соединяем файлы из списка
string[] files = { "C:/Users/BachininaEO/result/result1.tif", "C:/Users/BachininaEO/result/result2.tif" };
tiffFiles.Merge(files, "C:/Users/BachininaEO/merged2.tif");