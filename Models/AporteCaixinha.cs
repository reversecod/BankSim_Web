using Banksim_Web.Models;
using System.ComponentModel.DataAnnotations;

namespace Banksim_Web.Models
{
    public class AporteCaixinha
    {
        [Required]
        public int ID { get; set; }
        public int ContaBancariaID { get; set; }
        public int CaixinhaID { get; set; }
        public decimal ValorAporte { get; set; }
        public int DiasAplicados { get; set; }
        public decimal RendimentoAporte { get; set; }
        public int DiaCriacao { get; set; }
        public int MesCriacao { get; set; }
        public int AnoCriacao { get; set; }

        public Caixinha Caixinha { get; set; }
    }
}