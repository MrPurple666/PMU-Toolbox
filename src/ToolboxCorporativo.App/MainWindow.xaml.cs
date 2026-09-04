using Microsoft.UI.Xaml;
using ToolboxCorporativo.Aplicacao.ViewModels;
using ToolboxCorporativo.Infraestrutura.Cache;

namespace ToolboxCorporativo.App;

public sealed partial class MainWindow : Window
{
    public MainWindow(
        OnboardingViewModel onboarding,
        CatalogoViewModel catalogo,
        ServicoInicializacaoCliente inicializacao)
    {
        InitializeComponent();
        Onboarding = onboarding;
        Catalogo = catalogo;
        inicializacao.SnapshotAtualizado += snapshot =>
            DispatcherQueue.TryEnqueue(() => Catalogo.AplicarSnapshot(snapshot));
        inicializacao.StatusAtualizado += status =>
            DispatcherQueue.TryEnqueue(() => Onboarding.AtualizarStatus(status));
        Raiz.DataContext = this;
        Title = "Toolbox Corporativo";
    }

    public OnboardingViewModel Onboarding { get; }
    public CatalogoViewModel Catalogo { get; }
}
