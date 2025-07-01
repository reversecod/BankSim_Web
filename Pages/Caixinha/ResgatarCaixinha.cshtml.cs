using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Banksim_Web.Pages.Caixinha
{
    [Authorize]
    public class ResgatarCaixinhaModel : PageModel
    {
        private readonly AppDbContext _db;

        public ResgatarCaixinhaModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public decimal Valor { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public Models.Caixinha Caixinha { get; set; } = null!;

        public decimal ValorDisponivel { get; set; }

        public IActionResult OnGet()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
            if (conta == null) return RedirectToPage("/Error");

            Caixinha = _db.Caixinhas.FirstOrDefault(c => c.ID == Id && c.ContaBancariaID == conta.ID)!;
            if (Caixinha == null) return RedirectToPage("/Error");

            ValorDisponivel = Caixinha.ValorCaixinhaAtual;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToPage("/Error");

            var userId = int.Parse(userIdClaim);
            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
            if (conta == null) return RedirectToPage("/Error");

            var caixinha = _db.Caixinhas.FirstOrDefault(c => c.ID == Id && c.ContaBancariaID == conta.ID);
            if (caixinha == null) return RedirectToPage("/Error");

            var dataSimulada = _db.DataSimulada.FirstOrDefault(d => d.ContaBancariaID == conta.ID);
            if (dataSimulada == null) return RedirectToPage("/Error");

            // Resgate total se o campo estiver vazio ou 0
            if (Valor <= 0)
            {
                Valor = caixinha.ValorCaixinhaAtual;
            }

            if (Valor > caixinha.ValorCaixinhaAtual)
            {
                ModelState.AddModelError("", "Valor superior ao disponível na caixinha.");
                return Page();
            }

            caixinha.ValorCaixinhaAtual -= Valor;
            conta.Saldo += Valor;

            _db.Extratos.Add(new Models.Extrato
            {
                ContaBancariaID = conta.ID,
                TipoTransacao = "Resgate da Caixinha",
                Valor = Valor,
                Descricao = $"Resgatado da caixinha \"{caixinha.NomeCaixinha}\"",
                DiaTransacao = dataSimulada.DiaAtual,
                MesTransacao = dataSimulada.MesAtual,
                AnoTransacao = dataSimulada.AnoAtual,
                Timestamp = DateTime.Now
            });

            await _db.SaveChangesAsync();

            TempData["Valor"] = Valor.ToString("F2");
            TempData["Descricao"] = $"Caixinha \"{caixinha.NomeCaixinha}\"";
            TempData["Dia"] = dataSimulada.DiaAtual;
            TempData["Mes"] = dataSimulada.MesAtual;
            TempData["Ano"] = dataSimulada.AnoAtual;
            TempData["Acao"] = "Resgate da Caixinha";

            return RedirectToPage("/PagesPosCarregamento/CaixinhaSucesso");
        }
    }
}