using Banksim_Web.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<ContaBancaria> ContasBancarias { get; set; }
    public DbSet<DataSimulada> DataSimulada { get; set; }
    public DbSet<Deposito> Depositos { get; set; }
    public DbSet<Transferencia> Transferencias { get; set; }
    public DbSet<Anotacao> Anotacoes { get; set; }
    public DbSet<Caixinha> Caixinhas { get; set; }
    public DbSet<Emprestimo> Emprestimos { get; set; }
    public DbSet<Fatura> Faturas { get; set; }
    public DbSet<Extrato> Extratos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Mapeamento para nomes exatos das tabelas no banco
        modelBuilder.Entity<Usuario>().ToTable("Usuarios");
        modelBuilder.Entity<ContaBancaria>().ToTable("ContasBancarias");
        modelBuilder.Entity<DataSimulada>().ToTable("DataSimulada");
        modelBuilder.Entity<Deposito>().ToTable("Depositos");
        modelBuilder.Entity<Transferencia>().ToTable("Transferencias");
        modelBuilder.Entity<Anotacao>().ToTable("Anotacoes");
        modelBuilder.Entity<Caixinha>().ToTable("Caixinhas");
        modelBuilder.Entity<Emprestimo>().ToTable("Emprestimos");
        modelBuilder.Entity<Fatura>().ToTable("Fatura"); // singular conforme o banco
        modelBuilder.Entity<Extrato>().ToTable("Extrato");

        // Relacionamento 1:1 entre ContaBancaria e DataSimulada
        modelBuilder.Entity<DataSimulada>()
            .HasOne(c => c.ContaBancaria)
            .WithOne(cb => cb.dataSimulada)
            .HasForeignKey<DataSimulada>(c => c.ContaBancariaID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Fatura>()
       .HasIndex(f => new { f.ContaBancariaID, f.MesPagamento, f.AnoPagamento })
       .IsUnique();
    }
}