using Adex.Application.Health;
using Adex.Domain.Decisions;

namespace Adex.UnitTests;

public sealed class HealthReportTests
{
    [Fact]
    public void Is_healthy_when_every_dependency_answers()
    {
        HealthReport report = HealthReport.From(
        [
            new HealthCheckResult("postgres", HealthStatus.Healthy, Required: true),
            new HealthCheckResult("redis", HealthStatus.Healthy, Required: false),
        ]);

        Assert.Equal(HealthStatus.Healthy, report.Status);
    }

    [Fact]
    public void Degrades_rather_than_fails_when_only_an_optional_dependency_is_down()
    {
        // Draining instances because a cache is down turns a latency problem
        // into an availability incident (ADR-0004).
        HealthReport report = HealthReport.From(
        [
            new HealthCheckResult("postgres", HealthStatus.Healthy, Required: true),
            new HealthCheckResult("redis", HealthStatus.Unhealthy, Required: false),
        ]);

        Assert.Equal(HealthStatus.Degraded, report.Status);
    }

    [Fact]
    public void Is_unhealthy_when_a_required_dependency_is_down()
    {
        HealthReport report = HealthReport.From(
        [
            new HealthCheckResult("postgres", HealthStatus.Unhealthy, Required: true),
            new HealthCheckResult("redis", HealthStatus.Healthy, Required: false),
        ]);

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
    }

    [Fact]
    public void Reports_healthy_with_no_dependencies_at_all()
    {
        Assert.Equal(HealthStatus.Healthy, HealthReport.From([]).Status);
    }
}

public sealed class DecisionContextTests
{
    [Theory]
    [InlineData("device_class", true)]
    [InlineData("referrer_group", true)]
    [InlineData("session_ordinal", true)]
    [InlineData("Device-Class", false)]
    [InlineData("1leading_digit", false)]
    [InlineData("has space", false)]
    [InlineData("", false)]
    public void Accepts_only_lower_snake_case_keys(string key, bool expected) =>
        Assert.Equal(expected, DecisionContext.IsValidKey(key));

    [Fact]
    public void Rejects_more_keys_than_the_contract_allows()
    {
        var tooMany = Enumerable
            .Range(0, DecisionContext.MaxKeys + 1)
            .Select(i => new KeyValuePair<string, string?>($"key_{i}", "value"));

        Assert.Throws<ArgumentException>(() => DecisionContext.Create(tooMany));
    }

    [Fact]
    public void Rejects_a_value_longer_than_the_contract_allows()
    {
        var tooLong = new[]
        {
            new KeyValuePair<string, string?>("page_group", new string('x', DecisionContext.MaxValueLength + 1)),
        };

        Assert.Throws<ArgumentException>(() => DecisionContext.Create(tooLong));
    }

    [Fact]
    public void Keeps_null_values_which_mean_absent_not_invalid()
    {
        DecisionContext context = DecisionContext.Create(
        [
            new KeyValuePair<string, string?>("referrer_group", null),
        ]);

        Assert.True(context.ContainsKey("referrer_group"));
        Assert.Null(context["referrer_group"]);
    }

    [Fact]
    public void Is_immutable_once_created()
    {
        var source = new Dictionary<string, string?>(StringComparer.Ordinal) { ["device_class"] = "mobile" };
        DecisionContext context = DecisionContext.Create(source);

        source["device_class"] = "desktop";

        Assert.Equal("mobile", context["device_class"]);
    }
}
