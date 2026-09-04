using System.Net.Http;
using System.Security.Cryptography;

namespace ToolboxCorporativo.Infraestrutura.Rede;

public sealed class ServicoFaviconCache(HttpClient cliente, string? diretorio = null)
{
    private readonly string diretorio = diretorio ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ToolboxCorporativo", "favicons");

    public async Task<string?> ObterAsync(Uri origem, CancellationToken tokenCancelamento = default)
    {
        if (origem.Scheme is not ("http" or "https"))
            return null;
        Directory.CreateDirectory(diretorio);
        var nome = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(origem.Host.ToLowerInvariant()))) + ".ico";
        var caminho = Path.Combine(diretorio, nome);
        if (File.Exists(caminho))
            return caminho;
        try
        {
            var resposta = await cliente.GetAsync(new Uri(origem, "/favicon.ico"), tokenCancelamento);
            if (!resposta.IsSuccessStatusCode)
                return null;
            await using var origemFluxo = await resposta.Content.ReadAsStreamAsync(tokenCancelamento);
            await using var destino = File.Create(caminho);
            await origemFluxo.CopyToAsync(destino, tokenCancelamento);
            return caminho;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
