using CheckPage.Services;

var loginUrl = Environment.GetEnvironmentVariable("MONITOR_LOGIN_PAGE_URL");
var targetUrl = Environment.GetEnvironmentVariable("MONITOR_TARGET_URL");
var username = Environment.GetEnvironmentVariable("MONITOR_USERNAME");
var password = Environment.GetEnvironmentVariable("MONITOR_PASSWORD");
var webhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL");
var searchText = Environment.GetEnvironmentVariable("MONITOR_SEARCH_TEXT") ?? "Vagas Esgotadas";
var timeout = int.TryParse(Environment.GetEnvironmentVariable("MONITOR_TIMEOUT"), out var t) ? t : 15;


var missing = new[] { loginUrl, targetUrl, username, password, webhookUrl }
    .Where(string.IsNullOrEmpty).ToList();

if (missing.Count > 0)
{
    Console.WriteLine("Variáveis de ambiente obrigatórias não definidas:");
    if (string.IsNullOrEmpty(loginUrl)) Console.WriteLine("  - MONITOR_LOGIN_PAGE_URL");
    if (string.IsNullOrEmpty(targetUrl)) Console.WriteLine("  - MONITOR_TARGET_URL");
    if (string.IsNullOrEmpty(username)) Console.WriteLine("  - MONITOR_USERNAME");
    if (string.IsNullOrEmpty(password)) Console.WriteLine("  - MONITOR_PASSWORD");
    if (string.IsNullOrEmpty(webhookUrl)) Console.WriteLine("  - DISCORD_WEBHOOK_URL");
    return;
}

Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] CheckPage iniciado.");
Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] URL alvo: {targetUrl}");
Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Buscando por: \"{searchText}\"");

var fetcher = new PageFetcherService(timeout);
var checker = new ContentCheckerService();
var notifier = new DiscordNotifierService();

var loggedIn = await fetcher.LoginAsync(loginUrl!, username!, password!);
if (!loggedIn)
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Login falhou. Encerrando.");
    return;
}

var html = await fetcher.FetchPageAsync(targetUrl!);
if (html is null)
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Não foi possível obter a página. Encerrando.");
    return;
}

var textFound = checker.IsTextPresent(html, searchText);

if (textFound)
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] \"{searchText}\" encontrado.");
}
else
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] \"{searchText}\" NÃO encontrado!");
    await notifier.SendNotificationAsync(
        webhookUrl!,
        $"ATENÇÃO: \"{searchText}\" não encontrado em {targetUrl}. Verifique a página!");
}
