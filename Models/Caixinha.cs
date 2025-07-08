using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Banksim_Web.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Banksim_Web.Models
{
    public class Caixinha
    {
        [Required]
        public int ID { get; set; }

        public int ContaBancariaID { get; set; }

        [ForeignKey("ContaBancariaID")]
        [ValidateNever]
        public ContaBancaria ContaBancaria { get; set; }
        public string NomeCaixinha { get; set; } = string.Empty;
        public string? FotoCaixinha { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorCaixinhaAtual { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorCaixinhaInicial { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorRendido { get; set; }
        public decimal PorcentagemRendimento { get; set; }
        public int DiasCorridos { get; set; }
        public int DiaCriacao { get; set; }
        public int MesCriacao { get; set; }
        public int AnoCriacao { get; set; }

    }
}