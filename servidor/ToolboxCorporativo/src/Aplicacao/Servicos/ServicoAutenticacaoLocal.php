<?php

declare(strict_types=1);

namespace App\Aplicacao\Servicos;

use App\Infraestrutura\Persistencia\BancoDados;
use DateTimeImmutable;
use DateTimeZone;

final readonly class ServicoAutenticacaoLocal
{
    public function __construct(private BancoDados $banco) {}

    public function autenticar(string $login, string $senha): ?string
    {
        $partes = str_contains($login, '\\') ? explode('\\', $login, 2) : ['LOCAL', $login];
        $dominio = $partes[0] ?? 'LOCAL';
        $nome = $partes[1] ?? '';
        if ($nome === '' || $senha === '') {
            return null;
        }

        $conexao = $this->banco->obterConexao();
        $stmt = $conexao->prepare('SELECT * FROM usuarios WHERE nome_usuario = :nome AND dominio = :dominio AND ativo = 1');
        $stmt->execute(['nome' => $nome, 'dominio' => $dominio]);
        /** @var array<string, mixed>|false $usuario */
        $usuario = $stmt->fetch();
        if ($usuario === false) {
            return null;
        }
        $hash = $usuario['senha_hash'] ?? null;
        if (!is_string($hash) || !password_verify($senha, $hash)) {
            return null;
        }

        $token = bin2hex(random_bytes(32));
        $agora = new DateTimeImmutable('now', new DateTimeZone('UTC'));
        $expira = $agora->modify('+8 hours');
        $conexao->prepare('INSERT INTO sessoes_autenticacao (token_hash, usuario_id, expira_em, criada_em) VALUES (:token, :usuario, :expira, :criada)')->execute([
            'token' => hash('sha256', $token),
            'usuario' => (string) $usuario['id'],
            'expira' => $expira->format('c'),
            'criada' => $agora->format('c'),
        ]);
        return $token;
    }

    public function principalDaRequisicao(): string
    {
        $principal = $_SERVER['REMOTE_USER'] ?? '';
        if ($principal !== '') {
            return $principal;
        }
        $token = $_COOKIE['toolbox_sessao'] ?? '';
        if ($token === '') {
            return '';
        }
        $stmt = $this->banco->obterConexao()->prepare(<<<'SQL'
            SELECT u.dominio, u.nome_usuario
            FROM sessoes_autenticacao s
            INNER JOIN usuarios u ON u.id = s.usuario_id
            WHERE s.token_hash = :token AND s.expira_em > :agora AND u.ativo = 1
            SQL);
        $stmt->execute(['token' => hash('sha256', $token), 'agora' => gmdate('c')]);
        /** @var array<string, mixed>|false $usuario */
        $usuario = $stmt->fetch();
        if ($usuario === false) {
            return '';
        }
        return (string) $usuario['dominio'] . '\\' . (string) $usuario['nome_usuario'];
    }

    public function encerrarSessao(): void
    {
        $token = $_COOKIE['toolbox_sessao'] ?? '';
        if ($token !== '') {
            $stmt = $this->banco->obterConexao()->prepare('DELETE FROM sessoes_autenticacao WHERE token_hash = :token');
            $stmt->execute(['token' => hash('sha256', $token)]);
        }
    }
}
