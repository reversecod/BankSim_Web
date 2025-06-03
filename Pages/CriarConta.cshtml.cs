using Banksim_Web.Enums;
using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

[Authorize]
public class CriarContaModel : PageModel
{
    private readonly AppDbContext _db;

    public CriarContaModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public ContaBancaria Conta { get; set; }

    [BindProperty]
    public int DiaInicial { get; set; }

    [BindProperty]
    public int MesInicial { get; set; }

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

        if (!ModelState.IsValid || DiaInicial < 1 || DiaInicial > DateTime.DaysInMonth(DateTime.Now.Year, MesInicial))
        {
            ModelState.AddModelError("", "Preencha todos os campos corretamente.");
            return Page();
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        Conta.UsuarioID = userId;

        try
        {
            _db.ContasBancarias.Add(Conta);
            await _db.SaveChangesAsync();

            var dataSimulada = new DataSimulada(DiaInicial, MesInicial)
            {
                ContaBancariaID = Conta.ID
            };

            _db.DataSimulada.Add(dataSimulada);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Home");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao criar conta: {ex.Message}");
            ModelState.AddModelError("", "Erro interno ao criar a conta.");
            return Page();
        }
    }
}