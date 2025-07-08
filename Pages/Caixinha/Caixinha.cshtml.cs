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

            foreach (var caixinha in ListaCaixinha)
            {
                var aportes = await _db.AportesCaixinha
                    .Where(a => a.CaixinhaID == caixinha.ID && a.ValorAporte > 0)
                    .ToListAsync();

                decimal rendimentoCaixinha = 0;

                foreach (var aporte in aportes)
                {
                    int dias = aporte.DiasAplicados;
                    if (dias <= 0) continue;

                    // Calcular rendimento acumulado do aporte
                    decimal taxaAnual = caixinha.PorcentagemRendimento / 100M;
                    decimal taxaDiaria = Decimal.Divide(taxaAnual, 365M);

                    // Fórmula de juros compostos com decimal
                    decimal rendimentoFinal = aporte.ValorAporte * (DecimalPow(1 + taxaDiaria, dias) - 1);

                    // Atualiza apenas se houve mudança no rendimento
                    if (aporte.RendimentoAporte != rendimentoFinal)
                    {
                        aporte.RendimentoAporte = rendimentoFinal;
                    }

                    rendimentoCaixinha += rendimentoFinal;
                }

                caixinha.ValorRendido = rendimentoCaixinha;
                caixinha.ValorCaixinhaAtual = aportes.Sum(a => a.ValorAporte + a.RendimentoAporte);

                TotalRendido += rendimentoCaixinha;
            }

            await _db.SaveChangesAsync();

            return Page();
        }

        public static decimal DecimalPow(decimal baseValue, int exponent)
        {
            decimal result = 1;
            for (int i = 0; i < exponent; i++)
                result *= baseValue;
            return result;
        }
    }
}
