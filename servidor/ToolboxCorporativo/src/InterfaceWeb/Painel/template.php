<?php

declare(strict_types=1);

/** @var array{usuariosAtivos:int, computadores:int, recursos:int, auditoria:int} $metricas */
?>
<h1>Toolbox Administração</h1>
<nav aria-label="Menu administrativo">
    <a href="/admin">Dashboard</a> |
    <a href="/api/v1/admin/recursos">Recursos</a> |
    <a href="/api/v1/recursos">Catálogo</a>
</nav>
<section class="cartoes-dashboard" aria-label="Indicadores">
    <article><strong><?= $metricas['usuariosAtivos'] ?></strong><span>Usuários ativos</span></article>
    <article><strong><?= $metricas['computadores'] ?></strong><span>Computadores</span></article>
    <article><strong><?= $metricas['recursos'] ?></strong><span>Recursos ativos</span></article>
    <article><strong><?= $metricas['auditoria'] ?></strong><span>Eventos de auditoria</span></article>
</section>
