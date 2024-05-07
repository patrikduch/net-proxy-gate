using Microsoft.AspNetCore.Mvc;

namespace NetProxyGate.Controllers;

[ApiController]
[Route("[controller]")]
public class ProxyController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public ProxyController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    // This route handles all types of HTTP methods dynamically
    [Route("{*url}", Order = int.MaxValue)]
    public async Task<IActionResult> ProxyAnyMethod([FromRoute] string url)
    {
        // Capture the actual HTTP method from the incoming request
        var method = Request.Method;

        // Modify the URL if necessary, for example, to add api versioning paths
        string externalApiUrl = $"https://api.shopwinner.org/{url}";

        try
        {
            // Create HttpRequestMessage with dynamic method
            var requestMessage = new HttpRequestMessage(new HttpMethod(method), externalApiUrl);

            // If the method allows for a body, add it from the incoming request
            if (Request.HasJsonContentType() && (method == HttpMethod.Post.Method || method == HttpMethod.Put.Method ||
                                                 method == HttpMethod.Patch.Method))
            {
                var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
                requestMessage.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");
            }

            // Forward the request to the external API
            var responseMessage = await _httpClient.SendAsync(requestMessage);

            // Relay the response back to the original caller
            if (responseMessage.IsSuccessStatusCode)
            {
                var content = await responseMessage.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            else
            {
                return StatusCode((int) responseMessage.StatusCode, await responseMessage.Content.ReadAsStringAsync());
            }
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, ex.Message); // Bad Gateway error
        }
    }

}