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
        modelBuilder.Entity<DataSimulada>()
            .HasOne(c => c.ContaBancaria)
            .WithOne(cb => cb.dataSimulada)
            .HasForeignKey<DataSimulada>(c => c.ContaBancariaID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
