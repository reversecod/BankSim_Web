using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using Banksim_Web.Enums;

[Authorize]
public class TransferenciaModel : PageModel
{
    private readonly AppDbContext _db;

    public TransferenciaModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public Transferencia Transferencia { get; set; }

    public List<SelectListItem> ListaMeses { get; set; }

    public IActionResult OnGet()
    {
        ListaMeses = Enum.GetValues(typeof(Meses))
            .Cast<Meses>()
            .Select(m => new SelectListItem
            {
                Value = ((int)m).ToString(),
                Text = m.ToString()
            }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ListaMeses = Enum.GetValues(typeof(Meses))
        .Cast<Meses>()
        .Select(m => new SelectListItem
        {
            Value = ((int)m).ToString(),
            Text = m.ToString()
        }).ToList();

        Console.WriteLine("?? [POST] Iniciando OnPostAsync de Transferência...");

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($"?? [INFO] ID do usuário logado: {userIdStr}");

        if (!int.TryParse(userIdStr, out var userId))
        {
            Console.WriteLine("?? [ERRO] ID do usuário é inválido.");
            ModelState.AddModelError("", "Erro ao identificar usuário.");
            return Page();
        }

        var contaOrigem = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
        if (contaOrigem == null)
        {
            Console.WriteLine("?? [ERRO] Conta bancária não encontrada.");
            ModelState.AddModelError("", "Conta bancária não encontrada.");
            return Page();
        }

        Console.WriteLine($"?? [INFO] Conta origem encontrada com ID: {contaOrigem.ID}");

        var dataSimulada = _db.DataSimulada.FirstOrDefault(c => c.ContaBancariaID == contaOrigem.ID);
        if (dataSimulada == null)
        {
            Console.WriteLine("?? [ERRO] Calendário não encontrado.");
            ModelState.AddModelError("", "Calendário não encontrado.");
            return Page();
        }

        Console.WriteLine($"?? [INFO] Calendário: {dataSimulada.DiaAtual}/{dataSimulada.MesAtual}/{dataSimulada.AnoAtual}");

        Transferencia.ContaBancariaID = contaOrigem.ID;
        Transferencia.DiaTransferencia = dataSimulada.DiaAtual;
        Transferencia.MesTransferencia = dataSimulada.MesAtual; 
        Transferencia.AnoTransferencia = dataSimulada.AnoAtual;

        decimal valorComJuros = Transferencia.Valor;
        if (Transferencia.TipoTransferencia == "C" && Transferencia.TipoJuros != null && Transferencia.ValorPorcentagem > 0)
        {
            decimal taxa = Transferencia.ValorPorcentagem / 100;
            if (Transferencia.TipoJuros == "S")
            {
                valorComJuros += Transferencia.Valor * taxa;
                Console.WriteLine($"?? [INFO] Juros simples aplicados: {Transferencia.Valor * taxa}");
            }
            else if (Transferencia.TipoJuros == "C")
            {
                valorComJuros = Transferencia.Valor * (decimal)Math.Pow(1 + (double)taxa, 1);
                Console.WriteLine($"[INFO] Juros compostos aplicados: {valorComJuros - Transferencia.Valor}");
            }
        }

        Console.WriteLine($" [INFO] Valor total da transferência: R$ {valorComJuros}");

        if (Transferencia.TipoTransferencia == "D")
        {
            Console.WriteLine(" [INFO] Tipo de transferência: Débito");
            if (contaOrigem.Saldo < valorComJuros)
            {
                Console.WriteLine("?? [ERRO] Saldo insuficiente.");
                ModelState.AddModelError("", "Saldo insuficiente para transferência por débito.");
                return Page();
            }
            if (Transferencia.DiaTransferenciaAgendada == null && Transferencia.MesTransferenciaAgendada == null && Transferencia.AnoTransferenciaAgendada == null)
            {
                contaOrigem.Saldo -= valorComJuros;
                Transferencia.Efetivado = true;
                Console.WriteLine($"? [OK] Transferência imediata via débito. Novo saldo: R$ {contaOrigem.Saldo}");
            }
        }
        else if (Transferencia.TipoTransferencia == "C")
        {
            Console.WriteLine("[INFO] Tipo de transferência: Crédito");
            if (contaOrigem.LimiteCredito < valorComJuros)
            {
                Console.WriteLine("?? [ERRO] Limite de crédito insuficiente.");
                ModelState.AddModelError("", "Limite de crédito insuficiente para transferência por crédito.");
                return Page();
            }
            if (Transferencia.DiaTransferenciaAgendada == null && Transferencia.MesTransferenciaAgendada == null && Transferencia.AnoTransferenciaAgendada == null)
            {
                contaOrigem.LimiteCredito -= valorComJuros;
                contaOrigem.FaturaCredito += valorComJuros;
                Transferencia.Efetivado = true;
                Console.WriteLine($"? [OK] Transferência imediata via crédito. Novo limite: R$ {contaOrigem.LimiteCredito}, Fatura: R$ {contaOrigem.FaturaCredito}");
            }
        }

        if (Transferencia.DiaTransferenciaAgendada.HasValue && Transferencia.MesTransferenciaAgendada.HasValue && Transferencia.AnoTransferenciaAgendada.HasValue)
        {
            var agendamentoDate = new DateTime(Transferencia.AnoTransferenciaAgendada.Value, Transferencia.MesTransferenciaAgendada.Value, Transferencia.DiaTransferenciaAgendada.Value);
            var currentDate = new DateTime(dataSimulada.AnoAtual, dataSimulada.MesAtual, dataSimulada.DiaAtual);

            Console.WriteLine($"?? [INFO] Agendamento: {agendamentoDate.ToShortDateString()} vs Hoje: {currentDate.ToShortDateString()}");

            if (agendamentoDate < currentDate)
            {
                Console.WriteLine("?? [ERRO] Agendamento no passado.");
                ModelState.AddModelError("", "A data de agendamento deve ser no futuro.");
                return Page();
            }

            Console.WriteLine("?? [INFO] Transferência agendada registrada.");
            Transferencia.Efetivado = false;
        }

        if (!ModelState.IsValid)
        {
            Console.WriteLine("?? [ERRO] ModelState inválido.");
            foreach (var entry in ModelState)
            {
                foreach (var error in entry.Value.Errors)
                {
                    Console.WriteLine($"? Campo: {entry.Key}, Erro: {error.ErrorMessage}");
                }
            }
            return Page();
        }

        _db.Transferencias.Add(Transferencia);
        await _db.SaveChangesAsync();

        Console.WriteLine("? [OK] Transferência registrada com sucesso.");

        return RedirectToPage("/Home");
    }
}