namespace S50APIService.Api.Modelos
{
    public sealed class ExtAlbcom
    {
        public string Empresa { get; set; }
        public string Numero { get; set; }
        public string Ejercicio { get; set; }
        public bool Finalizado { get; set; }
        public bool Enviarapp { get; set; }
        public bool Preparando { get; set; }
        public string Proveed { get; set; }
    }

    public sealed class AlbComComplete
    {
        public CAlbcom cabeceraCom { get; set; }
        public ExtAlbcom cabeceraComExt { get; set; }
    }
}
