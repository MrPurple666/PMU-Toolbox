<?php

declare(strict_types=1);

namespace App\Api;

use Psr\Http\Message\ResponseInterface;

final readonly class AcaoRecursos
{
    public function __construct(private AcaoConfiguracao $configuracao) {}

    public function __invoke(): ResponseInterface
    {
        return ($this->configuracao)();
    }
}
