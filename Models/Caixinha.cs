using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Banksim_Web.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Banksim_Web.Models
{
    public class Caixinha
    {
        public int ID { get; set; }

        [Required]
        public int ContaBancariaID { get; set; }

        [ForeignKey("ContaBancariaID")]
        [ValidateNever]
        public ContaBancaria ContaBancaria { get; set; }
        public string NomeCaixinha { get; set; } = string.Empty;
        public decimal ValorCaixinha { get; set; }
        public double PorcentagemRendimento { get; set; }
        public int DiaCriacao { get; set; }
        public int MesCriacao { get; set; }
        public int AnoCriacao { get; set; }

    }
}