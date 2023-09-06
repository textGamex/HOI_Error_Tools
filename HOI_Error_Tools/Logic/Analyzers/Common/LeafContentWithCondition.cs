namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class LeafContentWithCondition : LeafContent
{
    public Condition Condition { get; }
    
    public LeafContentWithCondition(LeafContent leafContent, Condition  condition) 
        : base(leafContent.Key, leafContent.Value, leafContent.Position)
    {
        Condition = condition;
    }
    
    public static LeafContentWithCondition Create(CWTools.Process.Leaf leaf, Condition condition)
    {
        return new LeafContentWithCondition(FromCWToolsLeaf(leaf), condition);
    }
}