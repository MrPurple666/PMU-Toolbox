# PROJETO — TOOLBOX CORPORATIVO PARA WINDOWS

Desenvolva uma aplicação corporativa chamada provisoriamente **Toolbox Corporativo**, composta por:

```text
┌──────────────────────────────┐
│ CLIENTE WINDOWS              │
│ C# / .NET / WinUI 3         │
└──────────────┬───────────────┘
               │ HTTPS/JSON
               ▼
┌──────────────────────────────┐
│ SERVIDOR TOOLBOX             │
│ PHP / Yii 3                  │
│ API REST + Administração     │
└──────────────┬───────────────┘
               │
        ┌──────┴───────┐
        ▼              ▼
   Banco de dados    LDAP/AD
```

O objetivo é substituir a distribuição excessiva de atalhos, scripts de logon e GPOs utilizados apenas para disponibilizar sistemas e ferramentas aos usuários.

O Toolbox deverá funcionar como um **catálogo corporativo inteligente e personalizado de recursos**.

O princípio fundamental é:

> O administrador define o que o usuário pode ou deve possuir. O usuário personaliza aquilo que lhe é permitido personalizar.

---

# 1. REGRA OBRIGATÓRIA — CÓDIGO EM PORTUGUÊS

TODO código desenvolvido especificamente para este projeto deverá utilizar nomenclatura em **português brasileiro**.

Isso inclui:

- classes;
- interfaces;
- métodos;
- propriedades;
- variáveis;
- enums;
- DTOs;
- serviços;
- ViewModels;
- nomes de arquivos próprios;
- comentários;
- documentação;
- mensagens de log;
- mensagens de erro;
- testes.

Exemplos:

CORRETO:

```csharp
public interface IServicoIdentidadeUsuario
{
    Task<UsuarioWindows> ObterUsuarioAtualAsync(
        CancellationToken tokenCancelamento);
}
```

```csharp
public sealed class UsuarioWindows
{
    public string NomeUsuario { get; init; }
    public string NomeExibicao { get; init; }
    public string Dominio { get; init; }
    public string NomeComputador { get; init; }
}
```

```csharp
var recursosPermitidos = await servicoRecursos
    .ObterRecursosDoUsuarioAsync(usuario);
```

EVITAR:

```csharp
UserService
GetCurrentUser()
allowedResources
ResourceManager
AuthenticationProvider
```

Naturalmente, nomes pertencentes ao .NET, WinUI, Yii, bibliotecas e APIs externas permanecem inalterados:

```csharp
CancellationToken
HttpClient
ObservableCollection
NavigationView
ICommand
Task
```

Não traduzir artificialmente APIs de terceiros.

O mesmo princípio deve ser aplicado ao backend Yii.

Exemplo:

```php
final class ServicoRecursosUsuario
{
    public function obterRecursosPermitidos(Usuario $usuario): array
    {
    }
}
```

---

# 2. TECNOLOGIAS

## Cliente

Utilizar preferencialmente:

```text
C#
.NET moderno/LTS ou versão estável adequada
WinUI 3
Windows App SDK
CommunityToolkit.Mvvm
Microsoft.Extensions.DependencyInjection
Microsoft.Extensions.Logging
SQLite
```

Se WinUI 3 apresentar uma limitação técnica significativa para alguma funcionalidade, avaliar WPF moderno + Fluent Design, mas somente substituir WinUI 3 mediante justificativa técnica.

Não utilizar WinForms.

## Servidor

Utilizar:

```text
PHP 8.x
Yii 3
API REST
SQLite inicialmente
```

Projetar a persistência para permitir migração simples para:

```text
PostgreSQL
```

em ambientes maiores.

O servidor deve ser leve o suficiente para funcionar em uma VM/container pequeno.

---

# 3. RESPONSABILIDADES

Separar claramente responsabilidades.

## Active Directory / LDAP

Responsável futuramente por:

```text
Quem é o usuário?
De quais grupos ele faz parte?
```

## Servidor Toolbox

Responsável por:

