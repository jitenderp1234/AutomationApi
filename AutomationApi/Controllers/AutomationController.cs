using Microsoft.AspNetCore.Mvc;
using CliWrap;
using CliWrap.Buffered;
using System.Text.RegularExpressions;
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AutomationApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutomationController : ControllerBase
    {
        // Helper method to extract IP addresses from the output
        private static string[] ExtractIPAddresses(string nslookupOutput)
        {
            var regex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            var matches = regex.Matches(nslookupOutput);

            return matches.Select(m => m.Value).ToArray();
        }

        [HttpGet("GetBackendAPI")]
        public async Task<IActionResult> GetBackendAPI([FromBody] BackendAPiRequest request)
        {

            String endpoint = $"http://{request.IPAddress.Replace('.', '-')}.default.pod.cluster.local:80";
            var client = new HttpClient() { BaseAddress = new Uri(endpoint) };
            var content = new StringContent("yourJsonString", Encoding.UTF8, "application/json");
            var response = await client.GetAsync("/weatherforecast");
            return Ok(response.Content.ReadAsStringAsync());

        }

            [HttpGet("GetServices")]
        public async Task<IActionResult> GetServices([FromBody] HelmDeploymentRequest request)
        {
            string nslookupCommand = $"get pods -l=app={request.ReleaseName} -o wide";
            var command = Cli.Wrap("kubectl")
                            .WithArguments(nslookupCommand)
                            .WithValidation(CommandResultValidation.None);

            // Execute the command and capture the output
            var result = await command.ExecuteBufferedAsync();

            if (result.ExitCode == 0)
            {
                // Extract IP addresses using a regex
                var ipAddresses = ExtractIPAddresses(result.StandardOutput);

                return Ok(new
                {
                    Success = true,
                    IPAddresses = ipAddresses
                });
            }
            else
            {
                return BadRequest(new
                {
                    Success = false,
                    Error = result.StandardError
                });
            }
        }
    

          [HttpPost("DeployUnInstall")]
        public async Task<IActionResult> DeployUnInstall([FromBody] HelmDeploymentRequest request)
        {
            string helmCommand = $"helm uninstall {request.ReleaseName} ";
            //string helmCommand = "kubectl apply -f frontend.yaml";
            // Execute Helm command inside the pod
            var result = await Cli.Wrap("sh")
                .WithArguments(new[] { "-c", helmCommand })
                .ExecuteBufferedAsync();

            if (result.ExitCode == 0)
            {
                return Ok("Deployment uninstall successful");
            }
            else
            {
                return StatusCode(500, $"Error: {result.StandardError}");
            }
        }
        [HttpPost("deploy")]
        public async Task<IActionResult> Deploy([FromBody] HelmDeploymentRequest request)
        {
            String helmlistCount = "kubectl get deployments --no-headers | wc -l";
            var helmlistcountoutput = await Cli.Wrap("sh")
            .WithArguments(new[] { "-c", helmlistCount })
            .ExecuteBufferedAsync();
            if (Convert.ToInt32(helmlistcountoutput.StandardOutput) > 3)
            {
                return BadRequest("Maximum number release already deployed");


            }
            string helmlistCommand = "helm list --all";

            var helmlistoutput = await Cli.Wrap("sh")
                .WithArguments(new[] { "-c", helmlistCommand })
                .ExecuteBufferedAsync();
            if (helmlistoutput.StandardOutput.Contains(request.ReleaseName))
            { 
                return BadRequest($"Release '{request.ReleaseName} already exists");


            }
            // Helm command with the replica count
            string helmCommand = $"helm install {request.ReleaseName} {request.ChartName} --set replicaCount={request.ReplicaCount},servicePort={request.ServicePort},releaseName={request.ReleaseName}";
            //string helmCommand = "kubectl apply -f frontend.yaml";
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
    public class BackendAPiRequest
    {
        public string IPAddress { get; set; }
    }
        public class HelmDeploymentRequest
    {
        public string ReleaseName { get; set; }
        public string ChartName { get; set; }
        public int ReplicaCount { get; set; }
        public int ServicePort { get; set; }
        
    }
}
