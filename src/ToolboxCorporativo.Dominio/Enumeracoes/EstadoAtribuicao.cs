namespace ToolboxCorporativo.Dominio.Enumeracoes;

public enum EstadoAtribuicao
{
    Herdado,
    Disponivel,
    Obrigatorio,
    Bloqueado,
}

public enum TipoAlvoAtribuicao
{
    Todos,
    Usuario,
    GrupoInterno,
    GrupoLdap,
    Setor,
    Computador,
    ConjuntoComputadores,
}
