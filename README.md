# TodoApp — API de Gerenciamento de Tarefas (LBCA)

API REST para gerenciamento de tarefas (To-Do List) com **importação em massa via planilha Excel (.xlsx)**, construída em **.NET 8** seguindo **Clean Architecture**, persistência com **Dapper** sobre **SQL Server**, proteção por **JWT** e um **front-end** em React/Vite/TypeScript.

> Teste técnico — Desenvolvedor Pleno (LBCA).

---

## Sumário

- [Stack](#stack)
- [Arquitetura](#arquitetura)
- [Como rodar](#como-rodar)
- [Autenticação](#autenticação)
- [Endpoints](#endpoints)
- [Importação em massa](#importação-em-massa)
- [Front-end](#front-end)
- [Banco de dados](#banco-de-dados)
- [Testes](#testes)
- [Coleção Postman](#coleção-postman)
- [Decisões técnicas](#decisões-técnicas)

---

## Stack

| Camada | Tecnologias |
|---|---|
| **API** | .NET 8 (LTS), C# |
| **Persistência** | Dapper (micro-ORM) + SQL Server |
| **Validação** | FluentValidation |
| **Excel** | ClosedXML |
| **Segurança** | JWT Bearer |
| **Docs** | Swagger / OpenAPI |
| **Logging** | Serilog |
| **Testes** | xUnit, FluentAssertions, NSubstitute, Testcontainers |
| **Front-end** | React + Vite + TypeScript |

---

## Arquitetura

**Clean Architecture** — a regra de dependência sempre aponta para o domínio:

```
TodoApp.Api            → Controllers, JWT, Swagger, middleware de exceção (composition root)
   ↓ depende de
TodoApp.Application    → Casos de uso (services), DTOs, validação, contratos (ports)
   ↓ depende de
TodoApp.Domain        → Entidade Tarefa, enums e invariantes (núcleo, sem dependências)

TodoApp.Infrastructure → Implementa os ports da Application: Dapper, SqlBulkCopy, ClosedXML
                         (a Api referencia a Infra apenas para compor a injeção de dependência)
```

```
.
├── src/
│   ├── TodoApp.Domain          # Entidade Tarefa, enums, invariantes (guard clauses)
│   ├── TodoApp.Application      # Services, DTOs, validators, ports (interfaces)
│   ├── TodoApp.Infrastructure   # Dapper, SqlBulkCopy, ClosedXML, migrator, clock
│   └── TodoApp.Api             # Controllers, JWT, Swagger, /health, middleware
├── tests/
│   ├── TodoApp.UnitTests        # Domínio + Application + leitor de Excel
│   └── TodoApp.IntegrationTests # API completa contra SQL Server (Testcontainers)
├── frontend/                    # SPA React + Vite + TypeScript
├── db/                          # Scripts .sql de criação
├── exemplos/                    # Planilhas .xlsx de exemplo
├── postman/                     # Coleção Postman
└── docker-compose.yml           # API + SQL Server + front-end
```

Princípios aplicados: **SOLID**, **injeção de dependência**, **separação de responsabilidades** e
código **assíncrono** (`async/await` + `CancellationToken`) de ponta a ponta.

---

## Como rodar

### Opção 1 — Docker Compose (recomendado)

Sobe **tudo** (API + SQL Server + front-end) com um comando. O schema do banco é criado
automaticamente no startup.

```bash
docker compose up --build
```

| Serviço | URL |
|---|---|
| Front-end | <http://localhost:5173> |
| Swagger (API) | <http://localhost:8080/swagger> |
| Health check | <http://localhost:8080/health> |

> A API espera o SQL Server ficar pronto (retry com backoff no startup), então o primeiro boot
> pode levar alguns segundos.

> **Não abre no navegador (ERR_CONNECTION_RESET)?** Em algumas máquinas Windows o encaminhamento
> de porta HTTP do Docker Desktop reseta conexões `host → container` (o TCP conecta, mas o HTTP cai).
> Nesse caso, rode a **API nativamente** (Opção 2) e o **front em modo dev** (seção [Front-end](#front-end)).
> O SQL pode continuar no container — a conexão ao banco (TDS) não é afetada:
> ```bash
> docker compose up -d sqlserver
> dotnet run --project src/TodoApp.Api --urls http://localhost:8080
> ```

### Opção 2 — API local (.NET SDK 8 + um SQL Server)

```bash
dotnet run --project src/TodoApp.Api
```

A connection string padrão (`appsettings.json`) aponta para `localhost,1433`. Ajuste se necessário,
ou sobrescreva pela variável de ambiente `ConnectionStrings__TodoDb`.

---

## Autenticação

Toda a API (exceto `login` e `health`) exige um token **JWT**.

```http
POST /api/auth/login
Content-Type: application/json

{ "usuario": "admin", "senha": "admin123" }
```

Resposta:

```json
{ "token": "eyJhbGciOi...", "expiraEm": "2026-06-03T12:00:00Z" }
```

- No **Swagger**: clique em **Authorize** e cole o token (sem o prefixo `Bearer`).
- Em chamadas diretas: header `Authorization: Bearer <token>`.

> As credenciais ficam em `appsettings.json` (seção `Auth`) e o segredo do JWT em `Jwt:Key`.
> Em produção, usuários ficariam em banco com senha *hasheada* e o segredo em um cofre (Key Vault/Secrets).

---

## Endpoints

| Método | Rota | Auth | Descrição |
|--------|------|:----:|-----------|
| `POST` | `/api/auth/login` | — | Autentica e devolve o token JWT |
| `GET` | `/health` | — | Saúde da API + conectividade com o banco |
| `GET` | `/api/tarefas` | 🔒 | Lista tarefas **paginadas** (filtros: `status`, `prioridade`, `busca`) |
| `GET` | `/api/tarefas/{id}` | 🔒 | Obtém uma tarefa |
| `POST` | `/api/tarefas` | 🔒 | Cria uma tarefa |
| `PUT` | `/api/tarefas/{id}` | 🔒 | Atualiza uma tarefa |
| `DELETE` | `/api/tarefas/{id}` | 🔒 | Remove uma tarefa |
| `GET` | `/api/tarefas/modelo` | 🔒 | Baixa planilha de exemplo (`?tipo=completo\|erros`) |
| `POST` | `/api/tarefas/importar` | 🔒 | Importa tarefas em massa via `.xlsx` |

Exemplo de listagem: `GET /api/tarefas?pagina=1&tamanhoPagina=20&status=Pendente&prioridade=Alta&busca=relatorio`

**Campos da tarefa:** `Titulo` (obrigatório, ≤ 100), `Descricao` (opcional),
`DataVencimento` (obrigatória; **não anterior a hoje** na criação), `Status`
(`Pendente`, `EmAndamento`, `Concluida`), `Prioridade` (`Baixa`, `Media`, `Alta`).

---

## Importação em massa

`POST /api/tarefas/importar` recebe um `.xlsx` (campo `arquivo`, multipart) no modelo:

| Título | Descrição | Data de Vencimento | Prioridade |
|--------|-----------|--------------------|------------|

> O `Status` não é importado — toda tarefa importada nasce **Pendente**.

### Resiliência

- **Linhas inválidas não interrompem** o processo nem geram erro 500: as válidas são salvas
  (em lote, via `SqlBulkCopy`) e o endpoint retorna um **relatório** com cada falha.
- **Arquivo corrompido ou fora do modelo** (cabeçalho com colunas erradas/ausentes) → `400`
  com mensagem clara (não importa dado errado silenciosamente).

```json
{
  "nomeArquivo": "tarefas.xlsx",
  "totalLinhas": 100,
  "importadas": 92,
  "falhas": 8,
  "erros": [
    { "linha": 5,  "coluna": "DataVencimento", "valor": "01/01/2020", "erro": "A data de vencimento não pode ser anterior a hoje." },
    { "linha": 12, "coluna": "Titulo",         "valor": "",           "erro": "O título é obrigatório." }
  ]
}
```

O parser é tolerante: aceita datas em vários formatos (`yyyy-MM-dd`, `dd/MM/yyyy`, etc.) e a
prioridade em texto, ignorando acentos e maiúsculas/minúsculas (`alta`, `Alta`, `ALTA`).

Há duas planilhas de exemplo em [`exemplos/`](exemplos/) — também baixáveis pela aplicação
(`GET /api/tarefas/modelo?tipo=completo|erros`, ou pelos botões na tela de importação):
- `tarefas-exemplo.xlsx` — 10 tarefas válidas (importação bem-sucedida);
- `tarefas-exemplo-com-erros.xlsx` — linhas válidas e inválidas (demonstra o relatório).

---

## Front-end

SPA em **React + Vite + TypeScript** (pasta [`frontend/`](frontend/)) com a identidade visual da LBCA
(tema dark, cor `#691440`):

- **Login** com JWT e **notificações** (toasts) padronizadas.
- **Kanban** de tarefas (colunas por status) com **arrastar-e-soltar** para mudar o status.
- CRUD completo (criar/editar/excluir) com modal de confirmação.
- **Importação** com área de upload (arrastar o arquivo) e o relatório de erros; download das
  planilhas de exemplo.

No **Docker Compose** o front já sobe pronto em <http://localhost:5173> (o nginx serve a SPA e faz
proxy de `/api` para a API). Para desenvolvimento:

```bash
cd frontend
npm install
npm run dev   # http://localhost:5173 (proxy /api -> http://localhost:8080)
```

---

## Banco de dados

- Persistência com **Dapper** (queries parametrizadas; `SqlBulkCopy` para a carga em massa).
- Scripts de criação versionados em [`db/`](db/) — entregues como `.sql` e também **embutidos** na
  aplicação, que os aplica de forma **idempotente** no startup (com retry, para o cenário de container).
- Índice `IX_Tarefas_Status_DataVencimento` (covering) dá suporte à listagem paginada com filtros.

---

## Testes

```bash
# Unitários (domínio + aplicação + leitor de Excel) — não exigem Docker
dotnet test tests/TodoApp.UnitTests

# Integração (sobem um SQL Server real via Testcontainers — exigem Docker)
dotnet test tests/TodoApp.IntegrationTests
```

- **Unitários:** entidade `Tarefa` (invariantes), `ConversorPlanilha`, `TarefaService`,
  `ImportacaoService` (cenário de planilha mista) e o leitor de Excel (arquivo inválido, cabeçalho).
- **Integração:** sobem a API completa (`WebApplicationFactory`) contra um SQL Server em container,
  cobrindo autenticação (401 sem token), CRUD ponta-a-ponta e a importação de um `.xlsx` real.

---

## Coleção Postman

Em [`postman/TodoApp.postman_collection.json`](postman/TodoApp.postman_collection.json).

1. Importe o arquivo no Postman.
2. Rode **Auth - Login** — o token é salvo automaticamente na variável `{{token}}` e reutilizado
   pelos demais requests.
3. A variável `{{baseUrl}}` aponta para `http://localhost:8080` (ajuste se necessário).

---

## Decisões técnicas

- **JWT Bearer:** *stateless*, padrão de mercado para APIs REST, carrega claims e integra bem com SPAs.
  Para integração máquina-a-máquina (sistemas parceiros), uma **API Key** seria alternativa válida.
  A autenticação foi mantida simples (usuário semeado em configuração) para o escopo do teste.
- **Importação síncrona:** o enunciado pede que *o endpoint retorne o relatório*, então o processamento
  é síncrono (validação linha-a-linha + `SqlBulkCopy` em lote). Para volumes muito grandes, os próximos
  passos de escala seriam: leitura em *streaming* via OpenXML SAX (o ClosedXML carrega a planilha em
  memória) e processamento **assíncrono** (`202 Accepted` + `jobId` + endpoint de status, com fila/worker).
- **Validação em camadas (defesa em profundidade):** regras de entrada (título, datas, enums) no
  **FluentValidation** da Application; *guard clauses* no **domínio** como última linha; e `CHECK constraints`
  no **banco**. Isso mantém a importação resiliente, sem usar exceções como fluxo de controle.
- **Regra de data inteligente:** criação não aceita data anterior a hoje; na atualização é possível
  **manter** a data de uma tarefa já vencida (para concluí-la), mas não **movê-la** para o passado.
- **Mapeamento manual DTO↔entidade** (sem AutoMapper) e **Dapper** (sem EF): menos "mágica", controle
  explícito do SQL e da performance.
