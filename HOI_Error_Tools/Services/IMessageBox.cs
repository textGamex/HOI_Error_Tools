namespace HOI_Error_Tools.Services;

public interface IMessageBox
{
    void Show(string message, string? caption = null);
}