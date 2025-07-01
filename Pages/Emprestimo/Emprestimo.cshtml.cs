using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

[Authorize]
public class EmprestimoModel : PageModel
{
    private readonly AppDbContext _db;

    public EmprestimoModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public string? NomeEmprestimo { get; set; }

    [BindProperty]
    public decimal ValorEmprestimo { get; set; }

    [BindProperty]
    public int QntdParcela { get; set; }

    [BindProperty]
    public int DiaPagamento { get; set; }

    public double PorcentagemJuros { get; set; }

    public IActionResult OnGet()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return RedirectToPage("/Erro");

        var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
        if (conta == null) return RedirectToPage("/Erro");

        // Verifica se já há um empréstimo em aberto
        var emprestimoExistente = _db.Emprestimos
            .FirstOrDefault(e => e.ContaBancariaID == conta.ID && !e.Pago);

        if (emprestimoExistente != null)
        {
            return RedirectToPage("/Emprestimo/StatusEmprestimo");
        }

        PorcentagemJuros = conta.PorcentagemEmprestimo;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return RedirectToPage("/Erro");

        var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
        var dataSimulada = _db.DataSimulada.FirstOrDefault(d => d.ContaBancariaID == conta.ID);
        if (conta == null || dataSimulada == null) return RedirectToPage("/Erro");

        var emprestimoExistente = _db.Emprestimos
            .FirstOrDefault(e => e.ContaBancariaID == conta.ID && e.Pago == false);

        if (emprestimoExistente != null)
        {
            return RedirectToPage("/Emprestimo/StatusEmprestimo");
        }

        var dataAtual = new DateTime(dataSimulada.AnoAtual, dataSimulada.MesAtual, dataSimulada.DiaAtual);

        // Define o dia do vencimento (o DiaPagamento vindo do usuário)
        int mesBase = dataSimulada.MesAtual;
        int anoBase = dataSimulada.AnoAtual;
        int diaVencimento = DiaPagamento;

        DateTime vencimentoProposto = new DateTime(anoBase, mesBase, diaVencimento);

        // Incrementa mês até encontrar uma data de vencimento futura com pelo menos 7 dias de distância
        while ((vencimentoProposto - dataAtual).TotalDays < 7)
        {
            mesBase++;
            if (mesBase > 12)
            {
                mesBase = 1;
                anoBase++;
            }

            // Atualiza a nova data de vencimento proposta
            vencimentoProposto = new DateTime(anoBase, mesBase, diaVencimento);
        }

        // Cálculo da Tabela Price (fixando o valor da parcela)
        var juros = (decimal)(conta.PorcentagemEmprestimo / 100);
        var fator = (decimal)Math.Pow(1 + (double)juros, QntdParcela);
        var valorParcela = ValorEmprestimo * (juros * fator) / (fator - 1);
        var valorTotalComJuros = valorParcela * QntdParcela;

        var novoEmprestimo = new Emprestimo
        {
            ContaBancariaID = conta.ID,
            NomeEmprestimo = $"{NomeEmprestimo} • {QntdParcela} parcela(s)",
            ValorEmprestimo = valorTotalComJuros, // valor total com juros
            ValorParcela = valorParcela,          // valor fixo da parcela
            QntdParcela = QntdParcela,
            TotalParcelas = QntdParcela,
            diaPagamento = DiaPagamento,
            MesInicioPagamento = mesBase,
            MesProxPagamento = mesBase,
            ValorPago = 0,
            Pago = false
        };

        _db.Emprestimos.Add(novoEmprestimo);

        // Lança o valor do crédito no extrato com o valor BRUTO (sem juros)
        _db.Extratos.Add(new Extrato
        {
            ContaBancariaID = conta.ID,
            TipoTransacao = "Empréstimo",
            Valor = ValorEmprestimo,
            Descricao = $"{NomeEmprestimo} • {QntdParcela} parcela(s)",
            DiaTransacao = dataSimulada.DiaAtual,
            MesTransacao = dataSimulada.MesAtual,
            AnoTransacao = dataSimulada.AnoAtual,
            Timestamp = DateTime.Now
        });

        await _db.SaveChangesAsync();

        TempData["NomeEmprestimo"] = NomeEmprestimo;
        TempData["ValorEmprestimo"] = ValorEmprestimo.ToString("F2");

        return RedirectToPage("/PagesPosCarregamento/EmprestimoSucesso");
    }
}