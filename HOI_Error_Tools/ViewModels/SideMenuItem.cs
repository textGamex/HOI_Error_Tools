using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace HOI_Error_Tools.ViewModels;

public class SideMenuItem
{
    public SideMenuItem(string title, Type? screenType = null, string? icon = null, IList<SideMenuItem>? items = null)
    {
        Title = title;
        Icon = icon;
        ScreenType = screenType;
        Items = items;
    }

    public string Title { get; }

    public Type? ScreenType { get; }
    public string? Icon { get; }
    public IList<SideMenuItem>? Items { get; set; }
}