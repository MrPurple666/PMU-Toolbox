using ToolboxCorporativo.Dominio.Interfaces;
using ToolboxCorporativo.Infraestrutura.Persistencia;

namespace ToolboxCorporativo.Infraestrutura.Identidade;

public sealed record DiagnosticoCliente(
    string UsuarioDominio,
    string NomeComputador,
    string SistemaOperacional,
    string Arquitetura,
    int? VersaoConfiguracao,
    int RecursosEmCache);

public sealed class ServicoDiagnosticoCliente(
    IServicoIdentidadeUsuario identidade,
    ContextoBancoDados banco)
{
    public async Task<DiagnosticoCliente> ColetarAsync(CancellationToken tokenCancelamento = default)
    {
        var usuario = await identidade.ObterUsuarioAtualAsync(tokenCancelamento);
        var snapshot = await banco.LerSnapshotAsync(tokenCancelamento);
        return new DiagnosticoCliente(
            usuario.UsuarioDominio,
            usuario.NomeComputador,
            Environment.OSVersion.VersionString,
            System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
            snapshot?.Versao,
            snapshot?.Recursos.Count ?? 0);
    }
}
