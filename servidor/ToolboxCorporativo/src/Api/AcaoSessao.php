<?php

declare(strict_types=1);

namespace App\Api;

use Psr\Http\Message\ResponseInterface;

final readonly class AcaoSessao
{
    public function __construct(private AcaoSincronizacao $sincronizacao) {}

    public function __invoke(): ResponseInterface
    {
        return ($this->sincronizacao)();
    }
}
