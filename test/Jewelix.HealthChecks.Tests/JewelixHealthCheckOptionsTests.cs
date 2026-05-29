namespace Jewelix.HealthChecks.Tests;

public class JewelixHealthCheckOptionsTests
{
    // ── Constants ─────────────────────────────────────────────────────────

    [Fact]
    public void LivenessPath_ConstantValue_IsSlashHealthzLive()
        => JewelixHealthCheckOptions.LivenessPath.ShouldBe("/healthz/live");

    [Fact]
    public void ReadinessPath_ConstantValue_IsSlashHealthzReady()
        => JewelixHealthCheckOptions.ReadinessPath.ShouldBe("/healthz/ready");

    [Fact]
    public void UiFeedPath_ConstantValue_IsSlashHealthzUiFeed()
        => JewelixHealthCheckOptions.UiFeedPath.ShouldBe("/healthz/ui-feed");

    [Fact]
    public void UiPath_ConstantValue_IsSlashHealthchecksUi()
        => JewelixHealthCheckOptions.UiPath.ShouldBe("/healthchecks-ui");

    [Fact]
    public void AdminPolicyName_ConstantValue_IsAdmin()
        => JewelixHealthCheckOptions.AdminPolicyName.ShouldBe("Admin");

    [Fact]
    public void ConnectionCheckName_ConstantValue_IsSqlServer()
        => JewelixHealthCheckOptions.ConnectionCheckName.ShouldBe("sql-server");

    // ── Settable defaults ─────────────────────────────────────────────────

    [Fact]
    public void DefaultOptions_ConnectionString_IsNull()
        => new JewelixHealthCheckOptions().ConnectionString.ShouldBeNull();

    [Fact]
    public void DefaultOptions_Services_IsEmptyList()
        => new JewelixHealthCheckOptions().Services.ShouldBeEmpty();

    // ── JewelixServiceCheck defaults ──────────────────────────────────────

    [Fact]
    public void ServiceCheck_DefaultName_IsEmptyString()
        => new JewelixServiceCheck().Name.ShouldBe(string.Empty);

    [Fact]
    public void ServiceCheck_DefaultUri_IsEmptyString()
        => new JewelixServiceCheck().Uri.ShouldBe(string.Empty);
}
