using Microsoft.Extensions.Configuration;
using PruebaTecnicaGabriel.Models;

namespace PruebaTecnicaGabriel.Services
{
    public class ProcesadorPagos : BackgroundService
    {
        private readonly EncolamientoPagosPendiente _cola;
        private readonly ContenedorPagos _contendor;
        private readonly ILogger<ProcesadorPagos> _log;
        private readonly string _nodeId;
        private readonly IConfiguration _configuration;
        private readonly ClienteMallaNodos _malla;

        public ProcesadorPagos(
            EncolamientoPagosPendiente cola,
            ContenedorPagos contenedor,
            IConfiguration configuration,
            ClienteMallaNodos malla,
            ILogger<ProcesadorPagos> log
        )
        {
            _cola = cola;
            _contendor = contenedor;
            _log = log;
            _malla = malla;
            _configuration = configuration;
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
            string transaccionId,
            CancellationToken cancellationToken
        )
        {


            if (!_contendor.TryTomarParaProcesar(
                     transaccionId,
                     _nodeId,
                     out var pago) ||
                 pago is null)
            {
                _log.LogInformation(
                    "[{NodeId}] No se pudo tomar {TransactionId}",
                    _nodeId,
                    transaccionId);

                return;
            }

            await _malla.ReplicarAsync(
                pago,
                cancellationToken);

            var segundos = Random.Shared.Next(5, 15);

            _log.LogInformation(
                "[{NodeId}] Procesando {TransactionId}. Intento {Attempt}. Duración: {Seconds} segundos",
                _nodeId,
                transaccionId,
                pago.NumeroIntento,
                segundos);

            await Task.Delay(
                TimeSpan.FromSeconds(segundos),
                cancellationToken);

            //var forzarPrimerFallo =
            //    _configuration.GetValue<bool>(
            //        "Simulacion:ForzarFalloPrimerIntento");

            bool debeFallar;

            lock (pago)
            {
                // Primero intento de cada peticion.
                //if(pago.NumeroIntento == 1)

                // Probabilidad de fallo 50%
                debeFallar = Random.Shared.Next(0, 100) < 50;
            }

            if (!debeFallar)
            {
                if (!_contendor.MarcarCompleto(
                        transaccionId,
                        _nodeId,
                        out pago) ||
                    pago is null)
                {
                    return;
                }

                _log.LogInformation(
                    "[{NodeId}] Transacción {TransactionId} completada. Intento {Attempt}",
                    _nodeId,
                    transaccionId,
                    pago.NumeroIntento);

                await _malla.ReplicarAsync(
                    pago,
                    cancellationToken);

                return;
            }

            if (!_contendor.MarcarError(
                    transaccionId,
                    _nodeId,
                    "Fallo simulado durante el procesamiento.",
                    out pago) ||
                pago is null)
            {
                return;
            }

            _log.LogWarning(
                "[{NodeId}] Transacción {TransactionId} falló. Solicitando relevo",
                _nodeId,
                transaccionId);

            // Primero comunica el estado Error.
            await _malla.ReplicarAsync(
                pago,
                cancellationToken);

            // Luego pide al siguiente nodo que la asuma.
            await _malla.SolicitarReprocesoAsync(
                transaccionId,
                _nodeId,
                cancellationToken);
        }

    }
}
