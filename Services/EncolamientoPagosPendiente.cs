using System.Threading.Channels;

namespace PruebaTecnicaGabriel.Services
{
    public class EncolamientoPagosPendiente
    {
        private readonly Channel<string> _cola = Channel.CreateUnbounded<string>();

        public ValueTask ColaAsync(
            string transaccionId,
            CancellationToken cancellationToken
        )
        {
            return _cola.Writer.WriteAsync(
                transaccionId,
                cancellationToken
            );
        }

        public IAsyncEnumerable<string> ReadAllAsync(
            CancellationToken cancellationToken)
        {
            return _cola.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
