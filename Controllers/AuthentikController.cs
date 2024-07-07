using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AuthentikProxy.Models;

namespace AuthentikProxy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthentikController(IHttpClientFactory httpClientFactory, IOptions<AuthentikSettings> options, ILogger<AuthentikController> logger) : ControllerBase
    {
        private readonly AuthentikSettings _authentikSettings = options.Value;

        [HttpPost("authorize")]
        public async Task<IActionResult> Authorize([FromBody] Dictionary<string, string> request)
        {
            try
            {
                request.Add("client_id", _authentikSettings.ClientId);
                var client = httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync($"{_authentikSettings.Url}/authorize", request);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError($"Authorization failed with status code {response.StatusCode} and message {await response.Content.ReadAsStringAsync()}");
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
                }

                return Ok(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException ex)
            {
                logger.LogError($"HttpRequestException: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpPost("token")]
        public async Task<IActionResult> Token([FromBody] Dictionary<string, string> request)
        {
            try
            {
                request.Add("client_id", _authentikSettings.ClientId);
                request.Add("client_secret", _authentikSettings.ClientSecret);
                var client = httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync($"{_authentikSettings.Url}/token", request);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError($"Token exchange failed with status code {response.StatusCode} and message {await response.Content.ReadAsStringAsync()}");
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
                }

                return Ok(await response.Content.ReadAsStringAsync());
            }
            catch (HttpRequestException ex)
            {
                logger.LogError($"HttpRequestException: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}
