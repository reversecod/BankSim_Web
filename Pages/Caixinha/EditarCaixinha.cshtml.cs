using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Security.Claims;

namespace Banksim_Web.Pages.Caixinha
{
    [Authorize]
    public class EditarCaixinhaModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public EditarCaixinhaModel(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [BindProperty]
        public Models.Caixinha Caixinha { get; set; } = null!;

        [BindProperty]
        [ValidateNever]
        public IFormFile? ArquivoImagem { get; set; }

        public IActionResult OnGet(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);

            if (conta == null)
                return RedirectToPage("/Error");

            Caixinha = _db.Caixinhas.FirstOrDefault(c => c.ID == id && c.ContaBancariaID == conta.ID)!;

            if (Caixinha == null)
                return RedirectToPage("/Error");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);

            if (conta == null)
                return RedirectToPage("/Error");

            var original = _db.Caixinhas.FirstOrDefault(c => c.ID == Caixinha.ID && c.ContaBancariaID == conta.ID);

            if (original == null)
                return RedirectToPage("/Error");

            original.NomeCaixinha = Caixinha.NomeCaixinha;

            if (ArquivoImagem != null && ArquivoImagem.Length > 0)
            {
                var extensao = Path.GetExtension(ArquivoImagem.FileName).ToLower();
                if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(extensao))
                {
                    ModelState.AddModelError("", "Formato de imagem inválido. Use JPG, PNG ou GIF.");
                    return Page();
                }

                var pasta = Path.Combine(_env.WebRootPath, "img", "caixinhas");
                if (!Directory.Exists(pasta))
                    Directory.CreateDirectory(pasta);

                var nomeArquivo = $"{Guid.NewGuid()}{extensao}";
                var caminho = Path.Combine(pasta, nomeArquivo);

                using var stream = new FileStream(caminho, FileMode.Create);
                await ArquivoImagem.CopyToAsync(stream);

                original.FotoCaixinha = nomeArquivo;
            }

            await _db.SaveChangesAsync();
            return RedirectToPage("/Caixinha/DetalheCaixinha", new { id = original.ID });
        }

        public async Task<IActionResult> OnPostExcluirAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var conta = _db.ContasBancarias.FirstOrDefault(c => c.UsuarioID == userId);

            if (conta == null)
                return RedirectToPage("/Error");

            var caixinha = _db.Caixinhas.FirstOrDefault(c => c.ID == Caixinha.ID && c.ContaBancariaID == conta.ID);

            if (caixinha == null)
                return RedirectToPage("/Error");

            if (caixinha.ValorCaixinhaAtual > 0)
            {
                ModelState.AddModelError("", "Para excluir esta caixinha, é necessário resgatar todo o valor guardado.");
                Caixinha = caixinha; // Recarrega dados na tela em caso de erro
                return Page();
            }

            _db.Caixinhas.Remove(caixinha);
            await _db.SaveChangesAsync();

            return RedirectToPage("/Caixinha/caixinha");
        }
    }
}