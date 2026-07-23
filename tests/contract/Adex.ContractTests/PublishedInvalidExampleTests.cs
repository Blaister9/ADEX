using System.Net;
using System.Text;

namespace Adex.ContractTests;

public sealed class PublishedInvalidExampleTests(AdexApiFactory factory) : IClassFixture<AdexApiFactory>
{
    public static IEnumerable<object[]> RequestExamples()
    {
        string directory = FindInvalidExampleDirectory();
        return Directory.EnumerateFiles(directory, "*-request.*.json")
            .Order(StringComparer.Ordinal)
            .Select(path => new object[] { path });
    }

    [Theory]
    [MemberData(nameof(RequestExamples))]
    public async Task Every_published_invalid_request_example_is_rejected_by_the_running_api(string path)
    {
        string endpoint = Path.GetFileName(path).StartsWith("decision-request", StringComparison.Ordinal)
            ? "/v1/decisions"
            : "/v1/events";
        using var content = new StringContent(await File.ReadAllTextAsync(path), Encoding.UTF8, "application/json");

        HttpResponseMessage response = await factory.CreateClientFor(AdexApiFactory.TenantAKey)
            .PostAsync(endpoint, content);

        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"{Path.GetFileName(path)} returned {(int)response.StatusCode}.");
    }

    private static string FindInvalidExampleDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName, "packages", "contracts", "examples", "invalid");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find packages/contracts/examples/invalid.");
    }
}
