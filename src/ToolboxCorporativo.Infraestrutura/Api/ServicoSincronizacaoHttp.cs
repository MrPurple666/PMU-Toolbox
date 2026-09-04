using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ToolboxCorporativo.Dominio.Entidades;
using ToolboxCorporativo.Dominio.Interfaces;
using ToolboxCorporativo.Infraestrutura.Persistencia;

namespace ToolboxCorporativo.Infraestrutura.Api;

public sealed partial class ServicoSincronizacaoHttp(
    HttpClient cliente,
    ServicoConfiguracaoEndpoint configuracaoEndpoint,
    IServicoIdentidadeUsuario identidade,
    ContextoBancoDados banco,
    ILogger<ServicoSincronizacaoHttp> logger) : IServicoSincronizacao
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    public async Task SincronizarAsync(CancellationToken tokenCancelamento = default)
    {
        var usuario = await identidade.ObterUsuarioAtualAsync(tokenCancelamento);
        var snapshotAtual = await banco.LerSnapshotAsync(tokenCancelamento);
        var requisicao = new RequisicaoSincronizacao(
            new DadosComputador(Environment.MachineName, Environment.OSVersion.VersionString),
            new DadosCliente("0.1.0"),
            snapshotAtual?.Versao ?? 0,
            new DadosUsuario(usuario.NomeUsuario, usuario.Dominio));

        cliente.Timeout = TimeSpan.FromSeconds(10);
        using var resposta = await cliente.PostAsJsonAsync(
            new Uri(configuracaoEndpoint.Endereco, "api/v1/sincronizacao"),
            requisicao,
            OpcoesJson,
            tokenCancelamento);
        if (resposta.StatusCode == System.Net.HttpStatusCode.NotModified)
            return;
        resposta.EnsureSuccessStatusCode();

        var documento = await resposta.Content.ReadFromJsonAsync<JsonElement>(OpcoesJson, tokenCancelamento);
        var versao = documento.GetProperty("versao").GetInt32();
        if (snapshotAtual is not null && versao < snapshotAtual.Versao)
            throw new InvalidDataException("O servidor devolveu uma versão de configuração regressiva.");

        var recursos = documento.GetProperty("recursos")
            .EnumerateArray()
            .Select(ConverterRecurso)
            .ToArray();
        var politica = ConverterPolitica(documento);
        var novoSnapshot = new SnapshotConfiguracao(versao, DateTimeOffset.UtcNow, recursos, politica);
        if (await banco.SubstituirSnapshotAsync(novoSnapshot, tokenCancelamento))
            RegistrarSnapshotRecebido(versao, recursos.Length);
    }

    public async Task SincronizarPeriodicamenteAsync(
        TimeSpan intervalo,
        CancellationToken tokenCancelamento = default)
    {
        using var temporizador = new PeriodicTimer(intervalo);
        await SincronizarAsync(tokenCancelamento);
        while (await temporizador.WaitForNextTickAsync(tokenCancelamento))
            await SincronizarAsync(tokenCancelamento);
    }

    private static RegistroRecursoCache ConverterRecurso(JsonElement recurso)
    {
        return new RegistroRecursoCache(
            recurso.GetProperty("id").GetGuid(),
            recurso.GetProperty("nome").GetString() ?? throw new InvalidDataException("Recurso sem nome."),
            recurso.TryGetProperty("descricao", out var descricao) && descricao.ValueKind != JsonValueKind.Null ? descricao.GetString() : null,
            Enum.Parse<ToolboxCorporativo.Dominio.Enumeracoes.TipoRecurso>(recurso.GetProperty("tipo").GetString() ?? string.Empty, true),
            recurso.GetProperty("destino").GetString() ?? string.Empty,
            true,
            recurso.GetProperty("obrigatorio").GetBoolean(),
            recurso.GetProperty("ocultavel").GetBoolean(),
            recurso.GetProperty("favoritavel").GetBoolean(),
            recurso.TryGetProperty("tags", out var tags) ? tags.GetRawText() : "[]",
            recurso.TryGetProperty("aliases", out var aliases) ? aliases.GetRawText() : "[]");
    }

    private static PoliticaRecursos ConverterPolitica(JsonElement documento)
    {
        if (!documento.TryGetProperty("politicas", out var politica))
            return new();
        return new PoliticaRecursos(
            politica.TryGetProperty("precedenciaUsuarioSobreHerancaGrupo", out var precedencia) &&
                (precedencia.ValueKind == JsonValueKind.True ||
                 (precedencia.ValueKind == JsonValueKind.Number && precedencia.GetInt32() != 0)),
            true,
            politica.TryGetProperty("intervaloSincronizacaoSegundos", out var intervalo)
                ? TimeSpan.FromSeconds(intervalo.GetInt32())
                : null);
    }

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Snapshot de configuração recebido: versão {Versao}, {QuantidadeRecursos} recursos.")]
    private partial void RegistrarSnapshotRecebido(int versao, int quantidadeRecursos);

    private sealed record RequisicaoSincronizacao(
        DadosComputador Computador,
        DadosCliente Cliente,
        int ConfiguracaoAtual,
        DadosUsuario Usuario);

    private sealed record DadosComputador(string Nome, string VersaoWindows);
    private sealed record DadosCliente(string Versao);
    private sealed record DadosUsuario(string NomeUsuario, string Dominio);
}
