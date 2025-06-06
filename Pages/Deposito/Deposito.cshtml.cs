using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

[Authorize]
public class DepositoModel : PageModel
{
    private readonly AppDbContext _db;

    public DepositoModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public Deposito Deposito { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            ModelState.AddModelError("", "Erro ao identificar o usuário.");
            return Page();
        }

        var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
        if (conta == null)
        {
            ModelState.AddModelError("", "Conta bancária não encontrada.");
            return Page();
        }

        var dataSimulada = _db.DataSimulada.FirstOrDefault(d => d.ContaBancariaID == conta.ID);
        if (dataSimulada == null)
        {
            ModelState.AddModelError("", "Data simulada não localizada.");
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

        TempData["Valor"] = Deposito.Valor.ToString("F2");
        TempData["Descricao"] = Deposito.Descricao ?? "-";
        TempData["Dia"] = novoDeposito.DiaDeposito;
        TempData["Mes"] = novoDeposito.MesDeposito;
        TempData["Ano"] = novoDeposito.AnoDeposito;

        return RedirectToPage("/PagesPosCarregamento/DepositoSucesso");
    }
}