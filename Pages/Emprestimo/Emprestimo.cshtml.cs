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

        int mesBase = dataSimulada.MesAtual;
        int anoBase = dataSimulada.AnoAtual;

        int diasFaltando = DiaPagamento - dataSimulada.DiaAtual;
        if (diasFaltando <= 7 && diasFaltando >= 0)
        {
            mesBase++;
            if (mesBase > 12)
            {
                mesBase = 1;
                anoBase++;
            }
        }

        var novoEmprestimo = new Emprestimo
        {
            ContaBancariaID = conta.ID,
            NomeEmprestimo = $"{NomeEmprestimo} • {QntdParcela} parcela(s)",
            ValorEmprestimo = ValorEmprestimo,
            QntdParcela = QntdParcela,
            diaPagamento = DiaPagamento,
            MesInicioPagamento = mesBase,
            MesProxPagamento = mesBase,
            ValorPago = 0, // começa em zero
            Pago = false
        };

        _db.Emprestimos.Add(novoEmprestimo);

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