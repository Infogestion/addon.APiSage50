using System;
using System.Collections.Generic;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class CDocEntRep
    {
        public string Ejercicio { get; set; }
        public string Empresa { get; set; }
        public string Numero { get; set; }
        public DateTime Fecha { get; set; }
        public string Camion { get; set; }
        public string Cliente { get; set; }
        public string Albaran { get; set; }
        public bool Entregado { get; set; }
        public string Observaciones { get; set; }
        [Columna("GUID_ID")]
        public string GuidId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string Tipo { get; set; }
        public string Almacen { get; set; }
        public string Codigo { get; set; }
        public string Direccioncliente { get; set; }
        public string Cpcliente { get; set; }
        public string Poblacion { get; set; }
        public string Provincia { get; set; }
        public string Telefono { get; set; }
        public string Tfcontacto { get; set; }
        public string Contacto { get; set; }
        public DateTime Fechaentrega { get; set; }
        public string Conductor { get; set; }
        public DateTime Fechaentregaprev { get; set; }
        public string Firma { get; set; }
        public string FirmaSeller { get; set; }
        public string Letraalb { get; set; }
        public bool Pagado { get; set; }
        public bool CobrarDestino { get; set; }
        public decimal ImporteCobrar { get; set; }
        public string TramoHorario { get; set; }
        public bool CheckHorario { get; set; }
        public string Ruta { get; set; }
        public string Incidencia { get; set; }
        public string Zona { get; set; }
        public string ObsInt { get; set; }
    }

    public class DDocEntRep
    {
        public string Ejercicio { get; set; }
        public string Empresa { get; set; }
        public string Numero { get; set; }
        public int Linea { get; set; }
        public string Articulo { get; set; }
        [Columna("GUID_ID")]
        public string GuidId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public decimal Unidades { get; set; }
        public string Observaciones { get; set; }
        public int LINEAALB { get; set; }
        public string ALMACEN { get; set; }
        public string extrarep { get; set; }
    }

    public sealed class DDocEntRepExt : DDocEntRep
    {
        /// <summary>La línea sin su extrarep, que interface.s50c tampoco copia.</summary>
        public DDocEntRepExt(DDocEntRep linea)
        {
            Ejercicio = linea.Ejercicio;
            Empresa = linea.Empresa;
            Numero = linea.Numero;
            Linea = linea.Linea;
            Articulo = linea.Articulo;
            GuidId = linea.GuidId;
            Created = linea.Created;
            Modified = linea.Modified;
            Unidades = linea.Unidades;
            Observaciones = linea.Observaciones;
            LINEAALB = linea.LINEAALB;
            ALMACEN = linea.ALMACEN;
        }

        public string ArticuloNombre { get; set; }
    }

    public sealed class Mantecamiones
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Observaciones { get; set; }
        [Columna("GUID_ID")]
        public string GuidId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }

    public sealed class inciden
    {
        public string CODIGO { get; set; }
        public string NOMBRE { get; set; }
        public string DESCRIPCION { get; set; }
        public string GUID_ID { get; set; }
        public DateTime CREATED { get; set; }
        public DateTime MODIFIED { get; set; }
    }

    public sealed class rep_app_reparto
    {
        public string NOMBRE { get; set; }
        public string DNI { get; set; }
        public string REPENT { get; set; }
        public string EMAIL { get; set; }
        public bool? ENVIARFAC { get; set; }
    }

    public sealed class DocReparto
    {
        public CDocEntRep CDocEntRep { get; set; }
        public Clientes Cliente { get; set; }
        public List<DDocEntRepExt> Details { get; set; }
        public Mantecamiones Mantecamiones { get; set; }
        public inciden inciden { get; set; }
        public rep_app_reparto rep_app_reparto { get; set; }
        public string Ruta { get; set; }
        public string Zona { get; set; }
    }
}
