using Microsoft.AspNetCore.Components.Web;
using System.Text.Json;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Data.SqlClient;
using AutoPecasChat.Web;
using AutoPecasChat.Web.Services;
using AutoPecasChat.Web.Models.Frete;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<ApiClient>();
// JadLogService não precisa mais de HttpClient (usando cálculo estimado)
builder.Services.AddScoped<JadLogService>();

// Configura CORS para permitir requisições do mobile
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configura JSON para aceitar tanto PascalCase quanto camelCase
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = null; // Mantém o nome exato das propriedades
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Comentado para permitir requisições HTTP diretas (desktop precisa)
// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Habilita CORS antes dos endpoints - IMPORTANTE para mobile
app.UseCors();

// Log para confirmar que o servidor iniciou
app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = app.Urls;
    Console.WriteLine($"[SERVER] ✅ Servidor iniciado!");
    Console.WriteLine($"[SERVER] URLs: {string.Join(", ", urls)}");
    Console.WriteLine($"[SERVER] Aguardando requisições na porta 5099...");
});

app.MapGet("/api/health", () => 
{
    Console.WriteLine($"[API HEALTH] Health check recebido");
    return Results.Ok(new { ok = true, message = "Servidor está funcionando" });
}).AllowAnonymous();

app.MapPost("/api/setup/chamados", async (ApiClient apiClient) =>
{
    await apiClient.CriarTabelaChamados();
    return Results.Ok();
}).AllowAnonymous();

