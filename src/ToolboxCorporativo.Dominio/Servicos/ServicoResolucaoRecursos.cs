using ToolboxCorporativo.Dominio.Entidades;
using ToolboxCorporativo.Dominio.Enumeracoes;
using ToolboxCorporativo.Dominio.Interfaces;

namespace ToolboxCorporativo.Dominio.Servicos;

public sealed class ServicoResolucaoRecursos : IServicoResolucaoRecursos
{
    public IReadOnlyList<ResultadoRecursoResolvido> Resolver(
        ContextoResolucaoRecursos contexto,
        IEnumerable<RecursoToolbox> recursos)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(recursos);

        var atribuicoes = contexto.Atribuicoes.ToArray();
        foreach (var atribuicao in atribuicoes)
            atribuicao.Validar();

        return recursos
            .Where(recurso => recurso.Ativo)
            .OrderBy(recurso => recurso.Nome, StringComparer.OrdinalIgnoreCase)
            .ThenBy(recurso => recurso.Id)
            .Select(recurso => ResolverRecurso(recurso, contexto, atribuicoes))
            .ToArray();
    }

    private static ResultadoRecursoResolvido ResolverRecurso(
        RecursoToolbox recurso,
        ContextoResolucaoRecursos contexto,
        IReadOnlyCollection<AtribuicaoRecurso> atribuicoes)
    {
        var candidatas = atribuicoes
            .Where(atribuicao => atribuicao.RecursoId == recurso.Id && EhAplicavel(atribuicao, contexto))
            .ToArray();

        if (candidatas.Length == 0)
            return new ResultadoRecursoResolvido(recurso, EstadoAtribuicao.Herdado, null, false);

        var bloqueios = candidatas.Where(atribuicao => atribuicao.Estado == EstadoAtribuicao.Bloqueado);
        var selecionaveis = bloqueios.Any()
            ? bloqueios
            : AplicarPrecedenciaDoUsuario(candidatas, contexto.Politica);

        var origem = selecionaveis
            .OrderByDescending(atribuicao => OrdemEstado(atribuicao.Estado))
            .ThenByDescending(atribuicao => OrdemEscopo(atribuicao, contexto))
            .ThenBy(atribuicao => atribuicao.Id)
            .First();

        return new ResultadoRecursoResolvido(
            recurso,
            origem.Estado,
            origem,
            origem.Estado == EstadoAtribuicao.Disponivel);
    }

    private static IEnumerable<AtribuicaoRecurso> AplicarPrecedenciaDoUsuario(
        IEnumerable<AtribuicaoRecurso> candidatas,
        PoliticaRecursos politica)
    {
        var materializadas = candidatas.ToArray();
        if (!politica.PrecedenciaUsuarioSobreHerancaGrupo)
            return materializadas;

        var possuiUsuarioExplicito = materializadas.Any(atribuicao =>
            atribuicao.TipoAlvo == TipoAlvoAtribuicao.Usuario &&
            !atribuicao.Herdada &&
            atribuicao.Estado != EstadoAtribuicao.Bloqueado);

        return possuiUsuarioExplicito
            ? materializadas.Where(atribuicao => !EhHerancaSubstituivel(atribuicao))
            : materializadas;
    }

    private static bool EhHerancaSubstituivel(AtribuicaoRecurso atribuicao) =>
        atribuicao.Herdada || atribuicao.TipoAlvo is
            TipoAlvoAtribuicao.Todos or
            TipoAlvoAtribuicao.Setor or
            TipoAlvoAtribuicao.GrupoInterno or
            TipoAlvoAtribuicao.GrupoLdap;

    private static bool EhAplicavel(AtribuicaoRecurso atribuicao, ContextoResolucaoRecursos contexto) =>
        atribuicao.TipoAlvo switch
        {
            TipoAlvoAtribuicao.Todos => atribuicao.AlvoId is null,
            TipoAlvoAtribuicao.Usuario => atribuicao.AlvoId == contexto.Usuario.Id,
            TipoAlvoAtribuicao.GrupoInterno => atribuicao.AlvoId is Guid id && contexto.GruposInternos.Contains(id),
            TipoAlvoAtribuicao.GrupoLdap => atribuicao.AlvoId is Guid id && contexto.GruposLdap.Contains(id),
            TipoAlvoAtribuicao.Setor => atribuicao.AlvoId is Guid id && contexto.SetoresDoPaiAoFilho.Contains(id),
            TipoAlvoAtribuicao.Computador => atribuicao.AlvoId == contexto.Computador.Id,
            TipoAlvoAtribuicao.ConjuntoComputadores => atribuicao.AlvoId is Guid id && contexto.ConjuntosComputadores.Contains(id),
            _ => false,
        };

    private static int OrdemEstado(EstadoAtribuicao estado) => estado switch
    {
        EstadoAtribuicao.Bloqueado => 3,
        EstadoAtribuicao.Obrigatorio => 2,
        EstadoAtribuicao.Disponivel => 1,
        _ => 0,
    };

    private static int OrdemEscopo(AtribuicaoRecurso atribuicao, ContextoResolucaoRecursos contexto)
    {
        var ordemBase = atribuicao.TipoAlvo switch
        {
            TipoAlvoAtribuicao.Todos => 0,
            TipoAlvoAtribuicao.Setor => 1,
            TipoAlvoAtribuicao.GrupoInterno or TipoAlvoAtribuicao.GrupoLdap => 2,
            TipoAlvoAtribuicao.ConjuntoComputadores => 3,
            TipoAlvoAtribuicao.Computador => 4,
            TipoAlvoAtribuicao.Usuario => 5,
            _ => -1,
        };

        if (atribuicao.TipoAlvo != TipoAlvoAtribuicao.Setor || atribuicao.AlvoId is not Guid setorId)
            return ordemBase * 1000;

        var profundidade = contexto.SetoresDoPaiAoFilho
            .Select((id, indice) => (id, indice))
            .FirstOrDefault(item => item.id == setorId)
            .indice;
        return ordemBase * 1000 + Math.Max(profundidade, 0);
    }
}
