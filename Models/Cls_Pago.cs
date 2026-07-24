namespace PruebaTecnicaGabriel.Models
{
    public sealed class Cls_Pago : Cls_Auditoria
    {
        public required string TransaccionId { get; init; }
        public decimal Valor { get; init; }
        public required string Moneda { get; init; }
    }
}
