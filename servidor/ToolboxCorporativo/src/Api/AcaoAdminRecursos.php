<?php

declare(strict_types=1);

namespace App\Api;

use App\Aplicacao\Servicos\ServicoCatalogo;
use App\Dominio\Entidades\Recurso;
use Psr\Http\Message\ResponseFactoryInterface;
use Psr\Http\Message\ResponseInterface;
use Psr\Http\Message\StreamFactoryInterface;

final readonly class AcaoAdminRecursos
{
    public function __construct(
        private ServicoCatalogo $catalogo,
        private ResponseFactoryInterface $fabricaResposta,
        private StreamFactoryInterface $fabricaFluxo,
    ) {}

    public function __invoke(?string $id = null): ResponseInterface
    {
        $principal = $this->obterPrincipal();
        $id ??= basename((string) parse_url($_SERVER['REQUEST_URI'] ?? '', PHP_URL_PATH));
        if (!$this->catalogo->podeAdministrar($principal)) {
            return $this->json(['erro' => 'Perfil sem permissão administrativa.'], 403);
        }
        $autor = (string) $this->catalogo->usuarioDoPrincipal($principal)['id'];

        try {
            return match ($_SERVER['REQUEST_METHOD'] ?? 'GET') {
                'GET' => $this->json(array_map(static fn (Recurso $recurso): array => $recurso->paraResposta(), $this->catalogo->listarRecursos())),
                'POST', 'PUT', 'PATCH' => $this->json($this->catalogo->salvarRecurso($this->lerCorpo(), $autor)->paraResposta(), 201),
                'DELETE' => $this->excluir($id, $autor),
                default => $this->json(['erro' => 'Método não permitido.'], 405),
            };
        } catch (\JsonException|\InvalidArgumentException $erro) {
            return $this->json(['erro' => $erro->getMessage()], 400);
        }
    }

    private function excluir(?string $id, string $autor): ResponseInterface
    {
        if ($id === null || $id === '') {
            return $this->json(['erro' => 'Identificador do recurso obrigatório.'], 400);
        }
        $this->catalogo->desativarRecurso($id, $autor);
        return $this->fabricaResposta->createResponse(204);
    }

    /** @return array<string, mixed> */
    private function lerCorpo(): array
    {
        $dados = json_decode((string) file_get_contents('php://input'), true, flags: JSON_THROW_ON_ERROR);
        if (!is_array($dados)) {
            throw new \InvalidArgumentException('O corpo deve ser um objeto JSON.');
        }
        /** @var array<string, mixed> $dados */
        return $dados;
    }

    private function obterPrincipal(): string
    {
        $principal = $this->catalogo->principalDaRequisicao();
        if ($principal !== '') {
            return $principal;
        }
        if (getenv('APP_ENV') === 'dev' && getenv('TOOLBOX_IDENTIDADE_SIMULADA') === '1') {
            return $_SERVER['HTTP_X_TOOLBOX_USUARIO'] ?? 'LOCAL\\administrador';
        }
        return '';
    }

    private function json(array $dados, int $status = 200): ResponseInterface
    {
        return $this->fabricaResposta->createResponse($status)
            ->withHeader('Content-Type', 'application/json; charset=utf-8')
            ->withBody($this->fabricaFluxo->createStream(json_encode($dados, JSON_THROW_ON_ERROR)));
    }
}
