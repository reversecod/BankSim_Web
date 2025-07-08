using Banksim_Web.Enums;

namespace Banksim_Web.Dtos
{
    public class CalendarioDto
    {
        public int Id { get; set; }
        public int Dia { get; set; }
        public int Mes { get; set; }
        public int Ano { get; set; }
        public string Tipo { get; set; } = string.Empty; // Evento ou Lembrete
        public string? Titulo { get; set; }
        public string? Descricao { get; set; }
        public decimal Valor { get; set; }
        public bool Recorrente { get; set; }
        public TipoEventoCalendario TipoEvento { get; set; }
    }
}
