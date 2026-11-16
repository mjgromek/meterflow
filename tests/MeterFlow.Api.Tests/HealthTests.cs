using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MeterFlow.Api.Tests;

public class HealthTests(MeterFlowFactory factory) : IClassFixture<MeterFlowFactory>
{
    [Fact]
    public async Task Health_returns_200()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
