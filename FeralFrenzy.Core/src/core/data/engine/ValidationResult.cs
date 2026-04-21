using System.Collections.Generic;

namespace FeralFrenzy.Core.Data.Engine;

public record ValidationResult(
    bool IsValid,
    List<ValidationError> Errors);
