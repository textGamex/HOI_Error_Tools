using System.Windows;

namespace HOI_Error_Tools.Services;

public class MessageBox : IMessageBox
{
    public void Show(string message, string? caption = null)
    {
        System.Windows.MessageBox.Show(message, caption);
    }

    public void ErrorTip(string message, string? caption = null)
    {
        System.Windows.MessageBox.Show(message, caption ?? "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}