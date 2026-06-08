using CivicLedger.Application;
using CivicLedger.Domain;

namespace CivicLedger.Tests;

public sealed class RiskAssessmentServiceTests
{
    private readonly RiskAssessmentService _service = new();

    [Fact]
    public void Assess_ReturnsHighRisk_WhenSpendingOutpacesTimeline()
    {
        var grant = CreateActiveGrant();
        grant.AddExpense(
            91_000m,
            "Equipment",
            "Single Vendor",
            new DateOnly(2026, 2, 1));

        var result = _service.Assess(grant, new DateOnly(2026, 3, 1));

        Assert.Equal("High", result.Level);
        Assert.True(result.Score >= 60);
        Assert.Contains(result.Reasons, reason => reason.Contains("90%"));
    }

    [Fact]
    public void Assess_ReturnsLowRisk_ForBalancedGrant()
    {
        var grant = CreateActiveGrant();
        grant.AddExpense(
            20_000m,
            "Services",
            "Regional Partner",
            new DateOnly(2026, 5, 1));

        var result = _service.Assess(grant, new DateOnly(2026, 6, 1));

        Assert.Equal("Low", result.Level);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void Assess_FlagsUnspentFundsNearDeadline()
    {
        var grant = CreateActiveGrant();

        var result = _service.Assess(grant, new DateOnly(2026, 12, 15));

        Assert.Equal("Medium", result.Level);
        Assert.Contains(result.Reasons, reason => reason.Contains("30 days"));
    }

    private static Grant CreateActiveGrant()
    {
        var grant = new Grant(
            "Community Resilience",
            "Emergency Management",
            "Federal Resilience Fund",
            100_000m,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31));
        grant.Activate();
        return grant;
    }
}
