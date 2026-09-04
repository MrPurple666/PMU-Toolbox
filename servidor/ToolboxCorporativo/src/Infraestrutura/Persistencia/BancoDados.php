<?php

declare(strict_types=1);

namespace App\Infraestrutura\Persistencia;

use App\Infraestrutura\Persistencia\Migracoes\MigracaoInicial;
use App\Infraestrutura\Persistencia\Migracoes\MigracaoAutenticacao;
use PDO;

final class BancoDados
{
    private ?PDO $conexao = null;

    public function obterConexao(): PDO
    {
        if ($this->conexao !== null) {
            return $this->conexao;
        }

        $caminho = $_ENV['TOOLBOX_DB_PATH'] ?? dirname(__DIR__, 4) . '/runtime/toolbox.sqlite';
        $diretorio = dirname($caminho);
        if (!is_dir($diretorio)) {
            mkdir($diretorio, 0770, true);
        }

        $this->conexao = new PDO('sqlite:' . $caminho, options: [
            PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES => false,
        ]);
        MigracaoInicial::executar($this->conexao);
        MigracaoAutenticacao::executar($this->conexao);
        return $this->conexao;
    }
}
