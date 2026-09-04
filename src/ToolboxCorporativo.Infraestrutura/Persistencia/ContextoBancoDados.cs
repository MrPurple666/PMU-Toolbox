using Microsoft.Data.Sqlite;
using System.Data.Common;
using ToolboxCorporativo.Dominio.Entidades;
using ToolboxCorporativo.Dominio.Enumeracoes;

namespace ToolboxCorporativo.Infraestrutura.Persistencia;

public sealed record RegistroRecursoCache(
    Guid Id,
    string Nome,
    string? Descricao,
    TipoRecurso Tipo,
    string Destino,
    bool Ativo,
    bool Obrigatorio,
    bool Ocultavel,
    bool Favoritavel,
    string TagsJson = "[]",
    string AliasesJson = "[]");

public sealed record SnapshotConfiguracao(
    int Versao,
    DateTimeOffset UltimaSincronizacao,
    IReadOnlyCollection<RegistroRecursoCache> Recursos,
    PoliticaRecursos Politica);

public sealed class ContextoBancoDados : IDisposable
{
    private const int VersaoEsquema = 1;
    private readonly string caminho;
    private readonly SemaphoreSlim bloqueio = new(1, 1);

    public ContextoBancoDados(string? caminho = null)
    {
        this.caminho = caminho ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ToolboxCorporativo",
            "toolbox.db");
    }

    public async Task InicializarAsync(CancellationToken tokenCancelamento = default)
    {
        await bloqueio.WaitAsync(tokenCancelamento);
        try
        {
            await using var conexao = await AbrirConexaoAsync(tokenCancelamento);
            await using var comando = conexao.CreateCommand();
            comando.CommandText = """
                CREATE TABLE IF NOT EXISTS metadados (
                    chave TEXT PRIMARY KEY,
                    valor TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS recursos (
                    id TEXT PRIMARY KEY,
                    nome TEXT NOT NULL,
                    descricao TEXT,
                    tipo INTEGER NOT NULL,
                    destino TEXT NOT NULL,
                    ativo INTEGER NOT NULL,
                    obrigatorio INTEGER NOT NULL,
                    ocultavel INTEGER NOT NULL,
                    favoritavel INTEGER NOT NULL,
                    tags_json TEXT NOT NULL,
                    aliases_json TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS politicas (
                    id INTEGER PRIMARY KEY CHECK (id = 1),
                    precedencia_usuario INTEGER NOT NULL,
                    permitir_ocultacao INTEGER NOT NULL,
                    intervalo_sincronizacao_segundos INTEGER
                );
                INSERT INTO metadados (chave, valor)
                VALUES ('versao_esquema', $versao)
                ON CONFLICT(chave) DO UPDATE SET valor = excluded.valor;
                """;
            comando.Parameters.AddWithValue("$versao", VersaoEsquema.ToString());
            await comando.ExecuteNonQueryAsync(tokenCancelamento);
        }
        finally
        {
            bloqueio.Release();
        }
    }

    public async Task<SnapshotConfiguracao?> LerSnapshotAsync(CancellationToken tokenCancelamento = default)
    {
        await InicializarAsync(tokenCancelamento);
        await bloqueio.WaitAsync(tokenCancelamento);
        try
        {
            await using var conexao = await AbrirConexaoAsync(tokenCancelamento);
            var versao = await LerInteiroAsync(conexao, "versao_configuracao", tokenCancelamento) ?? 0;
            var dataTexto = await LerTextoAsync(conexao, "ultima_sincronizacao", tokenCancelamento);
            if (dataTexto is null)
                return null;

            var politica = await LerPoliticaAsync(conexao, tokenCancelamento);
            var recursos = new List<RegistroRecursoCache>();
            await using var comando = conexao.CreateCommand();
            comando.CommandText = "SELECT id, nome, descricao, tipo, destino, ativo, obrigatorio, ocultavel, favoritavel, tags_json, aliases_json FROM recursos ORDER BY nome, id";
            await using var leitor = await comando.ExecuteReaderAsync(tokenCancelamento);
            while (await leitor.ReadAsync(tokenCancelamento))
            {
                recursos.Add(new RegistroRecursoCache(
                    Guid.Parse(leitor.GetString(0)),
                    leitor.GetString(1),
                    leitor.IsDBNull(2) ? null : leitor.GetString(2),
                    (TipoRecurso)leitor.GetInt32(3),
                    leitor.GetString(4),
                    leitor.GetBoolean(5),
                    leitor.GetBoolean(6),
                    leitor.GetBoolean(7),
                    leitor.GetBoolean(8),
                    leitor.GetString(9),
                    leitor.GetString(10)));
            }

            return new SnapshotConfiguracao(versao, DateTimeOffset.Parse(dataTexto), recursos, politica);
        }
        finally
        {
            bloqueio.Release();
        }
    }

    public async Task<bool> SubstituirSnapshotAsync(SnapshotConfiguracao snapshot, CancellationToken tokenCancelamento = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Versao < 0)
            throw new ArgumentOutOfRangeException(nameof(snapshot), "A versão não pode ser negativa.");

        await InicializarAsync(tokenCancelamento);
        await bloqueio.WaitAsync(tokenCancelamento);
        try
        {
            await using var conexao = await AbrirConexaoAsync(tokenCancelamento);
            var versaoAtual = await LerInteiroAsync(conexao, "versao_configuracao", tokenCancelamento) ?? 0;
            if (snapshot.Versao <= versaoAtual && await LerTextoAsync(conexao, "ultima_sincronizacao", tokenCancelamento) is not null)
                return false;

            await using var transacao = (SqliteTransaction)await conexao.BeginTransactionAsync(tokenCancelamento);
            await ExecutarAsync(conexao, transacao, "DELETE FROM recursos", tokenCancelamento);
            foreach (var recurso in snapshot.Recursos)
            {
                await using var comando = conexao.CreateCommand();
                comando.Transaction = transacao;
                comando.CommandText = """
                    INSERT INTO recursos (id, nome, descricao, tipo, destino, ativo, obrigatorio, ocultavel, favoritavel, tags_json, aliases_json)
                    VALUES ($id, $nome, $descricao, $tipo, $destino, $ativo, $obrigatorio, $ocultavel, $favoritavel, $tags, $aliases)
                    """;
                comando.Parameters.AddWithValue("$id", recurso.Id.ToString("D"));
                comando.Parameters.AddWithValue("$nome", recurso.Nome);
                comando.Parameters.AddWithValue("$descricao", (object?)recurso.Descricao ?? DBNull.Value);
                comando.Parameters.AddWithValue("$tipo", (int)recurso.Tipo);
                comando.Parameters.AddWithValue("$destino", recurso.Destino);
                comando.Parameters.AddWithValue("$ativo", recurso.Ativo);
                comando.Parameters.AddWithValue("$obrigatorio", recurso.Obrigatorio);
                comando.Parameters.AddWithValue("$ocultavel", recurso.Ocultavel);
                comando.Parameters.AddWithValue("$favoritavel", recurso.Favoritavel);
                comando.Parameters.AddWithValue("$tags", recurso.TagsJson);
                comando.Parameters.AddWithValue("$aliases", recurso.AliasesJson);
                await comando.ExecuteNonQueryAsync(tokenCancelamento);
            }

            await ExecutarAsync(conexao, transacao, "DELETE FROM politicas", tokenCancelamento);
            await using (var comando = conexao.CreateCommand())
            {
                comando.Transaction = transacao;
                comando.CommandText = "INSERT INTO politicas (id, precedencia_usuario, permitir_ocultacao, intervalo_sincronizacao_segundos) VALUES (1, $precedencia, $ocultacao, $intervalo)";
                comando.Parameters.AddWithValue("$precedencia", snapshot.Politica.PrecedenciaUsuarioSobreHerancaGrupo);
                comando.Parameters.AddWithValue("$ocultacao", snapshot.Politica.PermitirOcultacao);
                comando.Parameters.AddWithValue("$intervalo", (object?)snapshot.Politica.IntervaloSincronizacao?.TotalSeconds ?? DBNull.Value);
                await comando.ExecuteNonQueryAsync(tokenCancelamento);
            }

            await GravarMetadadoAsync(conexao, transacao, "versao_configuracao", snapshot.Versao.ToString(), tokenCancelamento);
            await GravarMetadadoAsync(conexao, transacao, "ultima_sincronizacao", snapshot.UltimaSincronizacao.ToUniversalTime().ToString("O"), tokenCancelamento);
            await transacao.CommitAsync(tokenCancelamento);
            return true;
        }
        finally
        {
            bloqueio.Release();
        }
    }

    private async Task<SqliteConnection> AbrirConexaoAsync(CancellationToken tokenCancelamento)
    {
        var diretorio = Path.GetDirectoryName(caminho);
        if (!string.IsNullOrEmpty(diretorio))
            Directory.CreateDirectory(diretorio);
        var conexao = new SqliteConnection($"Data Source={caminho}");
        await conexao.OpenAsync(tokenCancelamento);
        return conexao;
    }

    private static async Task<int?> LerInteiroAsync(SqliteConnection conexao, string chave, CancellationToken tokenCancelamento)
    {
        var texto = await LerTextoAsync(conexao, chave, tokenCancelamento);
        return texto is null ? null : int.Parse(texto);
    }

    private static async Task<string?> LerTextoAsync(SqliteConnection conexao, string chave, CancellationToken tokenCancelamento)
    {
        await using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT valor FROM metadados WHERE chave = $chave";
        comando.Parameters.AddWithValue("$chave", chave);
        return (string?)await comando.ExecuteScalarAsync(tokenCancelamento);
    }

    private static async Task<PoliticaRecursos> LerPoliticaAsync(SqliteConnection conexao, CancellationToken tokenCancelamento)
    {
        await using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT precedencia_usuario, permitir_ocultacao, intervalo_sincronizacao_segundos FROM politicas WHERE id = 1";
        await using var leitor = await comando.ExecuteReaderAsync(tokenCancelamento);
        if (!await leitor.ReadAsync(tokenCancelamento))
            return new PoliticaRecursos();
        TimeSpan? intervalo = leitor.IsDBNull(2) ? null : TimeSpan.FromSeconds(leitor.GetDouble(2));
        return new PoliticaRecursos(leitor.GetBoolean(0), leitor.GetBoolean(1), intervalo);
    }

    private static async Task GravarMetadadoAsync(SqliteConnection conexao, SqliteTransaction transacao, string chave, string valor, CancellationToken tokenCancelamento)
    {
        await using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = "INSERT INTO metadados (chave, valor) VALUES ($chave, $valor) ON CONFLICT(chave) DO UPDATE SET valor = excluded.valor";
        comando.Parameters.AddWithValue("$chave", chave);
        comando.Parameters.AddWithValue("$valor", valor);
        await comando.ExecuteNonQueryAsync(tokenCancelamento);
    }

    private static async Task ExecutarAsync(SqliteConnection conexao, SqliteTransaction transacao, string sql, CancellationToken tokenCancelamento)
    {
        await using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = sql;
        await comando.ExecuteNonQueryAsync(tokenCancelamento);
    }
    public void Dispose() => bloqueio.Dispose();
}
