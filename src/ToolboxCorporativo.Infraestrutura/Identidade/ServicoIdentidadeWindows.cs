using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using ToolboxCorporativo.Dominio.Entidades;
using ToolboxCorporativo.Dominio.Interfaces;

namespace ToolboxCorporativo.Infraestrutura.Identidade;

public sealed class ServicoIdentidadeWindows : IServicoIdentidadeUsuario
{
    public Task<UsuarioWindows> ObterUsuarioAtualAsync(CancellationToken tokenCancelamento = default)
    {
        tokenCancelamento.ThrowIfCancellationRequested();
        using var identidade = WindowsIdentity.GetCurrent();
        var nomeExibicao = ObterNomeExibicaoSeguro();
        var sid = identidade.User?.Value ?? string.Empty;

        return Task.FromResult(new UsuarioWindows(
            Guid.Empty,
            Environment.UserName,
            nomeExibicao,
            Environment.UserDomainName,
            Environment.MachineName,
            sid));
    }

    private static string ObterNomeExibicaoSeguro()
    {
        try
        {
            return UserPrincipal.Current.DisplayName ?? Environment.UserName;
        }
        catch (PrincipalException)
        {
            return Environment.UserName;
        }
        catch (UnauthorizedAccessException)
        {
            return Environment.UserName;
        }
    }
}
