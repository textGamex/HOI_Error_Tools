using System;
using System.IO;
using System.Text;
using CWTools.CSharp;
using CWTools.Process;

namespace HOI_Error_Tools.Logic.HOIParser;

public class Parser
{
    public string FilePath { get; }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    private readonly ParserError? _error;

    private readonly Node? _node;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath"></param>
    /// <exception cref="FileNotFoundException">如果文件不存在</exception>
    public Parser(string filePath)
    {
        FilePath = File.Exists(filePath) ? filePath : throw new FileNotFoundException($"找不到文件: {filePath}" , filePath);
        var fileName = Path.GetFileName(filePath);
        var result = Parsers.ParseScriptFile(fileName, File.ReadAllText(filePath));
        IsSuccess = result.IsSuccess;
        if (IsFailure)
        {
            _error = result.GetError();
            return;
        }
        _node = Parsers.ProcessStatements(fileName, filePath, result.GetResult());
    }

    static Parser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public Node GetResult()
    {
        return _node ?? throw new InvalidOperationException($"文件解析失败, 无法返回解析结果, 文件路径: {FilePath}.");
    }

    public ParserError GetError()
    {
        return _error ?? throw new InvalidOperationException();
    }
}