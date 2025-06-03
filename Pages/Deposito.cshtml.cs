using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using Banksim_Web.Enums;

[Authorize]
public class DepositoModel : PageModel
{
    private readonly AppDbContext _db;

    public DepositoModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public Deposito Deposito { get; set; }

    public List<SelectListItem> ListaMeses { get; set; }

    public IActionResult OnGet()
    {
        ListaMeses = Enum.GetValues(typeof(Meses))
            .Cast<Meses>()
            .Select(m => new SelectListItem
            {
                Value = ((int)m).ToString(),
                Text = m.ToString()
            }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ListaMeses = Enum.GetValues(typeof(Meses))
            .Cast<Meses>()
            .Select(m => new SelectListItem
            {
                Value = ((int)m).ToString(),
                Text = m.ToString()
            }).ToList();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            ModelState.AddModelError("", "Erro ao identificar usuário.");
            return Page();
        }

        var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
        if (conta == null)
        {
            ModelState.AddModelError("", "Conta bancária não encontrada.");
            return Page();
        }

        var dataSimulada = _db.DataSimulada.FirstOrDefault(c => c.ContaBancariaID == conta.ID);
        if (dataSimulada == null)
        {
            ModelState.AddModelError("", "Calendário não encontrado.");
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var novoDeposito = new Deposito
        {
            ContaBancariaID = conta.ID,
            Valor = Deposito.Valor,
            Descricao = Deposito.Descricao,
            DiaDeposito = dataSimulada.DiaAtual,
            MesDeposito = dataSimulada.MesAtual,
            AnoDeposito = dataSimulada.AnoAtual
        };

        conta.Saldo += Deposito.Valor;

        _db.Depositos.Add(novoDeposito);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Home");
    }
}