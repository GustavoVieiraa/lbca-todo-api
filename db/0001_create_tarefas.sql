-- =============================================================
-- 0001_create_tarefas.sql
-- Cria a tabela de tarefas + índice de apoio à listagem paginada.
-- Idempotente e em lote único (sem GO) para poder rodar via sqlcmd
-- ou ser aplicado pela própria API no startup (Dapper ExecuteAsync).
-- Executar conectado ao banco 'TodoDb'.
-- =============================================================
IF OBJECT_ID(N'dbo.Tarefas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tarefas
    (
        Id              BIGINT         IDENTITY(1,1) NOT NULL,
        Titulo          NVARCHAR(100)  NOT NULL,
        Descricao       NVARCHAR(MAX)  NULL,
        DataVencimento  DATETIME2(0)   NOT NULL,
        Status          TINYINT        NOT NULL CONSTRAINT DF_Tarefas_Status   DEFAULT (0),
        Prioridade      TINYINT        NOT NULL,
        CriadoEm        DATETIME2(0)   NOT NULL CONSTRAINT DF_Tarefas_CriadoEm DEFAULT (SYSUTCDATETIME()),
        AtualizadoEm    DATETIME2(0)   NULL,

        CONSTRAINT PK_Tarefas PRIMARY KEY CLUSTERED (Id),

        -- Garante a integridade dos enums no nível do banco (defesa em profundidade).
        CONSTRAINT CK_Tarefas_Status     CHECK (Status     IN (0, 1, 2)),
        CONSTRAINT CK_Tarefas_Prioridade CHECK (Prioridade IN (0, 1, 2)),
        CONSTRAINT CK_Tarefas_Titulo     CHECK (LEN(LTRIM(RTRIM(Titulo))) > 0)
    );

    -- Índice de apoio à listagem paginada: filtra por Status e ordena por
    -- DataVencimento; INCLUDE torna o índice "covering" para a consulta da lista,
    -- evitando key lookups na tabela base.
    CREATE NONCLUSTERED INDEX IX_Tarefas_Status_DataVencimento
        ON dbo.Tarefas (Status, DataVencimento)
        INCLUDE (Titulo, Prioridade, CriadoEm);
END
