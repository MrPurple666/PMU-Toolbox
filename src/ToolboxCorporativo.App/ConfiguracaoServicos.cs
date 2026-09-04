using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ToolboxCorporativo.Aplicacao.ViewModels;
using ToolboxCorporativo.Dominio.Interfaces;
using ToolboxCorporativo.Dominio.Servicos;
using ToolboxCorporativo.Infraestrutura.Api;
using ToolboxCorporativo.Infraestrutura.Cache;
using ToolboxCorporativo.Infraestrutura.Identidade;
using ToolboxCorporativo.Infraestrutura.Persistencia;
using ToolboxCorporativo.Infraestrutura.Notificacoes;
using ToolboxCorporativo.Infraestrutura.Rede;
namespace ToolboxCorporativo.App;

internal static class ConfiguracaoServicos
{
    public static ServiceProvider Criar()
    {
        var servicos = new ServiceCollection();
        servicos.AddLogging(configuracao => configuracao.AddConsole());
        servicos.AddSingleton<IServicoIdentidadeUsuario, ServicoIdentidadeWindows>();
        servicos.AddSingleton<CatalogoViewModel>();
        servicos.AddSingleton<ServicoPreferenciasCliente>();
        servicos.AddSingleton<ServicoDiagnosticoCliente>();
        servicos.AddHttpClient<ServicoFaviconCache>();
        servicos.AddSingleton<IServicoNotificacoes, ServicoNotificacoes>();
        servicos.AddSingleton<IServicoResolucaoRecursos, ServicoResolucaoRecursos>();
        servicos.AddSingleton<IServicoResolucaoVariaveis, ServicoResolucaoVariaveis>();
        servicos.AddSingleton<ValidadorAberturaRecursos>();
        servicos.AddSingleton<ServicoAberturaRecursos>();
        servicos.AddSingleton<ContextoBancoDados>();
        servicos.AddHttpClient<IServicoSincronizacao, ServicoSincronizacaoHttp>(cliente =>
            cliente.BaseAddress = new Uri("http://127.0.0.1:8080/"))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseProxy = false,
            });
        servicos.AddSingleton<ServicoInicializacaoCliente>();
        servicos.AddTransient<OnboardingViewModel>();
        servicos.AddTransient<MainWindow>();
        return servicos.BuildServiceProvider();
    }
}