```text
Quais recursos existem?
Quem pode utilizá-los?
Quem deve obrigatoriamente recebê-los?
Quais recursos foram removidos?
Quais políticas existem?
Qual configuração deve chegar a cada cliente?
```

## Cliente Windows

Responsável por:

```text
Identificar usuário
Sincronizar configuração
Aplicar políticas
Apresentar Toolbox
Abrir recursos
Manter cache
Manter preferências permitidas
```

NÃO utilizar GPO ou LDAP como banco de configuração do Toolbox.

---

# 4. HIERARQUIA ADMINISTRATIVA

Implementar RBAC no servidor.

Perfis iniciais:

```text
Superadministrador
Administrador
GestorDeSetor
Usuario
```

## Superadministrador

Pode:

- administrar tudo;
- criar administradores;
- alterar configurações globais;
- administrar integrações;
- administrar políticas;
- visualizar auditoria.

## Administrador

Pode:

- cadastrar recursos;
- editar recursos;
- desativar recursos;
- atribuir recursos;
- remover atribuições;
- administrar usuários;
- administrar grupos internos;
- administrar máquinas;
- consultar sincronizações.

## GestorDeSetor

Opcionalmente poderá administrar somente recursos e usuários pertencentes ao seu escopo.

## Usuário

Somente utiliza e personaliza aquilo que a política permitir.

---

# 5. CONCEITO DE CATÁLOGO

O administrador mantém um:

```text
Catálogo Corporativo
```

Exemplo:

```text
ID    RECURSO                   TIPO
01    Portal Institucional      Web
02    Portal de Contratos       Web
03    Webmail                   Web
04    Pasta Financeiro          Rede
05    Pasta RH                  Rede
06    Área de Trabalho Remota   Aplicação
07    Sistema Tributário        Web
08    Ferramentas TI            Grupo
```

Cadastrar uma vez e posteriormente atribuir.

---

# 6. TIPOS DE RECURSOS

Implementar:

```text
Web
PastaRede
PastaLocal
Aplicacao
Documento
FerramentaWindows
ComandoControlado
GrupoDeRecursos
```

Criar arquitetura extensível para novos tipos.

Modelo:

```csharp
public abstract class RecursoToolbox
{
    public Guid Id { get; init; }

    public string Nome { get; set; }

    public string? Descricao { get; set; }

    public TipoRecurso Tipo { get; set; }

    public bool Ativo { get; set; }
}
```

---

# 7. ATRIBUIÇÕES

Este é um dos componentes MAIS IMPORTANTES.

Um recurso poderá ser atribuído a:

```text
Todos
Usuário
Grupo interno
Grupo LDAP/AD
Setor
Computador
Conjunto de computadores
```

Exemplo:

```text
Portal Institucional
    └── Todos

Sistema de Contratos
    └── Grupo: Contratos

Pasta Financeiro
    └── Setor: Financeiro

Ferramentas avançadas
    └── Grupo AD: TI

Ferramenta específica
    └── Usuário: antonio
```

---

# 8. ATRIBUIÇÃO DIRETA PELO ADMINISTRADOR

O administrador deverá conseguir acessar:

```text
Administração
   ↓
Usuários
   ↓
antonio
```

e visualizar:

```text
Antônio

Recursos efetivos: 12

OBRIGATÓRIOS
✓ Portal Institucional
✓ Webmail

ATRIBUÍDOS
✓ Portal de Contratos
✓ Pasta TI

OPCIONAIS
○ Sistema de chamados
○ Documentação

BLOQUEADOS
✕ Sistema Financeiro
```

O administrador poderá:

```text
[ + Adicionar recurso ]

[ Remover ]

[ Tornar obrigatório ]

[ Bloquear ]

[ Restaurar herança ]
```

A alteração deverá chegar automaticamente ao cliente na próxima sincronização.

---

# 9. PRECEDÊNCIA DAS REGRAS

Criar mecanismo determinístico de resolução de políticas.

Um recurso poderá estar:

```csharp
public enum EstadoAtribuicao
{
    Herdado,
    Disponivel,
    Obrigatorio,
    Bloqueado
}
```

Regra geral:

