using FluentAssertions;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;

namespace TodoApp.UnitTests.Domain;

public class TarefaTests
{
    private static readonly DateTime Agora = new(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Vencimento = new(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

    private static Tarefa CriarValida() =>
        Tarefa.Criar("Estudar Dapper", "Ler a documentação oficial", Vencimento, Prioridade.Alta, Agora);

    // ------------------------------------------------------------------ Criar

    [Fact]
    public void Criar_ComDadosValidos_DefinePropriedades()
    {
        var tarefa = Tarefa.Criar("Comprar pão", "Padaria da esquina", Vencimento, Prioridade.Media, Agora);

        tarefa.Titulo.Should().Be("Comprar pão");
        tarefa.Descricao.Should().Be("Padaria da esquina");
        tarefa.DataVencimento.Should().Be(Vencimento);
        tarefa.Prioridade.Should().Be(Prioridade.Media);
        tarefa.CriadoEm.Should().Be(Agora);
    }

    [Fact]
    public void Criar_SempreNasceComoPendente()
        => CriarValida().Status.Should().Be(StatusTarefa.Pendente);

    [Fact]
    public void Criar_NaoDefineAtualizadoEm()
        => CriarValida().AtualizadoEm.Should().BeNull();

    [Fact]
    public void Criar_NaoAtribuiId_AntesDePersistir()
        => CriarValida().Id.Should().Be(0);

    [Fact]
    public void Criar_PermiteDescricaoNula()
        => Tarefa.Criar("Sem descrição", null, Vencimento, Prioridade.Baixa, Agora)
            .Descricao.Should().BeNull();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_ComTituloVazio_Lanca(string? titulo)
    {
        var act = () => Tarefa.Criar(titulo!, null, Vencimento, Prioridade.Baixa, Agora);
        act.Should().Throw<ArgumentException>().WithParameterName("titulo");
    }

    [Fact]
    public void Criar_ComTituloDe100Caracteres_EhAceito()
    {
        var act = () => Tarefa.Criar(new string('a', 100), null, Vencimento, Prioridade.Baixa, Agora);
        act.Should().NotThrow();
    }

    [Fact]
    public void Criar_ComTituloAcimaDe100Caracteres_Lanca()
    {
        var act = () => Tarefa.Criar(new string('a', 101), null, Vencimento, Prioridade.Baixa, Agora);
        act.Should().Throw<ArgumentException>().WithParameterName("titulo");
    }

    [Fact]
    public void Criar_ComDataVencimentoDefault_Lanca()
    {
        var act = () => Tarefa.Criar("Título", null, default, Prioridade.Baixa, Agora);
        act.Should().Throw<ArgumentException>().WithParameterName("dataVencimento");
    }

    [Fact]
    public void Criar_ComPrioridadeInvalida_Lanca()
    {
        var act = () => Tarefa.Criar("Título", null, Vencimento, (Prioridade)99, Agora);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("prioridade");
    }

    [Theory]
    [InlineData(Prioridade.Baixa)]
    [InlineData(Prioridade.Media)]
    [InlineData(Prioridade.Alta)]
    public void Criar_AceitaCadaPrioridadeValida(Prioridade prioridade)
    {
        var act = () => Tarefa.Criar("Título", null, Vencimento, prioridade, Agora);
        act.Should().NotThrow();
    }

    [Fact]
    public void Criar_AceitaDataVencimentoNoPassado()
    {
        // "Data futura" é regra de entrada (FluentValidation), não invariante de domínio.
        var act = () => Tarefa.Criar("Título", null, new DateTime(2000, 1, 1), Prioridade.Baixa, Agora);
        act.Should().NotThrow();
    }

    // -------------------------------------------------------------- Atualizar

    [Fact]
    public void Atualizar_ComDadosValidos_AtualizaCamposEMarcaAtualizadoEm()
    {
        var tarefa = CriarValida();
        var novaData = new DateTime(2027, 1, 1);
        var quando = Agora.AddDays(1);

        tarefa.Atualizar("Novo título", "Nova descrição", novaData,
            StatusTarefa.EmAndamento, Prioridade.Baixa, quando);

        tarefa.Titulo.Should().Be("Novo título");
        tarefa.Descricao.Should().Be("Nova descrição");
        tarefa.DataVencimento.Should().Be(novaData);
        tarefa.Status.Should().Be(StatusTarefa.EmAndamento);
        tarefa.Prioridade.Should().Be(Prioridade.Baixa);
        tarefa.AtualizadoEm.Should().Be(quando);
    }

    [Fact]
    public void Atualizar_PreservaCriadoEm()
    {
        var tarefa = CriarValida();
        tarefa.Atualizar("X", null, Vencimento, StatusTarefa.Concluida, Prioridade.Alta, Agora.AddDays(1));
        tarefa.CriadoEm.Should().Be(Agora);
    }

    [Fact]
    public void Atualizar_PermiteLimparDescricao()
    {
        var tarefa = CriarValida();
        tarefa.Atualizar("Título", null, Vencimento, StatusTarefa.Pendente, Prioridade.Alta, Agora);
        tarefa.Descricao.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Atualizar_ComTituloVazio_Lanca(string? titulo)
    {
        var tarefa = CriarValida();
        var act = () => tarefa.Atualizar(titulo!, null, Vencimento, StatusTarefa.Pendente, Prioridade.Baixa, Agora);
        act.Should().Throw<ArgumentException>().WithParameterName("titulo");
    }

    [Fact]
    public void Atualizar_ComTituloAcimaDe100Caracteres_Lanca()
    {
        var tarefa = CriarValida();
        var act = () => tarefa.Atualizar(new string('a', 101), null, Vencimento,
            StatusTarefa.Pendente, Prioridade.Baixa, Agora);
        act.Should().Throw<ArgumentException>().WithParameterName("titulo");
    }

    [Fact]
    public void Atualizar_ComDataVencimentoDefault_Lanca()
    {
        var tarefa = CriarValida();
        var act = () => tarefa.Atualizar("Título", null, default, StatusTarefa.Pendente, Prioridade.Baixa, Agora);
        act.Should().Throw<ArgumentException>().WithParameterName("dataVencimento");
    }

    [Fact]
    public void Atualizar_ComStatusInvalido_Lanca()
    {
        var tarefa = CriarValida();
        var act = () => tarefa.Atualizar("Título", null, Vencimento, (StatusTarefa)99, Prioridade.Baixa, Agora);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("status");
    }

    [Fact]
    public void Atualizar_ComPrioridadeInvalida_Lanca()
    {
        var tarefa = CriarValida();
        var act = () => tarefa.Atualizar("Título", null, Vencimento, StatusTarefa.Pendente, (Prioridade)99, Agora);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("prioridade");
    }

    [Theory]
    [InlineData(StatusTarefa.Pendente)]
    [InlineData(StatusTarefa.EmAndamento)]
    [InlineData(StatusTarefa.Concluida)]
    public void Atualizar_AceitaCadaStatusValido(StatusTarefa status)
    {
        var tarefa = CriarValida();
        var act = () => tarefa.Atualizar("Título", null, Vencimento, status, Prioridade.Baixa, Agora);
        act.Should().NotThrow();
    }

    [Fact]
    public void Atualizar_PermiteDataNoPassado()
    {
        // Editar uma tarefa antiga (vencimento já passou) deve continuar possível.
        var tarefa = CriarValida();
        var act = () => tarefa.Atualizar("Título", null, new DateTime(2000, 1, 1),
            StatusTarefa.Concluida, Prioridade.Baixa, Agora);
        act.Should().NotThrow();
    }
}
