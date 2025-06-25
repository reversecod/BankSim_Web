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
            ModelState.AddModelError("", "Você já possui um empréstimo em andamento. Pague o empréstimo atual antes de solicitar outro.");
            return Page();
        }

        var mesInicio = (dataSimulada.MesAtual % 12) + 1;

        var novoEmprestimo = new Emprestimo
        {
            ContaBancariaID = conta.ID,
            NomeEmprestimo = NomeEmprestimo,
            ValorEmprestimo = ValorEmprestimo,
            QntdParcela = QntdParcela,
            diaPagamento = DiaPagamento,
            MesInicioPagamento = mesInicio,
            Pago = false
        };

        _db.Emprestimos.Add(novoEmprestimo);

        _db.Extratos.Add(new Extrato
        {
            ContaBancariaID = conta.ID,
            TipoTransacao = "Empréstimo",
            Valor = ValorEmprestimo,
            Descricao = NomeEmprestimo,
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