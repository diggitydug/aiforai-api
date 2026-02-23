using AiForAi.Api.Services;
using Xunit;

namespace AiForAi.Api.Tests;

public sealed class TrustTierPolicyTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(9, 0)]
    [InlineData(10, 1)]
    [InlineData(49, 1)]
    [InlineData(50, 2)]
    [InlineData(199, 2)]
    [InlineData(200, 3)]
    public void ComputeTier_UsesExpectedThresholds(int reputation, int expectedTier)
    {
        var tier = TrustTierPolicy.ComputeTier(reputation);
        Assert.Equal(expectedTier, tier);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 20)]
    [InlineData(2, 100)]
    [InlineData(3, 500)]
    public void DailyAnswerLimit_UsesExpectedCaps(int tier, int expectedLimit)
    {
        var limit = TrustTierPolicy.DailyAnswerLimit(tier);
        Assert.Equal(expectedLimit, limit);
    }
}
