namespace S50APIService.Api.Modelos
{
    public sealed class ExtAlbven
    {
        public string Ejercicio { get; set; }
        public string Empresa { get; set; }
        public string Numero { get; set; }
        public string Letra { get; set; }
        public bool Finalizado { get; set; }
        public bool Enviarapp { get; set; }
        public bool Preparando { get; set; }
    }

    public sealed class AlbVenComplete
    {
        public CAlbven cabeceraVen { get; set; }
        public ExtAlbven cabeceraVenExt { get; set; }
    }
}
