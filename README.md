# TodoApp — API de Gerenciamento de Tarefas

API REST para gerenciamento de tarefas (To-Do List) com **importação em massa via planilha Excel (.xlsx)**, construída em **.NET 8** seguindo **Clean Architecture**, persistência com **Dapper** sobre **SQL Server** e proteção por **JWT**.

> Teste técnico — Desenvolvedor Pleno (LBCA).

---

## Sumário

- [Stack](#stack)
- [Arquitetura](#arquitetura)
- [Como rodar](#como-rodar)
- [Autenticação](#autenticação)
- [Endpoints](#endpoints)
- [Importação em massa](#importação-em-massa)
- [Banco de dados](#banco-de-dados)
- [Testes](#testes)
- [Decisões técnicas](#decisões-técnicas)

---

## Stack

- **.NET 8** (LTS) / C#
- **Dapper** (micro-ORM) + **SQL Server**
- **FluentValidation** (validação de entrada)
- **ClosedXML** (leitura de .xlsx)
- **JWT Bearer** (autenticação)
- **Swagger / OpenAPI** (documentação)
- **Serilog** (logging estruturado)
- **xUnit + FluentAssertions + NSubstitute + Testcontainers** (testes)

---

## Arquitetura

Clean Architecture com a regra de dependência apontando sempre para o domínio:

```
TodoApp.Api            → Controllers, JWT, Swagger, middleware de exceção (composition root)
   ↓ depende de
TodoApp.Application     → Casos de uso (services), DTOs, validação, contratos (ports)
   ↓ depende de
TodoApp.Domain         → Entidade Tarefa, enums e invariantes (núcleo, sem dependências)

TodoApp.Infrastructure → Implementa os ports da Application: Dapper, SqlBulkCopy, ClosedXML
                         (a Api referencia a Infra apenas para compor a injeção de dependência)
```

Princípios aplicados: **SOLID**, **injeção de dependência**, **separação de responsabilidades**,
código **assíncrono** (`async/await` + `CancellationToken`) de ponta a ponta.

---

## Como rodar

### Opção 1 — Docker Compose (recomendado)

Sobe a API **e** o SQL Server com um comando. O schema é criado automaticamente no startup.

```bash
docker compose up --build
```

- Swagger: <http://localhost:8080/swagger>
- A API espera o SQL Server ficar pronto (retry com backoff no startup), então o primeiro boot pode levar alguns segundos.

### Opção 2 — Local (.NET SDK 8 + um SQL Server)

```bash
dotnet run --project src/TodoApp.Api
```

A connection string padrão (`appsettings.json`) aponta para `localhost,1433`. Ajuste se necessário,
ou use uma variável de ambiente: `ConnectionStrings__TodoDb`.

> **Não abre o Swagger no navegador (ERR_CONNECTION_RESET)?** Em algumas máquinas Windows o
> encaminhamento de porta HTTP do Docker Desktop reseta conexões `host → container` (o TCP conecta,
> mas o HTTP cai). Solução: rode a **API nativamente** (Opção 2) — o SQL pode continuar no container,
> pois a conexão ao banco (TDS) não é afetada:
> ```bash
> docker compose up -d sqlserver
> dotnet run --project src/TodoApp.Api --urls http://localhost:8080
> ```

### Front-end (opcional)

Um front-end mínimo em **React + Vite + TypeScript** está em [`frontend/`](frontend/) — login, lista de
tarefas com paginação/filtros, CRUD e a tela de importação com o relatório de erros.

```bash
cd frontend
npm install
npm run dev
```

Acesse <http://localhost:5173>. O Vite faz proxy de `/api` para a API em `http://localhost:8080`
(sem CORS), então basta ter a API rodando (via Docker Compose ou local).

---

## Autenticação

Toda a API (exceto o login) exige um token **JWT**. Para obter um token:

```http
POST /api/auth/login
Content-Type: application/json

{ "usuario": "admin", "senha": "admin123" }
```

Resposta:

```json
{ "token": "eyJhbGciOi...", "expiraEm": "2026-06-03T12:00:00Z" }
```

No **Swagger**, clique em **Authorize** e cole o token (sem o prefixo `Bearer`).
Em chamadas diretas, use o header `Authorization: Bearer <token>`.

> As credenciais ficam em `appsettings.json` (seção `Auth`) e o segredo do JWT em `Jwt:Key`.

---

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/auth/login` | Autentica e devolve o token JWT |
| `GET` | `/api/tarefas` | Lista tarefas **paginadas** (filtros: `status`, `prioridade`, `busca`) |
| `GET` | `/api/tarefas/{id}` | Obtém uma tarefa |
| `POST` | `/api/tarefas` | Cria uma tarefa |
| `PUT` | `/api/tarefas/{id}` | Atualiza uma tarefa |
| `DELETE` | `/api/tarefas/{id}` | Remove uma tarefa |
| `POST` | `/api/tarefas/importar` | Importa tarefas em massa via `.xlsx` |

Exemplo de listagem paginada: `GET /api/tarefas?pagina=1&tamanhoPagina=20&status=Pendente&prioridade=Alta&busca=relatorio`

**Campos da tarefa:** `Titulo` (obrigatório, ≤ 100), `Descricao` (opcional),
`DataVencimento` (obrigatória; **futura** na criação), `Status` (`Pendente`, `EmAndamento`, `Concluida`),
`Prioridade` (`Baixa`, `Media`, `Alta`).

---

## Importação em massa

`POST /api/tarefas/importar` recebe um arquivo `.xlsx` (campo `arquivo`, multipart) com as colunas:

| Título | Descrição | Data de Vencimento | Prioridade |
|--------|-----------|--------------------|------------|

> O `Status` não é importado — toda tarefa importada nasce **Pendente**.

### Resiliência

Linhas inválidas **não interrompem** o processo nem geram erro 500. As linhas válidas são salvas
(em lote, via `SqlBulkCopy`) e o endpoint retorna um **relatório** indicando cada falha:

```json
{
  "nomeArquivo": "tarefas.xlsx",
  "totalLinhas": 100,
  "importadas": 92,
  "falhas": 8,
  "erros": [
    { "linha": 5,  "coluna": "DataVencimento", "valor": "01/01/2020", "erro": "A data de vencimento deve ser futura." },
    { "linha": 12, "coluna": "Titulo",         "valor": "",           "erro": "O título é obrigatório." }
  ]
}
```

O parser é tolerante: aceita datas em vários formatos (`yyyy-MM-dd`, `dd/MM/yyyy`, etc.) e a prioridade
em texto, ignorando acentos e maiúsculas/minúsculas (`alta`, `Alta`, `ALTA`).

Uma planilha de exemplo (com cenários de sucesso e erro) está em [`exemplos/`](exemplos/).

---

## Banco de dados

- Persistência com **Dapper** (queries parametrizadas; `SqlBulkCopy` para a carga em massa).
- Scripts de criação versionados em [`db/`](db/) — entregues como `.sql` e também **embutidos** na
  aplicação, que os aplica de forma **idempotente** no startup (com retry, para o cenário de container).
- Índice `IX_Tarefas_Status_DataVencimento` (covering) dá suporte à listagem paginada com filtros.

---

## Testes

```bash
# Testes unitários (domínio + aplicação) — não exigem Docker
dotnet test tests/TodoApp.UnitTests

# Testes de integração (sobem um SQL Server real via Testcontainers — exigem Docker)
dotnet test tests/TodoApp.IntegrationTests
```

- **Unitários:** entidade `Tarefa` (invariantes), `ConversorPlanilha`, `TarefaService` e
  `ImportacaoService` (incluindo o cenário de planilha mista válida/inválida).
- **Integração:** sobem a API completa (`WebApplicationFactory`) contra um SQL Server em container,
  cobrindo autenticação (401 sem token), CRUD ponta-a-ponta e a importação de um `.xlsx` real.

---

## Decisões técnicas

- **JWT Bearer:** escolhido por ser *stateless*, padrão de mercado para APIs REST, carregar claims e
  integrar bem com SPAs. Para o cenário de integração com sistemas parceiros (máquina-a-máquina), uma
  **API Key** seria uma alternativa válida. A autenticação foi mantida simples (usuário semeado em
  configuração); em produção, usuários ficariam em banco com senha *hasheada*.
- **Importação síncrona:** o enunciado pede que *o endpoint retorne o relatório*, então o processamento
  é síncrono (leitura em streaming + validação linha-a-linha + `SqlBulkCopy` em lote). Para volumes
  muito grandes, o passo natural de escala seria um processamento **assíncrono** (`202 Accepted` + `jobId`
  + endpoint de status, com uma fila/worker) — o relatório passaria a ser consultado, não retornado.
- **Validação em duas camadas:** regras de entrada (incl. "data futura", relativa ao tempo) ficam no
  **FluentValidation** da Application; o **domínio** tem *guard clauses* como última linha de defesa, e o
  **banco** tem `CHECK constraints` (defesa em profundidade). Isso mantém a importação resiliente, sem usar
  exceções como fluxo de controle.
- **Mapeamento manual DTO↔entidade** (sem AutoMapper) e **Dapper** (sem EF): menos "mágica", controle
  explícito do SQL e da performance.
