using ClosedXML.Excel;

// Gera uma planilha de exemplo com cenários de SUCESSO e ERRO para testar a importação.
// Uso: dotnet run --project tools/GerarPlanilhaExemplo -- [caminho-de-saida.xlsx]

var caminho = args.Length > 0 ? args[0] : "exemplos/tarefas-exemplo.xlsx";
var diretorio = Path.GetDirectoryName(Path.GetFullPath(caminho));
if (!string.IsNullOrEmpty(diretorio))
    Directory.CreateDirectory(diretorio);

// Datas fixas no futuro distante para a planilha continuar válida ao longo do tempo.
var futura = new DateTime(2030, 12, 31);
var passada = new DateTime(2020, 1, 1);

using var workbook = new XLWorkbook();
var ws = workbook.AddWorksheet("Tarefas");

ws.Cell(1, 1).Value = "Título";
ws.Cell(1, 2).Value = "Descrição";
ws.Cell(1, 3).Value = "Data de Vencimento";
ws.Cell(1, 4).Value = "Prioridade";

// ----- Linhas válidas -----
ws.Cell(2, 1).Value = "Preparar apresentação";
ws.Cell(2, 2).Value = "Slides do fechamento do trimestre";
ws.Cell(2, 3).Value = futura;
ws.Cell(2, 4).Value = "Alta";

ws.Cell(3, 1).Value = "Revisar Pull Request #128";
ws.Cell(3, 3).Value = futura;
ws.Cell(3, 4).Value = "Média";

ws.Cell(4, 1).Value = "Comprar materiais de escritório";
ws.Cell(4, 2).Value = "Canetas, papel e toner";
ws.Cell(4, 3).Value = futura;
ws.Cell(4, 4).Value = "baixa"; // caixa diferente — deve ser aceita

// ----- Linhas inválidas (devem aparecer no relatório de erros) -----
ws.Cell(5, 1).Value = "";        // título vazio
ws.Cell(5, 3).Value = futura;
ws.Cell(5, 4).Value = "Alta";

ws.Cell(6, 1).Value = "Tarefa já vencida";
ws.Cell(6, 3).Value = passada;   // data no passado
ws.Cell(6, 4).Value = "Média";

ws.Cell(7, 1).Value = "Prioridade desconhecida";
ws.Cell(7, 3).Value = futura;
ws.Cell(7, 4).Value = "Urgente"; // prioridade inválida

ws.Cell(8, 1).Value = "Data em formato inválido";
ws.Cell(8, 3).Value = "data-invalida"; // texto que não é data
ws.Cell(8, 4).Value = "Baixa";

ws.Columns().AdjustToContents();
workbook.SaveAs(caminho);

Console.WriteLine($"Planilha gerada em: {Path.GetFullPath(caminho)}");
Console.WriteLine("Esperado: 3 importadas (linhas 2-4) e 4 falhas (linhas 5-8).");
