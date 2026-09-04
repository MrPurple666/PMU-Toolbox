using ToolboxCorporativo.Dominio.Entidades;
using ToolboxCorporativo.Dominio.Enumeracoes;
using ToolboxCorporativo.Dominio.Interfaces;
using ToolboxCorporativo.Dominio.Servicos;

namespace ToolboxCorporativo.Testes.Dominio;

public sealed class ResolucaoRecursosTestes
{
    private static readonly Guid IdUsuario = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid IdComputador = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid IdGrupo = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid IdSetorPai = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private static readonly Guid IdSetorFilho = Guid.Parse("00000000-0000-0000-0000-000000000005");
    private static readonly Guid IdRecurso = Guid.Parse("00000000-0000-0000-0000-000000000010");

    [Fact]
    public void ObrigatorioDeGrupoVenceDisponivelDeUsuarioSemPolitica()
    {
        var resultado = Resolver(
            new AtribuicaoRecurso { Id = Guid.NewGuid(), RecursoId = IdRecurso, TipoAlvo = TipoAlvoAtribuicao.GrupoInterno, AlvoId = IdGrupo, Estado = EstadoAtribuicao.Obrigatorio, Herdada = true },
            new AtribuicaoRecurso { Id = Guid.NewGuid(), RecursoId = IdRecurso, TipoAlvo = TipoAlvoAtribuicao.Usuario, AlvoId = IdUsuario, Estado = EstadoAtribuicao.Disponivel });

        Assert.Equal(EstadoAtribuicao.Obrigatorio, resultado.Estado);
    }

    [Fact]
    public void UsuarioExplicitoVenceHerancaQuandoPoliticaAtiva()
    {
        var contexto = CriarContexto(
            new AtribuicaoRecurso { Id = Guid.NewGuid(), RecursoId = IdRecurso, TipoAlvo = TipoAlvoAtribuicao.GrupoInterno, AlvoId = IdGrupo, Estado = EstadoAtribuicao.Obrigatorio, Herdada = true },
            new AtribuicaoRecurso { Id = Guid.NewGuid(), RecursoId = IdRecurso, TipoAlvo = TipoAlvoAtribuicao.Usuario, AlvoId = IdUsuario, Estado = EstadoAtribuicao.Disponivel });
        contexto = contexto with { Politica = new PoliticaRecursos(PrecedenciaUsuarioSobreHerancaGrupo: true) };

        var resultado = new ServicoResolucaoRecursos().Resolver(contexto, [CriarRecurso()]).Single();

        Assert.Equal(EstadoAtribuicao.Disponivel, resultado.Estado);
    }

    [Fact]
    public void BloqueioContinuaAbsolutoComPrecedenciaDoUsuario()
    {
        var contexto = CriarContexto(
            new AtribuicaoRecurso { Id = Guid.NewGuid(), RecursoId = IdRecurso, TipoAlvo = TipoAlvoAtribuicao.GrupoInterno, AlvoId = IdGrupo, Estado = EstadoAtribuicao.Bloqueado, Herdada = true },
            new AtribuicaoRecurso { Id = Guid.NewGuid(), RecursoId = IdRecurso, TipoAlvo = TipoAlvoAtribuicao.Usuario, AlvoId = IdUsuario, Estado = EstadoAtribuicao.Disponivel });
        contexto = contexto with { Politica = new PoliticaRecursos(PrecedenciaUsuarioSobreHerancaGrupo: true) };

        var resultado = new ServicoResolucaoRecursos().Resolver(contexto, [CriarRecurso()]).Single();

        Assert.Equal(EstadoAtribuicao.Bloqueado, resultado.Estado);
    }

    [Fact]
    public void SetorFilhoVenceSetorPaiNoMesmoEstado()
    {
        var resultado = Resolver(
            new AtribuicaoRecurso { Id = Guid.NewGuid(), RecursoId = IdRecurso, TipoAlvo = TipoAlvoAtribuicao.Setor, AlvoId = IdSetorPai, Estado = EstadoAtribuicao.Disponivel, Herdada = true },
            new AtribuicaoRecurso { Id = Guid.NewGuid(), RecursoId = IdRecurso, TipoAlvo = TipoAlvoAtribuicao.Setor, AlvoId = IdSetorFilho, Estado = EstadoAtribuicao.Obrigatorio, Herdada = true });

        Assert.Equal(IdSetorFilho, resultado.Origem!.AlvoId);
    }

    [Fact]
    public void RecursoInativoNaoEntraNoResultado()
    {
        var recurso = CriarRecurso() with { Ativo = false };

        var resultado = new ServicoResolucaoRecursos().Resolver(CriarContexto(), [recurso]);

        Assert.Empty(resultado);
    }

    private static ResultadoRecursoResolvido Resolver(params AtribuicaoRecurso[] atribuicoes) =>
        new ServicoResolucaoRecursos().Resolver(CriarContexto(atribuicoes), [CriarRecurso()]).Single();

    private static ContextoResolucaoRecursos CriarContexto(params AtribuicaoRecurso[] atribuicoes) => new()
    {
        Usuario = new UsuarioWindows(IdUsuario, "antonio", "Antônio", "DOMINIO", "PC-001", "S-1-5-21"),
        Computador = new Computador(IdComputador, "PC-001", "Windows 11"),
        GruposInternos = new HashSet<Guid> { IdGrupo },
        SetoresDoPaiAoFilho = [IdSetorPai, IdSetorFilho],
        Atribuicoes = atribuicoes,
    };

    private static RecursoCatalogo CriarRecurso() => new()
    {
        Id = IdRecurso,
        Nome = "Portal",
        Tipo = TipoRecurso.Web,
    };
}

public sealed class ResolucaoVariaveisTestes
{
    [Fact]
    public void ResolveVariaveisPermitidas()
    {
        var usuario = new UsuarioWindows(Guid.NewGuid(), "antonio", "Antônio", "DOMINIO", "PC-001", "S-1");
        var servico = new ServicoResolucaoVariaveis();

        var resultado = servico.Resolver(@"\\arquivos\{nomeUsuario}\{usuarioDominio}\{documentos}", usuario, "Usuario", "C:\\Users\\antonio\\Documents", "C:\\Users\\antonio\\Desktop");

        Assert.Equal(@"\\arquivos\antonio\DOMINIO\antonio\C:\Users\antonio\Documents", resultado);
    }

    [Fact]
    public void VariavelDesconhecidaFalhaNaValidacao()
    {
        var servico = new ServicoResolucaoVariaveis();

        Assert.Contains("naoExiste", servico.Validar("{naoExiste}"));
        Assert.Throws<ArgumentException>(() => servico.Resolver("{naoExiste}", new UsuarioWindows(Guid.NewGuid(), "a", "A", "D", "P", "S"), "U", "D", "A"));
    }
}
