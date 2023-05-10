# TiffLib
## :mortar_board: About 
TiffLib is a .NET library for operating multipage TIFFs without decoding/encoding. It allows you
* to __split__ multipage TIFF file into several one-page TIFF
* to __merge__ several one-page or multipage TIFFs into one multipage

I couldn't find any free library that could handle OJPEG in TIFF files. So I wrote this one and it works almost perfectly (check out _"Your help"_ section). This library doesn't care what compression is used in the file: it doesn't change the compression or the final size of the file, just copies all the data with regard to the offsets.

## :computer: Starting
The library is written using .NET 6.0. It doesn't require any third-party libraries or packages.
Here is the code example for you:
``` csharp
using TiffLib;


// Splits multipage TIFF
TiffFile tiffFile = new("D:/example/test.tif"); // location of the source file
tiffFile.Split("D:/result/result"); // path to the splitted files (names will be result1.tif, result2.tif...)


// Merges all the files in the directory into the one multipage file
TiffFilesList tiffFiles = new();
tiffFiles.MergeDirectory("D:/result", "D:/example/merged1.tif");


// Merges files from the list
string[] files = { "D:/result/result1.tif", "D:/result/result2.tif" };
tiffFiles.Merge(files, "D:/example/merged2.tif");
```

## :heart: Your help
Now TiffLib works only with files with little-endian (intel) bytes order. Soon I will fix this.

Also I plan to make a NuGet package from this library.

So if you find any other limitations of the lib or bugs or other problems, feel free to write me and I will do my best to make it better.
