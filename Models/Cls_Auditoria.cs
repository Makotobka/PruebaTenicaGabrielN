namespace PruebaTecnicaGabriel.Models
{
    public class Cls_Auditoria
    {
        /// <summary>
        /// Estado de la proceso
        /// </summary>
        public Enum_EstadoPago Estado { get; set; } = Enum_EstadoPago.Pendiente;
        /// <summary>
        /// Nodo que ha tomado el proceso en curso
        /// </summary>
        public string? NodoPropietario { get; set; }
        /// <summary>
        /// Fecha de creacion del proceso
        /// </summary>
        public DateTime FechaCreacion { get; init; } = DateTime.Now;
        /// <summary>
        /// Fecha de toma de proceso
        /// </summary>
        public DateTime? FechaInicio { get; set; }
        /// <summary>
        /// Fecha del fallo del proceso
        /// </summary>
        public DateTime? FechaFallo { get; set; }
        /// <summary>
        /// Fecha de finalizacion
        /// </summary>
        public DateTime? FechaCompletado { get; set; }
        /// <summary>
        /// Intentos de control
        /// </summary>
        public int NumeroIntento { get; set; }
        /// <summary>
        /// Version ejecutada
        /// </summary>
        public long Version { get; set; }
        /// <summary>
        /// Ultimo error registrado
        /// </summary>
        public string? UltimoError { get; set; }


    }
}