```text
Bloqueado
    >
Obrigatório
    >
Disponível
    >
Não atribuído
```

Entretanto, permitir que uma regra individual explícita tenha precedência sobre herança de grupo quando configurado.

Documentar precisamente o algoritmo.

Criar testes automatizados extensivos para conflitos.

---

# 10. EXEMPLO DE HERANÇA

Considere:

```text
Todos
 └── Portal Institucional

Setor TI
 ├── GLPI
 ├── Zabbix
 └── Pasta TI

Grupo Administradores
 └── Ferramentas Administrativas

Usuário antonio
 ├── + Portal de Contratos
 └── - Zabbix
```

O servidor deverá calcular o conjunto efetivo automaticamente.

Criar:

```csharp
IServicoResolucaoRecursos
```

e equivalente no backend.

---

# 11. REMOÇÃO CENTRAL

Se TI desativar:

```text
Sistema Antigo
```

o servidor deverá marcar:

```text
Ativo = false
```

Na próxima sincronização:

```text
Servidor
   ↓
Cliente recebe catálogo atualizado
   ↓
Toolbox remove Sistema Antigo
```

Não deve ser necessário:

- alterar GPO;
- acessar computador;
- excluir `.lnk`;
- executar script;
- reiniciar máquina.

---

# 12. SINCRONIZAÇÃO

O cliente deverá sincronizar:

```text
Login
Inicialização
Intervalo configurável
Solicitação manual
```

Criar:

```csharp
IServicoSincronizacao
```

Fluxo:

```text
Identificar usuário
        ↓
Carregar cache
        ↓
Exibir Toolbox imediatamente
        ↓
Consultar servidor em background
        ↓
Receber nova configuração
        ↓
Validar
        ↓
Atualizar cache
        ↓
Atualizar UI
```

IMPORTANTE:

Não deixar o usuário olhando uma tela de loading enquanto o servidor responde.

Utilizar abordagem cache-first.

---

# 13. VERSIONAMENTO DE CONFIGURAÇÃO

Cada configuração deverá possuir versão.

Exemplo:

```json
{
    "versao": 142,
    "ultimaAlteracao": "2026-09-04T14:20:00Z"
}
```

Cliente envia:

```text
versaoAtual=141
```

Servidor poderá responder:

```text
304 / sem alteração
```

ou configuração 142.

Evitar baixar todo catálogo desnecessariamente.

---

# 14. CACHE OFFLINE

O Toolbox deverá continuar funcionando quando:

```text
Servidor Toolbox offline
Internet offline
LDAP offline
VPN indisponível
Rede corporativa instável
```

Utilizar última configuração válida.

Mostrar discretamente:

```text
Modo offline
Última sincronização: 09:32
```

Nunca apagar configuração válida apenas porque uma sincronização falhou.

---

# 15. IDENTIDADE WINDOWS

Identificar automaticamente:

```text
username
nome de exibição
domínio
DOMAIN\username
hostname
SID quando apropriado
```

Criar:

```csharp
public interface IServicoIdentidadeUsuario
```

Implementações:

```text
ServicoIdentidadeWindows
ServicoIdentidadeSimulada
ServicoIdentidadeLdap
```

LDAP inicialmente poderá ser stub, mas a arquitetura deverá estar pronta.

---

# 16. LOGIN

Em máquina pertencente ao domínio:

priorizar identidade Windows transparente.

Não solicitar senha novamente sem necessidade.

Preparar autenticação futura:

```text
Windows
LDAP
Token da API
```

Nunca armazenar senha do domínio.

---

# 17. ADMINISTRAÇÃO DE USUÁRIOS

Backend deverá possuir:

```text
Usuários
```

Cada usuário poderá possuir:

```text
username
domínio
nome
SID opcional
setor
grupos
estado
última sincronização
último computador
```

Não considerar hostname como identidade primária do usuário.

---

# 18. DESCOBERTA AUTOMÁTICA

Se um usuário autenticado ainda não existir no servidor:

```text
Cliente
   ↓
POST /api/sessoes
   ↓
Servidor identifica novo usuário
   ↓
Cria registro pendente/ativo conforme política
```

