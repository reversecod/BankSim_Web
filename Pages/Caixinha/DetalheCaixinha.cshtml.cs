using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Banksim_Web.Pages.Caixinha
{
    [Authorize]
    public class DetalheCaixinhaModel : PageModel
    {
        private readonly AppDbContext _db;

        public DetalheCaixinhaModel(AppDbContext db)
        {
            _db = db;
        }
        public decimal ValorLiquido { get; set; }
        public Models.Caixinha Caixinha { get; set; } = null!;
        public int DiaAtual { get; set; }

        public IActionResult OnGet(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
            var data = _db.DataSimulada.FirstOrDefault(d => d.ContaBancariaID == conta!.ID);

            Caixinha = _db.Caixinhas.FirstOrDefault(c => c.ID == id && c.ContaBancariaID == conta.ID)!;

            if (Caixinha == null || data == null)
                return RedirectToPage("/Erro");

            DiaAtual = data.DiaAtual;
            ValorLiquido = Caixinha.ValorCaixinhaAtual - Caixinha.ValorRendido;

            return Page();
        }
    }
}