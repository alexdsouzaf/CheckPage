# AGENTS.md

Instruções para o agente responsável por implementar e manter este projeto.

## Visão geral do projeto

Aplicação console em C# (.NET) que:

1. Acessa uma página web (URL configurável).
2. Baixa o HTML da página.
3. Verifica se uma informação específica (texto, padrão ou seletor) está presente no HTML.
4. Se a informação **não** for encontrada, envia uma notificação via webhook do Discord.
5. Se a informação for encontrada, apenas loga o resultado (ou finaliza silenciosamente, conforme configuração).

O objetivo é rodar periodicamente (via agendador do SO, cron, Task Scheduler, ou loop interno com `Timer`) para monitorar mudanças/disponibilidade de conteúdo em uma página.

## Stack e requisitos técnicos

- Linguagem: C#
- Runtime: .NET 8 (LTS) — usar `dotnet new console` como base do projeto.
- Tipo de projeto: Console App.
- Dependências permitidas (adicionar via NuGet apenas se necessário):
  - `HttpClient` nativo (`System.Net.Http`) para requisições HTTP — preferir a nativa em vez de libs externas.
  - `HtmlAgilityPack` para parsing de HTML (se a checagem exigir seletor/DOM em vez de simples `Contains`).
  - `Microsoft.Extensions.Configuration` (+ `.Json` e `.EnvironmentVariables`) para carregar configurações de `appsettings.json` e variáveis de ambiente.
  - `Microsoft.Extensions.Logging` (ou similar) para logs estruturados no console.
- Não introduzir frameworks pesados (ASP.NET, Blazor, etc.) — o programa deve continuar sendo um console app simples e leve.

## Estrutura de pastas esperada

```
/src
  Program.cs                 # entry point, orquestra o fluxo
  /Services
    PageFetcherService.cs    # responsável por baixar o HTML
    ContentCheckerService.cs # responsável por checar a presença da informação
    DiscordNotifierService.cs# responsável por enviar o webhook
  /Models
    AppSettings.cs           # modelo de configuração tipada
  /Config
    appsettings.json         # URL alvo, texto/padrão a buscar, webhook URL, intervalo, etc.
appsettings.example.json     # exemplo sem segredos, para versionar
.gitignore                   # deve ignorar appsettings.json real com segredos
AGENTS.md
README.md
```

Ajustar nomes/organização se o projeto já existir com outra convenção — **manter consistência** com o que já está no repositório em vez de forçar essa estrutura.

## Fluxo do algoritmo (obrigatório)

1. Carregar configurações (URL da página, texto/regex/seletor a verificar, URL do webhook do Discord, timeout de requisição, intervalo de execução se for loop).
2. Fazer requisição HTTP GET para a URL alvo com `HttpClient`.
   - Definir timeout razoável (ex.: 15s).
   - Tratar falhas de rede/timeout como caso separado (não confundir "página fora do ar" com "informação ausente") — decidir se isso também deve notificar.
3. Verificar se a informação esperada está presente no HTML retornado.
   - Se for texto simples: `html.Contains(texto, StringComparison.OrdinalIgnoreCase)` (avaliar case sensitivity conforme necessidade).
   - Se for estrutura HTML: usar `HtmlAgilityPack` com XPath ou CSS-like selector.
4. Se a informação **NÃO** for encontrada:
   - Montar payload JSON no formato esperado pelo Discord webhook (`content` ou `embeds`).
   - Enviar `POST` para a URL do webhook.
   - Logar sucesso/falha do envio.
5. Se a informação for encontrada:
   - Logar no console que está tudo certo (não notificar, a menos que configurado o contrário).
6. Retornar código de saída apropriado (`0` sucesso, `!=0` erro) para permitir uso em scripts/cron.

## Configuração e segredos

- **Nunca** hardcodar a URL do webhook do Discord ou a URL da página alvo diretamente no código.
- Usar `appsettings.json` para valores não sensíveis e variáveis de ambiente (ou `dotnet user-secrets` em dev) para o webhook URL.
- Fornecer `appsettings.example.json` com placeholders, e garantir que o arquivo real com segredos esteja no `.gitignore`.
- Exemplo de `appsettings.json`:
```json
{
  "Monitor": {
    "TargetUrl": "https://exemplo.com/pagina",
    "SearchText": "texto ou padrão a verificar",
    "RequestTimeoutSeconds": 15
  },
  "Discord": {
    "WebhookUrl": "https://discord.com/api/webhooks/XXXX/YYYY"
  }
}
```

## Tratamento de erros

- Toda chamada de rede (fetch da página e envio ao Discord) deve estar em `try/catch` com log claro do erro.
- Diferenciar nos logs:
  - Erro ao acessar a página (timeout, DNS, status HTTP != 2xx).
  - Erro ao enviar o webhook (rate limit do Discord, URL inválida, etc.).
  - Informação não encontrada (fluxo normal esperado, não é "erro").
- Não deixar exceções não tratadas derrubarem o processo sem log.
- Respeitar rate limit do Discord (webhooks têm limite de requisições); se for rodar em loop curto, considerar isso.

## Logging

- Logar no console (stdout) cada execução com timestamp, resultado da checagem e status do envio (se aplicável).
- Nível de detalhe mínimo: início da execução, resultado da checagem, ação tomada, fim da execução.
- Evitar logar o conteúdo completo do HTML baixado (pode ser grande); logar só um trecho relevante se necessário para debug.

## Estilo de código

- Seguir convenções padrão de C# (PascalCase para métodos/classes, camelCase para variáveis locais).
- Usar `async`/`await` em todas as operações de I/O (HTTP requests).
- Injeção de dependência simples é bem-vinda (via `Microsoft.Extensions.DependencyInjection`) mas não é obrigatória — pode ser instanciação direta se o projeto for pequeno.
- Separar responsabilidades em serviços (fetch, check, notify) em vez de tudo em `Program.cs`.
- Adicionar comentários apenas onde a lógica não for óbvia (ex.: regras de parsing específicas).

## Testes

- Se o repositório já tiver estrutura de testes, adicionar testes unitários para:
  - `ContentCheckerService` (dado um HTML de exemplo, verificar detecção correta de presença/ausência).
  - `DiscordNotifierService` (mockar o `HttpClient` para validar o payload enviado).
- Não é obrigatório testar `PageFetcherService` contra a internet real — usar HTML fixo/mock nos testes.

## O que o agente NÃO deve fazer

- Não enviar dados sensíveis (webhook URL, cookies, headers de auth) para logs ou para o próprio conteúdo da notificação do Discord.
- Não fazer scraping agressivo (sem delay/rate limit) que possa sobrecarregar o site alvo.
- Não adicionar autenticação, banco de dados, ou funcionalidades fora do escopo (o programa é um scraper simples e pontual) sem que isso seja pedido explicitamente.
- Não versionar arquivos de configuração com segredos reais.

## Definição de pronto (Definition of Done)

- O programa compila e roda via `dotnet run` sem erros.
- Consegue baixar o HTML de uma URL configurada.
- Detecta corretamente presença/ausência da informação configurada.
- Envia notificação ao Discord corretamente quando a informação está ausente (validar manualmente com um webhook de teste).
- Trata erros de rede sem crashar.
- README atualizado com instruções de como configurar e rodar o projeto.
