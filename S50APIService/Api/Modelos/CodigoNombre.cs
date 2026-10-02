namespace S50APIService.Api.Modelos
{
    /// <summary>Solo el código y el nombre de un maestro (artículos, almacenes...), para buscar nombres sin leer la fila entera.</summary>
    public sealed class CodigoNombre
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
    }
}
