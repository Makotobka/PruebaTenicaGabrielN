using System.Collections.Concurrent;
using PruebaTecnicaGabriel.Contracts;
using PruebaTecnicaGabriel.Models;

namespace PruebaTecnicaGabriel.Services
{
    public sealed class ContenedorPagos
    {
        private readonly ConcurrentDictionary<string, Cls_Pago> _payments = new(StringComparer.OrdinalIgnoreCase);

        public (Cls_Pago pago, bool existente) GetOrCreate( CreacionSolicitudPago request, string Nodo)
        {
            var nuevoPago = new Cls_Pago
            {
                TransaccionId = request.TransaccionId.Trim(),
                Valor = request.Valor,
                Moneda = request.Moneda.Trim(),
                Estado = Enum_EstadoPago.Pendiente,
                NodoPropietario = Nodo
            };

            var pagoActual = _payments.GetOrAdd(nuevoPago.TransaccionId,nuevoPago);

            var existente = ReferenceEquals(pagoActual, nuevoPago);

            return (pagoActual, existente);
        }

        public bool TryGet(string transactionId, out Cls_Pago? payment)
        {
            return _payments.TryGetValue(transactionId, out payment);
        }

        public IReadOnlyCollection<Cls_Pago> GetAll()
        {
            return _payments.Values.ToArray();
        }
    }
}