Configuração global:

```text
[✓] Permitir descoberta automática de usuários
```

---

# 19. COMPUTADORES

Manter inventário mínimo:

```text
Hostname
Usuário recente
Versão Windows
Versão Toolbox
Última sincronização
```

Não transformar o Toolbox em software invasivo de inventário.

Objetivo é diagnóstico e política.

---

# 20. PAINEL ADMINISTRATIVO WEB

O Yii deverá fornecer painel administrativo responsivo.

Dashboard:

```text
Toolbox Administração

Usuários ativos               184
Computadores                  213
Recursos                       37
Clientes desatualizados         8
Sem sincronizar > 7 dias        5
```

Menu:

```text
Dashboard
Recursos
Usuários
Grupos
Setores
Computadores
Políticas
Auditoria
Configurações
```

---

# 21. ADMINISTRAÇÃO DE RECURSOS

Tela:

```text
Recursos

[ + Novo recurso ]

Nome                  Tipo        Usuários
Portal                 Web          184
Contratos              Web           32
Financeiro             Web           18
Pasta TI               Rede          11
```

Ao editar:

```text
Nome
Descrição
Tipo
Destino
Categoria
Tags
Ícone
Ativo

ATRIBUIÇÕES

Todos
Usuários
Grupos
Setores
Computadores

COMPORTAMENTO

Obrigatório
Opcional
Ocultável
Favoritável
```

---

# 22. AÇÕES EM MASSA

Fundamental para administração corporativa.

Permitir selecionar vários:

```text
☑ João
☑ Maria
☑ Antônio
☑ Carlos
```

e executar:

```text
Adicionar recurso
Remover recurso
Adicionar ao setor
Adicionar ao grupo
Bloquear recurso
```

Também permitir:

```text
Setor inteiro
Grupo inteiro
Todos os usuários
```

---

# 23. GRUPOS INTERNOS

Além de grupos LDAP, permitir grupos próprios:

```text
TI
Compras
RH
Contabilidade
Secretários
Gestores
Estagiários
```

Um usuário pode pertencer a vários grupos.

Isso evita criar grupos AD exclusivamente para organizar atalhos.

---

# 24. SETORES

Criar conceito separado de grupo:

```text
Secretaria de Administração
 ├── RH
 ├── Compras
 └── TI
```

Permitir hierarquia opcional.

Um setor poderá herdar recursos do setor pai.

---

# 25. AUDITORIA

Toda alteração administrativa relevante deverá gerar evento.

Exemplo:

```text
04/09/2026 10:32
admin.ti

ADICIONOU:
Portal de Contratos

PARA:
Grupo Compras
```

Registrar:

```text
quem
quando
ação
objeto
valor anterior
valor novo
```

Nunca registrar credenciais.

---

# 26. HISTÓRICO E ROLLBACK

Manter histórico das alterações de catálogo/políticas.

Permitir visualizar:

```text
Versão 140
Versão 141
Versão 142 ← atual
```

Idealmente permitir restaurar configuração anterior.

Uma restauração deve criar uma NOVA versão, não apagar histórico.

---

# 27. WEB

Recursos web devem:

- abrir navegador padrão;
- obter favicon;
- cachear favicon;
- suportar ícone personalizado;
- suportar URLs parametrizadas.

Exemplo:

```text
https://sistema.exemplo.local/usuario/{nomeUsuario}
```

---

# 28. PASTAS DE REDE

Suportar UNC:

```text
\\arquivos\publico
\\arquivos\usuarios\{nomeUsuario}
```

Abrir via Explorer.

Opcionalmente verificar disponibilidade.

Nunca bloquear UI.

---

# 29. APLICAÇÕES

Suportar:

```text
.exe
.lnk
.msc
documentos
protocolos registrados
```

Exemplo:

```text
mstsc.exe
control.exe
compmgmt.msc
```

---

# 30. SEGURANÇA PARA COMANDOS

Configuração recebida do servidor NÃO poderá resultar em execução arbitrária.

Criar allowlist.

Separar:

```text
Aplicação
```

de:

```text
Comando administrativo
```

