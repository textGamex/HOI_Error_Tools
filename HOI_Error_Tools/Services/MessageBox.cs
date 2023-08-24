namespace HOI_Error_Tools.Services;

public class MessageBox : IMessageBox
{
    public void Show(string message, string? caption = null)
    {
        System.Windows.MessageBox.Show(message, caption);
    }
}