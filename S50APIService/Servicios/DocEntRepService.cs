using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Documentos de entrega y reparto de la app de repartos (tablas c_doc_ent_rep y d_doc_ent_rep del addon FERRETERIATIA).</summary>
    internal static class DocEntRepService
    {
        public static ListaJson Select()
        {
            return FerreTiasDb.LeerJson<CDocEntRep>("c_doc_ent_rep");
        }

        public static ListaJson SelectType(string type)
        {
            return FerreTiasDb.LeerJson<CDocEntRep>("c_doc_ent_rep", new Filtro { ["TIPO"] = type });
        }

        /// <summary>
        /// Los repartos de un conductor, por fecha de entrega prevista, cada uno con su cliente, su camión, su incidencia y,
        /// si se piden, sus líneas. Como en interface.s50c, el reparto del que no se puede leer algo se queda fuera: con un
        /// {year} sin base de datos no sale ninguno. Los del mismo día salen por número; en interface.s50c, en un orden
        /// distinto en cada llamada.
        /// </summary>
        public static List<DocReparto> SelectRepartosByConductor(string year, string conductor, DateTime? fechaEntregaPrev,
            bool returnLineas, bool onlyNotDelivered, bool onlyLast, string dateLastSync)
        {
            var filtro = new Filtro { ["TIPO"] = "R", ["CONDUCTOR"] = conductor };
            if (fechaEntregaPrev != null)
                filtro.Dia("FECHAENTREGAPREV", fechaEntregaPrev.Value);
            filtro.Sql("[ENTREGADO] = {0}", onlyNotDelivered ? "0" : "1");
            if (onlyLast)
            {
                if (!DateTime.TryParse(dateLastSync, out DateTime desde))
                    return new List<DocReparto>();
                filtro.Sql("[MODIFIED] >= {0}", desde.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
            }

            var repartos = new List<DocReparto>();
            var documentos = FerreTiasDb.Leer<CDocEntRep>("c_doc_ent_rep", filtro)
                .OrderBy(d => d.Fechaentregaprev.Date).ThenBy(d => d.Numero, StringComparer.Ordinal);
            foreach (var documento in documentos)
            {
                try
                {
                    repartos.Add(new DocReparto
                    {
                        CDocEntRep = documento,
                        Mantecamiones = FerreTiasDb.Primero<Mantecamiones>("mantecamiones", "CODIGO", documento.Camion),
                        Details = returnLineas ? Lineas(year, documento.Numero) : null,
                        Cliente = Db.Lector.LeerEjercicio<Clientes>(year, "clientes", new Filtro { ["CODIGO"] = documento.Cliente }).FirstOrDefault(),
                        inciden = IncidenciService.Select(documento.Incidencia),
                        rep_app_reparto = FerreTiasDb.Primero<rep_app_reparto>("rep_app_reparto", "REPENT", documento.Codigo),
                        Ruta = "",
                        Zona = "",
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing item {documento.Numero}: {ex.Message}");
                }
            }
            return repartos;
        }

        /// <summary>
        /// Guarda lo que la app de repartos sincroniza de un reparto: entrega, pago, observaciones, incidencia, las firmas
        /// y fotos (ficheros en la carpeta RUTA_FIRMA del addon) y, si viene nombre o DNI, quién lo recibe (rep_app_reparto).
        /// La cabecera se guarda con la clase de negocio del addon; rep_app_reparto no tiene clase y va por SQL.
        /// </summary>
        public static ResultadoEscritura UpdateRepartos(string num, bool? entregado, string fechaEntregado, string observaciones, string obsInt,
            bool? pagado, string incidencia, string nombre, string dni, string email, bool? enviarFac,
            List<FicheroFormulario> firma, List<FicheroFormulario> firmaSeller, List<FicheroFormulario> photos)
        {
            try
            {
                var documento = FerreTiasDb.Primero<CDocEntRep>("c_doc_ent_rep", "NUMERO", num);
                if (documento == null)
                    return ResultadoEscritura.Error("No se encontró el documento.");

                string numero = documento.Numero.Trim();
                bool conReceptor = nombre != null || dni != null;
                DateTime fechaEntrega = default;
                bool conFecha = entregado == true
                    && DateTime.TryParseExact(fechaEntregado, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaEntrega);
                if (Largo(incidencia) > 2 || conFecha && fechaEntrega.Year < 1753
                    || conReceptor && (Largo(nombre) > 50 || Largo(dni) > 20 || Largo(email) > 100))
                    return ResultadoEscritura.Error(ResultadoEscritura.ErrorAlGuardar);

                var cambios = new CambiosDocumento
                {
                    Libreria = "sage.addons.FerreteriaTia",
                    Clase = "sage.addons.FerreteriaTia.Negocio.Documentos.DocumentoEntRep",
                    Ejercicio = documento.Ejercicio.Trim(),
                    Empresa = documento.Empresa.Trim(),
                    Numero = numero,
                };
                string carpeta = FerreTiasDb.Primero<Configgeneral>("configgeneral", "FIELD", "RUTA_FIRMA").Valor.Trim();
                if (carpeta.Length > 0)
                {
                    if (GuardarFichero(firma.FirstOrDefault(), carpeta, $"signed-{numero}.png"))
                        cambios.Cabecera["_Firma"] = $"signed-{numero}.png";
                    if (GuardarFichero(firmaSeller.FirstOrDefault(), carpeta, $"signed-seller-{numero}.png"))
                        cambios.Cabecera["_FirmaSeller"] = $"signed-seller-{numero}.png";
                    for (int i = 0; i < photos.Count; i++)
                    {
                        bool comprobante = photos[i].Nombre.ToUpper().Contains("COMPROBANTE");
                        File.WriteAllBytes(Ruta(carpeta, comprobante ? $@"comprobante\comprobante-{numero}.png" : $"photo-{i}-{numero}.png"), photos[i].Contenido);
                    }
                }

                if (entregado != null)
                    cambios.Cabecera["_Entregado"] = entregado.Value;
                if (conFecha)
                    cambios.Cabecera["_FechaEntrega"] = fechaEntrega;
                if (pagado != null)
                    cambios.Cabecera["_Pagado"] = pagado.Value;
                if (observaciones != null)
                    cambios.Cabecera["_Observaciones"] = observaciones;
                if (obsInt != null)
                    cambios.Cabecera["_ObsInt"] = obsInt;
                cambios.Cabecera["_Incidencia"] = incidencia ?? "";

                string motivo = Contexto.Escritor.GuardarDocumento(cambios);
                if (motivo != null)
                    return ResultadoEscritura.Error(motivo);

                if (conReceptor)
                    GuardarReceptor(numero, nombre ?? "", dni ?? "", email ?? "", enviarFac ?? false);
                return ResultadoEscritura.Guardado("Sincronizado con éxito");
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

        private static int Largo(string texto) => (texto ?? "").TrimEnd(' ').Length;

        /// <summary>Guarda el fichero en la carpeta si trae contenido. Devuelve true si lo ha guardado.</summary>
        private static bool GuardarFichero(FicheroFormulario fichero, string carpeta, string nombre)
        {
            if (fichero == null || fichero.Contenido.Length == 0)
                return false;

            File.WriteAllBytes(Ruta(carpeta, nombre), fichero.Contenido);
            return true;
        }

        private static string Ruta(string carpeta, string nombre)
        {
            string ruta = Path.Combine(carpeta, nombre);
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            return ruta;
        }

        /// <summary>Quién recibe el reparto: cambia su fila de rep_app_reparto o, si no la tiene, la crea.</summary>
        private static void GuardarReceptor(string numero, string nombre, string dni, string email, bool enviarFac)
        {
            Contexto.Escritor.Ejecutar(FerreTiasDb.Addon,
                "UPDATE {rep_app_reparto} SET [NOMBRE] = @nombre, [DNI] = @dni, [email] = @email, [enviarfac] = @enviarfac WHERE LTRIM(RTRIM([REPENT])) = @repent;"
                + " IF @@ROWCOUNT = 0 INSERT INTO {rep_app_reparto} ([NOMBRE], [DNI], [REPENT], [email], [enviarfac]) VALUES (@nombre, @dni, @repent, @email, @enviarfac)",
                new Dictionary<string, string>
                {
                    ["@nombre"] = nombre,
                    ["@dni"] = dni,
                    ["@email"] = email,
                    ["@enviarfac"] = enviarFac ? "1" : "0",
                    ["@repent"] = numero,
                });
        }

        /// <summary>
        /// Separa un reparto en dos: crea otro con la misma cabecera y el número seguido de 1 (o 2, 3... si ya existe) y le
        /// pasa las unidades de <paramref name="lines"/> ("línea--unidades,línea--unidades"). Se hace con las clases del
        /// addon, como su botón "separar reparto"; en interface.s50c la línea original se cambia y la nueva no llega a guardarse.
        /// </summary>
        public static ResultadoEscritura MoveLinesToCopyDelivery(string num, string lines)
        {
            try
            {
                var documento = FerreTiasDb.Primero<CDocEntRep>("c_doc_ent_rep", "NUMERO", num);
                if (documento == null)
                    return ResultadoEscritura.Error("No se encontró el documento.");

                var pedidas = lines.Split(',').Select(l => l.Split(new[] { "--" }, StringSplitOptions.None)).ToList();
                if (pedidas.Any(p => p.Length < 2 || Entero(p[0]) == null || Entero(p[1]) == null))
                    return ResultadoEscritura.Error("Las líneas no son válidas: se esperan como línea--unidades, separadas por comas.");

                var mover = new Dictionary<int, decimal>();
                foreach (var linea in FerreTiasDb.Leer<DDocEntRep>("d_doc_ent_rep", Recepcion.Documento(documento.Numero, documento.Ejercicio, documento.Empresa)))
                {
                    var pedida = pedidas.FirstOrDefault(p => Entero(p[0]) == linea.Linea && Entero(p[1]) > 0 && Entero(p[1]) <= linea.Unidades);
                    if (pedida != null)
                        mover[linea.Linea] = Entero(pedida[1]).Value;
                }
                if (mover.Count == 0)
                    return ResultadoEscritura.Error("No se ha separado el reparto: ninguna línea coincide o las unidades no son válidas.");

                string numeroNuevo = NumeroLibre(documento.Numero.Trim());
                if (numeroNuevo.Length > 20)
                    return ResultadoEscritura.Error("No se ha separado el reparto: el número del nuevo no cabe.");

                string motivo = Contexto.Escritor.SepararReparto(documento.Ejercicio.Trim(), documento.Empresa.Trim(), documento.Numero.Trim(), numeroNuevo, mover);
                return motivo == null ? ResultadoEscritura.Guardado("Realizado con éxito.") : ResultadoEscritura.Error(motivo);
            }
            catch (Exception ex)
            {
                return ResultadoEscritura.Error(ex.Message);
            }
        }

        /// <summary>Como interface.s50c: el punto vale de coma decimal y los decimales se quitan. Null si no es un número.</summary>
        private static int? Entero(string texto)
        {
            bool esNumero = decimal.TryParse(texto.Replace(".", ","), NumberStyles.Number, new CultureInfo("es-ES"), out decimal numero);
            return esNumero && Math.Abs(numero) < int.MaxValue ? (int)numero : (int?)null;
        }

        private static string NumeroLibre(string numero)
        {
            int contador = 1;
            while (FerreTiasDb.Primero<CDocEntRep>("c_doc_ent_rep", "NUMERO", numero + contador) != null)
                contador++;
            return numero + contador;
        }

        /// <summary>
        /// Las líneas de los documentos con ese número (de cualquier ejercicio y empresa), con el nombre de su artículo en el
        /// ejercicio.
        /// </summary>
        private static List<DDocEntRepExt> Lineas(string year, string numero)
        {
            var lineas = FerreTiasDb.Leer<DDocEntRep>("d_doc_ent_rep", new Filtro { ["NUMERO"] = numero });
            var articulos = Db.Lector.LeerEjercicio<CodigoNombre>(year, "articulo", new Filtro().En("CODIGO", lineas.Select(l => l.Articulo)));

            return lineas.Select(l => new DDocEntRepExt(l)
            {
                ArticuloNombre = articulos.SingleOrDefault(a => a.Codigo.Trim() == l.Articulo.Trim())?.Nombre,
            }).ToList();
        }
    }
}
