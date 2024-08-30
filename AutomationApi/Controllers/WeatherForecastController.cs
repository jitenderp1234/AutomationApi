using Microsoft.AspNetCore.Mvc;
using CliWrap;
using CliWrap.Buffered;

namespace AutomationApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
        [HttpPost("deploy")]
        public async Task<IActionResult> Deploy([FromBody] HelmDeploymentRequest request)
        {
            // Helm command with the replica count
            //string helmCommand = $"helm install {request.ReleaseName} {request.ChartName} --set replicaCount={request.ReplicaCount}";
            string helmCommand = "kubectl apply -f frontend.yaml";
            // Execute Helm command inside the pod
            var result = await Cli.Wrap("sh")
                .WithArguments(new[] { "-c", helmCommand })
                .ExecuteBufferedAsync();

            if (result.ExitCode == 0)
            {
                return Ok("Deployment successful");
            }
            else
            {
                return StatusCode(500, $"Error: {result.StandardError}");
            }
        }
    }
    public class HelmDeploymentRequest
    {
        public string ReleaseName { get; set; }
        public string ChartName { get; set; }
        public int ReplicaCount { get; set; }
    }
}
