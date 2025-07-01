using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize]
public class CalendarioModel : PageModel
{
    private readonly AppDbContext _db;

    public CalendarioModel(AppDbContext db)
    {
        _db = db;
    }

    public DateOnly DataSimulada { get; set; }
    public List<Calendario> Eventos { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var conta = await _db.ContasBancarias
            .Include(c => c.dataSimulada)
            .FirstOrDefaultAsync(c => c.UsuarioID.ToString() == userId);

        if (conta == null || conta.dataSimulada == null)
        {
            // Redireciona se a conta ou data simulada estiverem ausentes
            Response.Redirect("/Erro");
            return;
        }

        // Define data simulada
        DataSimulada = new DateOnly(
            conta.dataSimulada.AnoAtual,
            conta.dataSimulada.MesAtual,
            conta.dataSimulada.DiaAtual
        );

        // Busca eventos do mês atual (recorrentes ou específicos do mês)
        Eventos = await _db.Calendario
            .Where(e => e.ContaBancariaID == conta.ID &&
                       (e.Recorrente && e.Mes == DataSimulada.Month ||
                        !e.Recorrente && e.Mes == DataSimulada.Month && e.Ano == DataSimulada.Year))
            .ToListAsync();
    }
}