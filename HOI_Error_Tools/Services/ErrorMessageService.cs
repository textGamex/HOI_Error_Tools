using System;
using System.Collections.Generic;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Services;

public class ErrorMessageService : IErrorMessageService
{
    private IReadOnlyList<ErrorMessage>? _errorMessages;
    public IReadOnlyList<ErrorMessage> GetErrorMessages()
    {
        return _errorMessages ?? throw new InvalidOperationException($"{nameof(_errorMessages)} is null");
    }

    public void SetErrorMessages(IReadOnlyList<ErrorMessage> errorMessages)
    {
        _errorMessages = errorMessages;
    }

    public void Clear()
    {
        _errorMessages = null;
    }
}