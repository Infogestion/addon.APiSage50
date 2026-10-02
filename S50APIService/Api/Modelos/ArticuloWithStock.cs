namespace S50APIService.Api.Modelos
{
    /// <summary>
    /// Un artículo con su stock en un almacén. Al heredar, StockByAlmacen sale en el JSON antes que los campos del
    /// artículo, como en interface.s50c.
    /// </summary>
    public sealed class ArticuloWithStock : Articulo
    {
        public decimal StockByAlmacen { get; set; }
    }
}
