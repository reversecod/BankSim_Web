using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

[Authorize]
public class ExtratoModel : PageModel
{
    private readonly AppDbContext _db;

    public ExtratoModel(AppDbContext db)
    {
        _db = db;
        ListaExtrato = new List<Extrato>();
    }

    public List<Extrato> ListaExtrato { get; set; }

    public void OnGet()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return;

        var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
        if (conta == null) return;

        ListaExtrato = _db.Extratos
            .Where(e => e.ContaBancariaID == conta.ID)
            .OrderByDescending(e => e.Timestamp)
            .ToList();
    }
}