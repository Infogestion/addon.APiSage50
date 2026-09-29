using System.Collections.Generic;
using System.Collections.Specialized;

namespace S50APIService.Seguridad
{
    /// <summary>
    /// Usuarios con los que las aplicaciones piden el token (POST /api/token/{username}/{password}).
    /// Son los mismos que tiene interface.s50c en su AuthenticationService, pero aquí se leen de la configuración
    /// (claves "ApiUsuario:nombre" = contraseña) en vez de estar escritos en el código.
    /// </summary>
    public sealed class UsuariosApi
    {
        private const string Prefijo = "ApiUsuario:";
        private readonly Dictionary<string, string> _usuarios = new Dictionary<string, string>();

        public UsuariosApi(NameValueCollection configuracion)
        {
            foreach (string clave in configuracion.AllKeys)
                if (clave.StartsWith(Prefijo) && clave.Length > Prefijo.Length)
                    _usuarios[clave.Substring(Prefijo.Length)] = configuracion[clave];
        }

        public int Cantidad => _usuarios.Count;

        /// <summary>Igual que interface.s50c: usuario y contraseña distinguen mayúsculas y minúsculas.</summary>
        public bool Autenticar(string usuario, string password)
        {
            return usuario != null && _usuarios.TryGetValue(usuario, out string esperada) && esperada == password;
        }
    }
}
