using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnumsNET;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Analyzers.Error;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace HOI_Error_Tools.ViewModels;

public partial class CommonErrorMessageSettingsControlViewModel : ObservableObject
{
    [ObservableProperty]
    private List<ErrorTypeVo> _errorTypes;
    private readonly GlobalSettings _settings;
    private static readonly ILogger Log = App.Current.Services.GetRequiredService<ILogger>();

    public CommonErrorMessageSettingsControlViewModel(GlobalSettings settings)
    {
        _settings = settings;
        _errorTypes = GetErrorTypes();
    }

    private List<ErrorTypeVo> GetErrorTypes()
    {
        return Enums.GetValues<ErrorType>().Select(type => 
            new ErrorTypeVo(type, _settings.InhibitedErrorTypes.Contains(type))).ToList();
    }

    [RelayCommand]
    private void ClickErrorTypeCheckBox(ErrorType type)
    {
        Log.Debug(type);
        if (_settings.InhibitedErrorTypes.Contains(type))
        {
            _settings.InhibitedErrorTypes.Remove(type);
        }
        else
        {
            _settings.InhibitedErrorTypes.Add(type);
        }
    }

    public class ErrorTypeVo
    {
        public ErrorType Type { get; set; }
        public bool IsSelected { get; set; }

        public ErrorTypeVo(ErrorType type, bool isSelected)
        {
            Type = type;
            IsSelected = isSelected;
        }
    }
}