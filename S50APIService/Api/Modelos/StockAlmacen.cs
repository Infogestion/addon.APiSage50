namespace S50APIService.Api.Modelos
{
    /// <summary>El stock de un artículo en un almacén (Almacen y StockByAlmacen de ArticuloWithStockAndAlmacen en interface.s50c).</summary>
    public sealed class StockAlmacen
    {
        public string Almacen { get; set; }
        public decimal Stock { get; set; }
    }
}
