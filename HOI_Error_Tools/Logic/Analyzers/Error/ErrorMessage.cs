using System;
using HOI_Error_Tools.Logic.Analyzers.Common;
using System.Collections.Generic;
using System.Linq;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public class ErrorMessage
{
    public ErrorCode Code { get; }
    public IEnumerable<ParameterFileInfo> FileInfo { get; }
    public string Message { get; }
    public ErrorLevel Level { get; }
    public ErrorType Type { get; }

    public ErrorMessage(ErrorCode code, IEnumerable<ParameterFileInfo> fileInfo, string message, ErrorLevel level)
    {
        Code = code;
        FileInfo = fileInfo;
        Message = message;
        Level = level;
        Type = GetErrorType(FileInfo);
    }

    private static ErrorType GetErrorType(IEnumerable<ParameterFileInfo> fileInfos)
    {
        var fileInfosList = fileInfos.ToList();
        

        if (fileInfosList.Count == 1)
        {
            return IsModification(fileInfosList[0]) ? ErrorType.Modification : ErrorType.Game;
        }

        var type = ErrorType.Unknown;
        foreach (var info in fileInfosList)
        {
            if (IsModification(info))
            {
                if (type == ErrorType.Game)
                {
                    return ErrorType.Mixing;
                }
                type = ErrorType.Modification;
            }
            else
            {
                if (type == ErrorType.Modification)
                {
                    return ErrorType.Mixing;
                }

                type = ErrorType.Game;
            }
        }

        return type;
    }

    private static bool IsModification(ParameterFileInfo info)
    {
        const string localModPath = @"Hearts of Iron IV\mod";
        const string steamModPath = @"steamapps\workshop";

        return info.FilePath.Contains(localModPath) ||
               info.FilePath.Contains(steamModPath);
    }
}