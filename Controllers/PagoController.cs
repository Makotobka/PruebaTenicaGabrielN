using Microsoft.AspNetCore.Mvc;
using PruebaTecnicaGabriel.Contracts;
using PruebaTecnicaGabriel.Services;

namespace PruebaTecnicaGabriel.Controllers
{
    [ApiController]
    [Route("pago")]
    public class PagoController : ControllerBase
    {
        private readonly ContenedorPagos _contenedor;
        private readonly EncolamientoPagosPendiente _cola;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PagoController> _log;

        public PagoController(
        ContenedorPagos contenedor,
        EncolamientoPagosPendiente cola,
        IConfiguration configuration,
        ILogger<PagoController> logger)
        {
            _contenedor = contenedor;
            _cola = cola;
            _configuration = configuration;
            _log = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment(
            [FromBody] CreacionSolicitudPago request,
            CancellationToken cancellationToken)
        {
            if (request.Valor <= 0)
            {
                return BadRequest(new
                {
                    error = "El monto debe ser mayor que cero."
                });
            }

            var nodeId = _configuration["Node:Id"]
                ?? Environment.MachineName;

            var result = _contenedor.GetOrCreate(request, nodeId);

            if (!result.existente)
            {
                _log.LogInformation(
                    "[{NodeId}] Solicitud repetida para {TransactionId}. Estado actual: {Status}",
                    nodeId,
                    result.pago.TransaccionId,
                    result.pago.Estado);

                return Ok(result.pago);
            }

            _log.LogInformation(
                "[{NodeId}] Transacción {TransactionId} recibida",
                nodeId,
                result.pago.TransaccionId);

            await _cola.ColaAsync(
                result.pago.TransaccionId,
                cancellationToken);

            return AcceptedAtAction(
                nameof(GetPayment),
                new
                {
                    transactionId = result.pago.TransaccionId
                },
                result.pago);
        }

        [HttpGet("{transactionId}")]
        public IActionResult GetPayment(string transactionId)
        {
            if (!_contenedor.TryGet(transactionId, out var payment)
                || payment is null)
            {
                return NotFound(new
                {
                    error = $"No existe la transacción {transactionId}."
                });
            }

            return Ok(payment);
        }

        [HttpGet]
        public IActionResult GetPayments()
        {
            return Ok(_contenedor.GetAll());
        }
    }
}
