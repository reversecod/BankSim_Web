using Microsoft.AspNetCore.Mvc.RazorPages;

public class FaturaSucessoModel : PageModel
{
    public string ValorPago { get; set; } = "0,00";
    public string MesFatura { get; set; } = "-";

    public void OnGet()
    {
        ValorPago = TempData["ValorPago"] as string ?? "0,00";

        if (int.TryParse(TempData["Mes"]?.ToString(), out int mes) &&
            int.TryParse(TempData["Ano"]?.ToString(), out int ano))
        {
            MesFatura = $"{(Banksim_Web.Enums.Meses)mes}/{ano}";
        }
    }
}