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

            decimal valorRestanteResgate = Valor;

            // Buscar aportes ativos da caixinha, em ordem FIFO (do mais antigo para o mais recente)
            var aportes = _db.AportesCaixinha
                .Where(a => a.CaixinhaID == caixinha.ID && a.ContaBancariaID == conta.ID && a.ValorAporte > 0)
                .OrderBy(a => a.ID) // FIFO
                .ToList();

            foreach (var aporte in aportes)
            {
                if (valorRestanteResgate <= 0) break;

                decimal totalDisponivelAporte = aporte.ValorAporte + aporte.RendimentoAporte;

                if (totalDisponivelAporte >= valorRestanteResgate)
                {
                    // Calcula a proporção de desconto entre capital e rendimento
                    decimal proporcaoCapital = aporte.ValorAporte / totalDisponivelAporte;
                    decimal proporcaoRendimento = aporte.RendimentoAporte / totalDisponivelAporte;

                    aporte.ValorAporte -= valorRestanteResgate * proporcaoCapital;
                    aporte.RendimentoAporte -= valorRestanteResgate * proporcaoRendimento;
                    valorRestanteResgate = 0;
                }
                else
                {
                    valorRestanteResgate -= totalDisponivelAporte;
                    aporte.ValorAporte = 0;
                    aporte.RendimentoAporte = 0;
                }
            }

            if (valorRestanteResgate > 0)
            {
                ModelState.AddModelError("", "Erro interno: valor solicitado excede os aportes disponíveis.");
                return Page();
            }
            // Atualiza o valor total da caixinha e o saldo da conta
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