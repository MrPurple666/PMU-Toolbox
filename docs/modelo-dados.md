# Modelo de dados

## Servidor

As tabelas previstas são `usuarios`, `computadores`, `recursos`, `categorias`, `grupos`, `usuarios_grupos`, `setores`, `conjuntos_computadores`, `computadores_conjuntos`, `atribuicoes`, `politicas`, `versoes_configuracao` e `eventos_auditoria`.

Relações consultáveis usam tabelas relacionais e chaves estrangeiras. JSON fica restrito ao destino/configuração específica de cada tipo de recurso. Datas são UTC. Alterações administrativas incrementam `versoes_configuracao` e geram evento de auditoria.

## Cliente

O cache SQLite local terá esquema versionado para:

- configuração e sua versão monotônica;
- recursos efetivos;
- políticas;
- preferências permitidas;
- metadados da última sincronização.

A troca de snapshot é atômica: uma resposta validada substitui o snapshot completo. Falha de rede, JSON inválido ou versão regressiva preserva o snapshot anterior.

## Identificadores e precedência

Recursos, usuários e atribuições usam UUID. A unicidade de atribuição é composta por recurso, tipo de alvo e alvo. O estado efetivo segue `Bloqueado > Obrigatorio > Disponivel > não atribuído`; empates usam escopo mais específico e, no mesmo escopo, o estado mais restritivo.
