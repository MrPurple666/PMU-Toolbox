<?php

declare(strict_types=1);

namespace App\Api;

use App\Aplicacao\Servicos\ServicoCatalogo;
use App\Aplicacao\Contratos\DadosSincronizacao;
use Psr\Http\Message\ResponseFactoryInterface;
use Psr\Http\Message\ResponseInterface;
use Psr\Http\Message\StreamFactoryInterface;

final readonly class AcaoSincronizacao
{
    public function __construct(
        private ServicoCatalogo $catalogo,
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
            /** @var array<string, mixed> $dados */
            $dto = DadosSincronizacao::deArray($dados);
            $principal = $this->obterPrincipal($dto->paraArray());
            return $this->json($this->catalogo->sincronizar($principal, $dto->paraArray()));
        } catch (\JsonException|\InvalidArgumentException $erro) {
            return $this->json(['erro' => $erro->getMessage()], 400);
        } catch (\RuntimeException $erro) {
            return $this->json(['erro' => $erro->getMessage()], $erro->getCode() === 401 ? 401 : 500);
        }
    }

    /** @param array<string, mixed> $dados */
    private function obterPrincipal(array $dados): string
    {
        $principal = $this->catalogo->principalDaRequisicao();
        if ($principal !== '') {
            return $principal;
        }
        if (getenv('APP_ENV') === 'dev' && getenv('TOOLBOX_IDENTIDADE_SIMULADA') === '1') {
            $usuario = $dados['usuario'] ?? [];
            if (!is_array($usuario)) {
                throw new \InvalidArgumentException('O campo usuario deve ser um objeto.');
            }
            $nome = trim((string) ($usuario['nomeUsuario'] ?? 'simulado'));
            $dominio = trim((string) ($usuario['dominio'] ?? 'LOCAL'));
            return $dominio . '\\' . $nome;
        }
        throw new \RuntimeException('Principal autenticado não confiável.', 401);
    }

    private function json(array $dados, int $status = 200): ResponseInterface
    {
        return $this->fabricaResposta->createResponse($status)
            ->withHeader('Content-Type', 'application/json; charset=utf-8')
            ->withBody($this->fabricaFluxo->createStream(json_encode($dados, JSON_THROW_ON_ERROR)));
    }
}
