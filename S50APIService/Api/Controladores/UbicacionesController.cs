using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Sage;
using S50APIService.Servicios;

namespace S50APIService.Api.Controladores
{
    [RoutePrefix("api/{year:int}/Ubicaciones")]
    [Autorizar]
    public sealed class UbicacionesController : ApiController
    {
        /// <summary>
        /// Permite obtener las ubicaciones de forma paginada
        /// </summary>
        [HttpGet]
        [Route("{page:int}/{pagesize:int}")]
        [ResponseType(typeof(List<Ubicaciones>))]
        public HttpResponseMessage Get(string year, int page, int pagesize)
        {
            return Respuestas.Json(UbicacionService.Select(page, pagesize));
        }

        /// <summary>
        /// Permite obtener una ubicación concreta
        /// </summary>
        [HttpGet]
        [Route("{code}")]
        [ResponseType(typeof(Ubicaciones))]
        public HttpResponseMessage GetByCode(string year, string code)
        {
            return Respuestas.JsonONada(UbicacionService.SelectByCode(code));
        }

        /// <summary>
        /// Permite obtener los artículos de una ubicación y sus almacenes
        /// </summary>
        [HttpGet]
        [Route("rel/{code}/{emp}")]
        [ResponseType(typeof(Ubicaciones))]
        public HttpResponseMessage GetByArticuloRel(string year, string code, string emp)
        {
            return Respuestas.Json(UbicacionService.SelectByArticuloRel(year, code, emp));
        }

        /// <summary>
        /// Permite obtener las ubicaciones por almacén y stock de un artículo
        /// </summary>
        [HttpGet]
        [Route("rel-art/{code}/{emp}")]
        [ResponseType(typeof(Ubicaciones))]
        public HttpResponseMessage GetByArticuloRelAr(string year, string code, string emp)
        {
            return Respuestas.Json(UbicacionService.SelectAllArticle(year, code, emp));
        }

        /// <summary>
        /// Permite modificar una ubicación
        /// </summary>
        [HttpPut]
        [Route("")]
        [ResponseType(typeof(Ubicaciones))]
        public HttpResponseMessage Update([FromBody] Ubicaciones item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item)) ?? Intentar(() =>
            {
                var itemUpdating = UbicacionService.SelectByCode(item.Codigo);
                if (itemUpdating == null)
                    return Respuestas.NoEncontrado(item);

                // El código tiene que llegar igual que está guardado, con sus espacios: si no, EF lo toma por un cambio de clave.
                if (item.Codigo != itemUpdating.Codigo)
                    return Respuestas.ErrorValidacion("The property 'ubicaciones.Codigo' is part of a key and so cannot be modified or marked as modified. "
                        + "To change the principal of an existing entity with an identifying foreign key, first delete the dependent and invoke 'SaveChanges', "
                        + "and then associate the dependent with the new principal.");

                UbicacionService.Update(itemUpdating.Codigo, item);
                return Respuestas.Json(item);
            });
        }

        /// <summary>
        /// Permite modificar una relación de artículo y ubicación
        /// </summary>
        [HttpPut]
        [Route("rel-art")]
        [ResponseType(typeof(RelUbiAlm))]
        public HttpResponseMessage UpdateRel([Obligatorio] string ubicacion = null, [Obligatorio] string emp = null, [Obligatorio] string almacen = null,
            [Obligatorio] string articulo = null, [Obligatorio] string newUbi = null)
        {
            return Intentar(() =>
            {
                var itemUpdating = UbicacionRelService.SelectRelUbi(ubicacion, emp, articulo, almacen);
                if (itemUpdating == null)
                    return Respuestas.NoEncontrado(null);

                UbicacionRelService.Update(itemUpdating, newUbi.Trim());
                return Respuestas.Json(itemUpdating);
            });
        }

        /// <summary>
        /// Permite crear una ubicación
        /// </summary>
        [HttpPost]
        [Route("")]
        [ResponseType(typeof(Ubicaciones))]
        public HttpResponseMessage Create([FromBody] Ubicaciones item)
        {
            return ValidacionCuerpo.Error(Request, item, ModelState, nameof(item)) ?? Intentar(() =>
            {
                UbicacionService.Add(item);
                return Respuestas.Json(item);
            });
        }

        /// <summary>
        /// Permite agregar una relación de ubicación y artículo
        /// </summary>
        [HttpPost]
        [Route("rel-art")]
        [ResponseType(typeof(RelUbiAlm))]
        public HttpResponseMessage CreateRel([Obligatorio] string ubicacion = null, [Obligatorio] string emp = null, [Obligatorio] string almacen = null,
            [Obligatorio] string articulo = null)
        {
            return Intentar(() =>
            {
                var relUbi = UbicacionRelService.SelectRelUbi(ubicacion, emp, articulo, almacen);
                if (relUbi != null && string.IsNullOrEmpty(relUbi.Ubicacion))
                {
                    UbicacionRelService.Update(relUbi, ubicacion);
                    return Respuestas.Json(relUbi);
                }

                var relUbiNew = new RelUbiAlm
                {
                    Ubicacion = ubicacion,
                    Articulo = articulo,
                    Empresa = emp,
                    Almacen = almacen,
                    Linea = UbicacionRelService.LastRel(emp, articulo, almacen),
                };
                UbicacionRelService.Add(relUbiNew);
                return Respuestas.Json(relUbiNew);
            });
        }

        /// <summary>
        /// Permite eliminar una ubicación que no tenga artículos
        /// </summary>
        [HttpDelete]
        [Route("")]
        [ResponseType(typeof(string))]
        public HttpResponseMessage Delete([Obligatorio] string code = null)
        {
            return Intentar(() =>
            {
                if (UbicacionRelService.SearchCode(code).Count > 0)
                    return Respuestas.ErrorValidacion("Ubicación con artículos asignados, no se puede eliminar.");

                var itemDeleting = UbicacionService.SelectByCode(code);
                if (itemDeleting == null)
                    return Respuestas.NoEncontrado(code);

                UbicacionService.Remove(itemDeleting.Codigo);
                return Respuestas.Texto(code);
            });
        }

        /// <summary>
        /// Permite eliminar una relación de ubicación y artículo
        /// </summary>
        [HttpDelete]
        [Route("rel-art")]
        [ResponseType(typeof(object))]
        public HttpResponseMessage DeleteRel([Obligatorio] string ubicacion = null, [Obligatorio] string emp = null, [Obligatorio] string almacen = null,
            [Obligatorio] string articulo = null)
        {
            return Intentar(() =>
            {
                var itemDeleting = UbicacionRelService.SelectRelUbi(ubicacion, emp, articulo, almacen);
                if (itemDeleting == null)
                    return Respuestas.NoEncontrado(ubicacion);

                UbicacionRelService.Remove(itemDeleting);
                return Respuestas.Json(new { result = "NULL" });
            });
        }

        /// <summary>
        /// Como el try/catch de las escrituras de interface.s50c: cualquier error es un 400 con su mensaje (el de SQL Server
        /// si viene de la base de datos), salvo que falte la base de datos del addon, que es el 404 de siempre.
        /// </summary>
        private static HttpResponseMessage Intentar(Func<HttpResponseMessage> escritura)
        {
            try
            {
                return escritura();
            }
            catch (EjercicioNoEncontradoException)
            {
                throw;
            }
            catch (ErrorSqlException ex)
            {
                return Respuestas.ErrorValidacion(ex.MensajeSql);
            }
            catch (Exception ex)
            {
                return Respuestas.ErrorValidacion(ex.Message);
            }
        }
    }
}
