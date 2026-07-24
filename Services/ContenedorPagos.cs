using System.Collections.Concurrent;
using PruebaTecnicaGabriel.Contracts;
using PruebaTecnicaGabriel.Models;

namespace PruebaTecnicaGabriel.Services
{
    public sealed class ContenedorPagos
    {
        private readonly ConcurrentDictionary<string, Cls_Pago> _payments = new(StringComparer.OrdinalIgnoreCase);
        private const int MaximoIntentos = 2;
        public (Cls_Pago pago, bool creado) GetOrCreate(
            CreacionSolicitudPago request, 
            string Nodo
        )
        {
            var nuevoPago = new Cls_Pago
            {
                TransaccionId = request.TransaccionId.Trim(),
                Valor = request.Valor,
                Moneda = request.Moneda.Trim(),
                Estado = Enum_EstadoPago.Pendiente,
                NodoPropietario = Nodo,
                Version = 1
            };

            var pagoActual = _payments.GetOrAdd(nuevoPago.TransaccionId, nuevoPago);

            var creado = ReferenceEquals(pagoActual, nuevoPago);

            return (pagoActual, creado);
        }

        public bool TryGet(string transactionId, out Cls_Pago? payment)
        {
            return _payments.TryGetValue(transactionId, out payment);
        }

        public IReadOnlyCollection<Cls_Pago> GetAll()
        {
            return _payments.Values.ToArray();
        }

        #region Metodos de control de nodos
        public Cls_Pago AplicarReplica(ReplicaPago replica)
        {
            return _payments.AddOrUpdate(
                replica.TransaccionId,
                _ => replica.PagoBase(),
                (_, pagoActual) =>
                {
                    lock (pagoActual)
                    {
                        // Ignora réplicas antiguas o repetidas.
                        if (replica.Version <= pagoActual.Version)
                            return pagoActual;

                        pagoActual.Estado = replica.Estado;
                        pagoActual.NodoPropietario =
                            replica.NodoPropietario;

                        pagoActual.FechaInicio =
                            replica.FechaInicio;

                        pagoActual.FechaFallo =
                            replica.FechaFallo;

                        pagoActual.FechaCompletado =
                            replica.FechaCompletado;

                        pagoActual.NumeroIntento =
                            replica.NumeroIntento;

                        pagoActual.Version =
                            replica.Version;

                        pagoActual.UltimoError =
                            replica.UltimoError;

                        return pagoActual;
                    }
                });
        }

        public bool TryTomarParaProcesar(
            string transaccionId,
            string nodoId,
            out Cls_Pago? pago)
        {
            pago = null;

            if (!_payments.TryGetValue(
                    transaccionId,
                    out var pagoActual))
            {
                return false;
            }

            lock (pagoActual)
            {
                if (pagoActual.Estado != Enum_EstadoPago.Pendiente)
                {
                    return false;
                }

                if (!string.Equals(
                        pagoActual.NodoPropietario,
                        nodoId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                pagoActual.Estado = Enum_EstadoPago.Procesando;
                pagoActual.NumeroIntento++;
                pagoActual.FechaInicio = DateTime.UtcNow;
                pagoActual.FechaFallo = null;
                pagoActual.UltimoError = null;
                pagoActual.Version++;

                pago = pagoActual;
                return true;
            }
        }

        public bool TryPrepararReintento(
            string transaccionId,
            string nuevoNodo,
            out Cls_Pago? pago)
        {
            pago = null;

            if (!_payments.TryGetValue(
                    transaccionId,
                    out var pagoActual))
            {
                return false;
            }

            lock (pagoActual)
            {
                if (pagoActual.Estado == Enum_EstadoPago.Completo)
                {
                    return false;
                }

                if (pagoActual.Estado != Enum_EstadoPago.Error)
                {
                    return false;
                }

                if (pagoActual.NumeroIntento >= MaximoIntentos)
                {
                    return false;
                }

                pagoActual.Estado = Enum_EstadoPago.Pendiente;
                pagoActual.NodoPropietario = nuevoNodo;
                pagoActual.FechaInicio = null;
                pagoActual.Version++;

                pago = pagoActual;
                return true;
            }
        }

        public bool MarcarCompleto(
            string transaccionId,
            string nodoId,
            out Cls_Pago? pago)
        {
            pago = null;

            if (!_payments.TryGetValue(
                    transaccionId,
                    out var pagoActual))
            {
                return false;
            }

            lock (pagoActual)
            {
                if (pagoActual.Estado != Enum_EstadoPago.Procesando ||
                    !string.Equals(
                        pagoActual.NodoPropietario,
                        nodoId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                pagoActual.Estado = Enum_EstadoPago.Completo;
                pagoActual.FechaCompletado = DateTime.UtcNow;
                pagoActual.Version++;

                pago = pagoActual;
                return true;
            }
        }

        public bool MarcarError(
            string transaccionId,
            string nodoId,
            string mensaje,
            out Cls_Pago? pago)
        {
            pago = null;

            if (!_payments.TryGetValue(
                    transaccionId,
                    out var pagoActual))
            {
                return false;
            }

            lock (pagoActual)
            {
                if (pagoActual.Estado != Enum_EstadoPago.Procesando ||
                    !string.Equals(
                        pagoActual.NodoPropietario,
                        nodoId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                pagoActual.Estado = Enum_EstadoPago.Error;
                pagoActual.FechaFallo = DateTime.UtcNow;
                pagoActual.UltimoError = mensaje;
                pagoActual.Version++;

                pago = pagoActual;
                return true;
            }
        }
        #endregion
    }
}
