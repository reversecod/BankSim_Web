using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

    [BindProperty]
    public decimal NovoLimite { get; set; }

    public string? MensagemErro { get; set; }

    public void OnGet()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID.ToString() == userId);

        if (conta != null)
        {
            Saldo = conta.Saldo;
            LimiteCreditoDisponivel = conta.LimiteCreditoDisponivel;
            LimiteCreditoInicial = conta.LimiteCreditoInicial;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID.ToString() == userId);

        if (conta == null) return RedirectToPage("/Erro");

        // Verifica se há faturas em aberto
        bool possuiFaturaAberta = _db.Faturas
            .Any(f => f.ContaBancariaID == conta.ID && !f.Efetivado);

        if (possuiFaturaAberta && conta.LimiteCreditoInicial != conta.LimiteCreditoDisponivel)
        {
            ModelState.AddModelError("", "Não é possível alterar o limite enquanto houver faturas em aberto.");
            Saldo = conta.Saldo;
            LimiteCreditoDisponivel = conta.LimiteCreditoDisponivel;
            LimiteCreditoInicial = conta.LimiteCreditoInicial;
            return Page();
        }

        conta.LimiteCreditoInicial = NovoLimite;
        conta.LimiteCreditoDisponivel = NovoLimite;
        await _db.SaveChangesAsync();

        return RedirectToPage(); // Atualiza valores
    }
}