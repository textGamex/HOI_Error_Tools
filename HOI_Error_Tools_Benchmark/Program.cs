using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;

namespace HOI_Error_Tools_Benchmark;

[MemoryDiagnoser(false)]
public class BenchmarkTest
{
    private Node _node;
    private Node _node1;

    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<BenchmarkTest>();
    }

    [GlobalSetup]
    public void Setup()
    {
        _node = ParseHelper.ParseFileToNode(new List<ErrorMessage>(), "D:\\STEAM\\steamapps\\common\\Hearts of Iron IV\\history\\countries\\GRE - Greece.txt") ?? throw new Exception();
        _node1 = ParseHelper.ParseFileToNode(new List<ErrorMessage>(), "D:\\STEAM\\steamapps\\common\\Hearts of Iron IV\\history\\countries\\GRE - Greece.txt") ?? throw new Exception();
    }

    [Benchmark(Baseline = true)]
    public object MultipleUes()
    {
        var list = new List<LeafContent>(8);
        list.AddRange(ParseHelper.GetLeafContentsInAllChildren(_node, "oob"));
        list.AddRange(ParseHelper.GetLeafContentsInAllChildren(_node, "set_oob"));
        list.AddRange(ParseHelper.GetLeafContentsInAllChildren(_node, "set_naval_oob"));
        list.AddRange(ParseHelper.GetLeafContentsInAllChildren(_node, "set_air_oob"));
        return list;
    }



    [Benchmark]
    public object SingleUse()
    {
        var keywords = new HashSet<string>(4)
        {
            "oob",
            "set_oob",
            "set_naval_oob",
            "set_air_oob"
        };
        return ParseHelper.GetLeafContentsInAllChildren(_node1, keywords).ToList();
    }
}
