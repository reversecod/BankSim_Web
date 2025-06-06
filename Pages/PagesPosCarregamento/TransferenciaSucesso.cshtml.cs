using Microsoft.AspNetCore.Mvc.RazorPages;

public class TransferenciaSucessoModel : PageModel
{
    public string? Tipo { get; set; }
    public string? Valor { get; set; }
    public string? Descricao { get; set; }
    public string DataFormatada { get; set; } = "-";

    public void OnGet()
    {
        Tipo = TempData["Tipo"] as string ?? "-";
        Valor = TempData["Valor"] as string ?? "0,00";
        Descricao = TempData["Descricao"] as string ?? "-";

        // Obter os três valores como strings ou inteiros
        var diaStr = TempData["Dia"]?.ToString();
        var mesStr = TempData["Mes"]?.ToString();
        var anoStr = TempData["Ano"]?.ToString();

        if (int.TryParse(diaStr, out int dia) && int.TryParse(mesStr, out int mes) && int.TryParse(anoStr, out int ano))
        {
            DataFormatada = $"{dia:D2}/{mes:D2}/{ano}";
        }
    }
}