Comandos administrativos devem exigir política específica.

Validar argumentos.

Evitar:

```text
cmd.exe /c {conteudoRemoto}
powershell.exe {conteudoRemoto}
```

---

# 31. VARIÁVEIS

Criar:

```csharp
IServicoResolucaoVariaveis
```

Suportar:

```text
{nomeUsuario}
{dominio}
{usuarioDominio}
{nomeComputador}
{perfilUsuario}
{documentos}
{areaTrabalho}
```

Exemplo:

```text
\\arquivos\usuarios\{nomeUsuario}
```

---

# 32. TOOLBOX DO USUÁRIO

Home:

```text
Bom dia, Antônio

Favoritos
[Contratos] [Webmail] [Pasta TI]

Recentes
[...]

Sistemas
[...]

Arquivos
[...]

Ferramentas
[...]
```

Adicionar:

```text
Pesquisa
Categorias
Favoritos
Recentes
Mais utilizados
```

---

# 33. PESQUISA RÁPIDA

Atalho:

```text
Ctrl + K
```

Pesquisar:

```text
nome
descrição
tags
aliases
categoria
```

---

# 34. QUICK LAUNCHER

Implementar janela compacta opcional:

```text
Ctrl + Alt + Space
```

Exemplo:

```text
┌─────────────────────────────────────┐
│ contratos                           │
├─────────────────────────────────────┤
│ Portal de Contratos                 │
│ Pasta Contratos                     │
└─────────────────────────────────────┘
```

---

# 35. FAVICONS

Criar:

```csharp
IServicoFavicon
```

Implementar:

- discovery HTML;
- `<link rel="icon">`;
- ICO;
- PNG;
- SVG quando suportado;
- `/favicon.ico`;
- cache;
- expiração;
- fallback por iniciais.

Ícone definido pelo administrador deverá ter prioridade sobre favicon automático.

---

# 36. PERSONALIZAÇÃO

O usuário poderá, quando política permitir:

```text
favoritar
reordenar
ocultar opcional
alterar tamanho
selecionar recursos opcionais
```

O usuário NÃO poderá remover recurso marcado como:

```text
Obrigatório
```

O servidor sempre terá precedência sobre preferência local.

---

# 37. ATUALIZAÇÃO EM TEMPO QUASE REAL

Preparar arquitetura para que alterações administrativas possam chegar rapidamente aos clientes.

Inicialmente:

```text
sincronização periódica
```

Por exemplo:

```text
a cada 5 minutos
```

Mas deixar abstração preparada para:

```text
SignalR
WebSocket
Server-Sent Events
```

Não implementar complexidade desnecessária no MVP.

---

# 38. BANCO DE DADOS DO SERVIDOR

Modelar pelo menos:

```text
usuarios
computadores
recursos
categorias
grupos
usuarios_grupos
setores
atribuicoes
politicas
versoes_configuracao
eventos_auditoria
```

Evitar duplicação de dados.

Criar migrations.

---

# 39. API

Projetar API versionada:

```text
/api/v1/
```

Exemplos:

```text
POST /api/v1/sessao

GET /api/v1/configuracao
GET /api/v1/recursos
GET /api/v1/usuario/perfil

POST /api/v1/sincronizacao
```

Admin:

```text
/api/v1/admin/recursos
/api/v1/admin/usuarios
/api/v1/admin/grupos
/api/v1/admin/setores
/api/v1/admin/atribuicoes
```

Painel web não deve necessariamente consumir endpoints administrativos externos se Yii puder executar essas operações internamente de forma mais segura.

---

# 40. DTO DE SINCRONIZAÇÃO

Exemplo:

```json
{
  "usuario": {
    "nomeUsuario": "antonio",
    "dominio": "PREFEITURA"
  },
  "computador": {
    "nome": "PC-TI-023",
    "versaoWindows": "..."
  },
  "cliente": {
    "versao": "1.0.0"
  },
  "configuracaoAtual": 141
}
```

Resposta:

```json
{
  "versao": 142,
  "usuario": {
    "nomeExibicao": "Antonio"
  },
  "recursos": [],
  "politicas": {}
}
```

