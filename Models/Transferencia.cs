using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Banksim_Web.Models
{
    public class Transferencia
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int ContaBancariaID { get; set; } // Conta de origem

        [ForeignKey("ContaBancariaID")]
        [ValidateNever]
        public ContaBancaria ContaBancaria { get; set; }

        [Required]
        public decimal Valor { get; set; }

        [Required]
        public string TipoTransferencia { get; set; } = "D"; // 'C' para Crédito, 'D' para Débito
        public string? Descricao { get; set; }

        [Required]
        public int DiaTransferencia { get; set; }

        [Required]
        public int MesTransferencia { get; set; }

        [Required]
        public int AnoTransferencia { get; set; }

        public bool Efetivado { get; set; } = false;
    }
}