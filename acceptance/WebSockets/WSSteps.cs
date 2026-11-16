using Ocelot.Infrastructure.Extensions;
using Ocelot.Testing.Steps;

namespace Ocelot.Acceptance.WebSockets;

public class WSSteps : WebSocketsSteps
{
    public override CancellationToken CancelMe => Xunit.TestContext.Current.CancellationToken; // TODO Move to WebSocketsSteps if testing project has Xunit.v3 reference

    public void SkipWhenCiCd(string message = null) => Assert.SkipWhen(IsCiCd(), message.IfEmpty(Skipped_In_CI_CD));

    public override void SkipWhen(Func<bool> when = null, string message = null)
    {
        when ??= IsCiCd;
        Assert.SkipWhen(when(), message.IfEmpty(Skipped_In_CI_CD));
    }
}
