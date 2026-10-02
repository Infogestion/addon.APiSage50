using S50APIService.Sage;
using S50APIService.Seguridad;

namespace S50APIService.Api
{
    /// <summary>Servicios compartidos por los controladores. Se asignan una vez al arrancar, antes de aceptar peticiones.</summary>
    public static class Contexto
    {
        public static SesionSage Sesion { get; set; }
        public static LectorSage Lector { get; set; }
        public static ServicioJwt Jwt { get; set; }
        public static UsuariosApi Usuarios { get; set; }
        public static UsuariosNadilux UsuariosNadilux { get; set; }

        /// <summary>Nombre con el que Sage conoce al addon Nadilux SGA (SageAddonSga en App.config).</summary>
        public static string AddonSga { get; set; }
    }
}
