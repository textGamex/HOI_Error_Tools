using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.ViewModels;

public partial class ErrorMessageWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private IImmutableList<ErrorMessage> _errorMessage;

    public ErrorMessageWindowViewModel(IImmutableList<ErrorMessage> errors)
    {
        _errorMessage = errors;
    }
}