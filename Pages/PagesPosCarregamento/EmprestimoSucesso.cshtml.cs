using Microsoft.AspNetCore.Mvc.RazorPages;

public class EmprestimoSucessoModel : PageModel
{
    public string NomeEmprestimo { get; set; } = "-";
    public string ValorFormatado { get; set; } = "R$ 0,00";

    public void OnGet()
    {
        NomeEmprestimo = TempData["NomeEmprestimo"] as string ?? "-";
        ValorFormatado = "R$ " + (TempData["ValorEmprestimo"] as string ?? "0,00");
    }
}