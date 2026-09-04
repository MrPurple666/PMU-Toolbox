<?php

declare(strict_types=1);

namespace App\Aplicacao\Contratos;

use InvalidArgumentException;

final readonly class DadosSincronizacao
{
    /** @param array<string, mixed> $usuario
     * @param array<string, mixed> $computador
     * @param array<string, mixed> $cliente
     * @param array<string, mixed> $diagnostico
     */
    public function __construct(
        public array $usuario,
        public array $computador,
        public array $cliente,
        public array $diagnostico,
    ) {}

    /** @param array<string, mixed> $dados */
    public static function deArray(array $dados): self
    {
        return new self(
            self::objeto($dados, 'usuario'),
            self::objeto($dados, 'computador'),
            self::objeto($dados, 'cliente'),
            self::objeto($dados, 'diagnostico', false),
        );
    }

    /** @return array<string, mixed> */
    public function paraArray(): array
    {
        return [
            'usuario' => $this->usuario,
            'computador' => $this->computador,
            'cliente' => $this->cliente,
            'diagnostico' => $this->diagnostico,
        ];
    }

    /** @return array<string, mixed> */
    private static function objeto(array $dados, string $campo, bool $obrigatorio = true): array
    {
        $valor = $dados[$campo] ?? [];
        if (!is_array($valor)) {
            if ($obrigatorio) {
                throw new InvalidArgumentException("O campo {$campo} deve ser um objeto.");
            }
            return [];
        }
        /** @var array<string, mixed> $valor */
        return $valor;
    }
}
