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

        Console.WriteLine("?? [POST] Iniciando OnPostAsync de Transfer�ncia...");

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine($"?? [INFO] ID do usu�rio logado: {userIdStr}");

        if (!int.TryParse(userIdStr, out var userId))
        {
            Console.WriteLine("?? [ERRO] ID do usu�rio � inv�lido.");
            ModelState.AddModelError("", "Erro ao identificar usu�rio.");
            return Page();
        }

        var contaOrigem = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);
        if (contaOrigem == null)
        {
            Console.WriteLine("?? [ERRO] Conta banc�ria n�o encontrada.");
            ModelState.AddModelError("", "Conta banc�ria n�o encontrada.");
            return Page();
        }

        Console.WriteLine($"?? [INFO] Conta origem encontrada com ID: {contaOrigem.ID}");

        var dataSimulada = _db.DataSimulada.FirstOrDefault(c => c.ContaBancariaID == contaOrigem.ID);
        if (dataSimulada == null)
        {
            Console.WriteLine("?? [ERRO] Calend�rio n�o encontrado.");
            ModelState.AddModelError("", "Calend�rio n�o encontrado.");
            return Page();
        }

        Console.WriteLine($"?? [INFO] Calend�rio: {dataSimulada.DiaAtual}/{dataSimulada.MesAtual}/{dataSimulada.AnoAtual}");

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

        Console.WriteLine($" [INFO] Valor total da transfer�ncia: R$ {valorComJuros}");

        if (Transferencia.TipoTransferencia == "D")
        {
            Console.WriteLine(" [INFO] Tipo de transfer�ncia: D�bito");
            if (contaOrigem.Saldo < valorComJuros)
            {
                Console.WriteLine("?? [ERRO] Saldo insuficiente.");
                ModelState.AddModelError("", "Saldo insuficiente para transfer�ncia por d�bito.");
                return Page();
            }
            if (Transferencia.DiaTransferenciaAgendada == null && Transferencia.MesTransferenciaAgendada == null && Transferencia.AnoTransferenciaAgendada == null)
            {
                contaOrigem.Saldo -= valorComJuros;
                Transferencia.Efetivado = true;
                Console.WriteLine($"? [OK] Transfer�ncia imediata via d�bito. Novo saldo: R$ {contaOrigem.Saldo}");
            }
        }
        else if (Transferencia.TipoTransferencia == "C")
        {
            Console.WriteLine("[INFO] Tipo de transfer�ncia: Cr�dito");
            if (contaOrigem.LimiteCredito < valorComJuros)
            {
                Console.WriteLine("?? [ERRO] Limite de cr�dito insuficiente.");
                ModelState.AddModelError("", "Limite de cr�dito insuficiente para transfer�ncia por cr�dito.");
                return Page();
            }
            if (Transferencia.DiaTransferenciaAgendada == null && Transferencia.MesTransferenciaAgendada == null && Transferencia.AnoTransferenciaAgendada == null)
            {
                contaOrigem.LimiteCredito -= valorComJuros;
                contaOrigem.FaturaCredito += valorComJuros;
                Transferencia.Efetivado = true;
                Console.WriteLine($"? [OK] Transfer�ncia imediata via cr�dito. Novo limite: R$ {contaOrigem.LimiteCredito}, Fatura: R$ {contaOrigem.FaturaCredito}");
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

            Console.WriteLine("?? [INFO] Transfer�ncia agendada registrada.");
            Transferencia.Efetivado = false;
        }

        if (!ModelState.IsValid)
        {
            Console.WriteLine("?? [ERRO] ModelState inv�lido.");
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

        Console.WriteLine("? [OK] Transfer�ncia registrada com sucesso.");

        return RedirectToPage("/Home");
    }
}