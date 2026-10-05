using System;
using System.Collections.Generic;

namespace S50APIService.Api.Modelos
{
    /// <summary>
    /// Cabecera de una recepción de mercancías con envíos (tabla c_repmerc del addon FERRETERIATIA). Las propiedades se
    /// llaman como las columnas, igual que en interface.s50c.
    /// </summary>
    public sealed class CRepMerc
    {
        public string EJERCICIO { get; set; }
        public string EMPRESA { get; set; }
        public string NUMERO { get; set; }
        public DateTime FECHA { get; set; }
        public string OPERARIO { get; set; }
        public string ALMAORIGEN { get; set; }
        public string ALMADESTINO { get; set; }
        public string FAMILIA { get; set; }
        public string SUBFAMILIA { get; set; }
        public string MARCA { get; set; }
        public string PROVEEDOR { get; set; }
        public string UBIORIGEN { get; set; }
        public string UBIDESTINO { get; set; }
        public string GUID_ID { get; set; }
        public DateTime CREATED { get; set; }
        public DateTime MODIFIED { get; set; }
        public string ESTADOO { get; set; }
    }

    /// <summary>Línea de una recepción de mercancías con envíos (tabla d_repmerc del addon FERRETERIATIA).</summary>
    public sealed class DRepMerc
    {
        public string EJERCICIO { get; set; }
        public string EMPRESA { get; set; }
        public string NUMERO { get; set; }
        public int LINEA { get; set; }
        public string CODIGO { get; set; }
        public string DESCRIPCION { get; set; }
        public decimal CANTIDAD { get; set; }
        public string ALMAORIGEN { get; set; }
        public string UBIORIGEN { get; set; }
        public decimal STOCKORIGEN { get; set; }
        public string ALMADESTINO { get; set; }
        public decimal STOCKDESTINO { get; set; }
        public decimal STOCKMAX { get; set; }
        public decimal STOCKMIN { get; set; }
        public string UBIDESTINO { get; set; }
        public decimal TRASPASADO { get; set; }
        public string GUID_ID { get; set; }
        public DateTime CREATED { get; set; }
        public DateTime MODIFIED { get; set; }
    }

    /// <summary>Cabecera de una recepción de mercancías sin envíos (tabla c_repart del addon FERRETERIATIA).</summary>
    public sealed class CRepArt
    {
        public string EJERCICIO { get; set; }
        public string EMPRESA { get; set; }
        public string NUMERO { get; set; }
        public DateTime FECHA { get; set; }
        public DateTime INIPERIODO { get; set; }
        public DateTime FINPERIODO { get; set; }
        public string ALMAORIGEN { get; set; }
        public string ALMADESTINO { get; set; }
        public string FAMILIA { get; set; }
        public string SUBFAMILIA { get; set; }
        public string MARCA { get; set; }
        public string UBIORIGEN { get; set; }
        public string UBIDESTINO { get; set; }
        public string GUID_ID { get; set; }
        public DateTime CREATED { get; set; }
        public DateTime MODIFIED { get; set; }
        public string proveed { get; set; }
        public string ARTD { get; set; }
        public string ARTH { get; set; }
        public string OPERADOR { get; set; }
        public string ESTADOO { get; set; }
    }

    /// <summary>Línea de una recepción de mercancías sin envíos (tabla d_repart del addon FERRETERIATIA).</summary>
    public sealed class DRepArt
    {
        public string EJERCICIO { get; set; }
        public string EMPRESA { get; set; }
        public string NUMERO { get; set; }
        public int LINEA { get; set; }
        public string CODIGO { get; set; }
        public string DESCRIPCION { get; set; }
        public decimal CANTIDAD { get; set; }
        public string ALMAORIGEN { get; set; }
        public string UBIORIGEN { get; set; }
        public decimal STOCKORIGEN { get; set; }
        public string ALMADESTINO { get; set; }
        public decimal STOCKDESTINO { get; set; }
        public decimal STOCKMAX { get; set; }
        public decimal STOCKMIN { get; set; }
        public string UBIDESTINO { get; set; }
        public string FAMILIA { get; set; }
        public string SUBFAMILIA { get; set; }
        public string MARCA { get; set; }
        public string GUID_ID { get; set; }
        public DateTime CREATED { get; set; }
        public DateTime MODIFIED { get; set; }
        public decimal TRASPASADO { get; set; }
    }

    /// <summary>Lo que se necesita de la cabecera de una recepción, de cualquiera de las dos clases, para guardarla.</summary>
    public sealed class CabeceraRecepcion
    {
        public string Ejercicio { get; set; }
        public string Empresa { get; set; }
        public string Numero { get; set; }
        public string Estadoo { get; set; }
    }

    /// <summary>Lo que se necesita de una línea de una recepción, de cualquiera de las dos clases, para traspasar unidades.</summary>
    public sealed class LineaRecepcion
    {
        public int Linea { get; set; }
        public string Codigo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Stockorigen { get; set; }
        public decimal Stockdestino { get; set; }
        public decimal Traspasado { get; set; }
    }

    /// <summary>El artículo de un código de barras (barras) o de una referencia de proveedor (referpro).</summary>
    public sealed class ArticuloRelacionado
    {
        public string Articulo { get; set; }
    }

    public sealed class RecepcionCompletaDto
    {
        public CRepMerc Cabecera { get; set; }
        public List<DRepMerc> Detalles { get; set; }
    }

    public sealed class RecepcionSinEnviosCompletaDto
    {
        public CRepArt Cabecera { get; set; }
        public List<DRepArt> Detalles { get; set; }
    }
}
