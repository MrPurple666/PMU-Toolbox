using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ToolboxCorporativo.Aplicacao.ViewModels;
using ToolboxCorporativo.Dominio.Entidades;
using ToolboxCorporativo.Infraestrutura.Api;
using ToolboxCorporativo.Infraestrutura.Cache;
using ToolboxCorporativo.Infraestrutura.Persistencia;
using ToolboxCorporativo.Infraestrutura.Rede;

namespace ToolboxCorporativo.App;

public sealed partial class MainWindow : Window
{
    private readonly ServicoConfiguracaoEndpoint configuracaoEndpoint;
    private readonly ServicoInicializacaoCliente inicializacao;
    private readonly ServicoAberturaRecursos abertura;

    public MainWindow(
        OnboardingViewModel onboarding,
        CatalogoViewModel catalogo,
        ServicoConfiguracaoEndpoint configuracaoEndpoint,
        ServicoInicializacaoCliente inicializacao,
        ServicoAberturaRecursos abertura)
    {
        InitializeComponent();
        Onboarding = onboarding;
        Catalogo = catalogo;
        this.configuracaoEndpoint = configuracaoEndpoint;
        this.inicializacao = inicializacao;
        this.abertura = abertura;
        Onboarding.UrlApi = configuracaoEndpoint.Endereco.AbsoluteUri;
        inicializacao.SnapshotAtualizado += snapshot =>
            DispatcherQueue.TryEnqueue(() => Catalogo.AplicarSnapshot(snapshot));
        inicializacao.StatusAtualizado += status =>
            DispatcherQueue.TryEnqueue(() => Onboarding.AtualizarStatus(status));
        inicializacao.ErroAtualizado += erro =>
            DispatcherQueue.TryEnqueue(() => Onboarding.AtualizarErro(erro));
        Raiz.DataContext = this;
        Title = "Toolbox Corporativo";
    }

    public OnboardingViewModel Onboarding { get; }
    public CatalogoViewModel Catalogo { get; }

    private async void SincronizarClick(object sender, RoutedEventArgs e)
    {
        try
        {
            configuracaoEndpoint.Definir(Onboarding.UrlApi);
            await inicializacao.SincronizarAgoraAsync();
        }
        catch (Exception excecao)
        {
            Onboarding.AtualizarErro(excecao);
            Onboarding.AtualizarStatus("A URL do servidor não é válida.");
        }
    }

    private async void RecursoClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not RegistroRecursoCache recurso)
            return;

        try
        {
            await abertura.AbrirAsync(new RecursoCatalogo
            {
                Id = recurso.Id,
                Nome = recurso.Nome,
                Descricao = recurso.Descricao,
                Tipo = recurso.Tipo,
                Destino = recurso.Destino,
                Ativo = recurso.Ativo,
                Obrigatorio = recurso.Obrigatorio,
                Ocultavel = recurso.Ocultavel,
                Favoritavel = recurso.Favoritavel,
            }, recurso.Destino);
            Onboarding.AtualizarStatus($"Abrindo {recurso.Nome}.");
        }
        catch (Exception excecao)
        {
            Onboarding.AtualizarErro(excecao);
            Onboarding.AtualizarStatus($"Não foi possível abrir {recurso.Nome}.");
        }
    }
}
