using CommunityToolkit.Mvvm.ComponentModel;

namespace ToolboxCorporativo.Aplicacao.ViewModels;

public sealed class OnboardingViewModel : ObservableObject
{
    private string urlApi = "http://127.0.0.1:8080";
    private string status = "Carregando catálogo local...";
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

    public string Explicacao => explicacao;

    public void AtualizarStatus(string mensagem) => Status = mensagem;
}
