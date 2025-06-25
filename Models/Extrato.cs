using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Banksim_Web.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Banksim_Web.Models
{
    public class Extrato
    {
        public int ID { get; set; }

        [Required]
        public int ContaBancariaID { get; set; }

        [ForeignKey("ContaBancariaID")]
        [ValidateNever]
        public ContaBancaria? ContaBancaria { get; set; }

        [Required]
        public string? TipoTransacao { get; set; }
        public decimal Valor { get; set; }
        public string? Descricao { get; set; }
        public int DiaTransacao { get; set; }
        public int MesTransacao { get; set; }
        public int AnoTransacao { get; set; }
        public DateTime Timestamp { get; set; }

    }
}