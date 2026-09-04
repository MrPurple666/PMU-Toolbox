<?php

declare(strict_types=1);

namespace App\Api;

use App\Aplicacao\Servicos\ServicoCatalogo;
use Psr\Http\Message\ResponseFactoryInterface;
use Psr\Http\Message\ResponseInterface;
use Psr\Http\Message\StreamFactoryInterface;

final readonly class AcaoAdminEmMassa
{
    public function __construct(
        private ServicoCatalogo $catalogo,
        private ResponseFactoryInterface $fabricaResposta,
        private StreamFactoryInterface $fabricaFluxo,
    ) {}

    public function __invoke(): ResponseInterface
    {
        $principal = $this->catalogo->principalDaRequisicao();
        if ($principal === '' && getenv('APP_ENV') === 'dev' && getenv('TOOLBOX_IDENTIDADE_SIMULADA') === '1') {
            $principal = $_SERVER['HTTP_X_TOOLBOX_USUARIO'] ?? 'LOCAL\\administrador';
        }
        if (!$this->catalogo->podeAdministrar($principal)) {
            return $this->json(['erro' => 'Perfil sem permissão administrativa.'], 403);
        }

        try {
            $dados = json_decode((string) file_get_contents('php://input'), true, flags: JSON_THROW_ON_ERROR);
            if (!is_array($dados) || ($dados['acao'] ?? '') !== 'desativar') {
                throw new \InvalidArgumentException('Ação em massa inválida.');
            }
            $ids = $dados['ids'] ?? [];
            if (!is_array($ids)) {
                throw new \InvalidArgumentException('Ids devem ser uma lista.');
            }
            $idsNormalizados = array_values(array_map(static fn (mixed $id): string => (string) $id, $ids));
            $quantidade = $this->catalogo->desativarEmMassa($idsNormalizados, (string) $this->catalogo->usuarioDoPrincipal($principal)['id']);
            return $this->json(['desativados' => $quantidade]);
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
