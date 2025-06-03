using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Banksim_Web.Models
{
    public class Anotacao
    {
        public int ID { get; set; }

        [Required]
        public int ContaBancariaID { get; set; }

        [ForeignKey("ContaBancariaID")]
        [ValidateNever]
        public ContaBancaria ContaBancaria { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string TextoAnotacao { get; set; } = string.Empty;

    }
}
