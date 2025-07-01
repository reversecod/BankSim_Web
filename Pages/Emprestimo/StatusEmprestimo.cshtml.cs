using Banksim_Web.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Banksim_Web.Pages.Emprestimo
{
    public class StatusEmprestimoModel : PageModel
    {
        private readonly AppDbContext _db;

        public StatusEmprestimoModel(AppDbContext db)
        {
            _db = db;
        }

        public Models.Emprestimo? Emprestimo { get; set; }
        public decimal ValorTotalComJuros { get; set; }
        public decimal ValorPago { get; set; }
        public decimal ValorRestante => Emprestimo != null
        ? Emprestimo.ValorEmprestimo - Emprestimo.ValorPago
        : 0;
        public string ProximaParcela { get; set; } = "-";

        public IActionResult OnGet()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
            if (conta == null) return RedirectToPage("/Erro");

            Emprestimo = _db.Emprestimos.FirstOrDefault(e => e.ContaBancariaID == conta.ID && !e.Pago);
            if (Emprestimo == null) return RedirectToPage("/Emprestimo");

            ValorTotalComJuros = Emprestimo.ValorEmprestimo;

            if (Emprestimo.MesProxPagamento > 0)
            {
                var dataSimulada = _db.DataSimulada.FirstOrDefault(d => d.ContaBancariaID == conta.ID);
                if (dataSimulada == null) return RedirectToPage("/Erro");

                int anoAtual = dataSimulada.AnoAtual;
                int mesAtual = dataSimulada.MesAtual;

                if (Emprestimo.MesProxPagamento >= 1 && Emprestimo.MesProxPagamento <= 12)
                {
                    if (Emprestimo.MesProxPagamento < mesAtual ||
                       (Emprestimo.MesProxPagamento == mesAtual && Emprestimo.diaPagamento <= dataSimulada.DiaAtual))
                    {
                        anoAtual++;
                    }
                }

                try
                {
                    var dataProximaParcela = new DateTime(anoAtual, Emprestimo.MesProxPagamento, Emprestimo.diaPagamento);
                    ProximaParcela = dataProximaParcela.ToString("dd/MM/yyyy");
                }
                catch
                {
                    ProximaParcela = "-";
                }
            }

            return Page();
        }
    }
}