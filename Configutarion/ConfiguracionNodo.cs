namespace PruebaTecnicaGabriel.Configutarion
{
    public sealed class ConfiguracionNodo
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public List<NodoPar> Pares { get; set; } = new List<NodoPar>();
    }

    public sealed class NodoPar
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
