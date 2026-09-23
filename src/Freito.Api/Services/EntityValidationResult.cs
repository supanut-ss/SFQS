namespace Freito.Api.Services;

public sealed record EntityValidationResult(IReadOnlyList<string> Errors, bool IsConflict = false)
{
    public bool IsValid => Errors.Count == 0;
}
