using PruebaTecnicaGabriel.Models;

namespace PruebaTecnicaGabriel.Services
{
    public class ProcesadorPagos : BackgroundService
    {
        private readonly EncolamientoPagosPendiente _cola;
        private readonly ContenedorPagos _contendor;
        private readonly ILogger<ProcesadorPagos> _log;
        private readonly string _nodeId;

        public ProcesadorPagos(
            EncolamientoPagosPendiente cola,
            ContenedorPagos contenedor,
            IConfiguration configuration,
            ILogger<ProcesadorPagos> log)
        {
            _cola = cola;
            _contendor = contenedor;
            _log = log;

            _nodeId = configuration["Node:Id"]
                ?? Environment.MachineName;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken
        )
        {
            await foreach (
                var transactionId in _cola.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcesoPagoAsync(
                        transactionId,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _log.LogError(
                        exception,
                        "[{NodeId}] Error procesando {TransactionId}",
                        _nodeId,
                        transactionId);
                }
            }
        }

        private async Task ProcesoPagoAsync(
            string transactionId,
            CancellationToken cancellationToken
        )
        {


            if (!_contendor.TryGet(transactionId, out var pago) || pago is null)
            {
                _log.LogWarning(
                    "[{NodeId}] No se encontró la transacción {TransactionId}",
                    _nodeId,
                    transactionId);

                return;
            }

            lock (pago)
            {
                if (pago.Estado != Enum_EstadoPago.Pendiente)
                {
                    _log.LogInformation(
                        "[{NodeId}] La transacción {TransactionId} ya está en estado {Status}",
                        _nodeId,
                        transactionId,
                        pago.Estado);

                    return;
                }

                pago.Estado = Enum_EstadoPago.Procesando;
                pago.NodoPropietario = _nodeId;
            }

            await Task.Delay(
                TimeSpan.FromSeconds(
                    Random.Shared.Next(1, 12)
                ), cancellationToken
            );
            pago.FechaInicio = DateTime.Now;




            var tiempoDemoraProceso = Random.Shared.Next(30, 120);

            _log.LogInformation(
                "[{NodeId}] Procesando transacción {TransactionId} durante {Seconds} segundos",
                _nodeId,
                transactionId,
                tiempoDemoraProceso
            );
            await Task.Delay(
                TimeSpan.FromSeconds(tiempoDemoraProceso),
                cancellationToken);

            lock (pago)
            {
                if (pago.Estado == Enum_EstadoPago.Completo)
                    return;
            }

            //Condicion de Error aleatorio
            var auxError = Random.Shared.Next(1, 99);
            if(auxError % 2 == 0)
            {
                pago.Estado = Enum_EstadoPago.Completo;
                pago.FechaCompletado = DateTime.Now;
                _log.LogInformation(
                   "[{NodeId}] Transacción {TransactionId} completada correctamente",
                   _nodeId,
                   transactionId
                );
            }
            else
            {
                pago.Estado = Enum_EstadoPago.Error;
                pago.FechaFallo = DateTime.Now;
                _log.LogInformation(
                   "[{NodeId}] Transacción {TransactionId} ha fallado",
                   _nodeId,
                   transactionId
                );
            }

            

           
        }
    }
}
