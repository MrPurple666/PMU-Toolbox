using CommunityToolkit.Mvvm.ComponentModel;
using ToolboxCorporativo.Infraestrutura.Persistencia;

namespace ToolboxCorporativo.App;

public sealed class CatalogoViewModel(ServicoPreferenciasCliente preferencias) : ObservableObject
{
    private readonly HashSet<Guid> favoritos = preferencias.LerFavoritos();
    private IReadOnlyList<RegistroRecursoCache> recursos = [];
    private string textoPesquisa = string.Empty;

    public IReadOnlyList<RegistroRecursoCache> Recursos
    {
        get => recursos;
        private set => SetProperty(ref recursos, value);
    }

    public string TextoPesquisa
    {
        get => textoPesquisa;
        set
        {
            if (SetProperty(ref textoPesquisa, value))
            {
                OnPropertyChanged(nameof(RecursosVisiveis));
                OnPropertyChanged(nameof(Mensagem));
            }
        }
    }
    public string Mensagem =>
        RecursosVisiveis.Count == 0
            ? string.IsNullOrWhiteSpace(TextoPesquisa)
                ? "Nenhum recurso disponível. Inicie o servidor e sincronize novamente."
                : "Nenhum recurso corresponde à pesquisa."
            : $"{RecursosVisiveis.Count} recurso(s) encontrado(s).";

    public IReadOnlyList<RegistroRecursoCache> RecursosVisiveis =>
        string.IsNullOrWhiteSpace(TextoPesquisa)
            ? Recursos
            : Recursos.Where(recurso =>
                recurso.Nome.Contains(TextoPesquisa, StringComparison.OrdinalIgnoreCase) ||
                (recurso.Descricao?.Contains(TextoPesquisa, StringComparison.OrdinalIgnoreCase) ?? false) ||
                recurso.Destino.Contains(TextoPesquisa, StringComparison.OrdinalIgnoreCase) ||
                recurso.TagsJson.Contains(TextoPesquisa, StringComparison.OrdinalIgnoreCase) ||
                recurso.AliasesJson.Contains(TextoPesquisa, StringComparison.OrdinalIgnoreCase)).ToArray();

    public void AplicarSnapshot(SnapshotConfiguracao snapshot)
    {
        Recursos = snapshot.Recursos.Where(recurso => recurso.Ativo).ToArray();
        OnPropertyChanged(nameof(RecursosVisiveis));
        OnPropertyChanged(nameof(Mensagem));
    }

    public bool AlternarFavorito(Guid recursoId)
    {
        if (!favoritos.Add(recursoId))
            favoritos.Remove(recursoId);
        preferencias.SalvarFavoritos(favoritos);
        OnPropertyChanged(nameof(RecursosVisiveis));
        return favoritos.Contains(recursoId);
    }

    public bool EhFavorito(Guid recursoId) => favoritos.Contains(recursoId);

    public IReadOnlyList<RegistroRecursoCache> QuickLauncher(string texto, int limite = 10) =>
        Recursos.Where(recurso =>
            recurso.Nome.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
            recurso.Destino.Contains(texto, StringComparison.OrdinalIgnoreCase)).Take(limite).ToArray();
}
