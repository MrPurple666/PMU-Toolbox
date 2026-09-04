using ToolboxCorporativo.Dominio.Enumeracoes;

namespace ToolboxCorporativo.Dominio.Entidades;

public sealed record UsuarioWindows(
    Guid Id,
    string NomeUsuario,
    string NomeExibicao,
    string Dominio,
    string NomeComputador,
    string Sid)
{
    public string UsuarioDominio => $"{Dominio}\\{NomeUsuario}";
}

public sealed record Computador(Guid Id, string Nome, string VersaoWindows);

public sealed record Categoria(Guid Id, string Nome);

public sealed record GrupoInterno(Guid Id, string Nome);

public sealed record Setor(Guid Id, string Nome, Guid? SetorPaiId);

public sealed record PreferenciaUsuario(Guid UsuarioId, Guid RecursoId, bool Favorito, bool Oculto);

public sealed record PoliticaRecursos(
    bool PrecedenciaUsuarioSobreHerancaGrupo = false,
    bool PermitirOcultacao = true,
    TimeSpan? IntervaloSincronizacao = null);

public abstract record RecursoToolbox
{
    public required Guid Id { get; init; }
    public required string Nome { get; init; }
    public string? Descricao { get; init; }
    public required TipoRecurso Tipo { get; init; }
    public string Destino { get; init; } = string.Empty;
    public Guid? CategoriaId { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<string> Aliases { get; init; } = [];
    public bool Ativo { get; init; } = true;
    public bool Obrigatorio { get; init; }
    public bool Ocultavel { get; init; } = true;
    public bool Favoritavel { get; init; } = true;

    public void Validar()
    {
        if (Id == Guid.Empty) throw new ArgumentException("O recurso deve possuir um identificador.", nameof(Id));
        if (string.IsNullOrWhiteSpace(Nome)) throw new ArgumentException("O recurso deve possuir um nome.", nameof(Nome));
    }
}

public sealed record RecursoCatalogo : RecursoToolbox;

public sealed record AtribuicaoRecurso
{
    public required Guid Id { get; init; }
    public required Guid RecursoId { get; init; }
    public required TipoAlvoAtribuicao TipoAlvo { get; init; }
    public Guid? AlvoId { get; init; }
    public required EstadoAtribuicao Estado { get; init; }
    public bool Herdada { get; init; }

    public void Validar()
    {
        if (Id == Guid.Empty || RecursoId == Guid.Empty)
            throw new ArgumentException("A atribuição deve possuir identificadores válidos.");
        if (TipoAlvo == TipoAlvoAtribuicao.Todos && AlvoId is not null)
            throw new ArgumentException("A atribuição para todos não possui alvo.");
        if (TipoAlvo != TipoAlvoAtribuicao.Todos && AlvoId is null)
            throw new ArgumentException("A atribuição exige um alvo.");
    }
}
