using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Banksim_Web.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Banksim_Web.Models
{
    public class Emprestimo
    {
        public int ID { get; set; }

        [Required]
        public int ContaBancariaID { get; set; }

        [Required]
        public string? NomeEmprestimo { get; set; }

        [ForeignKey("ContaBancariaID")]
        [ValidateNever]
        public ContaBancaria? ContaBancaria { get; set; }

        [Required]
        public decimal ValorEmprestimo { get; set; }

        [Required]
        public decimal ValorPago { get; set; } = 0;

        [Required]
        public int QntdParcela { get; set; } = 1;

        [Required]
        public int diaPagamento { get; set; } = 28;

        [Required]
        public int MesInicioPagamento { get; set; }
        [Required]

        public int MesProxPagamento { get; set; }

        [Required]
        public bool Pago { get; set; } = false;

    }
}