using System;
using System.Collections.Generic;

namespace S50APIService.Api.Modelos
{
    /// <summary>
    /// Cabecera de un documento de mercancías (tabla c_doc del addon GESTIONMERC). Las propiedades se llaman como las
    /// columnas, igual que en interface.s50c: de ahí salen nombres JSON como "guiD_ID" o "alb_numero".
    /// </summary>
    public sealed class CDoc
    {
        public string EJERCICIO { get; set; }
        public string EMPRESA { get; set; }
        public string NUMERO { get; set; }
        public DateTime FECHA { get; set; }
        public string OPERARIO { get; set; }
        public string ALMACEN { get; set; }
        public string GUID_ID { get; set; }
        public DateTime CREATED { get; set; }
        public DateTime MODIFIED { get; set; }
        public string alb_numero { get; set; }
        public string COMMENTS { get; set; }
        public bool transferido { get; set; }
        public string proveedor { get; set; }
        public string STATE { get; set; }
    }

    /// <summary>Línea de un documento de mercancías (tabla d_doc del addon GESTIONMERC).</summary>
    public class DDoc
    {
        public string EJERCICIO { get; set; }
        public string EMPRESA { get; set; }
        public string NUMERO { get; set; }
        public int LINEA { get; set; }
        public string ARTICULO { get; set; }
        public decimal REC { get; set; }
        public decimal PTE { get; set; }
        public string GUID_ID { get; set; }
        public DateTime CREATED { get; set; }
        public DateTime MODIFIED { get; set; }
    }

    public sealed class DDocMercanciaExt : DDoc
    {
        public DDocMercanciaExt(DDoc linea)
        {
            EJERCICIO = linea.EJERCICIO;
            EMPRESA = linea.EMPRESA;
            NUMERO = linea.NUMERO;
            LINEA = linea.LINEA;
            ARTICULO = linea.ARTICULO;
            REC = linea.REC;
            PTE = linea.PTE;
            GUID_ID = linea.GUID_ID;
            CREATED = linea.CREATED;
            MODIFIED = linea.MODIFIED;
        }

        public string ArticuloNombre { get; set; }
        public string ArticuloCodigoBarras { get; set; }
        public string ArticuloProveedor { get; set; }
        public string ArticuloUnidades { get; set; }
    }

    public sealed class DocMercancia
    {
        public CDoc CDocMercancia { get; set; }
        public string Proveedor { get; set; }
        public List<DDocMercanciaExt> Details { get; set; }
    }

    /// <summary>Las unidades por almacén de un artículo en un documento (tablas alma_solicitud y alma_envio): un texto JSON.</summary>
    public sealed class AlmaUnidades
    {
        public string Articulo { get; set; }
        public string Unidades { get; set; }
    }

    /// <summary>Lo que responden las escrituras de interface.s50c, siempre con 200: si se ha guardado y un mensaje.</summary>
    public sealed class ResultadoEscritura
    {
        public const string Actualizado = "Actualizado con éxito";
        public const string ErrorAlGuardar = "An error occurred while saving the entity changes. See the inner exception for details.";

        public bool Result { get; }
        public string Message { get; }

        private ResultadoEscritura(bool result, string message)
        {
            Result = result;
            Message = message;
        }

        public static ResultadoEscritura Guardado(string mensaje = Actualizado) => new ResultadoEscritura(true, mensaje);
        public static ResultadoEscritura Error(string mensaje) => new ResultadoEscritura(false, mensaje);
    }

    /// <summary>La clave de una fila de alma_envio, tal como está guardada (con sus espacios).</summary>
    public sealed class AlmaEnvio
    {
        public string Ejercicio { get; set; }
        public string Empresa { get; set; }
        public string Numero { get; set; }
        public string Articulo { get; set; }
    }

    /// <summary>Lo que se necesita de un albarán de compra (c_albcom) para saber su proveedor.</summary>
    public sealed class AlbaranCompra
    {
        public string Empresa { get; set; }
        public string Proveedor { get; set; }
        public string Numero { get; set; }
    }
}
