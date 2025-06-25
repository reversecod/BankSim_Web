using System.Security.Claims;
using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Banksim_Web.Pages.Faturas
{
    [Authorize]
    public class DetalheFaturaModel : PageModel
    {
        private readonly AppDbContext _db;

        public DetalheFaturaModel(AppDbContext db)
        {
            _db = db;
        }

        public Fatura Fatura { get; set; } = new();
        public List<ExtratoFaturas> ExtratoFaturaLista { get; set; } = new();

        [BindProperty]
        public decimal ValorPagamento { get; set; }

        [FromRoute]
        public int Id { get; set; }

        public IActionResult OnGet()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var uid)) return RedirectToPage("/Erro");

            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == uid);
            if (conta == null) return RedirectToPage("/Erro");

            Fatura = _db.Faturas.FirstOrDefault(f => f.ID == Id && f.ContaBancariaID == conta.ID);
            if (Fatura == null) return RedirectToPage("/Erro");

            ExtratoFaturaLista = _db.ExtratoFaturas
                .Where(e => e.ContaBancariaID == conta.ID &&
                            e.MesPagamento == Fatura.MesPagamento &&
                            e.AnoPagamento == Fatura.AnoPagamento)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var uid)) return RedirectToPage("/Erro");

            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == uid);
            if (conta == null) return RedirectToPage("/Erro");

            Fatura = _db.Faturas.FirstOrDefault(f => f.ID == Id && f.ContaBancariaID == conta.ID);
            if (Fatura == null || Fatura.Efetivado) return RedirectToPage("/Erro");

            var dataSimulada = _db.DataSimulada.FirstOrDefault(d => d.ContaBancariaID == conta.ID);
            if (dataSimulada == null) return RedirectToPage("/Erro");

            ExtratoFaturaLista = _db.ExtratoFaturas
                .Where(e => e.ContaBancariaID == conta.ID &&
                            e.MesPagamento == Fatura.MesPagamento &&
                            e.AnoPagamento == Fatura.AnoPagamento)
                .ToList();

            if (ValorPagamento <= 0)
            {
                ModelState.AddModelError("", "Informe um valor para pagamento.");
                return Page();
            }

            if (ValorPagamento > Fatura.ValorFaturaAtual)
            {
                ValorPagamento = Fatura.ValorFaturaAtual;
            }

            if (conta.Saldo < ValorPagamento)
            {
                ModelState.AddModelError("", "Saldo insuficiente.");
                return Page();
            }

            // Aplica pagamento
            conta.Saldo -= ValorPagamento;
            conta.LimiteCreditoDisponivel += ValorPagamento;
            Fatura.ValorFaturaAtual -= ValorPagamento;

            if (Fatura.ValorFaturaAtual <= 0.01m)
                Fatura.Efetivado = true;

            _db.Extratos.Add(new Models.Extrato
            {
                ContaBancariaID = conta.ID,
                TipoTransacao = "Pagamento de Fatura",
                Valor = ValorPagamento,
                DiaTransacao = dataSimulada.DiaAtual,
                MesTransacao = dataSimulada.MesAtual,
                AnoTransacao = dataSimulada.AnoAtual,
                Timestamp = DateTime.Now
            });

            await _db.SaveChangesAsync();

            TempData["ValorPago"] = ValorPagamento.ToString("F2");
            TempData["Mes"] = Fatura.MesPagamento;
            TempData["Ano"] = Fatura.AnoPagamento;

            return RedirectToPage("/PagesPosCarregamento/FaturaSucesso");
        }
    }
}