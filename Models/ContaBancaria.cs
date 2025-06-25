using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Banksim_Web.Models
{
    public class ContaBancaria
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [ForeignKey("Usuario")]
        public int UsuarioID { get; set; }

        public string? FotoPerfilUsuario { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Saldo { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LimiteCreditoInicial { get; set; } = 0;
        public decimal LimiteCreditoDisponivel { get; set; } = 0;

        public int DiaFatura { get; set; } = 25;

        public int DiaPagamentoFatura { get; set; } = 1;

        [Required]
        public double PorcentagemEmprestimo { get; set; } = 4.25;

        public DateTime DataCriacao { get; set; } = DateTime.Now;

        public Usuario? Usuario { get; set; }

        [ValidateNever]
        public DataSimulada? dataSimulada { get; set; }
    }
}