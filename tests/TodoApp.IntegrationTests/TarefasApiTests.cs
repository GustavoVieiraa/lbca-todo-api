using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClosedXML.Excel;
using FluentAssertions;
using TodoApp.Application.Importacao;
using TodoApp.Application.Tarefas.Dtos;
using TodoApp.Domain.Enums;

namespace TodoApp.IntegrationTests;

public class TarefasApiTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiFactory _factory;

    public TarefasApiTests(ApiFactory factory) => _factory = factory;

    // ---------------------------------------------------------------- Auth

    [Fact]
    public async Task Tarefas_SemToken_Retorna401()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/api/tarefas");

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ComCredenciaisInvalidas_Retorna401()
    {
        var client = _factory.CreateClient();

        var resposta = await client.PostAsJsonAsync("/api/auth/login",
            new { usuario = "errado", senha = "errado" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------- CRUD

    [Fact]
    public async Task Crud_FluxoCompleto()
    {
        var client = await ClienteAutenticadoAsync();

        // Cria
        var criar = new CriarTarefaRequest("Integração", "via teste", DateTime.UtcNow.AddDays(10), Prioridade.Alta);
        var respostaCriar = await client.PostAsJsonAsync("/api/tarefas", criar, Json);
        respostaCriar.StatusCode.Should().Be(HttpStatusCode.Created);

        var criada = await respostaCriar.Content.ReadFromJsonAsync<TarefaResponse>(Json);
        criada!.Id.Should().BeGreaterThan(0);
        criada.Status.Should().Be(StatusTarefa.Pendente);

        // Obtém
        var respostaGet = await client.GetAsync($"/api/tarefas/{criada.Id}");
        respostaGet.StatusCode.Should().Be(HttpStatusCode.OK);

        // Atualiza
        var atualizar = new AtualizarTarefaRequest("Atualizada", null, DateTime.UtcNow.AddDays(5),
            StatusTarefa.Concluida, Prioridade.Baixa);
        var respostaPut = await client.PutAsJsonAsync($"/api/tarefas/{criada.Id}", atualizar, Json);
        respostaPut.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Remove
        var respostaDelete = await client.DeleteAsync($"/api/tarefas/{criada.Id}");
        respostaDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Não encontra mais
        var respostaGet2 = await client.GetAsync($"/api/tarefas/{criada.Id}");
        respostaGet2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Criar_ComDataNoPassado_Retorna400()
    {
        var client = await ClienteAutenticadoAsync();

        var criar = new CriarTarefaRequest("Inválida", null, DateTime.UtcNow.AddDays(-1), Prioridade.Alta);
        var resposta = await client.PostAsJsonAsync("/api/tarefas", criar, Json);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Listar_RetornaResultadoPaginado()
    {
        var client = await ClienteAutenticadoAsync();

        var resposta = await client.GetAsync("/api/tarefas?pagina=1&tamanhoPagina=5");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagina = await resposta.Content.ReadFromJsonAsync<PaginaResposta>(Json);
        pagina!.TamanhoPagina.Should().Be(5);
        pagina.Pagina.Should().Be(1);
    }

    // -------------------------------------------------------------- Import

    [Fact]
    public async Task Importar_PlanilhaMista_SalvaValidasEReportaInvalidas()
    {
        var client = await ClienteAutenticadoAsync();
        var planilha = ConstruirPlanilha();

        using var conteudo = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(planilha);
        arquivo.Headers.ContentType =
            new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        conteudo.Add(arquivo, "arquivo", "tarefas.xlsx");

        var resposta = await client.PostAsync("/api/tarefas/importar", conteudo);

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await resposta.Content.ReadFromJsonAsync<ImportacaoResultado>(Json);
        resultado!.TotalLinhas.Should().Be(4);
        resultado.Importadas.Should().Be(2);
        resultado.Falhas.Should().Be(2);
        resultado.Erros.Should().HaveCount(2);
    }

    [Fact]
    public async Task Importar_ArquivoComExtensaoInvalida_Retorna400()
    {
        var client = await ClienteAutenticadoAsync();

        using var conteudo = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(new byte[] { 1, 2, 3 });
        conteudo.Add(arquivo, "arquivo", "tarefas.txt");

        var resposta = await client.PostAsync("/api/tarefas/importar", conteudo);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Importar_ArquivoXlsxCorrompido_Retorna400()
    {
        var client = await ClienteAutenticadoAsync();

        using var conteudo = new MultipartFormDataContent();
        var arquivo = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 }); // extensão .xlsx mas conteúdo inválido
        arquivo.Headers.ContentType =
            new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        conteudo.Add(arquivo, "arquivo", "corrompido.xlsx");

        var resposta = await client.PostAsync("/api/tarefas/importar", conteudo);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ------------------------------------------------------------- Helpers

    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { usuario = "admin", senha = "admin123" });
        login.EnsureSuccessStatusCode();

        var corpo = await login.Content.ReadFromJsonAsync<LoginResposta>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", corpo!.Token);

        return client;
    }

    private static byte[] ConstruirPlanilha()
    {
        using var workbook = new XLWorkbook();
        var planilha = workbook.AddWorksheet("Tarefas");

        planilha.Cell(1, 1).Value = "Título";
        planilha.Cell(1, 2).Value = "Descrição";
        planilha.Cell(1, 3).Value = "Data de Vencimento";
        planilha.Cell(1, 4).Value = "Prioridade";

        // Linha 2 — válida
        planilha.Cell(2, 1).Value = "Comprar pão";
        planilha.Cell(2, 3).Value = DateTime.UtcNow.AddDays(10);
        planilha.Cell(2, 4).Value = "Alta";

        // Linha 3 — válida
        planilha.Cell(3, 1).Value = "Estudar Dapper";
        planilha.Cell(3, 3).Value = DateTime.UtcNow.AddDays(5);
        planilha.Cell(3, 4).Value = "Baixa";

        // Linha 4 — inválida: título vazio
        planilha.Cell(4, 3).Value = DateTime.UtcNow.AddDays(3);
        planilha.Cell(4, 4).Value = "Media";

        // Linha 5 — inválida: prioridade desconhecida
        planilha.Cell(5, 1).Value = "Tarefa X";
        planilha.Cell(5, 3).Value = DateTime.UtcNow.AddDays(3);
        planilha.Cell(5, 4).Value = "Urgente";

        using var memoria = new MemoryStream();
        workbook.SaveAs(memoria);
        return memoria.ToArray();
    }

    private sealed record LoginResposta(string Token, DateTime ExpiraEm);

    private sealed record PaginaResposta(int Pagina, int TamanhoPagina, long TotalItens);
}
