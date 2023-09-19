using System;

namespace HOI_Error_Tools.Logic.Messages;

public sealed class AppUpdateMessage
{
    public bool HasNewVersion { get; }
    public Uri NewVersionAppUrl { get; }

    public AppUpdateMessage(bool hasNewVersion, Uri newVersionAppUrl)
    {
        HasNewVersion = hasNewVersion;
        NewVersionAppUrl = newVersionAppUrl;
    }
}