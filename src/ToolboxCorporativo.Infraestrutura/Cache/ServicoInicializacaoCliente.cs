using Microsoft.Extensions.Logging;
using ToolboxCorporativo.Dominio.Interfaces;
using ToolboxCorporativo.Infraestrutura.Persistencia;

namespace ToolboxCorporativo.Infraestrutura.Cache;

public sealed record EstadoInicializacaoCliente(
    SnapshotConfiguracao? Snapshot,
    bool ModoOffline,
    DateTimeOffset? UltimaSincronizacao);

public sealed partial class ServicoInicializacaoCliente(
    ContextoBancoDados banco,
    IServicoSincronizacao sincronizacao,
    ILogger<ServicoInicializacaoCliente> logger)
{
    public event Action<SnapshotConfiguracao>? SnapshotAtualizado;
    public event Action<string>? StatusAtualizado;
    public event Action<Exception?>? ErroAtualizado;
    public async Task<EstadoInicializacaoCliente> InicializarAsync(CancellationToken tokenCancelamento = default)
    {
        var snapshot = await banco.LerSnapshotAsync(tokenCancelamento);
        InformarStatus(snapshot is null ? "Cache local vazio. Sincronizando..." : "Cache local carregado. Sincronizando...");
        if (snapshot is not null)
            InformarSnapshotAtualizado(snapshot);
        _ = SincronizarEmSegundoPlanoAsync();
        _ = SincronizarPeriodicamenteAsync();
        return new EstadoInicializacaoCliente(
            snapshot,
            ModoOffline: true,
            snapshot?.UltimaSincronizacao);
    }


    public Task SincronizarAgoraAsync(CancellationToken tokenCancelamento = default) =>
        SincronizarAsync(tokenCancelamento);

    private Task SincronizarEmSegundoPlanoAsync() => SincronizarAsync(CancellationToken.None);

    private async Task SincronizarPeriodicamenteAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(5));
            await SincronizarAsync(CancellationToken.None);
        }
    }

    private async Task SincronizarAsync(CancellationToken tokenCancelamento)
    {
        try
        {
            InformarStatus("Sincronizando catálogo...");
            await sincronizacao.SincronizarAsync(tokenCancelamento);
            var snapshot = await banco.LerSnapshotAsync(tokenCancelamento);
            if (snapshot is not null)
                InformarSnapshotAtualizado(snapshot);
            ErroAtualizado?.Invoke(null);
            InformarStatus(snapshot is null
                ? "Servidor respondeu sem catálogo."
                : $"Sincronizado em {snapshot.UltimaSincronizacao.LocalDateTime:g}.");
        }
        catch (Exception excecao)
        {
            RegistrarFalhaSincronizacao(excecao);
            ErroAtualizado?.Invoke(excecao);
            InformarStatus("Não foi possível sincronizar. Mantendo o catálogo local.");
        }
    }
    public void InformarSnapshotAtualizado(SnapshotConfiguracao snapshot)
    {
        SnapshotAtualizado?.Invoke(snapshot);
    }

    public void InformarStatus(string mensagem)
    {
        StatusAtualizado?.Invoke(mensagem);
    }
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Sincronização em segundo plano indisponível; mantendo o cache local.")]
    private partial void RegistrarFalhaSincronizacao(Exception excecao);
}
