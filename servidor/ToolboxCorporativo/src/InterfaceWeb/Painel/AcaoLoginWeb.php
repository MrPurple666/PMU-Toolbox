<?php

declare(strict_types=1);

namespace App\InterfaceWeb\Painel;

use Psr\Http\Message\ResponseFactoryInterface;
use Psr\Http\Message\ResponseInterface;
use Psr\Http\Message\StreamFactoryInterface;

final readonly class AcaoLoginWeb
{
    public function __construct(
        private ResponseFactoryInterface $fabricaResposta,
        private StreamFactoryInterface $fabricaFluxo,
    ) {}

    public function __invoke(): ResponseInterface
    {
        $html = <<<'HTML'
            <!doctype html>
            <html lang="pt-BR">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Entrar | Toolbox</title><style>body{font-family:system-ui,sans-serif;background:#f4f7fb;color:#172033;display:grid;place-items:center;min-height:100vh;margin:0}form{background:#fff;padding:2rem;border-radius:14px;box-shadow:0 8px 30px #10204018;display:grid;gap:1rem;width:min(90vw,22rem)}label{display:grid;gap:.35rem}input,button{font:inherit;padding:.65rem;border:1px solid #c8d2e1;border-radius:8px}button{background:#1769aa;color:#fff;border:0;cursor:pointer}#erro{color:#b42318;min-height:1.2em}</style></head>
            <body><form id="login"><h1>Toolbox Corporativo</h1><label>Usuário<input name="usuario" autocomplete="username" required></label><label>Senha<input name="senha" type="password" autocomplete="current-password" required></label><button>Entrar</button><div id="erro" role="alert"></div></form><script>document.querySelector('#login').addEventListener('submit',async e=>{e.preventDefault();const f=new FormData(e.target);const r=await fetch('/api/v1/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({usuario:f.get('usuario'),senha:f.get('senha')})});if(r.ok)location='/admin';else document.querySelector('#erro').textContent=(await r.json()).erro||'Falha ao entrar.'})</script></body>
            </html>
            HTML;

        return $this->fabricaResposta->createResponse(200)
            ->withHeader('Content-Type', 'text/html; charset=utf-8')
            ->withBody($this->fabricaFluxo->createStream($html));
    }
}
