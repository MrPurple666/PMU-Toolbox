using ToolboxCorporativo.Dominio.Interfaces;

namespace ToolboxCorporativo.Infraestrutura.Notificacoes;

public sealed class ServicoNotificacoes : IServicoNotificacoes
{
    public event EventHandler<NotificacaoRecursoEventArgs>? NotificacaoPublicada;

    public void Publicar(NotificacaoRecursoEventArgs notificacao)
    {
        ArgumentNullException.ThrowIfNull(notificacao);
        NotificacaoPublicada?.Invoke(this, notificacao);
    }
}
