using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
public class HomeModel : PageModel
{
    private readonly AppDbContext _db;

    public HomeModel(AppDbContext db)
    {
        _db = db;
    }

    public decimal Saldo { get; set; }
    public decimal LimiteCreditoInicial { get; set; }
    public decimal LimiteCreditoDisponivel { get; set; }
    public DateOnly DataSimulada { get; set; }

    [BindProperty]
    public decimal NovoLimite { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? AcaoData { get; set; } // usado pelas setas (avancar / retroceder)

    public string? MensagemErro { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var conta = await _db.ContasBancarias
            .Include(c => c.dataSimulada)
            .FirstOrDefaultAsync(c => c.UsuarioID.ToString() == userId);

        if (conta == null || conta.dataSimulada == null)
            return RedirectToPage("/Error");

        if (AcaoData == "avancar")
        {
            conta.dataSimulada.AvancarDias(1);

            await _db.Caixinhas
                .Where(c => c.ContaBancariaID == conta.ID)
                .ExecuteUpdateAsync(c => c.SetProperty(x => x.DiasCorridos, x => x.DiasCorridos + 1));

            await _db.AportesCaixinha
                .Where(a => a.ContaBancariaID == conta.ID && a.ValorAporte > 0)
                .ExecuteUpdateAsync(a => a.SetProperty(x => x.DiasAplicados, x => x.DiasAplicados + 1));

            var dataAtual = new DateOnly(
                conta.dataSimulada.AnoAtual,
                conta.dataSimulada.MesAtual,
                conta.dataSimulada.DiaAtual
            );

            var eventosHoje = await _db.Calendario
                .Where(e => e.ContaBancariaID == conta.ID &&
                       (
                           (e.Recorrente && e.Dia == dataAtual.Day) ||
                           (!e.Recorrente && e.Dia == dataAtual.Day && e.Mes == dataAtual.Month && e.Ano == dataAtual.Year)
                       ))
                .ToListAsync();

            foreach (var evento in eventosHoje)
            {
                switch (evento.TipoEvento)
                {
                    case TipoEventoCalendario.Deposito:
                        conta.Saldo += evento.Valor;
                        TempData["DepositoTitulo"] = evento.Titulo;
                        TempData["DepositoDescricao"] = evento.Descricao ?? "Sem descrição";
                        TempData["DepositoValor"] = evento.Valor;
                        break;

                    case TipoEventoCalendario.Transferencia:
                        if (conta.Saldo >= evento.Valor)
                        {
                            conta.Saldo -= evento.Valor;
                            TempData["TransferenciaTitulo"] = evento.Titulo;
                            TempData["TransferenciaDescricao"] = evento.Descricao ?? "Sem descrição";
                            TempData["TransferenciaValor"] = evento.Valor;
                        }
                        else
                        {
                            TempData["TransferenciaTitulo"] = "Transferência Bloqueada";
                            TempData["TransferenciaDescricao"] = $"A transferência de R${evento.Valor:F2} foi bloqueada por saldo insuficiente.";
                            TempData["TransferenciaValor"] = evento.Valor;
                        }
                        break;

                    default:
                        TempData["LembreteMensagem"] = evento.Titulo ?? "Você tem um lembrete hoje!";
                        TempData["LembreteDescricao"] = evento.Descricao ?? "Sem detalhes";
                        break;
                }
            }

            await _db.SaveChangesAsync();
        }

        Saldo = conta.Saldo;
        LimiteCreditoDisponivel = conta.LimiteCreditoDisponivel;
        LimiteCreditoInicial = conta.LimiteCreditoInicial;
        DataSimulada = new DateOnly(
            conta.dataSimulada.AnoAtual,
            conta.dataSimulada.MesAtual,
            conta.dataSimulada.DiaAtual
        );

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var conta = await _db.ContasBancarias
            .Include(c => c.dataSimulada) 
            .FirstOrDefaultAsync(c => c.UsuarioID.ToString() == userId);

        if (conta == null)
            return RedirectToPage("/Erro");

        bool possuiFaturaAberta = await _db.Faturas
            .AnyAsync(f => f.ContaBancariaID == conta.ID && !f.Efetivado);

        if (possuiFaturaAberta && conta.LimiteCreditoInicial != conta.LimiteCreditoDisponivel)
        {
            ModelState.AddModelError("", "Não é possível alterar o limite enquanto houver faturas em aberto.");
            Saldo = conta.Saldo;
            LimiteCreditoDisponivel = conta.LimiteCreditoDisponivel;
            LimiteCreditoInicial = conta.LimiteCreditoInicial;
            return Page();
        }

        conta.LimiteCreditoInicial = NovoLimite;
        conta.LimiteCreditoDisponivel = NovoLimite;

        _db.Extratos.Add(new Extrato
        {
            ContaBancariaID = conta.ID,
            TipoTransacao = "Alteração do limite de Crédito",
            Valor = NovoLimite,
            Descricao = $"Limite de crédito alterado para R$ {NovoLimite:F2}",
            DiaTransacao = conta.dataSimulada.DiaAtual,
            MesTransacao = conta.dataSimulada.MesAtual,
            AnoTransacao = conta.dataSimulada.AnoAtual,
            Timestamp = DateTime.Now
        });

        await _db.SaveChangesAsync();

        return RedirectToPage();
    }
}