<?php

declare(strict_types=1);

namespace App\Infraestrutura\Persistencia;

use App\Dominio\Entidades\Recurso;
use PDO;

final readonly class RepositorioRecursos
{
    public function __construct(private BancoDados $banco) {}

    /** @return list<Recurso> */
    public function listarAtivos(): array
    {
        $stmt = $this->banco->obterConexao()->query('SELECT * FROM recursos WHERE ativo = 1 ORDER BY nome, id');
        /** @var list<array<string, mixed>> $linhas */
        $linhas = $stmt->fetchAll();
        return array_map(Recurso::deLinha(...), $linhas);
    }

    /** @return list<Recurso> */
    public function listarTodos(): array
    {
        $stmt = $this->banco->obterConexao()->query('SELECT * FROM recursos ORDER BY nome, id');
        /** @var list<array<string, mixed>> $linhas */
        $linhas = $stmt->fetchAll();
        return array_map(Recurso::deLinha(...), $linhas);
    }

    /** @param array<string, mixed> $dados */
    public function salvar(array $dados): Recurso
    {
        $conexao = $this->banco->obterConexao();
        $agora = gmdate('c');
        $id = (string) ($dados['id'] ?? self::uuid());
        /** @var array<array-key, mixed> $tags */
        $tags = is_array($dados['tags'] ?? null) ? $dados['tags'] : [];
        /** @var array<array-key, mixed> $aliases */
        $aliases = is_array($dados['aliases'] ?? null) ? $dados['aliases'] : [];
        $params = [
            'id' => $id,
            'nome' => trim((string) ($dados['nome'] ?? '')),
            'descricao' => isset($dados['descricao']) ? (string) $dados['descricao'] : null,
            'tipo' => (string) ($dados['tipo'] ?? 'Web'),
            'destino' => (string) ($dados['destino'] ?? ''),
            'categoria_id' => isset($dados['categoriaId']) ? (string) $dados['categoriaId'] : null,
            'tags_json' => json_encode(array_values($tags), JSON_THROW_ON_ERROR),
            'aliases_json' => json_encode(array_values($aliases), JSON_THROW_ON_ERROR),
            'ativo' => (int) ($dados['ativo'] ?? true),
            'obrigatorio' => (int) ($dados['obrigatorio'] ?? false),
            'ocultavel' => (int) ($dados['ocultavel'] ?? true),
            'favoritavel' => (int) ($dados['favoritavel'] ?? true),
            'criado_em' => $agora,
            'atualizado_em' => $agora,
        ];
        if ($params['nome'] === '' || $params['destino'] === '') {
            throw new \InvalidArgumentException('Nome e destino são obrigatórios.');
        }

        $stmt = $conexao->prepare(<<<'SQL'
            INSERT INTO recursos (id, nome, descricao, tipo, destino, categoria_id, tags_json, aliases_json, ativo, obrigatorio, ocultavel, favoritavel, criado_em, atualizado_em)
            VALUES (:id, :nome, :descricao, :tipo, :destino, :categoria_id, :tags_json, :aliases_json, :ativo, :obrigatorio, :ocultavel, :favoritavel, :criado_em, :atualizado_em)
            ON CONFLICT(id) DO UPDATE SET nome = excluded.nome, descricao = excluded.descricao, tipo = excluded.tipo, destino = excluded.destino, categoria_id = excluded.categoria_id, tags_json = excluded.tags_json, aliases_json = excluded.aliases_json, ativo = excluded.ativo, obrigatorio = excluded.obrigatorio, ocultavel = excluded.ocultavel, favoritavel = excluded.favoritavel, atualizado_em = excluded.atualizado_em
            SQL);
        $stmt->execute($params);
        $consulta = $conexao->prepare('SELECT * FROM recursos WHERE id = :id');
        $consulta->execute(['id' => $id]);
        /** @var array<string, mixed>|false $linha */
        $linha = $consulta->fetch();
        if ($linha === false) {
            throw new \RuntimeException('Recurso salvo não encontrado.');
        }
        return Recurso::deLinha($linha);
    }

    public function desativar(string $id): void
    {
        $stmt = $this->banco->obterConexao()->prepare('UPDATE recursos SET ativo = 0, atualizado_em = :data WHERE id = :id');
        $stmt->execute(['data' => gmdate('c'), 'id' => $id]);
    }

    private static function uuid(): string
    {
        $bytes = random_bytes(16);
        $bytes[6] = chr((ord($bytes[6]) & 0x0f) | 0x40);
        $bytes[8] = chr((ord($bytes[8]) & 0x3f) | 0x80);
        return vsprintf('%s%s-%s-%s-%s-%s%s%s', str_split(bin2hex($bytes), 4));
    }
}
