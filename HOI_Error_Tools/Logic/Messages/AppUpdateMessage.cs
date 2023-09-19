using System;

namespace HOI_Error_Tools.Logic.Messages;

public sealed class AppUpdateMessage
{
    public bool HasNewVersion { get; }
    public Uri NewVersionAppUrl { get; }
    public bool SilentCheck { get; }

    public AppUpdateMessage(bool hasNewVersion, Uri newVersionAppUrl, bool silentCheck)
    {
        HasNewVersion = hasNewVersion;
        NewVersionAppUrl = newVersionAppUrl;
        SilentCheck = silentCheck;
    }
}