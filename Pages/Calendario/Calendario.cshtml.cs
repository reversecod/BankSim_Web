using Banksim_Web.Dtos;
using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

[Authorize]
public class CalendarioModel : PageModel
{
    private readonly AppDbContext _db;

    public CalendarioModel(AppDbContext db)
    {
        _db = db;
    }

    public DateOnly DataSimulada { get; set; }
    public static (int mes, int ano) MesAnterior(int mes, int ano)
    {
        return mes == 1 ? (12, ano - 1) : (mes - 1, ano);
    }

    public static (int mes, int ano) ProximoMes(int mes, int ano)
    {
        return mes == 12 ? (1, ano + 1) : (mes + 1, ano);
    }

    [BindProperty(Name = "eventoId", SupportsGet = true)]
    public int EventoId { get; set; }
    public List<Calendario> Eventos { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? MesSelecionado { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? AnoSelecionado { get; set; }

    // Propriedade para uso no frontend (JavaScript)
    public string EventosJson => JsonSerializer.Serialize(
    Eventos.Select(e => new CalendarioDto
    {
        Id = e.ID,
        Dia = e.Dia,
        Mes = e.Mes,
        Ano = e.Ano,
        Tipo = e.Tipo,
        Titulo = e.Titulo,
        Descricao = e.Descricao,
        Valor = e.Valor,
        Recorrente = e.Recorrente,
        TipoEvento = e.TipoEvento
    }),
    new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

    public async Task<IActionResult> OnPostSalvarEventoAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var conta = await _db.ContasBancarias
            .Include(c => c.dataSimulada)
            .FirstOrDefaultAsync(c => c.UsuarioID.ToString() == userId);

        if (conta == null || conta.dataSimulada == null)
            return Redirect("/Erro");

        // Coleta os dados do formulário manualmente
        int dia = int.Parse(Request.Form["Dia"]);
        int mes = int.Parse(Request.Form["Mes"]);
        int ano = int.Parse(Request.Form["Ano"]);
        string tipo = Request.Form["Tipo"]; // "Evento" ou "Lembrete"
        string? titulo = Request.Form["Titulo"];
        string? descricao = Request.Form["Descricao"];
        decimal.TryParse(Request.Form["Valor"], out decimal valor);
        bool recorrente = Request.Form["Recorrente"] == "on";

        // Enum do tipo de evento (Transferência ou Depósito)
        TipoEventoCalendario tipoEvento = TipoEventoCalendario.Nenhum;

        if (tipo == "Evento" && Enum.TryParse(Request.Form["TipoEvento"], out TipoEventoCalendario parsed))
        {
            tipoEvento = parsed;
        }

        // Cria novo objeto Calendario
        var novo = new Calendario
        {
            ContaBancariaID = conta.ID,
            Dia = dia,
            Mes = mes,
            Ano = ano,
            Tipo = tipo,
            Titulo = titulo,
            Descricao = descricao,
            Valor = valor,
            Recorrente = recorrente,
            TipoEvento = tipoEvento,
            Concluido = false
        };

        _db.Calendario.Add(novo);
        await _db.SaveChangesAsync();

        return RedirectToPage(); // recarrega a página
    }
    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var conta = await _db.ContasBancarias
            .Include(c => c.dataSimulada)
            .FirstOrDefaultAsync(c => c.UsuarioID.ToString() == userId);

        if (conta == null || conta.dataSimulada == null)
        {
            Response.Redirect("/Erro");
            return;
        }

        // Define o mês/ano exibido
        int mes = MesSelecionado ?? conta.dataSimulada.MesAtual;
        int ano = AnoSelecionado ?? conta.dataSimulada.AnoAtual;
        DataSimulada = new DateOnly(ano, mes, 1);

        // Define a data atual da simulação para comparação na View
        ViewData["DataAtualSimulada"] = new DateOnly(conta.dataSimulada.AnoAtual, conta.dataSimulada.MesAtual, conta.dataSimulada.DiaAtual);

        // Busca eventos recorrentes para o mês e ano, incluindo os recorrentes
        Eventos = await _db.Calendario
        .Where(e => e.ContaBancariaID == conta.ID &&
                   (
                       (e.Recorrente) ||
                       (!e.Recorrente && e.Mes == DataSimulada.Month && e.Ano == DataSimulada.Year)
                   ))
        .ToListAsync();
    }
    public async Task<IActionResult> OnPostEditarEventoAsync()
    {
        int eventoId = int.Parse(Request.Form["eventoId"]);
        string titulo = Request.Form["titulo"];
        string descricao = Request.Form["descricao"];
        string tipo = Request.Form["tipo"]; // Evento ou Lembrete
        decimal.TryParse(Request.Form["valor"], out decimal valor);
        bool recorrente = Request.Form["recorrente"] == "on";

        TipoEventoCalendario tipoEvento = TipoEventoCalendario.Nenhum;
        if (tipo == "Evento" && Enum.TryParse(Request.Form["tipoEvento"], out TipoEventoCalendario parsed))
        {
            tipoEvento = parsed;
        }

        var evento = await _db.Calendario.FindAsync(eventoId);

        if (evento != null)
        {
            evento.Titulo = titulo;
            evento.Descricao = descricao;
            evento.Tipo = tipo;
            evento.Valor = valor;
            evento.Recorrente = recorrente;
            evento.TipoEvento = tipoEvento;

            _db.Update(evento);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExcluirEventoAsync()
    {
        Console.WriteLine("Entrou no método de exclusão");

        if (!int.TryParse(Request.Form["eventoId"], out int eventoId))
        {
            return BadRequest("ID do evento inválido.");
        }

        Console.WriteLine($"Tentando excluir evento ID: {eventoId}");

        var evento = await _db.Calendario.FindAsync(eventoId);

        if (evento == null)
        {
            return NotFound("Evento não encontrado.");
        }

        _db.Calendario.Remove(evento);
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

}