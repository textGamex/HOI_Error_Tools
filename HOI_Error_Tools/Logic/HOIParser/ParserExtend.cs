using CWTools.Process;

namespace HOI_Error_Tools.Logic.HOIParser;

public static class ParserExtend
{
    public static bool HasNot(this Node node, string key)
    {
        return !node.Has(key);
    }
}