// Endpoint simplificado e robusto para receber chamados do mobile
app.MapPost("/api/chamados", async (HttpRequest httpRequest, ApiClient apiClient) =>
{
    try
    {
        Console.WriteLine($"[API] ========== /api/chamados RECEBIDO ==========");
        Console.WriteLine($"[API] Método: {httpRequest.Method}");
        Console.WriteLine($"[API] Content-Type: {httpRequest.ContentType}");
        Console.WriteLine($"[API] Content-Length: {httpRequest.ContentLength}");
        
        // Lê o body manualmente
        httpRequest.EnableBuffering(); // Permite ler o body múltiplas vezes
        using var reader = new StreamReader(httpRequest.Body, System.Text.Encoding.UTF8, leaveOpen: true);
        var bodyJson = await reader.ReadToEndAsync();
        httpRequest.Body.Position = 0; // Reset para possível leitura futura
        
        Console.WriteLine($"[API] Body JSON: {bodyJson}");
        
        if (string.IsNullOrWhiteSpace(bodyJson))
        {
            Console.WriteLine($"[API] ❌ Body vazio");
            return Results.BadRequest(new { ok = false, erro = "Body vazio" });
        }
        
        // Extrai dados do JSON (aceita camelCase ou PascalCase)
        string? protocolo = null, nome = null, email = null, tipoProblema = null, descricao = null;
        
        try
        {
            using var doc = JsonDocument.Parse(bodyJson);
            var root = doc.RootElement;
            
            // Tenta PascalCase primeiro, depois camelCase
            protocolo = root.TryGetProperty("Protocolo", out var p1) ? p1.GetString() 
                       : root.TryGetProperty("protocolo", out var p2) ? p2.GetString() : null;
            nome = root.TryGetProperty("Nome", out var n1) ? n1.GetString()
                  : root.TryGetProperty("nome", out var n2) ? n2.GetString() : null;
            email = root.TryGetProperty("Email", out var e1) ? e1.GetString()
                   : root.TryGetProperty("email", out var e2) ? e2.GetString() : null;
            tipoProblema = root.TryGetProperty("TipoProblema", out var tp1) ? tp1.GetString()
                          : root.TryGetProperty("tipoProblema", out var tp2) ? tp2.GetString() : null;
            descricao = root.TryGetProperty("Descricao", out var d1) ? d1.GetString()
                       : root.TryGetProperty("descricao", out var d2) ? d2.GetString() : null;
            
            Console.WriteLine($"[API] Dados extraídos:");
            Console.WriteLine($"[API]   Protocolo: '{protocolo}'");
            Console.WriteLine($"[API]   Nome: '{nome}'");
            Console.WriteLine($"[API]   Email: '{email}'");
            Console.WriteLine($"[API]   TipoProblema: '{tipoProblema}'");
            Console.WriteLine($"[API]   Descricao: '{descricao ?? "(vazio)"}'");
        }
        catch (Exception ex2)
        {
            Console.WriteLine($"[API] ❌ Erro ao processar JSON: {ex2.Message}");
            Console.WriteLine($"[API] Stack: {ex2.StackTrace}");
            return Results.BadRequest(new { ok = false, erro = $"JSON inválido: {ex2.Message}" });
        }
        
        // Valida campos obrigatórios
        if (string.IsNullOrWhiteSpace(protocolo) || 
            string.IsNullOrWhiteSpace(nome) || 
            string.IsNullOrWhiteSpace(email) || 
            string.IsNullOrWhiteSpace(tipoProblema))
        {
            Console.WriteLine($"[API] ❌ Campos obrigatórios vazios");
            return Results.BadRequest(new { ok = false, erro = "Campos obrigatórios vazios" });
        }
        
        // Salva no banco de dados
        Console.WriteLine($"[API] ========== SALVANDO NO BANCO DE DADOS ==========");
        
        // Tenta salvar - método agora sempre retorna bool (não lança exceções)
        var sucesso = await apiClient.SalvarChamado(
            protocolo,
            nome,
            email,
            tipoProblema,
            descricao ?? string.Empty);

        if (sucesso)
        {
            Console.WriteLine($"[API] ✅ ✅ ✅ SUCESSO - Chamado {protocolo} salvo no banco!");
            return Results.Ok(new { 
                ok = true, 
                protocolo = protocolo, 
                message = "Chamado registrado com sucesso",
                sucesso = true
            });
        }
        
        // Se chegou aqui, houve falha
        Console.WriteLine($"[API] ❌ Falha ao salvar no banco - método retornou false");
        return Results.BadRequest(new { 
            ok = false, 
            erro = "Não foi possível salvar o chamado no banco de dados. Verifique os logs do servidor.",
            sucesso = false
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[API] ❌ EXCEÇÃO: {ex.GetType().Name} - {ex.Message}");
        Console.WriteLine($"[API] Stack: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"[API] Inner: {ex.InnerException.Message}");
        }
        return Results.Problem($"Erro: {ex.Message}", statusCode: 500);
    }
}).AllowAnonymous();

app.MapPost("/api/consultas/frete", async (ApiClient apiClient, JadLogService jadLogService, CalcularFreteRequest request) =>
{
    try
    {
        // Valida a requisição
        if (!request.IsValid())
        {
            return Results.BadRequest(new { erro = "CEP inválido. O CEP deve conter 8 dígitos." });
        }

        // Registra a consulta no banco de dados
        try
        {
            await apiClient.RegistrarConsultaFrete(request.Cep);
        }
        catch (Exception dbEx)
        {
            Console.WriteLine($"[API] Aviso: Não foi possível registrar consulta no banco: {dbEx.Message}");
            // Não interrompe o fluxo se falhar o registro no BD
        }

        // Calcula o frete usando a API da JadLog
        var cepLimpo = request.GetCepLimpo();
        var freteResponse = await jadLogService.CalcularFreteAsync(
            cepLimpo,
            request.Peso ?? 1.0m,
            request.Comprimento ?? 20m,
            request.Altura ?? 5m,
            request.Largura ?? 15m
        );

        if (!freteResponse.Sucesso)
        {
            return Results.BadRequest(new { erro = freteResponse.Erro ?? "Erro ao calcular frete" });
        }

        return Results.Ok(freteResponse);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[API] Erro ao calcular frete para CEP {request.Cep}: {ex}");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapPost("/api/consultas/pedido", async (ApiClient apiClient, NumeroPedidoRequest request) =>
{
    try
    {
        await apiClient.RegistrarConsultaPedido(request.NumeroPedido);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[API] Erro ao registrar consulta de pedido ({request.NumeroPedido}): {ex}");
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/cancelamentos", async (ApiClient apiClient, CancelamentoRequest request) =>
{
    try
    {
        var status = string.IsNullOrWhiteSpace(request.Status) ? "solicitado" : request.Status;
        await apiClient.RegistrarCancelamentoPedido(request.NumeroPedido, status);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[API] Erro ao registrar cancelamento ({request.NumeroPedido}): {ex}");
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/problemas-pagamento", async (ApiClient apiClient, DescricaoRequest request) =>
{
    try
    {
        await apiClient.RegistrarProblemaPagamento(request.Descricao);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[API] Erro ao registrar problema de pagamento: {ex}");
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/api/duvidas-produto", async (ApiClient apiClient, DescricaoRequest request) =>
{
    try
    {
        await apiClient.RegistrarDuvidaProduto(request.Descricao);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[API] Erro ao registrar dúvida de produto: {ex}");
        return Results.Problem(ex.Message);
    }
});

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Configura URLs de forma inteligente
// Se já houver URLs configuradas (via launchSettings.json ou variável de ambiente), usa-as
// Caso contrário, configura manualmente
var port = 5099;
var hasExistingUrls = app.Urls.Any();

if (!hasExistingUrls)
{
    // Se não houver URLs configuradas, define as padrões
    app.Urls.Add($"http://localhost:{port}");
    app.Urls.Add($"http://0.0.0.0:{port}");
}
else
{
    // Se já houver URLs, adiciona 0.0.0.0 apenas se não existir
    // Isso permite conexões mobile sem quebrar o launchBrowser
    var existingUrls = app.Urls.ToList();
    if (!existingUrls.Any(u => u.Contains("0.0.0.0")))
    {
        // Tenta adicionar na mesma porta do localhost existente
        var localhostUrl = existingUrls.FirstOrDefault(u => u.Contains("localhost"));
        if (localhostUrl != null)
        {
            var urlWithZero = localhostUrl.Replace("localhost", "0.0.0.0");
            app.Urls.Add(urlWithZero);
        }
        else
        {
            app.Urls.Add($"http://0.0.0.0:{port}");
        }
    }
}

Console.WriteLine($"[SERVER] =========================================");
Console.WriteLine($"[SERVER] 🚀 INICIANDO SERVIDOR");
Console.WriteLine($"[SERVER] =========================================");
Console.WriteLine($"[SERVER] Porta: {port}");
Console.WriteLine($"[SERVER] URLs configuradas:");
foreach (var url in app.Urls)
{
    Console.WriteLine($"[SERVER]   - {url}");
}
Console.WriteLine($"[SERVER] Para mobile Android: http://10.0.2.2:{port}/api/chamados");
Console.WriteLine($"[SERVER] Para desktop: http://localhost:{port}");
Console.WriteLine($"[SERVER] =========================================");
Console.WriteLine($"[SERVER] Aguardando requisições...");

// Abre o navegador automaticamente em ambiente de desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        try
        {
            var localhostUrl = app.Urls.FirstOrDefault(u => u.Contains("localhost")) 
                ?? $"http://localhost:{port}";
            
            // Aguarda um pouco para garantir que o servidor está pronto
            Task.Delay(500).ContinueWith(_ =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = localhostUrl,
                        UseShellExecute = true
                    });
                    Console.WriteLine($"[SERVER] 🌐 Navegador aberto automaticamente: {localhostUrl}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SERVER] ⚠️ Não foi possível abrir o navegador automaticamente: {ex.Message}");
                    Console.WriteLine($"[SERVER] 💡 Acesse manualmente: {localhostUrl}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SERVER] ⚠️ Erro ao tentar abrir navegador: {ex.Message}");
        }
    });
}

app.Run();

internal record NovoChamadoRequest(string Protocolo, string Nome, string Email, string TipoProblema, string? Descricao);
internal record CepRequest(string Cep);
internal record NumeroPedidoRequest(string NumeroPedido);
internal record CancelamentoRequest(string NumeroPedido, string? Status);
internal record DescricaoRequest(string Descricao);
