<?php

declare(strict_types=1);

namespace App\Api;

use App\Aplicacao\Servicos\ServicoCatalogo;
use Psr\Http\Message\ResponseFactoryInterface;
use Psr\Http\Message\ResponseInterface;
use Psr\Http\Message\StreamFactoryInterface;

final readonly class AcaoConfiguracao
{
    public function __construct(
        private ServicoCatalogo $catalogo,
        private ResponseFactoryInterface $fabricaResposta,
        private StreamFactoryInterface $fabricaFluxo,
    ) {}

    public function __invoke(): ResponseInterface
    {
        $principal = $this->catalogo->principalDaRequisicao();
        if ($principal === '') {
            return $this->json(['erro' => 'Principal autenticado obrigatório.'], 401);
        }
        $configuracao = $this->catalogo->configuracaoDoPrincipal($principal);
        $versaoParam = $_GET['versaoAtual'] ?? null;
        $versaoAtual = is_scalar($versaoParam) ? (int) $versaoParam : -1;
        if ($versaoAtual === $configuracao['versao']) {
            return $this->fabricaResposta->createResponse(304);
        }
        return $this->json($configuracao);
    }

    private function json(array $dados, int $status = 200): ResponseInterface
    {
        return $this->fabricaResposta->createResponse($status)
            ->withHeader('Content-Type', 'application/json; charset=utf-8')
            ->withBody($this->fabricaFluxo->createStream(json_encode($dados, JSON_THROW_ON_ERROR)));
    }
}
