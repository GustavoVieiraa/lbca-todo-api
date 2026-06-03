namespace TodoApp.Application.Importacao;

/// <summary>
/// Gera uma planilha .xlsx de exemplo (no modelo aceito pela importação), para o
/// usuário baixar, preencher e reenviar. Inclui linhas válidas e inválidas para
/// demonstrar o relatório de resiliência.
/// </summary>
public interface IPlanilhaExemploGenerator
{
    byte[] Gerar();
}
