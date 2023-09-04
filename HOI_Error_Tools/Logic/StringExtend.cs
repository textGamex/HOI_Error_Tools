﻿using System;
using System.Text;

namespace HOI_Error_Tools.Logic;

public static class StringExtend
{
    public static string ToFilePath(this string filePath)
    {
        var builder = new StringBuilder("file:///", filePath.Length + 16);
        foreach (var c in filePath)
        {
            switch (c)
            {
                case '\\':
                    builder.Append('/');
                    break;
                case ' ':
                    builder.Append("%20");
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }
        return builder.ToString();
    }
}