<?php

declare(strict_types=1);

namespace App\Api;

use App\Aplicacao\Servicos\ServicoAutenticacaoLocal;
use Psr\Http\Message\ResponseFactoryInterface;
use Psr\Http\Message\ResponseInterface;
use Psr\Http\Message\StreamFactoryInterface;

final readonly class AcaoLogin
{
    public function __construct(
        private ServicoAutenticacaoLocal $autenticacao,
        private ResponseFactoryInterface $fabricaResposta,
        private StreamFactoryInterface $fabricaFluxo,
    ) {}

    public function __invoke(): ResponseInterface
    {
        try {
            $dados = json_decode((string) file_get_contents('php://input'), true, flags: JSON_THROW_ON_ERROR);
            if (!is_array($dados)) {
                throw new \InvalidArgumentException('O corpo deve ser um objeto JSON.');
            }
            $token = $this->autenticacao->autenticar((string) ($dados['usuario'] ?? ''), (string) ($dados['senha'] ?? ''));
            if ($token === null) {
                return $this->json(['erro' => 'Credenciais inválidas.'], 401);
            }
            $cookie = 'toolbox_sessao=' . rawurlencode($token) . '; Max-Age=28800; Path=/; HttpOnly; SameSite=Strict';
            if (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') {
                $cookie .= '; Secure';
            }
            return $this->json(['estado' => 'autenticado'])->withHeader('Set-Cookie', $cookie);
        } catch (\JsonException|\InvalidArgumentException $erro) {
            return $this->json(['erro' => $erro->getMessage()], 400);
        }
    }

    private function json(array $dados, int $status = 200): ResponseInterface
    {
        return $this->fabricaResposta->createResponse($status)
            ->withHeader('Content-Type', 'application/json; charset=utf-8')
            ->withBody($this->fabricaFluxo->createStream(json_encode($dados, JSON_THROW_ON_ERROR)));
    }
}
