<?php

declare(strict_types=1);

namespace App\Infraestrutura\Persistencia\Migracoes;

use PDO;
use Throwable;

final class MigracaoAutenticacao
{
    public static function executar(PDO $conexao): void
    {
        if ($conexao->query('SELECT 1 FROM migracoes WHERE versao = 2')->fetchColumn() !== false) {
            return;
        }

        $conexao->beginTransaction();
        try {
            $conexao->exec(<<<'SQL'
                CREATE TABLE IF NOT EXISTS sessoes_autenticacao (
                    token_hash TEXT PRIMARY KEY,
                    usuario_id TEXT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
                    expira_em TEXT NOT NULL,
                    criada_em TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_sessoes_expira ON sessoes_autenticacao (expira_em);
                SQL);
            $stmt = $conexao->prepare('INSERT INTO migracoes (versao, aplicada_em) VALUES (2, :data)');
            $stmt->execute(['data' => gmdate('c')]);
            $conexao->commit();
        } catch (Throwable $erro) {
            $conexao->rollBack();
            throw $erro;
        }
    }
}
