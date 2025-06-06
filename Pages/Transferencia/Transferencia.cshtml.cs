using Banksim_Web.Enums;
using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

[Authorize]
public class TransferenciaModel : PageModel
{
    private readonly AppDbContext _db;

    public TransferenciaModel(AppDbContext db)
    {
        _db = db;
        TiposTransferencia = new List<SelectListItem>
        {
            new SelectListItem { Value = "D", Text = "Débito" },
            new SelectListItem { Value = "C", Text = "Crédito" }
        };
        MesesDisponiveis = new List<SelectListItem>();
    }

    [BindProperty]
    public Transferencia Transferencia { get; set; }

    [BindProperty]
    public int QtdParcelas { get; set; }

    [BindProperty]
    public int MesSelecionado { get; set; }

    [BindProperty]
    public decimal PorcentagemJuros { get; set; } = 0;

    public decimal ValorParcelaCalculado { get; set; } = 0;

    public List<SelectListItem> TiposTransferencia { get; set; }
    public List<SelectListItem> MesesDisponiveis { get; set; }

    public IActionResult OnGet()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            return RedirectToPage("/Erro");
        }

        var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
        var dataSimulada = _db.DataSimulada.FirstOrDefault(c => c.ContaBancariaID == conta.ID);

        ViewData["NomesMeses"] = Enum.GetValues(typeof(Meses))
            .Cast<Meses>()
            .Select(m => m.ToString())
            .ToArray();

        ViewData["DiaAtual"] = dataSimulada?.DiaAtual ?? 1;
        ViewData["MesAtual"] = dataSimulada?.MesAtual ?? 1;
        ViewData["AnoAtual"] = dataSimulada?.AnoAtual ?? DateTime.Now.Year;
        ViewData["DiaFechamento"] = conta?.DiaFatura ?? 25;

        MesesDisponiveis = Enumerable.Range(1, 3).Select(offset => {
            int mes = dataSimulada.MesAtual + offset;
            int ano = dataSimulada.AnoAtual + (mes - 1) / 12;
            mes = (mes - 1) % 12 + 1;
            return new SelectListItem { Value = offset.ToString(), Text = $"{((Meses)mes)}/{ano}" };
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
            {
                ModelState.AddModelError("", "Erro ao identificar usuário.");
                return Page();
            }

            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
            if (conta == null)
            {
                ModelState.AddModelError("", "Conta bancária não encontrada.");
                return Page();
            }

            var dataSimulada = _db.DataSimulada.FirstOrDefault(c => c.ContaBancariaID == conta.ID);
            if (dataSimulada == null)
            {
                ModelState.AddModelError("", "Calendário não encontrado.");
                return Page();
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Erro de validação no modelo.");
                return Page();
            }

            var novaTransferencia = new Transferencia
            {
                ContaBancariaID = conta.ID,
                Valor = Transferencia.Valor,
                TipoTransferencia = Transferencia.TipoTransferencia,
                Descricao = Transferencia.Descricao,
                DiaTransferencia = dataSimulada.DiaAtual,
                MesTransferencia = dataSimulada.MesAtual,
                AnoTransferencia = dataSimulada.AnoAtual,
                Efetivado = true
            };

            if (Transferencia.TipoTransferencia == "D")
            {
                if (conta.Saldo >= Transferencia.Valor)
                {
                    conta.Saldo -= Transferencia.Valor;
                }
                else
                {
                    ModelState.AddModelError("", "Saldo insuficiente.");
                    return Page();
                }
            }
            else if (Transferencia.TipoTransferencia == "C")
            {
                if (conta.LimiteCredito >= Transferencia.Valor)
                {
                    conta.LimiteCredito -= Transferencia.Valor;
                }
                else
                {
                    ModelState.AddModelError("", "Limite de crédito insuficiente.");
                    return Page();
                }

                decimal valorComJuros = Transferencia.Valor;
                if (PorcentagemJuros > 0)
                {
                    valorComJuros = (Transferencia.Valor * PorcentagemJuros / 100 * QtdParcelas) + Transferencia.Valor;
                }
                decimal valorParcela = Math.Round(valorComJuros / QtdParcelas, 2);
                ValorParcelaCalculado = valorParcela;

                for (int i = 0; i < QtdParcelas; i++)
                {
                    int mesPagamento = dataSimulada.MesAtual + MesSelecionado + i;
                    int anoPagamento = dataSimulada.AnoAtual + (mesPagamento - 1) / 12;
                    mesPagamento = (mesPagamento - 1) % 12 + 1;

                    var faturaExistente = _db.Faturas
                        .FirstOrDefault(f => f.ContaBancariaID == conta.ID && f.MesPagamento == mesPagamento && f.AnoPagamento == anoPagamento);

                    if (faturaExistente != null)
                    {
                        faturaExistente.ValorFatura += valorParcela;

                        if (faturaExistente.Efetivado)
                        {
                            faturaExistente.Efetivado = false; // ← resetar se ela estiver como paga
                        }
                    }
                    else
                    {
                        var novaFatura = new Fatura
                        {
                            ContaBancariaID = conta.ID,
                            ValorFatura = valorParcela,
                            MesPagamento = mesPagamento,
                            AnoPagamento = anoPagamento,
                            Efetivado = false
                        };
                        _db.Faturas.Add(novaFatura);
                    }
                }
            }

            _db.Transferencias.Add(novaTransferencia);
            await _db.SaveChangesAsync();
            TempData["Valor"] = Transferencia.Valor.ToString("F2");
            TempData["Tipo"] = Transferencia.TipoTransferencia == "D" ? "Débito" : "Crédito";
            TempData["Descricao"] = Transferencia.Descricao ?? "-";
            TempData["Dia"] = novaTransferencia.DiaTransferencia;
            TempData["Mes"] = novaTransferencia.MesTransferencia;
            TempData["Ano"] = novaTransferencia.AnoTransferencia;

            return RedirectToPage("/PagesPosCarregamento/TransferenciaSucesso");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Erro inesperado: " + ex.Message);
            return Page();
        }
    }
}