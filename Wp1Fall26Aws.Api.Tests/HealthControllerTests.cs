using Microsoft.AspNetCore.Mvc;
using Wp1Fall26Aws.Api.Controllers;

namespace Wp1Fall26Aws.Api.Tests;

/// <summary>
/// Unit tests for the health check endpoint.
/// </summary>
public class HealthControllerTests
{
    /// <summary>
    /// Test that the health check endpoint returns 200 OK with proper response structure.
    /// </summary>
    [Fact]
    public void HealthCheck_ReturnsOkWithStatusHealthy()
    {
        // Arrange
        var controller = new HealthController();

        // Act
        var result = controller.HealthCheck();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        Assert.NotNull(okResult.Value);

        // Verify response structure
        var valueObj = okResult.Value!.GetType();
        var statusProp = valueObj.GetProperty("status");
        var timestampProp = valueObj.GetProperty("timestamp");
        var serviceProp = valueObj.GetProperty("service");

        Assert.NotNull(statusProp);
        Assert.NotNull(timestampProp);
        Assert.NotNull(serviceProp);

        Assert.Equal("broken", statusProp!.GetValue(okResult.Value));
        Assert.Equal("Wp1Fall26Aws.Api", serviceProp!.GetValue(okResult.Value));
        Assert.IsType<DateTime>(timestampProp!.GetValue(okResult.Value));
    }
}
