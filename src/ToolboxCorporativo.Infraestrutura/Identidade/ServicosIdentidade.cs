using ToolboxCorporativo.Dominio.Entidades;
using ToolboxCorporativo.Dominio.Interfaces;

namespace ToolboxCorporativo.Infraestrutura.Identidade;

public sealed class ServicoIdentidadeSimulada(UsuarioWindows usuario) : IServicoIdentidadeUsuario
{
    public Task<UsuarioWindows> ObterUsuarioAtualAsync(CancellationToken tokenCancelamento = default)
    {
        tokenCancelamento.ThrowIfCancellationRequested();
        return Task.FromResult(usuario);
    }
}

public sealed class ServicoIdentidadeLdap : IServicoIdentidadeUsuario
{
    public Task<UsuarioWindows> ObterUsuarioAtualAsync(CancellationToken tokenCancelamento = default) =>
        Task.FromException<UsuarioWindows>(new NotSupportedException(
            "A identidade LDAP não está habilitada no MVP; use a identidade Windows."));
}
