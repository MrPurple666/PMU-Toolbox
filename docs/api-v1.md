# API v1

A API é publicada sob `/api/v1` e usa JSON UTF-8.

## Saúde

`GET /api/v1/saude` responde `200`:

```json
{"estado":"saudavel","servico":"Toolbox Corporativo"}
```

## Sessão e sincronização

`POST /api/v1/login` recebe `usuario` e `senha` de uma conta local com hash Argon2id. Em sucesso, emite o cookie HttpOnly `toolbox_sessao` por oito horas.

`POST /api/v1/sessao` registra ou atualiza usuário e computador e retorna o contexto inicial.

`GET /api/v1/configuracao?versaoAtual=N` responde `304 Not Modified` quando `N` é a versão atual; caso contrário retorna o snapshot completo.

`POST /api/v1/sincronizacao` recebe o DTO:

```json
{
  "usuario": {"nomeUsuario": "antonio", "dominio": "PREFEITURA"},
  "computador": {"nome": "PC-001", "versaoWindows": "Windows 11"},
  "cliente": {"versao": "0.1.0"},
  "diagnostico": {}
}
```

A resposta contém `versao`, `usuario.nomeExibicao`, `recursos` e `politicas`. A identidade enviada pelo cliente é usada somente no modo simulado de desenvolvimento; em produção, autorização usa principal Windows ou sessão autenticada.

## Administração

- `GET|POST /api/v1/admin/recursos`
- `PUT|DELETE /api/v1/admin/recursos/{id}`
- `POST /api/v1/admin/recursos/acoes` com `{"acao":"desativar","ids":[...]}`
- `POST /api/v1/admin/atribuicoes`
- `POST /api/v1/admin/configuracao/restaurar/{versao}`

Todas as rotas administrativas exigem perfil administrativo e registram auditoria. O painel web fica em `/admin`; visitantes são redirecionados para `/admin/login`.
