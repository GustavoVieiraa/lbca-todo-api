-- =============================================================
-- 0000_create_database.sql
-- Cria o banco de dados da aplicação (idempotente).
-- Deve ser executado conectado ao banco 'master'.
-- =============================================================
IF DB_ID(N'TodoDb') IS NULL
BEGIN
    CREATE DATABASE [TodoDb];
END
