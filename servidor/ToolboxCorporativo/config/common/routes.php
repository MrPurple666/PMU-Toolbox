<?php

declare(strict_types=1);

use App\Api;
use App\InterfaceWeb;
use App\Web;
use Yiisoft\Router\Group;
use Yiisoft\Router\Route;

return [
    Group::create()
        ->routes(
            Route::get('/')
                ->action(Web\HomePage\Action::class)
                ->name('home'),
            Route::get('/api/v1/saude')
                ->action(Api\AcaoSaude::class)
                ->name('api.saude'),
            Route::post('/api/v1/login')
                ->action(Api\AcaoLogin::class)
                ->name('api.login'),
            Route::post('/api/v1/sessao')
                ->action(Api\AcaoSessao::class)
                ->name('api.sessao'),
            Route::get('/api/v1/configuracao')
                ->action(Api\AcaoConfiguracao::class)
                ->name('api.configuracao'),
            Route::get('/api/v1/recursos')
                ->action(Api\AcaoRecursos::class)
                ->name('api.recursos'),
            Route::post('/api/v1/sincronizacao')
                ->action(Api\AcaoSincronizacao::class)
                ->name('api.sincronizacao'),
            Route::get('/api/v1/admin/recursos')
                ->action(Api\AcaoAdminRecursos::class)
                ->name('api.admin.recursos.listar'),
            Route::post('/api/v1/admin/recursos/acoes')
                ->action(Api\AcaoAdminEmMassa::class)
                ->name('api.admin.recursos.acoes'),
            Route::post('/api/v1/admin/recursos')
                ->action(Api\AcaoAdminRecursos::class)
                ->name('api.admin.recursos.criar'),
            Route::put('/api/v1/admin/recursos/{id}')
                ->action(Api\AcaoAdminRecursos::class)
                ->name('api.admin.recursos.editar'),
            Route::delete('/api/v1/admin/recursos/{id}')
                ->action(Api\AcaoAdminRecursos::class)
                ->name('api.admin.recursos.desativar'),
            Route::get('/admin/login')
                ->action(InterfaceWeb\Painel\AcaoLoginWeb::class)
                ->name('admin.login'),
            Route::post('/api/v1/admin/configuracao/restaurar/{versao}')
                ->action(Api\AcaoAdminRestaurar::class)
                ->name('api.admin.configuracao.restaurar'),
            Route::post('/api/v1/admin/atribuicoes')
                ->action(Api\AcaoAdminAtribuicoes::class)
                ->name('api.admin.atribuicoes'),
            Route::get('/admin')
                ->action(InterfaceWeb\Painel\AcaoPainel::class)
                ->name('admin.dashboard'),
        ),
];
