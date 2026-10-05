using System;
using System.Linq;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>
    /// Cabeceras de las recepciones de mercancías. Vale para las dos clases de <see cref="Recepcion"/>: hace de
    /// CRecepcionService y de CRecepcionSinEnviosService de interface.s50c.
    /// </summary>
    internal sealed class CRecepcionService<T> where T : new()
    {
        private readonly Recepcion _recepcion;

        public CRecepcionService(Recepcion recepcion)
        {
            _recepcion = recepcion;
        }

        /// <summary>Las recepciones del operario que están abiertas o no tienen estado.</summary>
        public ListaJson GetRecepcionesByOperator(string operador, string ejercicio, string empresa)
        {
            var filtro = new Filtro
            {
                [_recepcion.ColumnaOperario] = operador,
                ["EMPRESA"] = empresa,
                ["EJERCICIO"] = ejercicio,
            }.Sql("LOWER(LTRIM(RTRIM([ESTADOO]))) IN ('abierto', '')");

            return Db.Lector.LeerEjercicioJson<T>(Recepcion.Addon, _recepcion.Cabeceras, filtro);
        }

        public T GetRecepcionByNumero(string numero, string ejercicio, string empresa)
        {
            return Db.Lector.LeerEjercicio<T>(Recepcion.Addon, _recepcion.Cabeceras, Recepcion.Documento(numero, ejercicio, empresa)).FirstOrDefault();
        }

        /// <summary>
        /// Guarda el estado con la clase de negocio del addon. Si ya tiene ese estado no se guarda, porque Sage volvería a
        /// grabar el documento entero.
        /// </summary>
        public bool CambiarEstado(string num, string estado, string ejercicio, string empresa)
        {
            try
            {
                var cabecera = _recepcion.Cabecera(num, ejercicio, empresa);
                if (cabecera == null || estado.TrimEnd(' ').Length > 20)
                    return false;
                if (estado.TrimEnd(' ') == cabecera.Estadoo.TrimEnd(' '))
                    return true;

                var cambios = _recepcion.Cambios(cabecera);
                cambios.Cabecera["_Estadoo"] = estado;
                return _recepcion.Guardar(cambios);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
