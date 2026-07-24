using PruebaTecnicaGabriel.Models;

namespace PruebaTecnicaGabriel.Contracts
{
    public class ReplicaPago
    {
        public string TransaccionId { get; init; } = string.Empty;

        public decimal Valor { get; init; }

        public string Moneda { get; init; } = string.Empty;

        public Enum_EstadoPago Estado { get; init; }

        public string? NodoPropietario { get; init; }

        public DateTime FechaCreacion { get; init; }

        public DateTime? FechaInicio { get; init; }

        public DateTime? FechaFallo { get; init; }

        public DateTime? FechaCompletado { get; init; }

        public int NumeroIntento { get; init; }

        public long Version { get; init; }

        public string? UltimoError { get; init; }

        public static ReplicaPago ReplicarDesde(Cls_Pago pago)
        {
            return new ReplicaPago
            {
                TransaccionId = pago.TransaccionId,
                Valor = pago.Valor,
                Moneda = pago.Moneda,
                Estado = pago.Estado,
                NodoPropietario = pago.NodoPropietario,
                FechaCreacion = pago.FechaCreacion,
                FechaInicio = pago.FechaInicio,
                FechaFallo = pago.FechaFallo,
                FechaCompletado = pago.FechaCompletado,
                NumeroIntento = pago.NumeroIntento,
                Version = pago.Version,
                UltimoError = pago.UltimoError
            };
        }

        public Cls_Pago PagoBase()
        {
            return new Cls_Pago
            {
                TransaccionId = TransaccionId,
                Valor = Valor,
                Moneda = Moneda,
                Estado = Estado,
                NodoPropietario = NodoPropietario,
                FechaCreacion = FechaCreacion,
                FechaInicio = FechaInicio,
                FechaFallo = FechaFallo,
                FechaCompletado = FechaCompletado,
                NumeroIntento = NumeroIntento,
                Version = Version,
                UltimoError = UltimoError
            };
        }
    }
}
