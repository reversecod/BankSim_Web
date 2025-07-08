using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Banksim_Web.Pages
{
    [Authorize]
    public class DetalheAnotacaoModel : PageModel
    {
        private readonly AppDbContext _db;

        public DetalheAnotacaoModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public Models.Anotacao Anotacao { get; set; } = new();

        [FromRoute]
        public int Id { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToPage("/Erro");

            var conta = await _db.ContasBancarias
                .FirstOrDefaultAsync(c => c.UsuarioID == userId);

            if (conta == null)
                return RedirectToPage("/Erro");

            if (Id == 0)
            {
                // Criação de nova anotação ao acessar via "DetalheAnotacao"
                var nova = new Models.Anotacao
                {
                    ContaBancariaID = conta.ID,
                    Titulo = "",
                    TextoAnotacao = ""
                };

                _db.Anotacoes.Add(nova);
                await _db.SaveChangesAsync();

                // Redireciona para o ID gerado
                return RedirectToPage("DetalheAnotacao", new { id = nova.ID });
            }

            Anotacao = await _db.Anotacoes
                .FirstOrDefaultAsync(a => a.ID == Id && a.ContaBancariaID == conta.ID);

            if (Anotacao == null)
                return RedirectToPage("/Erro");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToPage("/Error");

            var conta = await _db.ContasBancarias
                .FirstOrDefaultAsync(c => c.UsuarioID == userId);

            if (conta == null)
                return RedirectToPage("/Error");

            var anotacaoExistente = await _db.Anotacoes
                .FirstOrDefaultAsync(a => a.ID == Anotacao.ID && a.ContaBancariaID == conta.ID);

            if (anotacaoExistente == null)
                return RedirectToPage("/Error");

            // 🚨 Validação de campos obrigatórios
            if (string.IsNullOrWhiteSpace(Anotacao.TextoAnotacao))
            {
                ModelState.AddModelError("Anotacao.TextoAnotacao", "O texto da anotação não pode estar vazio.");
                Anotacao = anotacaoExistente; // reatribui para exibir dados corretamente na view
                return Page();
            }

            // Atualiza os dados da anotação
            anotacaoExistente.Titulo = Anotacao.Titulo;
            anotacaoExistente.TextoAnotacao = Anotacao.TextoAnotacao;
            anotacaoExistente.CorNota = Anotacao.CorNota;

            await _db.SaveChangesAsync();

            return RedirectToPage("/Anotacao/Anotacoes");
        }
        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return RedirectToPage("/Error");

            var conta = await _db.ContasBancarias
                .FirstOrDefaultAsync(c => c.UsuarioID == userId);

            if (conta == null)
                return RedirectToPage("/Error");

            var anotacao = await _db.Anotacoes
                .FirstOrDefaultAsync(a => a.ID == Anotacao.ID && a.ContaBancariaID == conta.ID);

            if (anotacao == null)
                return RedirectToPage("/Error");

            _db.Anotacoes.Remove(anotacao);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Anotacao/Anotacoes");
        }
    }
}