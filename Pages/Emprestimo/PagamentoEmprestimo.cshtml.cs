using Banksim_Web.Enums;
using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Banksim_Web.Pages.Emprestimo
{
    [Authorize]
    public class PagamentoEmprestimoModel : PageModel
    {
        private readonly AppDbContext _db;

        public PagamentoEmprestimoModel(AppDbContext db)
        {
            _db = db;
        }

        public Models.Emprestimo? EmprestimoAtual { get; set; }
        public decimal ValorParcela { get; set; } = 0;
        public List<ParcelaVisual> ParcelasDisponiveis { get; set; } = new();

        [BindProperty]
        public List<string> ParcelasSelecionadas { get; set; } = new();

        public class ParcelaVisual
        {
            public int NumeroParcela { get; set; }
            public int Mes { get; set; }
            public int Ano { get; set; }
            public string MesNome => Enum.GetName(typeof(Meses), Mes)!;
            public string Codigo => $"{Mes}-{Ano}";
        }

        public IActionResult OnGet()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
            var dataSimulada = _db.DataSimulada.FirstOrDefault(d => d.ContaBancariaID == conta.ID);
            if (conta == null || dataSimulada == null) return RedirectToPage("/Erro");

            EmprestimoAtual = _db.Emprestimos.FirstOrDefault(e => e.ContaBancariaID == conta.ID && !e.Pago);
            if (EmprestimoAtual == null) return RedirectToPage("/Emprestimo");

            var totalComJuros = Math.Round(EmprestimoAtual.ValorEmprestimo * (1 + (decimal)(conta.PorcentagemEmprestimo / 100 * EmprestimoAtual.QntdParcela)), 2);
            ValorParcela = Math.Round(totalComJuros / EmprestimoAtual.QntdParcela, 2);

            for (int i = 0; i < EmprestimoAtual.QntdParcela; i++)
            {
                int mes = EmprestimoAtual.MesProxPagamento + i;
                int ano = dataSimulada.AnoAtual;

                while (mes > 12)
                {
                    mes -= 12;
                    ano++;
                }

                ParcelasDisponiveis.Add(new ParcelaVisual
                {
                    NumeroParcela = i + 1,
                    Mes = mes,
                    Ano = ano
                });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
            var dataSimulada = _db.DataSimulada.FirstOrDefault(d => d.ContaBancariaID == conta.ID);
            if (conta == null || dataSimulada == null) return RedirectToPage("/Erro");

            EmprestimoAtual = _db.Emprestimos.FirstOrDefault(e => e.ContaBancariaID == conta.ID && !e.Pago);
            if (EmprestimoAtual == null) return RedirectToPage("/Emprestimo");

            var totalComJuros = Math.Round(EmprestimoAtual.ValorEmprestimo * (1 + (decimal)(conta.PorcentagemEmprestimo / 100 * EmprestimoAtual.QntdParcela)), 2);
            ValorParcela = Math.Round(totalComJuros / EmprestimoAtual.QntdParcela, 2);

            int qtdParcelasPagas = ParcelasSelecionadas.Count;
            decimal valorPago = ValorParcela * qtdParcelasPagas;

            if (conta.Saldo < valorPago)
            {
                ModelState.AddModelError("", "Saldo insuficiente para pagar as parcelas selecionadas.");
                return await OnGetAsync();
            }

            conta.Saldo -= valorPago;

            EmprestimoAtual.QntdParcela -= qtdParcelasPagas;
            EmprestimoAtual.ValorEmprestimo -= valorPago;
            EmprestimoAtual.ValorPago += valorPago;

            if (dataSimulada.DiaAtual >= EmprestimoAtual.diaPagamento - 7)
                EmprestimoAtual.MesProxPagamento += qtdParcelasPagas;
            else
                EmprestimoAtual.MesProxPagamento += qtdParcelasPagas + 1;

            if (EmprestimoAtual.MesProxPagamento > 12)
            {
                EmprestimoAtual.MesProxPagamento = (EmprestimoAtual.MesProxPagamento - 1) % 12 + 1;
            }

            if (EmprestimoAtual.QntdParcela <= 0)
                EmprestimoAtual.Pago = true;

            _db.Extratos.Add(new Models.Extrato
            {
                ContaBancariaID = conta.ID,
                TipoTransacao = "Pagamento Empréstimo",
                Valor = valorPago,
                Descricao = $"Pagamento de {qtdParcelasPagas} parcela(s) do empréstimo \"{EmprestimoAtual.NomeEmprestimo}\"",
                DiaTransacao = dataSimulada.DiaAtual,
                MesTransacao = dataSimulada.MesAtual,
                AnoTransacao = dataSimulada.AnoAtual,
                Timestamp = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return RedirectToPage("/PagesPosCarregamento/EmprestimoPagoSucesso");
        }

        private async Task<IActionResult> OnGetAsync()
        {
            OnGet();
            return Page();
        }
    }
}