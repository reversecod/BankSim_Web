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

        [ForeignKey("ContaBancariaID")]
        [ValidateNever]
        public ContaBancaria ContaBancaria { get; set; }

        [Required]
        public decimal ValorEmprestimo { get; set; }
        [Required]
        public int QntdParcela { get; set; } = 1;

    }
}