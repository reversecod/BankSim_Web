using Banksim_Web.Models;
using Banksim_Web.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

[Authorize]
public class TodasFaturasModel : PageModel
{
    private readonly AppDbContext _db;

    public TodasFaturasModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Fatura> Faturas { get; set; } = new();

    public IActionResult OnGet()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out var uid)) return RedirectToPage("/Erro");

        var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == uid);
        if (conta == null) return RedirectToPage("/Erro");

        Faturas = _db.Faturas
            .Where(f => f.ContaBancariaID == conta.ID)
            .OrderByDescending(f => f.AnoPagamento)
            .ThenByDescending(f => f.MesPagamento)
            .ToList();

        return Page();
    }
}