using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Banksim_Web.Pages.Caixinha
{
    [Authorize]
    public class GuardarCaixinhaModel : PageModel
    {
        private readonly AppDbContext _db;

        public GuardarCaixinhaModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public decimal Valor { get; set; }

        [BindProperty]
        public int CaixinhaId { get; set; }

        public Models.Caixinha Caixinha { get; set; } = null!;

        public decimal SaldoDisponivel { get; set; }

        public IActionResult OnGet(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);

            if (conta == null)
                return RedirectToPage("/Error");

            Caixinha = _db.Caixinhas.FirstOrDefault(c => c.ID == id && c.ContaBancariaID == conta.ID)!;

            if (Caixinha == null)
                return RedirectToPage("/Error");

            CaixinhaId = Caixinha.ID;
            SaldoDisponivel = conta.Saldo;

            return Page();
        }


        public async Task<IActionResult> OnPostAsync()
        {
            Console.WriteLine("Iniciando POST de guardar na caixinha...");

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                Console.WriteLine("Erro: UserIdClaim está vazio.");
                return RedirectToPage("/Error");
            }

            var userId = int.Parse(userIdClaim);
            Console.WriteLine($"Usuário autenticado com ID: {userId}");

            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
            if (conta == null)
            {
                Console.WriteLine("Erro: Conta bancária não encontrada.");
                return RedirectToPage("/Error");
            }

            var caixinha = _db.Caixinhas.FirstOrDefault(c => c.ID == CaixinhaId && c.ContaBancariaID == conta.ID);
            if (caixinha == null)
            {
                Console.WriteLine($"Erro: Caixinha ID {CaixinhaId} não encontrada para a conta ID {conta.ID}.");
                return RedirectToPage("/Error");
            }

            var dataSimulada = _db.DataSimulada.FirstOrDefault(d => d.ContaBancariaID == conta.ID);
            if (dataSimulada == null)
            {
                Console.WriteLine("Erro: Data simulada não encontrada.");
                return RedirectToPage("/Error");
            }

            Console.WriteLine($"Valor a guardar: R$ {Valor:F2}, Saldo atual: R$ {conta.Saldo:F2}");

            if (Valor <= 0 || conta.Saldo < Valor)
            {
                Console.WriteLine("Erro: Valor inválido ou saldo insuficiente.");
                ModelState.AddModelError("", "Valor inválido ou saldo insuficiente.");
                return Page();
            }

            conta.Saldo -= Valor;
            caixinha.ValorCaixinhaAtual += Valor;

            var novoAporte = new AporteCaixinha
            {
                ContaBancariaID = conta.ID,
                CaixinhaID = caixinha.ID,
                ValorAporte = Valor,
                DiasAplicados = 0,
                DiaCriacao = dataSimulada.DiaAtual,
                MesCriacao = dataSimulada.MesAtual,
                AnoCriacao = dataSimulada.AnoAtual
            };

            _db.AportesCaixinha.Add(novoAporte);

            Console.WriteLine($"Novo saldo: R$ {conta.Saldo:F2}");
            Console.WriteLine($"Novo valor na caixinha \"{caixinha.NomeCaixinha}\": R$ {caixinha.ValorCaixinhaAtual:F2}");

            _db.Extratos.Add(new Models.Extrato
            {
                ContaBancariaID = conta.ID,
                TipoTransacao = "Guardar na Caixinha",
                Valor = Valor,
                Descricao = $"Transferido para a caixinha \"{caixinha.NomeCaixinha}\"",
                DiaTransacao = dataSimulada.DiaAtual,
                MesTransacao = dataSimulada.MesAtual,
                AnoTransacao = dataSimulada.AnoAtual,
                Timestamp = DateTime.Now
            });

            try
            {
                Console.WriteLine("Salvando alterações no banco...");
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar no banco: {ex.Message}");
                ModelState.AddModelError("", "Erro ao guardar o valor. Tente novamente.");
                return Page();
            }

            Console.WriteLine("Redirecionando para página de detalhes da caixinha...");
            TempData["Valor"] = Valor.ToString("F2");
            TempData["Descricao"] = $"Caixinha \"{caixinha.NomeCaixinha}\"";
            TempData["Dia"] = dataSimulada.DiaAtual;
            TempData["Mes"] = dataSimulada.MesAtual;
            TempData["Ano"] = dataSimulada.AnoAtual;
            TempData["Acao"] = "Depósito na Caixinha";

            return RedirectToPage("/PagesPosCarregamento/CaixinhaSucesso");
        }
    }
}