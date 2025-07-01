using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Banksim_Web.Pages.Caixinha
{
    [Authorize]
    public class CaixinhaModel : PageModel
    {
        private readonly AppDbContext _db;

        public CaixinhaModel(AppDbContext db)
        {
            _db = db;
        }

        public List<Models.Caixinha> ListaCaixinha { get; set; } = new();
        public decimal TotalRendido { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Conta/Login");

            if (!int.TryParse(userId, out var uid)) return RedirectToPage("/Conta/Login");

            var conta = await _db.ContasBancarias
                .FirstOrDefaultAsync(c => c.UsuarioID == uid);

            if (conta == null) return RedirectToPage("/Conta/Login");

            ListaCaixinha = await _db.Caixinhas
                .Where(c => c.ContaBancariaID == conta.ID)
                .ToListAsync();

            TotalRendido = 0;

            foreach (var c in ListaCaixinha)
            {
                var dataCriacao = new DateTime(c.AnoCriacao, c.MesCriacao, c.DiaCriacao);
                var diasAplicados = (DateTime.Now - dataCriacao).Days;

                if (diasAplicados < 0) diasAplicados = 0;

                var rendimento = c.ValorCaixinhaInicial *
                                (decimal)(c.PorcentagemRendimento / 100) *
                                (decimal)(diasAplicados / 365.0);

                c.ValorRendido = rendimento;

                TotalRendido += rendimento;
            }

            return Page();
        }
    }
}