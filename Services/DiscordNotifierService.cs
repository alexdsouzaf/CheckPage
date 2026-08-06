using System.Net.Http.Json;

namespace CheckPage.Services;

public class DiscordNotifierService
{
    private readonly HttpClient _client = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task SendNotificationAsync(string webhookUrl, string message)
    {
        try
        {
            var payload = new { content = message };
            var response = await _client.PostAsJsonAsync(webhookUrl, payload);

            if (response.IsSuccessStatusCode)
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Notificação enviada ao Discord.");
            else
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] FALHA ao enviar Discord - HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERRO ao enviar Discord: {ex.Message}");
        }
    }
}
