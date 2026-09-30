using System;
using System.Linq;
using S50APIService.Sage;

namespace S50APIService.Seguridad
{
    /// <summary>
    /// Usuarios de las apps de Nadilux, que están en las tablas de Sage (no en la configuración como <see cref="UsuariosApi"/>):
    /// - Repartos: conductores de "conductor" en el addon FERRETERIATIA (ConductorService.SelectAsyncLogin de interface.s50c).
    /// - Mercancías: operarios de "operario" en COMUNES, con la contraseña en "oper_contrasena" del addon GESTIONMERC
    ///   (OperariosService.SelectAsyncLoginMerchandise).
    /// Igual que interface.s50c: se leen todas las filas y se comparan en memoria, sin espacios a los lados y
    /// distinguiendo mayúsculas y minúsculas. Devuelven el código tal cual está en la tabla, o null si no coincide.
    /// </summary>
    public sealed class UsuariosNadilux
    {
        private readonly SesionSage _sesion;
        private readonly TimeSpan _timeout;

        public UsuariosNadilux(SesionSage sesion, TimeSpan timeout)
        {
            _sesion = sesion;
            _timeout = timeout;
        }

        public string AutenticarConductor(string nombre, string password)
        {
            var conductores = _sesion.Ejecutar(t => t.LeerTabla("FERRETERIATIA", "conductor", new[] { "CODIGO", "NOMBRE", "PASS" }), _timeout, "leer conductores");
            return conductores
                .Where(c => Igual(c[1], nombre) && Igual(c[2], password))
                .Select(c => (string)c[0])
                .FirstOrDefault();
        }

        /// <summary>Un operario sin contraseña en oper_contrasena no coincide (en interface.s50c provoca un 500).</summary>
        public string AutenticarOperario(string nombre, string password)
        {
            var operarios = _sesion.Ejecutar(t => t.LeerTabla("COMUNES", "operario", new[] { "CODIGO", "NOMBRE" }), _timeout, "leer operarios");
            var contrasenas = _sesion.Ejecutar(t => t.LeerTabla("GESTIONMERC", "oper_contrasena", new[] { "OPERARIO", "CONTRASENA" }), _timeout, "leer contraseñas de operarios");

            return (from o in operarios
                    join c in contrasenas on ((string)o[0])?.Trim() equals ((string)c[0])?.Trim()
                    where Igual(o[1], nombre) && Igual(c[1], password)
                    select (string)o[0])
                .FirstOrDefault();
        }

        private static bool Igual(object valorTabla, string valorPedido)
        {
            return valorTabla is string texto && valorPedido != null && texto.Trim() == valorPedido.Trim();
        }
    }
}
