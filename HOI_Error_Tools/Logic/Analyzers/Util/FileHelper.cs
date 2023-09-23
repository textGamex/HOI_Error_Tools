using System.IO;
using System.Linq;

namespace HOI_Error_Tools.Logic.Analyzers.Util;

public static class FileHelper
{
    public static long GetFilesSizeInBytes(string folderPath)
    {
        var filesPath = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
        return filesPath.Select(filePath => new FileInfo(filePath)).Sum(info => info.Length);
    }
}