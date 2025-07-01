using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
public class HomeModel : PageModel
{
    private readonly AppDbContext _db;

    public HomeModel(AppDbContext db)
    {
        _db = db;
    }

    public decimal Saldo { get; set; }
    public decimal LimiteCreditoInicial { get; set; }
    public decimal LimiteCreditoDisponivel { get; set; }
    public DateOnly DataSimulada { get; set; }

    [BindProperty]
    public decimal NovoLimite { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? AcaoData { get; set; } // usado pelas setas (avancar / retroceder)

    public string? MensagemErro { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var conta = await _db.ContasBancarias
            .Include(c => c.dataSimulada)
            .FirstOrDefaultAsync(c => c.UsuarioID.ToString() == userId);

        if (conta == null || conta.dataSimulada == null)
            return RedirectToPage("/Erro");

        if (AcaoData == "avancar")
        {
            conta.dataSimulada.AvancarDias(1);
            await _db.SaveChangesAsync();
        }

        Saldo = conta.Saldo;
        LimiteCreditoDisponivel = conta.LimiteCreditoDisponivel;
        LimiteCreditoInicial = conta.LimiteCreditoInicial;
        DataSimulada = new DateOnly(
            conta.dataSimulada.AnoAtual,
            conta.dataSimulada.MesAtual,
            conta.dataSimulada.DiaAtual
        );

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var conta = await _db.ContasBancarias
            .FirstOrDefaultAsync(c => c.UsuarioID.ToString() == userId);

        if (conta == null)
            return RedirectToPage("/Erro");

        // Impede alteração de limite com fatura em aberto e limite usado
        bool possuiFaturaAberta = await _db.Faturas
            .AnyAsync(f => f.ContaBancariaID == conta.ID && !f.Efetivado);

        if (possuiFaturaAberta && conta.LimiteCreditoInicial != conta.LimiteCreditoDisponivel)
        {
            ModelState.AddModelError("", "Não é possível alterar o limite enquanto houver faturas em aberto.");
            Saldo = conta.Saldo;
            LimiteCreditoDisponivel = conta.LimiteCreditoDisponivel;
            LimiteCreditoInicial = conta.LimiteCreditoInicial;
            return Page();
        }

        // Aplica novo limite
        conta.LimiteCreditoInicial = NovoLimite;
        conta.LimiteCreditoDisponivel = NovoLimite;
        await _db.SaveChangesAsync();

        return RedirectToPage(); // Recarrega dados
    }
}