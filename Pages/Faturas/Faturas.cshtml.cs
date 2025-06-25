using System.Security.Claims;
using Banksim_Web.Enums;
using Banksim_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

[Authorize]
public class FaturasModel : PageModel
{
    private readonly AppDbContext _db;

    public FaturasModel(AppDbContext db)
    {
        _db = db;
        MesesDisponiveis = new List<SelectListItem>();
    }

    [BindProperty]
    public int MesSelecionado { get; set; }

    [BindProperty]
    public decimal ValorPagamento { get; set; }

    public Fatura? FaturaSelecionada { get; set; }

    public List<SelectListItem> MesesDisponiveis { get; set; }

    public IActionResult OnGet()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out var uid)) return RedirectToPage("/Erro");

        var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == uid);
        if (conta == null) return RedirectToPage("/Erro");

        var data = _db.DataSimulada.FirstOrDefault(d => d.ContaBancariaID == conta.ID);
        if (data == null) return RedirectToPage("/Erro");

        // Fatura mais próxima do mês atual
        FaturaSelecionada = _db.Faturas
            .Where(f => f.ContaBancariaID == conta.ID && !f.Efetivado)
            .OrderBy(f => f.AnoPagamento)
            .ThenBy(f => f.MesPagamento)
            .FirstOrDefault();

        if (FaturaSelecionada == null)
        {
            return RedirectToPage("/Faturas/TodasFaturas");
        }


        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out var uid))
        {
            ModelState.AddModelError("", "Usuário inválido.");
            return Page();
        }

        var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == uid);
        if (conta == null)
        {
            ModelState.AddModelError("", "Conta bancária não encontrada.");
            return Page();
        }

        var fatura = _db.Faturas
            .Where(f => f.ContaBancariaID == conta.ID && !f.Efetivado)
            .OrderBy(f => f.AnoPagamento)
            .ThenBy(f => f.MesPagamento)
            .FirstOrDefault();

        var dataSimulada = _db.DataSimulada.FirstOrDefault(d => d.ContaBancariaID == conta.ID);
        if (dataSimulada == null)
        {
            ModelState.AddModelError("", "Data simulada não localizada.");
            return Page();
        }

        if (fatura == null)
        {
            return RedirectToPage("/Faturas/TodasFaturas");
        }

        // 🧠 Reatribui para manter visível no Razor
        FaturaSelecionada = fatura;

        if (ValorPagamento == 0)
        {
            ModelState.AddModelError("", "Digite um valor para pagamento ou clique em 'Pagar tudo'.");
            return Page();
        }

        if (ValorPagamento > fatura.ValorFaturaAtual)
        {
            ValorPagamento = fatura.ValorFaturaAtual;
        }

        if (ValorPagamento < 0.01m)
        {
            ModelState.AddModelError("", "O valor de pagamento deve ser maior que zero.");
            return Page();
        }

        if (conta.Saldo < ValorPagamento)
        {
            ModelState.AddModelError("", "Saldo insuficiente para pagar a fatura.");
            return Page();
        }

        conta.Saldo -= ValorPagamento;
        conta.LimiteCreditoDisponivel += ValorPagamento;
        fatura.ValorFaturaAtual -= ValorPagamento;
        if (fatura.ValorFaturaAtual <= 0.01m)
            fatura.Efetivado = true;

        _db.Extratos.Add(new Extrato
        {
            ContaBancariaID = conta.ID,
            TipoTransacao = "Pagamento de Fatura",
            Valor = ValorPagamento,
            DiaTransacao = dataSimulada.DiaAtual,
            MesTransacao = dataSimulada.MesAtual,
            AnoTransacao = dataSimulada.AnoAtual,
            Timestamp = DateTime.Now
        });

        await _db.SaveChangesAsync();

        TempData["Mes"] = fatura.MesPagamento;
        TempData["Ano"] = fatura.AnoPagamento;
        TempData["ValorPago"] = ValorPagamento.ToString("F2");

        return RedirectToPage("/PagesPosCarregamento/FaturaSucesso");
    }
}