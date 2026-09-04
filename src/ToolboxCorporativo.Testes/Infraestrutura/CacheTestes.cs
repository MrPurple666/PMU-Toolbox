using Microsoft.Data.Sqlite;
using ToolboxCorporativo.Dominio.Enumeracoes;
using ToolboxCorporativo.Infraestrutura.Persistencia;

namespace ToolboxCorporativo.Testes.Infraestrutura;

public sealed class CacheTestes
{
    [Fact]
    public async Task VersaoRegressivaPreservaSnapshotValido()
    {
        var caminho = Path.Combine(Path.GetTempPath(), $"toolbox-{Guid.NewGuid():N}.db");
        try
        {
            using var banco = new ContextoBancoDados(caminho);
            var primeiro = new SnapshotConfiguracao(
                1,
                DateTimeOffset.UtcNow,
                [new RegistroRecursoCache(Guid.NewGuid(), "Portal", null, TipoRecurso.Web, "https://exemplo.local", true, false, true, true)],
                new());

            Assert.True(await banco.SubstituirSnapshotAsync(primeiro));
            Assert.False(await banco.SubstituirSnapshotAsync(primeiro with { Versao = 0, Recursos = [] }));

            var lido = await banco.LerSnapshotAsync();
            Assert.Equal("Portal", Assert.Single(lido!.Recursos).Nome);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(caminho);
        }
    }
}
