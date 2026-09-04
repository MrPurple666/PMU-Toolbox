using System.Diagnostics;
using ToolboxCorporativo.Dominio.Entidades;
using ToolboxCorporativo.Dominio.Enumeracoes;

namespace ToolboxCorporativo.Infraestrutura.Rede;

public sealed class ValidadorAberturaRecursos
{
    private readonly HashSet<string> extensoesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".lnk", ".msc",
    };

    private readonly HashSet<string> aplicativosPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "mstsc.exe", "control.exe", "compmgmt.msc",
    };

    public string Validar(RecursoToolbox recurso, string destino)
    {
        ArgumentNullException.ThrowIfNull(recurso);
        if (string.IsNullOrWhiteSpace(destino))
            throw new ArgumentException("O destino do recurso é obrigatório.", nameof(destino));

        return recurso.Tipo switch
        {
            TipoRecurso.Web => ValidarWeb(destino),
            TipoRecurso.PastaRede => ValidarCaminhoRede(destino),
            TipoRecurso.PastaLocal or TipoRecurso.Documento => ValidarCaminhoLocal(destino),
            TipoRecurso.Aplicacao or TipoRecurso.FerramentaWindows => ValidarAplicacao(destino),
            TipoRecurso.ComandoControlado => throw new InvalidOperationException(
                "Comandos controlados exigem um identificador registrado pelo servidor."),
            TipoRecurso.GrupoDeRecursos => throw new InvalidOperationException(
                "Grupos de recursos não são diretamente executáveis."),
            _ => throw new ArgumentOutOfRangeException(nameof(recurso), "Tipo de recurso desconhecido."),
        };
    }

    private static string ValidarWeb(string destino)
    {
        if (!Uri.TryCreate(destino, UriKind.Absolute, out var uri) ||
            (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A URL deve usar HTTP ou HTTPS.");
        return uri.AbsoluteUri;
    }

    private static string ValidarCaminhoRede(string destino)
    {
        if (!destino.StartsWith(@"\\", StringComparison.Ordinal) || destino.Contains('\0'))
            throw new InvalidOperationException("O caminho de rede deve ser UNC.");
        return destino;
    }

    private static string ValidarCaminhoLocal(string destino)
    {
        if (!Path.IsPathFullyQualified(destino) || destino.Contains('\0'))
            throw new InvalidOperationException("O caminho local deve ser absoluto.");
        return Path.GetFullPath(destino);
    }

    private string ValidarAplicacao(string destino)
    {
        var nome = Path.GetFileName(destino);
        if (!extensoesPermitidas.Contains(Path.GetExtension(nome)) || !aplicativosPermitidos.Contains(nome))
            throw new InvalidOperationException("Aplicação fora da lista permitida.");
        return nome;
    }
}

public sealed class ServicoAberturaRecursos(ValidadorAberturaRecursos validador)
{
    public Task AbrirAsync(RecursoToolbox recurso, string destino, CancellationToken tokenCancelamento = default)
    {
        var destinoValidado = validador.Validar(recurso, destino);
        tokenCancelamento.ThrowIfCancellationRequested();
        var nomeArquivo = recurso.Tipo switch
        {
            TipoRecurso.PastaRede or TipoRecurso.PastaLocal or TipoRecurso.Documento => "explorer.exe",
            _ => destinoValidado,
        };
        var argumentos = nomeArquivo == "explorer.exe" ? destinoValidado : string.Empty;

        return Task.Run(() =>
        {
            tokenCancelamento.ThrowIfCancellationRequested();
            Process.Start(new ProcessStartInfo
            {
                FileName = nomeArquivo,
                Arguments = argumentos,
                UseShellExecute = true,
            });
        }, tokenCancelamento);
    }
}
