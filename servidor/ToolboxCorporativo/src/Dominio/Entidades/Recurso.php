<?php

declare(strict_types=1);

namespace App\Dominio\Entidades;

final readonly class Recurso
{
    public function __construct(
        public string $id,
        public string $nome,
        public ?string $descricao,
        public string $tipo,
        public string $destino,
        public ?string $categoriaId,
        public array $tags,
        public array $aliases,
        public bool $ativo,
        public bool $obrigatorio,
        public bool $ocultavel,
        public bool $favoritavel,
    ) {}

    /** @param array<string, mixed> $linha */
    public static function deLinha(array $linha): self
    {
        $tags = json_decode((string) $linha['tags_json'], true, flags: JSON_THROW_ON_ERROR);
        $aliases = json_decode((string) $linha['aliases_json'], true, flags: JSON_THROW_ON_ERROR);

        return new self(
            (string) $linha['id'],
            (string) $linha['nome'],
            isset($linha['descricao']) ? (string) $linha['descricao'] : null,
            (string) $linha['tipo'],
            (string) $linha['destino'],
            isset($linha['categoria_id']) ? (string) $linha['categoria_id'] : null,
            is_array($tags) ? $tags : [],
            is_array($aliases) ? $aliases : [],
            (bool) $linha['ativo'],
            (bool) $linha['obrigatorio'],
            (bool) $linha['ocultavel'],
            (bool) $linha['favoritavel'],
        );
    }

    public function paraResposta(string $estado = 'Herdado'): array
    {
        return [
            'id' => $this->id,
            'nome' => $this->nome,
            'descricao' => $this->descricao,
            'tipo' => $this->tipo,
            'destino' => $this->destino,
            'categoriaId' => $this->categoriaId,
            'tags' => $this->tags,
            'aliases' => $this->aliases,
            'estado' => $estado,
            'obrigatorio' => $this->obrigatorio || $estado === 'Obrigatorio',
            'ocultavel' => $this->ocultavel,
            'favoritavel' => $this->favoritavel,
        ];
    }
}
