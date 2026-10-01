using System;
using S50APIService.Sage;

namespace S50APIService.Api.Modelos
{
    public sealed class Clientes
    {
        [Columna("LIN_DES")]
        public string LinDes { get; set; }
        public string Codigo { get; set; }
        public string Cif { get; set; }
        public string Nombre { get; set; }
        public string Nombre2 { get; set; }
        public string Direccion { get; set; }
        public string Codpost { get; set; }
        public string Poblacion { get; set; }
        public string Provincia { get; set; }
        public string Pais { get; set; }
        public string Ruta { get; set; }
        public string Vendedor { get; set; }
        public string Tarifa { get; set; }
        [Columna("VALOR_ALB")]
        public bool ValorAlb { get; set; }
        public string Tipofac { get; set; }
        [Columna("COPIA_FRA")]
        public int CopiaFra { get; set; }
        public string Idioma { get; set; }
        public decimal Credito { get; set; }
        public decimal Descu1 { get; set; }
        public decimal Descu2 { get; set; }
        public decimal Pronto { get; set; }
        public int Diapag { get; set; }
        public int Diapag2 { get; set; }
        public string Fpag { get; set; }
        [Columna("TIPO_IVA")]
        public string TipoIva { get; set; }
        public bool Recargo { get; set; }
        public int Comunitari { get; set; }
        public bool Retencion { get; set; }
        [Columna("MODO_RET")]
        public bool ModoRet { get; set; }
        [Columna("TIPO_RET")]
        public string TipoRet { get; set; }
        public string Observacio { get; set; }
        public string Email { get; set; }
        public string Http { get; set; }
        public bool Refundir { get; set; }
        [Columna("ENV_CLI")]
        public int EnvCli { get; set; }
        public bool Albafra { get; set; }
        public bool? Csb { get; set; }
        [Columna("CIA_CRED")]
        public string CiaCred { get; set; }
        public string Operacio { get; set; }
        [Columna("IDIOMA_IMP")]
        public string IdiomaImp { get; set; }
        public string Agencia { get; set; }
        [Columna("BLOQ_VEN")]
        public bool BloqVen { get; set; }
        [Columna("LIM_MON")]
        public int LimMon { get; set; }
        public string Portes { get; set; }
        public decimal Portcomp { get; set; }
        public bool? Vista { get; set; }
        [Columna("F_ALTA")]
        public DateTime FAlta { get; set; }
        public bool Oferta { get; set; }
        [Columna("BLOQ_CLI")]
        public bool BloqCli { get; set; }
        [Columna("FECHA_BAJ")]
        public DateTime? FechaBaj { get; set; }
        public string Contrapar { get; set; }
        public string Zona { get; set; }
        public decimal Posicion { get; set; }
        public string Mensaje { get; set; }
        public bool? Pregvac { get; set; }
        public decimal Valportes { get; set; }
        public bool Fraped { get; set; }
        [Columna("VAL_PUNT")]
        public decimal ValPunt { get; set; }
        public bool Contado { get; set; }
        [Columna("C_ENT")]
        public string CEnt { get; set; }
        public bool Dia1 { get; set; }
        public bool Dia2 { get; set; }
        public bool Dia3 { get; set; }
        public bool Dia4 { get; set; }
        public bool Dia5 { get; set; }
        public bool Dia6 { get; set; }
        public bool Dia7 { get; set; }
        [Columna("ES_GRUPO")]
        public bool EsGrupo { get; set; }
        public string Clifinal { get; set; }
        public bool Pverde { get; set; }
        public string Tipcredit { get; set; }
        public decimal Recarfin { get; set; }
        public bool Retnofisc { get; set; }
        public decimal Tpcretnofi { get; set; }
        public string Libre1 { get; set; }
        public int Modretnofi { get; set; }
        public string Autotipdoc { get; set; }
        [Columna("EMAIL_F")]
        public string EmailF { get; set; }
        public bool Fraesi { get; set; }
        [Columna("TIPO_CLI")]
        public int TipoCli { get; set; }
        public bool Bloqalbvta { get; set; }
        public bool Bloqpedvta { get; set; }
        public bool Bloqprevta { get; set; }
        public bool Bloqdepvta { get; set; }
        public bool Nocomusms { get; set; }
        public bool Nocomuema { get; set; }
        public bool Nocomucar { get; set; }
        public DateTime? Fbloqnosms { get; set; }
        public DateTime? Fbloqnoema { get; set; }
        public DateTime? Fbloqnocar { get; set; }
        public string Nocomuobs { get; set; }
        public bool Regcaja { get; set; }
        public string Guid { get; set; }
        public DateTime? Exportar { get; set; }
        public DateTime? Importar { get; set; }
        public string Clienteerp { get; set; }
        public bool Recc { get; set; }
        public string Ctaerp { get; set; }
        public string Nombre3erp { get; set; }
        public string Poblacerp { get; set; }
        public string Provinerp { get; set; }
        public int Territerp { get; set; }
        public string Direcc2erp { get; set; }
        public string Delegerp { get; set; }
        [Columna("GUID_EXP")]
        public string GuidExp { get; set; }
        public bool Isp { get; set; }
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
        [Columna("BANCO_PREV")]
        public string BancoPrev { get; set; }
        public decimal Cambio { get; set; }
        [Columna("FEC_CAM")]
        public DateTime? FecCam { get; set; }
        public string Plefact { get; set; }
        public string Dire { get; set; }
        public bool Excluir349 { get; set; }
        [Columna("REFER_CAT")]
        public string ReferCat { get; set; }
    }
}
