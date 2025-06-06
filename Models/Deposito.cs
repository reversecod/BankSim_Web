using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Banksim_Web.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Banksim_Web.Models
{
    public class Deposito
    {
        public int ID { get; set; }

        [Required]
        public int ContaBancariaID { get; set; }

        [ForeignKey("ContaBancariaID")]
        [ValidateNever]
        public ContaBancaria? ContaBancaria { get; set; }

        [Required]
        public decimal Valor { get; set; }

        public string? Descricao { get; set; }

        [Required]
        public int DiaDeposito { get; set; }

        [Required]
        public int MesDeposito { get; set; }

        [Required]
        public int AnoDeposito { get; set; }

    }
}