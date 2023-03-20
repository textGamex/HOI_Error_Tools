using System;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic.CustomException;

public class ParseException : Exception
{
    public ParseException(string message) : base(message)
    { }

    public ParseException()
    { }
}