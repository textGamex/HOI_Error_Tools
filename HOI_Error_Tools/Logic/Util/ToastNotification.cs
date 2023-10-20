using Microsoft.Toolkit.Uwp.Notifications;

namespace HOI_Error_Tools.Logic.Util;

public static class ToastService
{
    public static void Push(string title, string? content = null)
    {
        var builder = new ToastContentBuilder().AddText(title);
        if (content is not null)
        {
            builder.AddText(content);
        }
        builder.Show();
    }
}