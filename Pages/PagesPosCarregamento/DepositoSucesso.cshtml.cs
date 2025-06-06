using Microsoft.AspNetCore.Mvc.RazorPages;

public class DepositoSucessoModel : PageModel
{
    public string? Valor { get; set; }
    public string? Descricao { get; set; }
    public string DataFormatada { get; set; } = "-";

    public void OnGet()
    {
        Valor = TempData["Valor"] as string ?? "0,00";
        Descricao = TempData["Descricao"] as string ?? "-";

        var diaStr = TempData["Dia"]?.ToString();
        var mesStr = TempData["Mes"]?.ToString();
        var anoStr = TempData["Ano"]?.ToString();

        if (int.TryParse(diaStr, out int dia) && int.TryParse(mesStr, out int mes) && int.TryParse(anoStr, out int ano))
        {
            DataFormatada = $"{dia:D2}/{mes:D2}/{ano}";
        }
    }
}
