using CWTools.Process;

namespace HOI_Error_Tools.Logic.HOIParser;

public static class ParserExtend
{
    private static readonly object _locker = new();
    public static bool HasNot(this Node node, string key)
    {
        lock (_locker)
        {
            return !node.Has(key);
        }       
    }
}