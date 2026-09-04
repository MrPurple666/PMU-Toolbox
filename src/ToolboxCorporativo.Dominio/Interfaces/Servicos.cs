using ToolboxCorporativo.Dominio.Entidades;

namespace ToolboxCorporativo.Dominio.Interfaces;

public interface IServicoResolucaoVariaveis
{
    IReadOnlyCollection<string> Validar(string modelo);
    string Resolver(string modelo, UsuarioWindows usuario, string perfilUsuario, string documentos, string areaTrabalho);
}

public interface IServicoIdentidadeUsuario
{
    Task<UsuarioWindows> ObterUsuarioAtualAsync(CancellationToken tokenCancelamento = default);
}

public interface IServicoSincronizacao
{
    Task SincronizarAsync(CancellationToken tokenCancelamento = default);
}

public interface IRepositorioRecursos
{
    Task<IReadOnlyList<RecursoToolbox>> ListarAtivosAsync(CancellationToken tokenCancelamento = default);
}

public interface IRepositorioAtribuicoes
{
    Task<IReadOnlyList<AtribuicaoRecurso>> ListarAsync(CancellationToken tokenCancelamento = default);
}
