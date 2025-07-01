using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // Para métodos assíncronos como FirstOrDefaultAsync
using System.Security.Claims;

namespace Banksim_Web.Pages.Caixinha
{
    [Authorize]
    public class CriarCaixinhaModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CriarCaixinhaModel> _logger;

        public CriarCaixinhaModel(AppDbContext db, IWebHostEnvironment env, ILogger<CriarCaixinhaModel> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty]
        public Models.Caixinha Caixinha { get; set; } = new();

        [BindProperty]
        [ValidateNever]
        public IFormFile? ArquivoImagem { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Validação do ID do usuário
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                {
                    _logger.LogError("ID do usuário inválido ou não autenticado.");
                    return RedirectToPage("/Erro");
                }

                // Busca assíncrona da conta e data simulada
                var conta = await _db.ContasBancarias
                    .FirstOrDefaultAsync(c => c.UsuarioID == userId);
                var dataSimulada = await _db.DataSimulada
                    .FirstOrDefaultAsync(d => d.ContaBancariaID == conta!.ID);

                if (conta == null || dataSimulada == null)
                {
                    _logger.LogWarning("Conta ou data simulada não encontrada para o usuário {UserId}.", userId);
                    return RedirectToPage("/Erro");
                }

                // Validação do saldo
                if (Caixinha.ValorCaixinhaAtual <= 0)
                {
                    ModelState.AddModelError("Caixinha.ValorCaixinha", "O valor inicial deve ser maior que zero.");
                    return Page();
                }

                if (Caixinha.ValorCaixinhaAtual > conta.Saldo)
                {
                    ModelState.AddModelError("Caixinha.ValorCaixinha", "Saldo insuficiente. O valor da caixinha não pode exceder o saldo disponível");
                    return Page();
                }

                // Configuração dos dados da caixinha
                Caixinha.ContaBancariaID = conta.ID;
                Caixinha.DiaCriacao = dataSimulada.DiaAtual;
                Caixinha.MesCriacao = dataSimulada.MesAtual;
                Caixinha.AnoCriacao = dataSimulada.AnoAtual;

                // Processamento da imagem
                if (ArquivoImagem != null && ArquivoImagem.Length > 0)
                {
                    try
                    {
                        if (ArquivoImagem.Length > 10 * 1024 * 1024)
                        {
                            ModelState.AddModelError("", "Imagem muito grande. Tamanho máximo permitido: 10 MB.");
                            return Page();
                        }

                        var extensao = Path.GetExtension(ArquivoImagem.FileName)?.ToLower();
                        if (string.IsNullOrEmpty(extensao) || !new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(extensao))
                        {
                            ModelState.AddModelError("", "Formato de imagem inválido. Use JPG, PNG ou GIF.");
                            return Page();
                        }

                        var pasta = Path.Combine(_env.WebRootPath, "img", "caixinhas");
                        if (!Directory.Exists(pasta))
                        {
                            Directory.CreateDirectory(pasta);
                            _logger.LogInformation("Diretório criado: {Pasta}", pasta);
                        }

                        var nomeArquivo = $"{Guid.NewGuid()}{extensao}";
                        var caminhoCompleto = Path.Combine(pasta, nomeArquivo);

                        using (var stream = new FileStream(caminhoCompleto, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await ArquivoImagem.CopyToAsync(stream);
                        }
                        Caixinha.FotoCaixinha = nomeArquivo;
                        _logger.LogInformation("Imagem salva com sucesso: {NomeArquivo}", nomeArquivo);
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Erro de I/O ao salvar a imagem em {Caminho}");
                        ModelState.AddModelError("", "Erro ao salvar a imagem devido a um problema de disco. Tente novamente.");
                        return Page();
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogError(ex, "Permissão negada ao salvar a imagem em {Caminho}");
                        ModelState.AddModelError("", "Permissão negada para salvar a imagem. Verifique as permissões do diretório.");
                        return Page();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro inesperado ao processar a imagem em {Caminho}");
                        ModelState.AddModelError("", "Erro ao processar a imagem. Tente novamente.");
                        return Page();
                    }
                }

                // Salvamento no banco de dados com transação
                using (var transaction = await _db.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _db.Caixinhas.Add(Caixinha);
                        conta.Saldo -= Caixinha.ValorCaixinhaAtual; // Deduzir o valor do saldo
                        await _db.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _logger.LogInformation("Caixinha salva no banco de dados com ID: {Id}. Saldo atualizado para {Saldo}", Caixinha.ID, conta.Saldo);
                    }
                    catch (DbUpdateException ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Erro ao atualizar o banco de dados para a caixinha com ID: {Id}", Caixinha.ID);
                        ModelState.AddModelError("", "Erro ao salvar os dados no banco. Verifique os valores inseridos.");
                        return Page();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Erro inesperado ao salvar a caixinha com ID: {Id}", Caixinha.ID);
                        ModelState.AddModelError("", "Ocorreu um erro inesperado ao salvar.");
                        return Page();
                    }
                }

                return RedirectToPage("/Caixinha/Caixinha");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico ao criar caixinha.");
                ModelState.AddModelError("", "Ocorreu um erro crítico. Tente novamente ou contate o suporte.");
                return Page();
            }
        }
    }
}