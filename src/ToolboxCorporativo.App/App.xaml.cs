using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using ToolboxCorporativo.Infraestrutura.Cache;

namespace ToolboxCorporativo.App;

public partial class App : Application
{
    private readonly ServiceProvider servicos;
    private Window? janelaPrincipal;

    public App()
    {
        InitializeComponent();
        servicos = ConfiguracaoServicos.Criar();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        janelaPrincipal = servicos.GetRequiredService<MainWindow>();
        _ = servicos.GetRequiredService<ServicoInicializacaoCliente>().InicializarAsync();
        janelaPrincipal.Activate();
    }
}
