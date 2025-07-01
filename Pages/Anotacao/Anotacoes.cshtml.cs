using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Banksim_Web.Pages
{
    [Authorize]
    public class AnotacoesModel : PageModel
    {
        private readonly AppDbContext _db;

        public AnotacoesModel(AppDbContext db)
        {
            _db = db;
        }

        public List<Models.Anotacao> ListaAnotacoes { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var uid)) return RedirectToPage("/Conta/Login");

            var conta = await _db.ContasBancarias.FirstOrDefaultAsync(c => c.UsuarioID == uid);
            if (conta == null) return RedirectToPage("/Conta/Login");

            ListaAnotacoes = await _db.Anotacoes
                .Where(a => a.ContaBancariaID == conta.ID)
                .OrderByDescending(a => a.ID)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var uid)) return RedirectToPage("/Conta/Login");

            var conta = await _db.ContasBancarias.FirstOrDefaultAsync(c => c.UsuarioID == uid);
            if (conta == null) return RedirectToPage("/Conta/Login");

            var novaAnotacao = new Models.Anotacao
            {
                ContaBancariaID = conta.ID,
                Titulo = "",
                TextoAnotacao = "",
                CorNota = "#ffffff" // ou qualquer cor padrão
            };

            _db.Anotacoes.Add(novaAnotacao);
            await _db.SaveChangesAsync();

            return RedirectToPage("DetalheAnotacao", new { id = novaAnotacao.ID });
        }
    }
}