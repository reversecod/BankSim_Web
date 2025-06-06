using System.Security.Claims;
using Banksim_Web.Enums;
using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

[Authorize]
public class FaturasModel : PageModel
{
    private readonly AppDbContext _db;

    public FaturasModel(AppDbContext db)
    {
        _db = db;
        MesesDisponiveis = new List<SelectListItem>();
    }

    [BindProperty]
    public int MesSelecionado { get; set; }

    [BindProperty]
    public decimal ValorPagamento { get; set; }

    public Fatura? FaturaSelecionada { get; set; }

    public List<SelectListItem> MesesDisponiveis { get; set; }

    public IActionResult OnGet()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out var uid)) return RedirectToPage("/Erro");

        var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == uid);
        if (conta == null) return RedirectToPage("/Erro");

        var data = _db.DataSimulada.FirstOrDefault(d => d.ContaBancariaID == conta.ID);
        if (data == null) return RedirectToPage("/Erro");

        // Fatura mais próxima do mês atual
        FaturaSelecionada = _db.Faturas
            .Where(f => f.ContaBancariaID == conta.ID && !f.Efetivado)
            .OrderBy(f => f.AnoPagamento)
            .ThenBy(f => f.MesPagamento)
            .FirstOrDefault();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out var uid)) return RedirectToPage("/Erro");

        var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == uid);
        if (conta == null) return RedirectToPage("/Erro");

        var fatura = _db.Faturas
            .Where(f => f.ContaBancariaID == conta.ID && !f.Efetivado)
            .OrderBy(f => f.AnoPagamento)
            .ThenBy(f => f.MesPagamento)
            .FirstOrDefault();

        if (fatura == null)
        {
            ModelState.AddModelError("", "Fatura não encontrada ou já foi paga.");
            return Page();
        }

        if (ValorPagamento <= 0 || ValorPagamento > fatura.ValorFatura)
        {
            ModelState.AddModelError("", "Valor de pagamento inválido.");
            return Page();
        }

        fatura.ValorFatura -= ValorPagamento;
        if (fatura.ValorFatura <= 0.01m)
        {
            fatura.Efetivado = true;
        }

        conta.Saldo -= ValorPagamento;

        await _db.SaveChangesAsync();

        TempData["Mes"] = fatura.MesPagamento;
        TempData["Ano"] = fatura.AnoPagamento;
        TempData["ValorPago"] = ValorPagamento.ToString("F2");

        return RedirectToPage("/PagesPosCarregamento/FaturaSucesso");
    }
}