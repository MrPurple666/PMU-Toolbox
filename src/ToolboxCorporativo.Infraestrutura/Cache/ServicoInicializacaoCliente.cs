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
    public async Task<EstadoInicializacaoCliente> InicializarAsync(CancellationToken tokenCancelamento = default)
    {
        var snapshot = await banco.LerSnapshotAsync(tokenCancelamento);
        InformarStatus(snapshot is null ? "Cache local vazio. Sincronizando..." : "Cache local carregado. Sincronizando...");
        if (snapshot is not null)
            InformarSnapshotAtualizado(snapshot);
        _ = SincronizarEmSegundoPlanoAsync();
        return new EstadoInicializacaoCliente(
            snapshot,
            ModoOffline: true,
            snapshot?.UltimaSincronizacao);
    }


    private async Task SincronizarEmSegundoPlanoAsync()
    {
        try
        {
            await sincronizacao.SincronizarAsync();
            var snapshot = await banco.LerSnapshotAsync();
            if (snapshot is not null)
                InformarSnapshotAtualizado(snapshot);
            InformarStatus("Sincronização concluída.");
        }
        catch (Exception excecao)
        {
            RegistrarFalhaSincronizacao(excecao);
            InformarStatus("Não foi possível sincronizar. Confira o servidor e tente novamente.");
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
