using Microsoft.AspNetCore.Mvc;
using PruebaTecnicaGabriel.Contracts;
using PruebaTecnicaGabriel.Services;

namespace PruebaTecnicaGabriel.Controllers
{
    [ApiController]
    [Route("interno/pagos")]
    public class ControlPagosInternosController : ControllerBase
    {
        private readonly ContenedorPagos _contenedor;
        private readonly EncolamientoPagosPendiente _cola;
        private readonly ClienteMallaNodos _malla;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ControlPagosInternosController> _log;

        public ControlPagosInternosController(
            ContenedorPagos contenedor,
            EncolamientoPagosPendiente cola,
            ClienteMallaNodos malla,
            IConfiguration configuration,
            ILogger<ControlPagosInternosController> log)
        {
            _contenedor = contenedor;
            _cola = cola;
            _malla = malla;
            _configuration = configuration;
            _log = log;
        }

        [HttpPost("replicar")]
        public IActionResult ReplicarPago(
            [FromBody] ReplicaPago request)
        {
            _contenedor.AplicarReplica(request);

            return Ok(new
            {
                request.TransaccionId,
                request.Version
            });
        }

        [HttpPost("{transaccionId}/reintentar")]
        public async Task<IActionResult> ReintentarPago(
            string transaccionId,
            CancellationToken cancellationToken)
        {
            var nodoId = _configuration["Node:Id"]
                ?? Environment.MachineName;

            if (!_contenedor.TryPrepararReintento(
                    transaccionId,
                    nodoId,
                    out var pago) ||
                pago is null)
            {
                return Conflict(new
                {
                    error =
                        "La transacción no puede ser reintentada."
                });
            }

            _log.LogInformation(
                "[{NodeId}] Asumiendo transacción {TransactionId}",
                nodoId,
                transaccionId);

            // Primero informa a los demás quién es el nuevo dueño.
            await _malla.ReplicarAsync(
                pago,
                cancellationToken);

            await _cola.ColaAsync(
                transaccionId,
                cancellationToken);

            return Accepted(new
            {
                transaccionId,
                nodoPropietario = nodoId,
                pago.NumeroIntento
            });
        }
    }
}
