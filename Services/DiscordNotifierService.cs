using System.Net.Http.Json;

namespace CheckPage.Services;

public class DiscordNotifierService
{
    private readonly HttpClient _client = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task<bool> SendNotificationAsync(string webhookUrl, string message)
    {
        try
        {
            var payload = new { content = message };
            var response = await _client.PostAsJsonAsync(webhookUrl, payload);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Notificação enviada ao Discord.");
                return true;
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] FALHA ao enviar Discord - HTTP {(int)response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            // Evita que a mensagem da exceção exponha a URL secreta do webhook.
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERRO ao enviar Discord ({ex.GetType().Name}).");
            return false;
        }
    }
}
