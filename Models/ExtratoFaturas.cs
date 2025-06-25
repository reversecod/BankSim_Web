using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Banksim_Web.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Banksim_Web.Models
{
    public class ExtratoFaturas
    {
        public int ID { get; set; }

        [Required]
        public int ContaBancariaID { get; set; }

        [ForeignKey("ContaBancariaID")]
        [ValidateNever]
        public ContaBancaria? ContaBancaria { get; set; }
        public decimal ValorTransferencia { get; set; }
        public string? Descricao { get; set; }
        public int MesPagamento { get; set; }
        public int AnoPagamento { get; set; }
        public int QtdParcelas { get; set; }

    }
}