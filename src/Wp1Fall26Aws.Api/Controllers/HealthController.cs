using Microsoft.AspNetCore.Mvc;

namespace Wp1Fall26Aws.Api.Controllers;

[ApiController]
[Route("api")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint for Elastic Beanstalk load balancer.
    /// Returns 200 OK with application status to indicate the app is healthy.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Wp1Fall26Aws.Api"
        });
    }
}
