using System;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class Vendedor
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public decimal Comision { get; set; }
        public string Dni { get; set; }
        public string Telefon { get; set; }
        public string Mobil { get; set; }
        public string Fax { get; set; }
        public string Direccion { get; set; }
        public string Poblacion { get; set; }
        public string Codpost { get; set; }
        public string Observacio { get; set; }
        public string Provincia { get; set; }
        public string Foto { get; set; }
        [Columna("COM_DTO")]
        public bool? ComDto { get; set; }
        public bool? Vista { get; set; }
        public string Ctacobro { get; set; }
        public string Pais { get; set; }
        public string Empresa { get; set; }
        public string Matricula { get; set; }
        public bool Menoscomi { get; set; }
        public string Ctacobro2 { get; set; }
        public string Clave { get; set; }
        public string Usuario { get; set; }
        public string Almauto { get; set; }
        [Columna("LIBRE_1")]
        public string Libre1 { get; set; }
        [Columna("LIBRE_2")]
        public string Libre2 { get; set; }
        [Columna("LIBRE_3")]
        public string Libre3 { get; set; }
        public string Clavepol { get; set; }
        public string Guid { get; set; }
        public DateTime? Importar { get; set; }
        public DateTime? Exportar { get; set; }
        public string Poblacerp { get; set; }
        public string Delegerp { get; set; }
        public string Provinerp { get; set; }
        [Columna("GUID_EXP")]
        public string GuidExp { get; set; }
        public string Email { get; set; }
        [Columna("GUID_ID")]
        public string GuidId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}
