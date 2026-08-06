using System.Net;
using System.Net.Http.Headers;

namespace CheckPage.Services;

public class PageFetcherService
{
    private readonly HttpClient _client;
    private readonly CookieContainer _cookies = new();

    public PageFetcherService(int timeoutSeconds)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookies,
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true
        };

        _client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        _client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
    }

    public async Task<bool> LoginAsync(string loginUrl, string username, string password)
    {
        try
        {
            var loginPageResponse = await _client.GetAsync(loginUrl);
            loginPageResponse.EnsureSuccessStatusCode();

            var loginPageHtml = await loginPageResponse.Content.ReadAsStringAsync();

            var formData = new Dictionary<string, string>
            {
                { "access-type", "cd_externo" },
                { "cd_externo", username },
                { "password", password }
            };

            var csrfToken = ExtractCsrfToken(loginPageHtml);
            if (csrfToken is not null)
                formData[csrfToken.Value.Name] = csrfToken.Value.Value;

            var postUrl = loginUrl.TrimEnd('/') + "/login";
            var content = new FormUrlEncodedContent(formData);

            var loginResponse = await _client.PostAsync(postUrl, content);
            var responseContent = await loginResponse.Content.ReadAsStringAsync();

            if (!loginResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] FALHA no login - HTTP {(int)loginResponse.StatusCode}: {responseContent[..Math.Min(200, responseContent.Length)]}");
                return false;
            }

            if (responseContent.Contains("login-form", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Login retornou HTTP 200 mas ainda contém o formulário. Credenciais inválidas?");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Trecho: {responseContent[..Math.Min(500, responseContent.Length)]}");
                return false;
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Login realizado com sucesso.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERRO no login: {ex.Message}");
            return false;
        }
    }

    public async Task<string?> FetchPageAsync(string url)
    {
        try
        {
            var response = await _client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] FALHA ao acessar {url} - HTTP {(int)response.StatusCode}");
                return null;
            }

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERRO ao acessar {url}: {ex.Message}");
            return null;
        }
    }

    private static (string Name, string Value)? ExtractCsrfToken(string html)
    {
        var patterns = new[] { "_token", "csrf_token", "csrf" };
        foreach (var pattern in patterns)
        {
            var idx = html.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;

            var valueStart = html.LastIndexOf("value=\"", idx, StringComparison.OrdinalIgnoreCase);
            if (valueStart < 0) continue;
            valueStart += 7;

            var valueEnd = html.IndexOf('"', valueStart);
            if (valueEnd < 0) continue;

            var nameStart = html.LastIndexOf("name=\"", idx, StringComparison.OrdinalIgnoreCase);
            if (nameStart < 0) continue;
            nameStart += 6;

            var nameEnd = html.IndexOf('"', nameStart);
            if (nameEnd < 0) continue;

            var name = html[nameStart..nameEnd];
            var value = html[valueStart..valueEnd];
            return (name, value);
        }

        return null;
    }
}
