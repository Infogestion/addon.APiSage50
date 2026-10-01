using System;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class Proveed
    {
        public string Codigo { get; set; }
        [Columna("ENV_PRO")]
        public int EnvPro { get; set; }
        public string Nombre { get; set; }
        public string Nombre2 { get; set; }
        public string Direccion { get; set; }
        public string Codpost { get; set; }
        public string Poblacion { get; set; }
        public string Provincia { get; set; }
        public string Cif { get; set; }
        public string Banco { get; set; }
        public string Fpag { get; set; }
        public decimal Pronto { get; set; }
        [Columna("TIPO_IVA")]
        public string TipoIva { get; set; }
        public bool Recargo { get; set; }
        public int Comunitari { get; set; }
        public bool Retencion { get; set; }
        [Columna("MODO_RET")]
        public bool ModoRet { get; set; }
        [Columna("TIPO_RET")]
        public string TipoRet { get; set; }
        public string Email { get; set; }
        public string Http { get; set; }
        [Columna("DIAS_ENT")]
        public int DiasEnt { get; set; }
        public string Observacio { get; set; }
        public decimal Descu1 { get; set; }
        public decimal Descu2 { get; set; }
        public string Idioma { get; set; }
        public string Pais { get; set; }
        public decimal Diapag { get; set; }
        public decimal Diapag2 { get; set; }
        public bool? Vista { get; set; }
        public bool Mod349 { get; set; }
        public string Contrapar { get; set; }
        [Columna("IDIOMA_IMP")]
        public string IdiomaImp { get; set; }
        [Columna("C_ENT")]
        public string CEnt { get; set; }
        [Columna("COD_AGRUP")]
        public string CodAgrup { get; set; }
        public decimal Comision { get; set; }
        public bool? Csb { get; set; }
        public string Mensaje { get; set; }
        public bool Nocomusms { get; set; }
        public bool Nocomuema { get; set; }
        public bool Nocomucar { get; set; }
        public DateTime? Fbloqnosms { get; set; }
        public DateTime? Fbloqnoema { get; set; }
        public DateTime? Fbloqnocar { get; set; }
        public string Nocomuobs { get; set; }
        public bool Regcaja { get; set; }
        public string Guid { get; set; }
        public DateTime? Importar { get; set; }
        public bool Recc { get; set; }
        public bool Recc2 { get; set; }
        public string Proveederp { get; set; }
        public string Ctaerp { get; set; }
        public string Nombre3erp { get; set; }
        public string Poblacerp { get; set; }
        public string Provinerp { get; set; }
        public int Territerp { get; set; }
        public string Direcc2erp { get; set; }
        public string Delegerp { get; set; }
        [Columna("GUID_EXP")]
        public string GuidExp { get; set; }
        public string Canal { get; set; }
        public string Facebook { get; set; }
        public string Twitter { get; set; }
        public string Skype { get; set; }
        [Columna("SYNC_CTC")]
        public bool? SyncCtc { get; set; }
        [Columna("GUID_ID")]
        public string GuidId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public decimal Cambio { get; set; }
        [Columna("FEC_CAM")]
        public DateTime? FecCam { get; set; }
        public bool Excluir349 { get; set; }
        [Columna("REFER_CAT")]
        public string ReferCat { get; set; }
    }
}
