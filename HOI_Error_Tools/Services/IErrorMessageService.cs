using System.Collections.Generic;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Services;

public interface IErrorMessageService
{
    IReadOnlyList<ErrorMessage> GetErrorMessages();
    void SetErrorMessages(IReadOnlyList<ErrorMessage> errorMessages);
    void Clear();
}