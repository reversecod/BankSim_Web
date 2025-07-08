using Banksim_Web.Models;

public class Calendario
{
    public int ID { get; set; }

    public int ContaBancariaID { get; set; }
    // Define o dia e mês em que o evento deve ocorrer na DataSimulada
    public int Dia { get; set; }
    public int Mes { get; set; }
    public int Ano { get; set; }
    public string Tipo { get; set; } = "Evento"; // "Evento" ou "Lembrete"
    public TipoEventoCalendario TipoEvento { get; set; }
    public decimal Valor { get; set; }
    public bool Recorrente { get; set; } // se true, o evento ocorre todo mês no mesmo dia
    public string? Titulo { get; set; }//Titulo do evento ou do lembrete
    public string? Descricao { get; set; }//Se for evento serve para descrever a transferencia/deposito, se não é o próprio lembrete
    public bool Concluido { get; set; }
    public ContaBancaria Conta { get; set; } = null!;
}