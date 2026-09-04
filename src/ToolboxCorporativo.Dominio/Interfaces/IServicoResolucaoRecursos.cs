using ToolboxCorporativo.Dominio.Entidades;
using ToolboxCorporativo.Dominio.Enumeracoes;

namespace ToolboxCorporativo.Dominio.Interfaces;

public sealed record ContextoResolucaoRecursos
{
    public required UsuarioWindows Usuario { get; init; }
    public IReadOnlySet<Guid> GruposInternos { get; init; } = new HashSet<Guid>();
    public IReadOnlySet<Guid> GruposLdap { get; init; } = new HashSet<Guid>();
    public IReadOnlyList<Guid> SetoresDoPaiAoFilho { get; init; } = [];
    public required Computador Computador { get; init; }
    public IReadOnlySet<Guid> ConjuntosComputadores { get; init; } = new HashSet<Guid>();
    public IReadOnlyCollection<AtribuicaoRecurso> Atribuicoes { get; init; } = [];
    public PoliticaRecursos Politica { get; init; } = new();
}

public sealed record ResultadoRecursoResolvido(
    RecursoToolbox Recurso,
    EstadoAtribuicao Estado,
    AtribuicaoRecurso? Origem,
    bool PodePersonalizar);

public interface IServicoResolucaoRecursos
{
    IReadOnlyList<ResultadoRecursoResolvido> Resolver(ContextoResolucaoRecursos contexto, IEnumerable<RecursoToolbox> recursos);
}
