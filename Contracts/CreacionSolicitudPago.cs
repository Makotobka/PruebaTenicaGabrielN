using System.ComponentModel.DataAnnotations;

namespace PruebaTecnicaGabriel.Contracts
{
    public sealed class CreacionSolicitudPago
    {
        [Required]
        public string TransaccionId { get; init; } = string.Empty;

        public decimal Valor { get; init; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Moneda { get; init; } = string.Empty;
    }
}
