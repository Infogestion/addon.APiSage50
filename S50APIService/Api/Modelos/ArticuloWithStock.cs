namespace S50APIService.Api.Modelos
{
    /// <summary>
    /// Un artículo con su stock en un almacén. Al heredar, StockByAlmacen sale en el JSON antes que los campos del
    /// artículo, como en interface.s50c.
    /// </summary>
    public class ArticuloWithStock : Articulo
    {
        public decimal StockByAlmacen { get; set; }
    }

    /// <summary>Un artículo con su stock en un almacén y el código de ese almacén, que sale el primero en el JSON.</summary>
    public sealed class ArticuloWithStockAndAlmacen : ArticuloWithStock
    {
        public string Almacen { get; set; }
    }
}
