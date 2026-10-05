using System;
using System.Collections.Generic;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>
    /// Unidades enviadas por almacén de los artículos de un documento (tabla alma_envio). El addon no tiene clase de negocio
    /// para esta tabla (también la escribe con SQL), así que se escribe por la capa de datos de Sage.
    /// </summary>
    internal static class AlmaEnvioService
    {
        /// <summary>El texto JSON con las unidades por almacén de un artículo del documento, o "[]" si no tiene.</summary>
        public static string GetWarehouseDetail(string numero, string empresa, string ejercicio, string articulo)
        {
            var envio = MercanciasDb.DelAddon<AlmaUnidades>("alma_envio", Clave(numero, empresa, ejercicio, articulo)).FirstOrDefault();
            return envio == null ? "[]" : envio.Unidades;
        }

        /// <summary>Guarda las unidades de un artículo del documento: cambia su fila o, si no la tiene, la crea.</summary>
        public static ResultadoEscritura UpdateDetailMercancia(string numero, string empresa, string ejercicio, string articulo, string unidades)
        {
            try
            {
                var envio = MercanciasDb.DelAddon<AlmaEnvio>("alma_envio", Clave(numero, empresa, ejercicio, articulo)).FirstOrDefault();
                return envio == null
                    ? Insertar(numero, empresa, ejercicio, articulo, unidades)
                    : Actualizar(envio, unidades.Trim());
            }
            catch (ErrorSqlException)
            {
                return ResultadoEscritura.Error(ResultadoEscritura.ErrorAlGuardar);
            }
            catch (Exception ex)
            {
                return ResultadoEscritura.Error(ex.Message);
            }
        }

        private static Filtro Clave(string numero, string empresa, string ejercicio, string articulo)
        {
            return new Filtro
            {
                ["NUMERO"] = numero,
                ["EMPRESA"] = empresa,
                ["ARTICULO"] = articulo,
                ["EJERCICIO"] = ejercicio,
            };
        }

        /// <summary>Los valores se guardan como llegan, sin recortar, igual que en interface.s50c.</summary>
        private static ResultadoEscritura Insertar(string numero, string empresa, string ejercicio, string articulo, string unidades)
        {
            // Por la capa de Sage un valor más largo que su columna se guardaría cortado; con EF es un error de SQL Server.
            if (NoCabe(articulo, 10) || NoCabe(numero, 10) || NoCabe(empresa, 2) || NoCabe(ejercicio, 15))
                return ResultadoEscritura.Error(ResultadoEscritura.ErrorAlGuardar);

            Contexto.Escritor.Ejecutar(MercanciasDb.Addon,
                "INSERT INTO {alma_envio} ([ARTICULO], [EJERCICIO], [EMPRESA], [NUMERO], [CREATED], [MODIFIED], [UNIDADES])"
                + " VALUES (@articulo, @ejercicio, @empresa, @numero, GETDATE(), GETDATE(), @unidades)",
                new Dictionary<string, string>
                {
                    ["@articulo"] = articulo,
                    ["@ejercicio"] = ejercicio,
                    ["@empresa"] = empresa,
                    ["@numero"] = numero,
                    ["@unidades"] = unidades,
                });
            return ResultadoEscritura.Guardado();
        }

        /// <summary>
        /// Solo cambia la fila si es la única de ese artículo en el documento. Si el artículo está en varias líneas,
        /// interface.s50c reintenta sin fin y no llega a responder; aquí no se cambia nada y se responde con un error.
        /// </summary>
        private static ResultadoEscritura Actualizar(AlmaEnvio envio, string unidades)
        {
            const string clave = "[ARTICULO] = @articulo AND [EJERCICIO] = @ejercicio AND [EMPRESA] = @empresa AND [NUMERO] = @numero";
            int filas = Contexto.Escritor.Ejecutar(MercanciasDb.Addon,
                "UPDATE {alma_envio} SET [UNIDADES] = @unidades, [MODIFIED] = GETDATE() WHERE " + clave
                + " AND (SELECT COUNT(*) FROM {alma_envio} WHERE " + clave + ") = 1",
                new Dictionary<string, string>
                {
                    ["@articulo"] = envio.Articulo,
                    ["@ejercicio"] = envio.Ejercicio,
                    ["@empresa"] = envio.Empresa,
                    ["@numero"] = envio.Numero,
                    ["@unidades"] = unidades,
                });
            return filas == 1
                ? ResultadoEscritura.Guardado()
                : ResultadoEscritura.Error("El artículo está en varias líneas del documento: no se han cambiado sus unidades.");
        }

        private static bool NoCabe(string texto, int largo) => texto.TrimEnd(' ').Length > largo;
    }
}
