using System.Text.RegularExpressions;
using ToolboxCorporativo.Dominio.Entidades;
using ToolboxCorporativo.Dominio.Interfaces;

namespace ToolboxCorporativo.Dominio.Servicos;

public sealed partial class ServicoResolucaoVariaveis : IServicoResolucaoVariaveis
{
    private static readonly HashSet<string> VariaveisPermitidas = new(StringComparer.Ordinal)
    {
        "nomeUsuario",
        "dominio",
        "usuarioDominio",
        "nomeComputador",
        "perfilUsuario",
        "documentos",
        "areaTrabalho",
    };

    public IReadOnlyCollection<string> Validar(string modelo)
    {
        ArgumentNullException.ThrowIfNull(modelo);
        return ExpressaoVariavel()
            .Matches(modelo)
            .Select(captura => captura.Groups[1].Value)
            .Where(nome => !VariaveisPermitidas.Contains(nome))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    public string Resolver(
        string modelo,
        UsuarioWindows usuario,
        string perfilUsuario,
        string documentos,
        string areaTrabalho)
    {
        var desconhecidas = Validar(modelo);
        if (desconhecidas.Count > 0)
            throw new ArgumentException($"Variáveis desconhecidas: {string.Join(", ", desconhecidas)}.", nameof(modelo));

        var valores = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["nomeUsuario"] = usuario.NomeUsuario,
            ["dominio"] = usuario.Dominio,
            ["usuarioDominio"] = usuario.UsuarioDominio,
            ["nomeComputador"] = usuario.NomeComputador,
            ["perfilUsuario"] = perfilUsuario,
            ["documentos"] = documentos,
            ["areaTrabalho"] = areaTrabalho,
        };

        return ExpressaoVariavel().Replace(modelo, captura => valores[captura.Groups[1].Value]);
    }

    [GeneratedRegex(@"\{([^{}]+)\}", RegexOptions.CultureInvariant)]
    private static partial Regex ExpressaoVariavel();
}
