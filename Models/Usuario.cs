

public class Usuario
{
    public int ID { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Senha { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}