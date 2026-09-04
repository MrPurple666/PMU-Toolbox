using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using ToolboxCorporativo.Dominio.Entidades;
using ToolboxCorporativo.Dominio.Interfaces;
using ToolboxCorporativo.Infraestrutura.Api;
using ToolboxCorporativo.Infraestrutura.Persistencia;

namespace ToolboxCorporativo.Testes.Infraestrutura;

public sealed class SincronizacaoHttpTestes
{
    [Fact]
    public async Task RespostaDoServidorSemAtivoEPoliticaNumericaAtualizaCache()
    {
        var diretorio = Path.Combine(Path.GetTempPath(), $"toolbox-{Guid.NewGuid():N}");
        Directory.CreateDirectory(diretorio);
        var bancoArquivo = Path.Combine(diretorio, "cache.db");
        try
        {
            using var banco = new ContextoBancoDados(bancoArquivo);
            var endpoint = new ServicoConfiguracaoEndpoint(Path.Combine(diretorio, "conexao.json"));
            endpoint.Definir("http://servidor.local");
            using var cliente = new HttpClient(new RespostaFalsa()) { BaseAddress = endpoint.Endereco };
            var sincronizacao = new ServicoSincronizacaoHttp(cliente, endpoint, new IdentidadeFalsa(), banco, NullLogger<ServicoSincronizacaoHttp>.Instance);

            await sincronizacao.SincronizarAsync();

            var snapshot = await banco.LerSnapshotAsync();
            var recurso = Assert.Single(snapshot!.Recursos);
            Assert.Equal("Portal", recurso.Nome);
            Assert.True(recurso.Ativo);
            Assert.False(snapshot.Politica.PrecedenciaUsuarioSobreHerancaGrupo);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(diretorio, true);
        }
    }

    private sealed class IdentidadeFalsa : IServicoIdentidadeUsuario
    {
        public Task<UsuarioWindows> ObterUsuarioAtualAsync(CancellationToken tokenCancelamento = default) =>
            Task.FromResult(new UsuarioWindows(Guid.NewGuid(), "antonio", "Antônio", "LOCAL", "PC", "S-1-5-21"));
    }

    private sealed class RespostaFalsa : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken tokenCancelamento) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"versao":1,"recursos":[{"id":"11111111-1111-1111-1111-111111111111","nome":"Portal","tipo":"Web","destino":"https://portal.exemplo","obrigatorio":false,"ocultavel":true,"favoritavel":true}],"politicas":{"precedenciaUsuarioSobreHerancaGrupo":0,"intervaloSincronizacaoSegundos":300}}""", Encoding.UTF8, "application/json"),
            });
    }
}
