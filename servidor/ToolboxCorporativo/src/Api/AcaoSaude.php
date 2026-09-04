<?php

declare(strict_types=1);

namespace App\Api;

use Psr\Http\Message\ResponseFactoryInterface;
use Psr\Http\Message\ResponseInterface;
use Psr\Http\Message\StreamFactoryInterface;

final readonly class AcaoSaude
{
    public function __construct(
        private ResponseFactoryInterface $fabricaResposta,
        private StreamFactoryInterface $fabricaFluxo,
    ) {}

    public function __invoke(): ResponseInterface
    {
        $corpo = $this->fabricaFluxo->createStream(json_encode([
            'estado' => 'saudavel',
            'servico' => 'Toolbox Corporativo',
        ], JSON_THROW_ON_ERROR));

        return $this->fabricaResposta
            ->createResponse(200)
            ->withHeader('Content-Type', 'application/json; charset=utf-8')
            ->withBody($corpo);
    }
}
