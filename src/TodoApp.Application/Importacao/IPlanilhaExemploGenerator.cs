namespace TodoApp.Application.Importacao;

/// <summary>Variações da planilha de exemplo gerada para download.</summary>
public enum TipoPlanilhaExemplo
{
    /// <summary>10 tarefas válidas (importação bem-sucedida).</summary>
    Completo,

    /// <summary>Linhas válidas e inválidas (demonstra o relatório de resiliência).</summary>
    ComErros
}

/// <summary>
/// Gera uma planilha .xlsx de exemplo (no modelo aceito pela importação), para o
/// usuário baixar, preencher e reenviar.
/// </summary>
public interface IPlanilhaExemploGenerator
{
    byte[] Gerar(TipoPlanilhaExemplo tipo);
}
