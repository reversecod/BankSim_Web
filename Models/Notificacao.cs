using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banksim_Web.Models
{
    public class Notificacao
    {
        public int ID { get; set; }

        [Required]
        public int ContaBancariaID { get; set; }

        public string Titulo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public DateTime DataNotificacao { get; set; }
        public int DiaNotificacao { get; set; }
        public int MesNotificacao { get; set; }
        public int AnoNotificacao { get; set; }

        [ForeignKey("ContaBancariaID")]
        [ValidateNever]
        public ContaBancaria? ContaBancaria { get; set; }
        public bool Lida { get; set; } = false;
    }
}
