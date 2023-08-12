﻿using Jot.Storage;
using Jot;
using System;
using System.IO;
using System.Windows;

namespace HOI_Error_Tools;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static readonly Tracker Tracker = new(new JsonFileStore(Path.Combine(Environment.CurrentDirectory, "Settings")));
}