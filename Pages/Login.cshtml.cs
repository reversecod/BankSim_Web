using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

public class LoginModel : PageModel
{
    private readonly AppDbContext _db;

    public LoginModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public string? Login { get; set; }

    [BindProperty]
    public string? Senha { get; set; }

    public string? MensagemErro { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Senha))
        {
            MensagemErro = "Login e senha são obrigatórios.";
            return Page();
        }

        // Busca o usuário pelo campo Login
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Login == Login);

        if (usuario == null)
        {
            MensagemErro = "Usuário ou Senha incorreto.";
            return Page();
        }

        var hasher = new PasswordHasher<object>();
        bool senhaValida = false;

        try
        {
            // Tenta verificar se é uma senha com hash
            var resultado = hasher.VerifyHashedPassword(null!, usuario.Senha, Senha);
            senhaValida = resultado == PasswordVerificationResult.Success;
        }
        catch (FormatException)
        {
            // Senha no banco não estava em hash (texto puro)
            senhaValida = usuario.Senha == Senha;

            // Se a senha em texto puro estiver correta, atualiza para hash
            if (senhaValida)
            {
                usuario.Senha = hasher.HashPassword(null!, Senha);
                await _db.SaveChangesAsync();
            }
        }

        if (!senhaValida)
        {
            MensagemErro = "Usuário ou Senha incorreto.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim("Login", usuario.Login),
            new Claim(ClaimTypes.NameIdentifier, usuario.ID.ToString()) 
        };

        var identity = new ClaimsIdentity(claims, "cookieAuth");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("cookieAuth", principal);

        bool possuiConta = await _db.ContasBancarias.AnyAsync(c => c.UsuarioID == usuario.ID);

        if (possuiConta)
        {
            return RedirectToPage("/Home");
        }
        else
        {
            return RedirectToPage("/CriarConta");
        }
    }
}