---

# 41. AUTENTICAÇÃO DA API

Não confiar simplesmente em:

```text
username enviado pelo cliente
```

como prova de identidade.

Criar arquitetura preparada para autenticação adequada.

No ambiente inicial, documentar as limitações.

Considerar futuramente:

```text
Windows Integrated Authentication
Kerberos
LDAP
tokens de dispositivo
tokens de sessão
```

O servidor nunca deve conceder acesso privilegiado simplesmente porque o cliente enviou:

```json
{
    "nomeUsuario": "administrador"
}
```

---

# 42. ARQUITETURA C#

Criar solution:

```text
ToolboxCorporativo.sln

src/
 ├── ToolboxCorporativo.App
 ├── ToolboxCorporativo.Dominio
 ├── ToolboxCorporativo.Aplicacao
 ├── ToolboxCorporativo.Infraestrutura
 └── ToolboxCorporativo.Testes
```

Exemplo:

```text
ToolboxCorporativo.Dominio/
 ├── Entidades/
 ├── Enumeracoes/
 ├── Interfaces/
 └── Excecoes/

ToolboxCorporativo.Aplicacao/
 ├── Servicos/
 ├── CasosDeUso/
 ├── DTOs/
 └── ViewModels/

ToolboxCorporativo.Infraestrutura/
 ├── Api/
 ├── Identidade/
 ├── Ldap/
 ├── Rede/
 ├── Persistencia/
 ├── Favicons/
 ├── Cache/
 └── Configuracao/
```

---

# 43. ARQUITETURA DO SERVIDOR

Separar:

```text
Dominio
Aplicacao
Infraestrutura
InterfaceWeb
Api
```

sem exagerar em abstrações desnecessárias.

O projeto deve permanecer compreensível para outra equipe de TI.

---

# 44. DIAGNÓSTICO

Cliente:

```text
Configurações
    ↓
Diagnóstico
```

Mostrar:

```text
Usuário
Domínio
Computador
Versão Toolbox
Servidor
Estado servidor
Estado LDAP
Versão configuração
Última sincronização
Cache
```

Botão:

```text
Copiar diagnóstico
```

Remover informações sensíveis.

---

# 45. PAINEL DE COMPUTADORES

Administrador poderá visualizar:

```text
PC-TI-001
Usuário: antonio
Toolbox: 1.3.2
Última sincronização: há 2 min
Configuração: 142
```

Isso permitirá identificar máquinas que não estão recebendo atualizações.

---

# 46. LOGS

Cliente e servidor devem utilizar logs estruturados.

Código e mensagens próprias em português.

Nunca registrar:

```text
senha
token completo
cookie
Authorization
credencial LDAP
```

---

# 47. TESTES

Cliente:

```text
ServicoResolucaoVariaveis
ServicoResolucaoRecursos
ServicoAutorizacaoRecursos
ServicoPesquisa
Cache
ParserConfiguracao
ValidadorUri
ValidadorCaminho
```

Servidor:

```text
atribuição individual
atribuição por grupo
atribuição por setor
bloqueio
obrigatoriedade
herança
precedência
RBAC
auditoria
sincronização
```

Criar especialmente testes para conflitos de regras.

---

# 48. EXPERIÊNCIA ADMINISTRATIVA DESEJADA

Um administrador deve conseguir fazer:

```text
Usuários
    ↓
Antônio
    ↓
Adicionar recurso
    ↓
Portal de Contratos
    ↓
Salvar
```

e, poucos segundos/minutos depois:

```text
Toolbox do Antônio

+ Portal de Contratos
```

Da mesma maneira:

```text
Recursos
    ↓
Sistema Antigo
    ↓
Desativar
```

deve removê-lo dos clientes na sincronização seguinte.

Este é um requisito central do sistema.

---

# 49. NÃO RECRIAR GPO

Não transformar o backend em uma cópia pior de GPO.

O Toolbox deve cuidar especificamente de:

```text
descoberta
organização
distribuição
autorização visual
personalização
abertura de recursos
```

Não tentar substituir políticas de segurança do Windows.

