using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ToolboxCorporativo.Aplicacao.ViewModels;
using ToolboxCorporativo.Dominio.Entidades;
using ToolboxCorporativo.Infraestrutura.Api;
using ToolboxCorporativo.Infraestrutura.Cache;
using ToolboxCorporativo.Infraestrutura.Persistencia;
using ToolboxCorporativo.Infraestrutura.Identidade;
using ToolboxCorporativo.Infraestrutura.Rede;

namespace ToolboxCorporativo.App;

public sealed partial class MainWindow : Window
{
    private readonly ServicoConfiguracaoEndpoint configuracaoEndpoint;
    private readonly ServicoInicializacaoCliente inicializacao;
    private readonly ServicoAberturaRecursos abertura;
    private readonly ServicoDiagnosticoCliente diagnostico;

    public MainWindow(
        OnboardingViewModel onboarding,
        CatalogoViewModel catalogo,
        ServicoConfiguracaoEndpoint configuracaoEndpoint,
        ServicoInicializacaoCliente inicializacao,
        ServicoAberturaRecursos abertura,
        ServicoDiagnosticoCliente diagnostico)
    {
        InitializeComponent();
        Onboarding = onboarding;
        Catalogo = catalogo;
        this.configuracaoEndpoint = configuracaoEndpoint;
        this.inicializacao = inicializacao;
        this.abertura = abertura;
        this.diagnostico = diagnostico;
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

    private async void DiagnosticoClick(object sender, RoutedEventArgs e)
    {
        var dados = await diagnostico.ColetarAsync();
        Onboarding.AtualizarDiagnostico(
            $"Usuário: {dados.UsuarioDominio} | Computador: {dados.NomeComputador} | " +
            $"Windows: {dados.SistemaOperacional} | Configuração: {dados.VersaoConfiguracao?.ToString() ?? "nenhuma"} | Cache: {dados.RecursosEmCache} recurso(s)");
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
            Catalogo.RegistrarUso(recurso.Id);
        }
        catch (Exception excecao)
        {
            Onboarding.AtualizarErro(excecao);
            Onboarding.AtualizarStatus($"Não foi possível abrir {recurso.Nome}.");
        }
    }
    private void FavoritoClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Guid id })
            Catalogo.AlternarFavorito(id);
    }
    private void OcultarClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: RegistroRecursoCache recurso })
            Catalogo.AlternarOcultacao(recurso);
    }

    private void JanelaKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var controle = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        var alt = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        if ((e.Key == Windows.System.VirtualKey.K && controle) ||
            (e.Key == Windows.System.VirtualKey.Space && controle && alt))
        {
            Pesquisa.Focus(FocusState.Programmatic);
            e.Handled = true;
        }
    }


}
