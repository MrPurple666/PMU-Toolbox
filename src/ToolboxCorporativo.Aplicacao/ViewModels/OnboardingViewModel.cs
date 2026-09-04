using CommunityToolkit.Mvvm.ComponentModel;

namespace ToolboxCorporativo.Aplicacao.ViewModels;

public sealed class OnboardingViewModel : ObservableObject
{
    private string urlApi = "http://127.0.0.1:8080";
    private string status = "Carregando catálogo local...";
    private string? erroTecnico;
    private string? diagnostico;
    private readonly string explicacao =
        "O Toolbox consulta o servidor para receber seu catálogo, identifica sua sessão Windows " +
        "e mantém a última configuração válida no cache local. Nenhuma senha de domínio é solicitada.";

    public string UrlApi
    {
        get => urlApi;
        set => SetProperty(ref urlApi, value);
    }

    public string Status
    {
        get => status;
        private set => SetProperty(ref status, value);
    }

    public string? ErroTecnico
    {
        get => erroTecnico;
        private set => SetProperty(ref erroTecnico, value);
    }

    public string? Diagnostico
    {
        get => diagnostico;
        private set => SetProperty(ref diagnostico, value);
    }

    public void AtualizarDiagnostico(string valor) => Diagnostico = valor;

    public string Explicacao => explicacao;

    public void AtualizarStatus(string mensagem) => Status = mensagem;

    public void AtualizarErro(Exception? excecao) =>
        ErroTecnico = excecao is null ? null : $"{excecao.GetType().Name}: {excecao.Message}";
}
