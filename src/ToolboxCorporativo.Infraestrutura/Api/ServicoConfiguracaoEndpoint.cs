using System.Text.Json;

namespace ToolboxCorporativo.Infraestrutura.Api;

public sealed class ServicoConfiguracaoEndpoint
{
    private readonly string caminho = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ToolboxCorporativo", "conexao.json");

    public ServicoConfiguracaoEndpoint()
    {
        Endereco = LerEndereco();
    }

    public Uri Endereco { get; private set; }

    public void Definir(string endereco)
    {
        if (!Uri.TryCreate(endereco, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Informe uma URL HTTP ou HTTPS válida.", nameof(endereco));

        Endereco = new Uri(uri.GetLeftPart(UriPartial.Authority) + "/");
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        File.WriteAllText(caminho, JsonSerializer.Serialize(new ConfiguracaoPersistida(Endereco.AbsoluteUri)));
    }

    private Uri LerEndereco()
    {
        try
        {
            var configuracao = JsonSerializer.Deserialize<ConfiguracaoPersistida>(File.ReadAllText(caminho));
            if (configuracao is not null && Uri.TryCreate(configuracao.UrlApi, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                return new Uri(uri.GetLeftPart(UriPartial.Authority) + "/");
        }
        catch (IOException) { }
        catch (JsonException) { }

        return new Uri("http://127.0.0.1:8080/");
    }

    private sealed record ConfiguracaoPersistida(string UrlApi);
}
