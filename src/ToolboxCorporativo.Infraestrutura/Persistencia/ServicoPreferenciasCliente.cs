using System.Text.Json;

namespace ToolboxCorporativo.Infraestrutura.Persistencia;

public sealed class ServicoPreferenciasCliente(string? caminho = null)
{
    private readonly string caminho = caminho ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ToolboxCorporativo", "preferencias.json");

    public HashSet<Guid> LerFavoritos()
    {
        try
        {
            var favoritos = JsonSerializer.Deserialize<HashSet<Guid>>(File.ReadAllText(caminho));
            return favoritos ?? [];
        }
        catch (IOException)
        {
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public void SalvarFavoritos(IEnumerable<Guid> favoritos)
    {
        var diretorio = Path.GetDirectoryName(caminho);
        if (!string.IsNullOrEmpty(diretorio))
            Directory.CreateDirectory(diretorio);
        var temporario = caminho + ".tmp";
        File.WriteAllText(temporario, JsonSerializer.Serialize(favoritos.Distinct().OrderBy(id => id)));
        File.Move(temporario, caminho, true);
    }
}
