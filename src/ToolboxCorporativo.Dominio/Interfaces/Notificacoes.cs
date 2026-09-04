namespace ToolboxCorporativo.Dominio.Interfaces;

public sealed class NotificacaoRecursoEventArgs : EventArgs
{
    public NotificacaoRecursoEventArgs(string titulo, string mensagem, DateTimeOffset criadaEm)
    {
        Titulo = titulo;
        Mensagem = mensagem;
        CriadaEm = criadaEm;
    }

    public string Titulo { get; }
    public string Mensagem { get; }
    public DateTimeOffset CriadaEm { get; }
}

public interface IServicoNotificacoes
{
    event EventHandler<NotificacaoRecursoEventArgs>? NotificacaoPublicada;
    void Publicar(NotificacaoRecursoEventArgs notificacao);
}
