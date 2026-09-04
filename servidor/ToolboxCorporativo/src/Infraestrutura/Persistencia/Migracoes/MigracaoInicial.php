<?php

declare(strict_types=1);

namespace App\Infraestrutura\Persistencia\Migracoes;

use PDO;
use Throwable;

final class MigracaoInicial
{
    public static function executar(PDO $conexao): void
    {
        $conexao->exec(<<<'SQL'
            CREATE TABLE IF NOT EXISTS migracoes (
                versao INTEGER PRIMARY KEY,
                aplicada_em TEXT NOT NULL
            );
            SQL);

        $aplicada = $conexao->query('SELECT 1 FROM migracoes WHERE versao = 1')->fetchColumn();
        if ($aplicada !== false) {
            return;
        }

        $conexao->beginTransaction();
        try {
            $conexao->exec(<<<'SQL'
                CREATE TABLE usuarios (
                    id TEXT PRIMARY KEY,
                    nome_usuario TEXT NOT NULL UNIQUE,
                    nome_exibicao TEXT NOT NULL,
                    dominio TEXT NOT NULL,
                    sid TEXT,
                    perfil TEXT NOT NULL DEFAULT 'Usuario',
                    setor_id TEXT,
                    senha_hash TEXT,
                    ativo INTEGER NOT NULL DEFAULT 1,
                    criado_em TEXT NOT NULL,
                    atualizado_em TEXT NOT NULL
                );
                CREATE TABLE computadores (
                    id TEXT PRIMARY KEY,
                    nome TEXT NOT NULL UNIQUE,
                    versao_windows TEXT NOT NULL,
                    usuario_recente_id TEXT,
                    toolbox_versao TEXT,
                    ultima_sincronizacao TEXT,
                    configuracao_versao INTEGER NOT NULL DEFAULT 0
                );
                CREATE TABLE categorias (
                    id TEXT PRIMARY KEY,
                    nome TEXT NOT NULL UNIQUE
                );
                CREATE TABLE recursos (
                    id TEXT PRIMARY KEY,
                    nome TEXT NOT NULL,
                    descricao TEXT,
                    tipo TEXT NOT NULL,
                    destino TEXT NOT NULL,
                    categoria_id TEXT,
                    tags_json TEXT NOT NULL DEFAULT '[]',
                    aliases_json TEXT NOT NULL DEFAULT '[]',
                    icone_personalizado TEXT,
                    ativo INTEGER NOT NULL DEFAULT 1,
                    obrigatorio INTEGER NOT NULL DEFAULT 0,
                    ocultavel INTEGER NOT NULL DEFAULT 1,
                    favoritavel INTEGER NOT NULL DEFAULT 1,
                    criado_em TEXT NOT NULL,
                    atualizado_em TEXT NOT NULL
                );
                CREATE TABLE grupos (
                    id TEXT PRIMARY KEY,
                    nome TEXT NOT NULL UNIQUE,
                    tipo TEXT NOT NULL DEFAULT 'Interno'
                );
                CREATE TABLE usuarios_grupos (
                    usuario_id TEXT NOT NULL,
                    grupo_id TEXT NOT NULL,
                    PRIMARY KEY (usuario_id, grupo_id)
                );
                CREATE TABLE setores (
                    id TEXT PRIMARY KEY,
                    nome TEXT NOT NULL UNIQUE,
                    setor_pai_id TEXT
                );
                CREATE TABLE conjuntos_computadores (
                    id TEXT PRIMARY KEY,
                    nome TEXT NOT NULL UNIQUE
                );
                CREATE TABLE computadores_conjuntos (
                    computador_id TEXT NOT NULL,
                    conjunto_id TEXT NOT NULL,
                    PRIMARY KEY (computador_id, conjunto_id)
                );
                CREATE TABLE atribuicoes (
                    id TEXT PRIMARY KEY,
                    recurso_id TEXT NOT NULL,
                    tipo_alvo TEXT NOT NULL,
                    alvo_id TEXT,
                    estado TEXT NOT NULL,
                    herdada INTEGER NOT NULL DEFAULT 0,
                    criado_em TEXT NOT NULL,
                    UNIQUE (recurso_id, tipo_alvo, alvo_id)
                );
                CREATE TABLE politicas (
                    id INTEGER PRIMARY KEY CHECK (id = 1),
                    precedencia_usuario_grupo INTEGER NOT NULL DEFAULT 0,
                    descoberta_automatica INTEGER NOT NULL DEFAULT 1,
                    intervalo_sincronizacao_segundos INTEGER NOT NULL DEFAULT 300,
                    atualizado_em TEXT NOT NULL
                );
                CREATE TABLE versoes_configuracao (
                    versao INTEGER PRIMARY KEY,
                    criado_em TEXT NOT NULL,
                    autor_id TEXT,
                    payload_json TEXT NOT NULL
                );
                CREATE TABLE eventos_auditoria (
                    id TEXT PRIMARY KEY,
                    autor_id TEXT,
                    criado_em TEXT NOT NULL,
                    acao TEXT NOT NULL,
                    objeto_tipo TEXT NOT NULL,
                    objeto_id TEXT NOT NULL,
                    valor_anterior_json TEXT,
                    valor_novo_json TEXT
                );
                CREATE INDEX idx_recursos_ativo ON recursos (ativo);
                CREATE INDEX idx_atribuicoes_recurso ON atribuicoes (recurso_id);
                CREATE INDEX idx_auditoria_criado ON eventos_auditoria (criado_em);
                INSERT INTO politicas (id, atualizado_em) VALUES (1, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'));
                INSERT INTO versoes_configuracao (versao, criado_em, payload_json) VALUES (0, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'), '{}');
                SQL);
            $stmt = $conexao->prepare('INSERT INTO migracoes (versao, aplicada_em) VALUES (1, :data)');
            $stmt->execute(['data' => gmdate('c')]);
            $conexao->commit();
        } catch (Throwable $erro) {
            $conexao->rollBack();
            throw $erro;
        }
    }
}
