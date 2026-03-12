using Ocelot.Infrastructure.Extensions;
using System.Runtime.CompilerServices;

namespace Ocelot.Testing;

/// <summary>
/// This is the base class for any unit testing classes.
/// It is recommended to always inherit from it.
/// </summary>
public class Unit
{
    protected readonly Guid _testId = Guid.NewGuid();
    protected string TestID { get => _testId.ToString("N"); }
    protected string TestName([CallerMemberName] string? testName = null)
        => testName.IfEmpty(TestID);

    protected virtual bool IsCiCd() => IsRunningInGitHubActions();
    protected static bool IsRunningInGitHubActions()
        => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
}
