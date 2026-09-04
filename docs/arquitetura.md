# Arquitetura

## Escopo da Fase 1

O workspace separa o cliente Windows e o servidor em processos distintos:

- `src/ToolboxCorporativo.App`: WinUI 3 empacotado em MSIX, x64.
- `src/ToolboxCorporativo.Dominio`: regras e contratos compartilhados do domínio.
- `src/ToolboxCorporativo.Aplicacao`: casos de uso e ViewModels.
- `src/ToolboxCorporativo.Infraestrutura`: rede, identidade, cache e persistência local.
- `src/ToolboxCorporativo.Testes`: testes automatizados do domínio.
- `servidor/ToolboxCorporativo`: uma aplicação Yii 3 para painel web e API REST.

O servidor usa as camadas `Dominio`, `Aplicacao`, `Infraestrutura`, `InterfaceWeb` e `Api` dentro do mesmo projeto Composer. A API v1 e o painel compartilham configuração e persistência; não há dois servidores.

## Decisões técnicas

- SDK .NET `10.0.400`, fixado em `global.json`.
- Windows App SDK `2.4.0`, WinUI 3 e alvo mínimo Windows `10.0.22621.0`.
- `CommunityToolkit.Mvvm` `8.4.2`.
- `Microsoft.Data.Sqlite` `10.0.1`, com `SQLitePCLRaw.lib.e_sqlite3` `2.1.12` para evitar a dependência vulnerável detectada na versão anterior.
- PHP `8.2–8.5`, Yii 3 pelo template oficial `yiisoft/app`.
- SQLite inicial; repositórios e SQL devem permanecer compatíveis com PostgreSQL.

## Fluxo de execução

O cliente identifica o usuário Windows, lê o último snapshot local e apresenta o catálogo. A sincronização ocorre em segundo plano via HTTPS/JSON. O servidor deriva a identidade do principal autenticado, calcula a configuração efetiva e devolve um snapshot versionado. Uma resposta válida substitui o snapshot local em uma transação.

## Fontes oficiais

- [Windows App SDK 2.4](https://learn.microsoft.com/windows/apps/windows-app-sdk/).
- [Yii 3](https://www.yiiframework.com/doc/guide/3.0/en).
- [Template yiisoft/app](https://github.com/yiisoft/app).
