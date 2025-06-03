using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

public class RegistroModel : PageModel
{
    private readonly AppDbContext _db;

    public RegistroModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public string? Nome { get; set; }

    [BindProperty]
    public string? Login { get; set; }

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public string? Senha { get; set; }

    public string? Mensagem { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Senha))
        {
            Mensagem = "Preencha todos os campos obrigatórios.";
            return Page();
        }

        // Verifica se o login já existe
        var loginExistente = await _db.Usuarios.FirstOrDefaultAsync(u => u.Login == Login);
        if (loginExistente != null)
        {
            Mensagem = "Este login já está em uso.";
            return Page();
        }

        var senhaHash = new PasswordHasher<Usuario>().HashPassword(null!, Senha);

        var novoUsuario = new Usuario
        {
            Nome = Nome!,
            Login = Login!,
            Email = Email,
            Senha = senhaHash,
            DataCriacao = DateTime.Now
        };

        _db.Usuarios.Add(novoUsuario);
        await _db.SaveChangesAsync();

        Mensagem = "Usuário registrado com sucesso!";
        return Page();
    }
}