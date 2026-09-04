<?php

declare(strict_types=1);

namespace App\Api;

use App\Aplicacao\Servicos\ServicoCatalogo;
use Psr\Http\Message\ResponseFactoryInterface;
use Psr\Http\Message\ResponseInterface;
use Psr\Http\Message\StreamFactoryInterface;

final readonly class AcaoAdminRestaurar
{
    public function __construct(
        private ServicoCatalogo $catalogo,
        private ResponseFactoryInterface $fabricaResposta,
        private StreamFactoryInterface $fabricaFluxo,
    ) {}

    public function __invoke(?int $versao = null): ResponseInterface
    {
        $principal = $this->catalogo->principalDaRequisicao();
        if ($principal === '' && getenv('APP_ENV') === 'dev' && getenv('TOOLBOX_IDENTIDADE_SIMULADA') === '1') {
            $principal = $_SERVER['HTTP_X_TOOLBOX_USUARIO'] ?? 'LOCAL\\administrador';
        }
        if (!$this->catalogo->podeAdministrar($principal)) {
            return $this->json(['erro' => 'Perfil sem permissão administrativa.'], 403);
        }
        $versao ??= (int) basename((string) parse_url($_SERVER['REQUEST_URI'] ?? '', PHP_URL_PATH));
        try {
            $this->catalogo->restaurarVersao($versao, (string) $this->catalogo->usuarioDoPrincipal($principal)['id']);
            return $this->json(['estado' => 'restaurado', 'versao' => $versao]);
        } catch (\InvalidArgumentException $erro) {
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
