using Banksim_Web.Enums;
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

    bool novaNotificacao = false;
    public List<Notificacao> Notificacoes { get; set; } = new();
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

            var diaAtual = conta.dataSimulada.DiaAtual;
            var mesAtual = conta.dataSimulada.MesAtual;
            var anoAtual = conta.dataSimulada.AnoAtual;

            // Verificação 1: Dia seguinte ao FECHAMENTO da fatura
            if (diaAtual == conta.DiaFatura + 1)
            {
                var faturaFechada = await _db.Faturas.FirstOrDefaultAsync(f =>
                    f.ContaBancariaID == conta.ID &&
                    f.MesPagamento == mesAtual &&
                    f.AnoPagamento == anoAtual &&
                    !f.Efetivado);

                if (faturaFechada != null)
                {
                    int mesReferencia = mesAtual == 1 ? 12 : mesAtual - 1;
                    int anoReferencia = mesAtual == 1 ? anoAtual - 1 : anoAtual;

                    string nomeMesReferencia = ((Meses)mesReferencia).ToString();
                    string nomeMesFechamento = ((Meses)mesAtual).ToString();

                    _db.Notificacoes.Add(new Notificacao
                    {
                        ContaBancariaID = conta.ID,
                        Titulo = "Fatura fechada",
                        Mensagem = $"Sua fatura referente ao mês de {nomeMesReferencia}/{anoReferencia} foi fechada em {conta.DiaFatura:D2}/{nomeMesFechamento}/{anoAtual}.",
                        DataNotificacao = DateTime.Now,
                        DiaNotificacao = diaAtual,
                        MesNotificacao = mesAtual,
                        AnoNotificacao = anoAtual,
                        Lida = false
                    });

                    novaNotificacao = true;
                }
            }

            // Verificação 2: Dia seguinte ao VENCIMENTO da fatura
            if (diaAtual == conta.DiaPagamentoFatura + 1)
            {
                var faturasVencidas = await _db.Faturas
                    .Where(f => f.ContaBancariaID == conta.ID &&
                                !f.Efetivado &&
                                f.AnoPagamento == anoAtual &&
                                f.MesPagamento == mesAtual)
                    .ToListAsync();

                foreach (var fatura in faturasVencidas)
                {
                    int mesReferencia = mesAtual == 1 ? 12 : mesAtual - 1;
                    int anoReferencia = mesAtual == 1 ? anoAtual - 1 : anoAtual;

                    string nomeMesReferencia = ((Meses)mesReferencia).ToString();
                    string nomeMesFechamento = ((Meses)mesAtual).ToString();

                    _db.Notificacoes.Add(new Notificacao
                    {
                        ContaBancariaID = conta.ID,
                        Titulo = "Fatura em atraso",
                        Mensagem = $"A fatura referente ao mês {nomeMesReferencia:D2}/{anoReferencia} (fechada em {conta.DiaPagamentoFatura:D2}/{nomeMesFechamento:D2}/{anoAtual}) está em atraso. Valor: R$ {fatura.ValorFaturaAtual:F2}.",
                        DataNotificacao = DateTime.Now,
                        DiaNotificacao = diaAtual,
                        MesNotificacao = mesAtual,
                        AnoNotificacao = anoAtual,
                        Lida = false
                    });

                    novaNotificacao = true;
                }
            }

            // Verifica empréstimos vencidos (baseado em diaPagamento e MesProxPagamento)
            var emprestimos = await _db.Emprestimos
            .Where(e => e.ContaBancariaID == conta.ID && !e.Pago)
            .ToListAsync();

            foreach (var emp in emprestimos)
            {
                // 1. Verifica se hoje é o dia após o vencimento da parcela
                bool ehDiaPosVencimento = diaAtual == emp.diaPagamento + 1;

                // 2. Verifica se a parcela pertence ao mês atual
                bool mesConfere = emp.MesProxPagamento == mesAtual;

                if (ehDiaPosVencimento && mesConfere)
                {
                    int mesReferencia = emp.MesProxPagamento;
                    int anoReferencia = anoAtual;

                    string nomeMesReferencia = ((Meses)mesReferencia).ToString();

                    // 3. Verifica se já existe notificação para esse empréstimo e mês
                    bool jaNotificado = await _db.Notificacoes.AnyAsync(n =>
                        n.ContaBancariaID == conta.ID &&
                        n.Titulo == "Parcela de empréstimo em atraso" &&
                        n.Mensagem.Contains($"referente a {nomeMesReferencia}/{anoReferencia}") &&
                        n.MesNotificacao == mesAtual &&
                        n.AnoNotificacao == anoAtual);

                    // 4. Cria a notificação se ainda não existir
                    if (!jaNotificado)
                    {
                        _db.Notificacoes.Add(new Notificacao
                        {
                            ContaBancariaID = conta.ID,
                            Titulo = "Parcela de empréstimo em atraso",
                            Mensagem = $"A parcela do empréstimo \"{emp.NomeEmprestimo}\" referente a {nomeMesReferencia}/{anoReferencia} está vencida.",
                            DataNotificacao = DateTime.Now,
                            DiaNotificacao = diaAtual,
                            MesNotificacao = mesAtual,
                            AnoNotificacao = anoAtual,
                            Lida = false
                        });

                        novaNotificacao = true;
                    }
                }
            }

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

            if (novaNotificacao)
            {
                TempData["NovaNotificacao"] = true;
            }
        }

        Saldo = conta.Saldo;
        LimiteCreditoDisponivel = conta.LimiteCreditoDisponivel;
        LimiteCreditoInicial = conta.LimiteCreditoInicial;
        DataSimulada = new DateOnly(
            conta.dataSimulada.AnoAtual,
            conta.dataSimulada.MesAtual,
            conta.dataSimulada.DiaAtual
        );

        Notificacoes = await _db.Notificacoes
        .Where(n => n.ContaBancariaID == conta.ID)
        .OrderByDescending(n => n.DataNotificacao)
        .ToListAsync();

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