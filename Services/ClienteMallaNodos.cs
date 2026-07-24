using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PruebaTecnicaGabriel.Configutarion;
using PruebaTecnicaGabriel.Contracts;
using PruebaTecnicaGabriel.Models;

namespace PruebaTecnicaGabriel.Services
{
    public class ClienteMallaNodos
    {
        private readonly HttpClient _httpClient;
        private readonly ConfiguracionNodo _configuracion;
        private readonly ILogger<ClienteMallaNodos> _log;
        private readonly string ApiConsulta = "interno/pagos/replicar";

        public ClienteMallaNodos(
            HttpClient httpClient,
            IOptions<ConfiguracionNodo> options,
            ILogger<ClienteMallaNodos> log)
        {
            _httpClient = httpClient;
            _configuracion = options.Value;
            _log = log;
        }

        public async Task ReplicarAsync(
            Cls_Pago pago,
            CancellationToken cancellationToken)
        {
            ReplicaPago replica;

            lock (pago)
            {
                replica = ReplicaPago.ReplicarDesde(pago);
            }

            var tareas = _configuracion.Pares
                .Where(peer => !string.Equals(
                    peer.Id,
                    _configuracion.Id,
                    StringComparison.OrdinalIgnoreCase))
                .Select(peer => EnviarReplicaAsync(
                    peer,
                    replica,
                    cancellationToken));

            await Task.WhenAll(tareas);
        }

        private async Task EnviarReplicaAsync(
            NodoPar peer,
            ReplicaPago replica,
            CancellationToken cancellationToken)
        {
            try
            {
                var url =
                    $"{peer.Url.TrimEnd('/')}/{ApiConsulta}";

                var response = await _httpClient.PostAsJsonAsync(
                    url,
                    replica,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _log.LogWarning(
                        "[{NodeId}] El nodo {PeerId} rechazó la réplica de {TransactionId}. HTTP {StatusCode}",
                        _configuracion.Id,
                        peer.Id,
                        replica.TransaccionId,
                        response.StatusCode);
                }
            }
            catch (Exception exception)
            {
                _log.LogWarning(
                    exception,
                    "[{NodeId}] No fue posible replicar {TransactionId} en {PeerId}",
                    _configuracion.Id,
                    replica.TransaccionId,
                    peer.Id);
            }
        }

        public async Task SolicitarReprocesoAsync(
            string transaccionId,
            string nodoAnterior,
            CancellationToken cancellationToken)
        {
            var siguienteNodo =
                ObtenerSiguienteNodo(nodoAnterior);

            if (siguienteNodo is null)
            {
                _log.LogError(
                    "[{NodeId}] No existe otro nodo para asumir {TransactionId}",
                    _configuracion.Id,
                    transaccionId);

                return;
            }

            try
            {
                var url =
                    $"{siguienteNodo.Url.TrimEnd('/')}" +
                    $"/interno/pagos/{transaccionId}/reintentar";

                var response = await _httpClient.PostAsync(
                    url,
                    content: null,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _log.LogWarning(
                        "[{NodeId}] {PeerId} no pudo asumir {TransactionId}. HTTP {StatusCode}",
                        _configuracion.Id,
                        siguienteNodo.Id,
                        transaccionId,
                        response.StatusCode);

                    return;
                }

                _log.LogInformation(
                    "[{NodeId}] El nodo {PeerId} asumirá la transacción {TransactionId}",
                    _configuracion.Id,
                    siguienteNodo.Id,
                    transaccionId);
            }
            catch (Exception exception)
            {
                _log.LogError(
                    exception,
                    "[{NodeId}] Error solicitando el relevo de {TransactionId}",
                    _configuracion.Id,
                    transaccionId);
            }
        }

        private NodoPar? ObtenerSiguienteNodo(
            string nodoActual)
        {
            var nodos = _configuracion.Pares
                .Append(new NodoPar
                {
                    Id = _configuracion.Id,
                    Url = _configuracion.Url
                })
                .GroupBy(
                    nodo => nodo.Id,
                    StringComparer.OrdinalIgnoreCase)
                .Select(grupo => grupo.First())
                .OrderBy(
                    nodo => nodo.Id,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            var posicionActual = nodos.FindIndex(
                nodo => string.Equals(
                    nodo.Id,
                    nodoActual,
                    StringComparison.OrdinalIgnoreCase));

            if (posicionActual < 0 || nodos.Count < 2)
            {
                return null;
            }

            return nodos[
                (posicionActual + 1) % nodos.Count];
        }
    }
}
