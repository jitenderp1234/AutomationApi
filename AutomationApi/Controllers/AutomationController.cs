using Microsoft.AspNetCore.Mvc;
using CliWrap;
using CliWrap.Buffered;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AutomationApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AutomationController : ControllerBase
    {
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
            String helmlistCount = "helm list --all | grep -c 'default'";
                var helmlistcountoutput = await Cli.Wrap("sh")
                .WithArguments(new[] { "-c", helmlistCount })
                .ExecuteBufferedAsync();
            if (Convert.ToInt32(helmlistcountoutput.StandardOutput)>2)
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
    public class HelmDeploymentRequest
    {
        public string ReleaseName { get; set; }
        public string ChartName { get; set; }
        public int ReplicaCount { get; set; }
        public int ServicePort { get; set; }
        
    }
}
