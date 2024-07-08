using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AuthentikProxy.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace AuthentikProxy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthentikController(IHttpClientFactory httpClientFactory, IOptions<AuthentikSettings> options, ILogger<AuthentikController> logger) : ControllerBase
    {
        private readonly AuthentikSettings _authentikSettings = options.Value;

        [HttpGet("authorize")]
        public IActionResult Authorize([FromQuery] Dictionary<string, string> request)
        {
            try
            {
                request.TryGetValue("redirect_uri", out string? redirect_uri);
                // Create state parameter with client_id for lookup in /callback
                var state = new
                {
                    redirect_uri,
                };
                var stateJson = JsonSerializer.Serialize(state);
                var stateEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(stateJson));

                request["state"] = stateEncoded;
                request["redirect_uri"] = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/Authentik/callback";

                var queryString = string.Join("&", request.Select(kvp => $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));
                var authUrl = $"{_authentikSettings.Url}/application/o/authorize/?{queryString}";

                return Redirect(authUrl);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError("HttpRequestException: {msg}", ex.Message);
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpPost("token")]
        public async Task<IActionResult> Token([FromForm] Dictionary<string, string> request)
        {
            try
            {
                request.TryGetValue("client_id", out string? clientId);

                var client = _authentikSettings.Clients.FirstOrDefault(c => c.ClientId == clientId);

                if (client == null)
                {
                    return BadRequest($"Invalid 'client_id'.");
                }

                request["client_secret"] = client.ClientSecret;
                request["redirect_uri"] = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/Authentik/callback";

                using var content = new FormUrlEncodedContent(request);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                var httpClient = httpClientFactory.CreateClient();
                var response = await httpClient.PostAsync($"{_authentikSettings.Url}/application/o/token/", content);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Token exchange failed with status code {statusCode} and message {resp}", response.StatusCode, await response.Content.ReadAsStringAsync());
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                return Content(jsonResponse, "application/json");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError("HttpRequestException: {msg}", ex.Message);
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpGet("userinfo")]
        public async Task<IActionResult> UserInfo()
        {
            try
            {
                var authorizationHeader = HttpContext.Request.Headers.Authorization.ToString();
                var bearerToken = authorizationHeader["Bearer ".Length..];
                var httpClient = httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var response = await httpClient.GetAsync($"{_authentikSettings.Url}/application/o/userinfo/");

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("UserInfo call failed with status code {statusCode} and message {resp}", response.StatusCode, await response.Content.ReadAsStringAsync());
                    return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                return Content(jsonResponse, "application/json");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError("HttpRequestException: {msg}", ex.Message);
                return StatusCode(500, "Internal Server Error");
            }
        }


        [HttpGet("callback")]
        public IActionResult Callback([FromQuery] Dictionary<string, string> request)
        {
            try
            {
                request.TryGetValue("state", out string? stateEncoded);

                if (stateEncoded != null)
                {
                    var stateJson = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(stateEncoded));
                    var state = JsonSerializer.Deserialize<Dictionary<string, string>>(stateJson);

                    if (state != null && state.TryGetValue("redirect_uri", out string? redirect_uri))
                    {
                        var httpClient = httpClientFactory.CreateClient();

                        var queryString = string.Join("&", request.Select(kvp => $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));
                        var callbackUrl = $"{redirect_uri}?{queryString}";

                        return Redirect(callbackUrl);
                    }
                }

                return BadRequest("Invalid state parameter.");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError("HttpRequestException: {msg}", ex.Message);
                return StatusCode(500, "Internal Server Error");
            }
        }

    }
}
