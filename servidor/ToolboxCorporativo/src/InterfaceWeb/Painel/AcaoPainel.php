<?php

declare(strict_types=1);

namespace App\InterfaceWeb\Painel;

use App\Aplicacao\Servicos\ServicoCatalogo;
use Psr\Http\Message\ResponseFactoryInterface;
use Psr\Http\Message\ResponseInterface;
use Yiisoft\Yii\View\Renderer\WebViewRenderer;

final readonly class AcaoPainel
{
    public function __construct(
        private WebViewRenderer $renderizador,
        private ServicoCatalogo $catalogo,
        private ResponseFactoryInterface $fabricaResposta,
    ) {}

    public function __invoke(): ResponseInterface
    {
        $principal = $this->catalogo->principalDaRequisicao();
        if (!$this->catalogo->podeAdministrar($principal)) {
            return $this->fabricaResposta->createResponse(302)->withHeader('Location', '/admin/login');
        }
        return $this->renderizador->render(__DIR__ . '/template', [
            'metricas' => $this->catalogo->metricasDashboard(),
        ]);
    }
}
