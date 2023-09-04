using System;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class Condition
{
    public static Condition Empty { get; } = new(default);
    public DateOnly Date { get; }

    public Condition(DateOnly date)
    {
        Date = date;
    }
}