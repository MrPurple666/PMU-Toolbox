using ToolboxCorporativo.Dominio.Entidades;
using ToolboxCorporativo.Dominio.Enumeracoes;
using ToolboxCorporativo.Infraestrutura.Rede;

namespace ToolboxCorporativo.Testes.Infraestrutura;

public sealed class AberturaRecursosTestes
{
    [Fact]
    public void ComandoControladoNaoAceitaTextoExecutavel()
    {
        var recurso = new RecursoCatalogo { Id = Guid.NewGuid(), Nome = "Comando", Tipo = TipoRecurso.ComandoControlado };

        Assert.Throws<InvalidOperationException>(() => new ValidadorAberturaRecursos().Validar(recurso, "cmd.exe /c whoami"));
    }

    [Fact]
    public void UrlHttpsEPermitida()
    {
        var recurso = new RecursoCatalogo { Id = Guid.NewGuid(), Nome = "Portal", Tipo = TipoRecurso.Web };

        var resultado = new ValidadorAberturaRecursos().Validar(recurso, "https://exemplo.local/portal");

        Assert.Equal("https://exemplo.local/portal", resultado);
    }
}
