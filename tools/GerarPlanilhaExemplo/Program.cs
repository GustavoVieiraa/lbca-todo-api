using TodoApp.Application.Importacao;
using TodoApp.Infrastructure.Excel;

// Regenera as planilhas de exemplo do repositório usando o MESMO gerador da aplicação
// (fonte única). Uso: dotnet run --project tools/GerarPlanilhaExemplo -- [pasta-de-saida]

var pasta = args.Length > 0 ? args[0] : "exemplos";
Directory.CreateDirectory(pasta);

var gerador = new ClosedXmlPlanilhaExemploGenerator();

File.WriteAllBytes(Path.Combine(pasta, "tarefas-exemplo.xlsx"),
    gerador.Gerar(TipoPlanilhaExemplo.Completo));

File.WriteAllBytes(Path.Combine(pasta, "tarefas-exemplo-com-erros.xlsx"),
    gerador.Gerar(TipoPlanilhaExemplo.ComErros));

Console.WriteLine($"Planilhas geradas em: {Path.GetFullPath(pasta)}");
Console.WriteLine(" - tarefas-exemplo.xlsx (10 tarefas válidas)");
Console.WriteLine(" - tarefas-exemplo-com-erros.xlsx (válidas + inválidas)");