Permissões reais de:

```text
SMB
sistema web
aplicação
banco
```

continuam sendo responsabilidade dos próprios serviços/AD.

Ocultar um recurso no Toolbox NÃO é mecanismo de segurança.

---

# 50. MVP

O MVP estará completo quando:

1. Cliente WinUI 3 executar corretamente.
2. Identificar usuário Windows.
3. Consultar servidor Yii.
4. Funcionar offline utilizando cache.
5. Exibir catálogo personalizado.
6. Abrir URLs.
7. Abrir UNC.
8. Abrir aplicações.
9. Obter favicons.
10. Permitir favoritos.
11. Possuir pesquisa.
12. Possuir onboarding.
13. Administrador conseguir cadastrar recurso.
14. Administrador conseguir atribuir recurso a usuário.
15. Administrador conseguir atribuir a grupo.
16. Administrador conseguir remover atribuição.
17. Administrador conseguir bloquear recurso.
18. Administrador conseguir tornar recurso obrigatório.
19. Cliente receber alterações.
20. Auditoria registrar alteração.
21. Preferências locais respeitarem políticas administrativas.
22. Testes automatizados passarem.

---

# 51. ORDEM DE IMPLEMENTAÇÃO

Não implementar tudo simultaneamente.

## Fase 1

Criar arquitetura, solution C#, projeto Yii, banco e documentação.

## Fase 2

Implementar cliente local sem servidor.

## Fase 3

Implementar catálogo e tipos de recurso.

## Fase 4

Implementar backend Yii e migrations.

## Fase 5

Implementar API e sincronização.

## Fase 6

Implementar painel administrativo.

## Fase 7

Implementar usuários, grupos, setores e atribuições.

## Fase 8

Implementar cache offline e versionamento.

## Fase 9

Implementar favicons, pesquisa, favoritos e Quick Launcher.

## Fase 10

Implementar auditoria e diagnóstico.

## Fase 11

Preparar LDAP/AD.

## Fase 12

Testes, segurança, performance e documentação.

Em TODA fase:

```text
implementar
    ↓
compilar
    ↓
executar testes
    ↓
corrigir erros
    ↓
corrigir warnings relevantes
    ↓
documentar
    ↓
prosseguir
```

Não deixar dezenas de TODOs para fases anteriores.

---

# 52. RESULTADO FINAL

A arquitetura deverá permitir:

```text
                       ACTIVE DIRECTORY
                              │
                        identidade/grupos
                              │
                              ▼
┌───────────────┐       ┌──────────────┐
│ ADMINISTRADOR │──────▶│ SERVIDOR YII │
└───────────────┘       └───────┬──────┘
                                │
                         catálogo/políticas
                                │
                ┌───────────────┼───────────────┐
                ▼               ▼               ▼
           PC Antônio       PC Maria         PC João
                │               │               │
                ▼               ▼               ▼
          Meu Toolbox      Meu Toolbox      Meu Toolbox
```

O administrador controla centralmente:

```text
O QUE existe
QUEM recebe
QUEM não recebe
O QUE é obrigatório
O QUE está bloqueado
```

O usuário controla:

```text
favoritos
ordem
recursos opcionais
layout
personalizações permitidas
```

O Active Directory controla:

```text
identidade
grupos
permissões reais dos recursos
```

O Toolbox controla:

```text
distribuição
descoberta
organização
experiência de acesso
```

Comece verificando as versões estáveis atuais de .NET, Windows App SDK, WinUI 3, Yii e bibliotecas necessárias.

Depois apresente:

1. arquitetura final;
2. modelo de dados;
3. árvore dos projetos;
4. fluxo de autenticação;
5. fluxo de sincronização;
6. algoritmo de resolução das atribuições;
7. decisões de segurança.

Somente depois comece a implementação.

Implemente primeiro a Fase 1, execute builds/testes aplicáveis e continue incrementalmente até obter uma aplicação funcional.

Não utilize pseudocódigo quando código funcional puder ser escrito.
Não simplifique requisitos silenciosamente.
Não substitua nomenclatura em português por inglês por conveniência.