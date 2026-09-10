using sage.ew.ewbase;
using sage.ew.formul;
using sage.ew.formul.Forms;
using sage.ew.interficies;
using System;
using System.Data;
using System.Reflection;
using System.Windows.Forms;


/// <summary>
/// Este es el espacio de nombres de su módulo.
/// Puede encontrar más información y ayuda en el fichero readme.txt
/// </summary>
namespace sage.addons.S50APIRest
{
    /// <summary>
    /// Clase base para el módulo S50APIRest
    /// </summary>
    public partial class S50APIRest : Modulo
    {

        #region PROPIEDADES

        /// <summary>
        /// Firma del addon
        /// </summary>
        public override string Firma { get; set; }

        /// <summary>
        /// Instancia del ensamblado al que pertenece la libreria
        /// </summary>
        ///public Assembly _Assembly;

        #endregion PROPIEDADES

        #region CONSTRUCTORES

        /// <summary>
        /// Inicializa una nueva instancia de la classe
        /// </summary>
        public S50APIRest()
        {
            // Asignar los tipos para la configuración del módulo
            this.UserControlConfigType = typeof(Visual.UserControls.S50APIRestConfig);
            this.ModuloConfigType = typeof(Negocio.Clases.S50APIRestConfig);
        }
        #endregion CONSTRUCTORES


        #region MÉTODOS

        /// <summary>
        /// Devuelve la lista prévia del documento
        /// </summary>
        /// <param name="tcClass"></param>
        /// <param name="toTipoObjeto"></param>
        /// <param name="tcPantalla"></param>
        /// <returns></returns>
        public override object _GetListaPrevia(string tcClass, Type toTipoObjeto, string tcPantalla)
        {
            object loInstancia = null;

            switch (tcClass)
            {
                default:
                    break;
            }

            return loInstancia;
        }

        /// <summary>
        /// Método para obtener las instancias de clases de extensiones desde los documentos y mantenimientos
        /// </summary>
        /// <param name="_key">Nombre por el que identificar el mantenimiento</param>
        /// <param name="_mantePrincipal">Instancia del mantenimiento que se va a extender</param>
        /// <returns></returns>
        public override object _Extension(string _key, sage.ew.interficies.IMante _mantePrincipal)
        {
            // Ya se ha definido una extensión del mantenimiento de empresa para la configuración del módulo.
            // Se instancia en la base y se recoge en loInstancia.
            // La clase de negocio es classConfig.cs y la parte visual usercontrolConfig.cs.

            object loInstancia = base._Extension(_key, _mantePrincipal);

            _key = _key.ToLower().Trim();

            switch (_key)
            {
                default:
                    break;
            }

            return loInstancia;
        }

        /// <summary>
        /// Método para obtener las instancias de clases de extensiones de mantenimientos de tablas relacionadas (ManteTRel)
        /// </summary>
        /// <param name="_key">Nombre por el que identificar el mantenimiento de relacionado</param>
        /// <param name="_manteTRelPrincipal">Mantenimiento relacionado principal (el que se va a extender)</param>
        /// <param name="_ordenAddon">Número de orden en que se cargará el addon y sus columnas</param>
        /// <returns></returns>
        public override object _ExtensionManteTRel(string _key, sage.ew.interficies.IManteTRel _manteTRelPrincipal, int _ordenAddon)
        {
            object loInstancia = null;

            _key = _key.ToLower().Trim();

            switch (_key)
            {
                default:
                    break;
            }

            return loInstancia;
        }

        /// <summary>
        /// Vincula los formularios de la aplicación con los del addon
        /// </summary>
        /// <param name="_nombreForm">Nombre del formulario</param>
        /// <param name="_formBase">Instancia del formulario base</param>
        public override void _BindForm(string _nombreForm, IFormBase _formBase)
        {
            base._BindForm(_nombreForm, _formBase);

            _nombreForm = _nombreForm.ToLower().Trim();

            // Puede que el formulario sea el FormMante base. En ese caso, utilizamos el nombre de _Pantalla
            if (_nombreForm == "formmante")
                _nombreForm = _formBase._Pantalla.ToLower().Trim();

            switch (_nombreForm)
            {
                default:
                    break;
            }
        }

        private ApiServer _apiServer;

        /// <summary>
        /// Se ejecutará este método cuando se cargue el módulo
        /// </summary>
        /// <returns>true si la carga del módulo finaliza con éxito, false en caso contrario</returns>
        public override Boolean _Load()
        {
            _apiServer = new ApiServer();
            _apiServer.Start("http://+:9000/");
            return base._Load();
        }

        /// <summary>
        /// Se ejecutará este método cuando se cierre la aplicación
        /// </summary>
        /// <returns>true si la descarga del módulo finaliza con éxito, false en caso contrario</returns>
        public override Boolean _Unload()
        {
            _apiServer?.Stop();
            return base._Unload();
        }

        /// <summary>
        /// Se ejecutará este método cuando se actualize el módulo via FTP
        /// </summary>
        /// <returns>true si se actualiza el módulo con éxito, false en caso contrario</returns>
        public override Boolean _Update()
        {
            return base._Update();
        }

        /// <summary>
        /// Método para desinstalar un addon
        /// </summary>
        /// <param name="tcExecute">tipo ejecución (after o before)</param>
        /// <returns>true si ha sido correcto</returns>
	    public override bool _Desinstalar(TipoExecute tcExecute)
        {
            return base._Desinstalar(tcExecute);
        }

        /// <summary>
        /// Método para instalar un addon
        /// </summary>
        /// <param name="tcExecute">tipo ejecución (after o before)</param>
        /// <returns>true si ha sido correcto</returns>
	    public override bool _Instalar(TipoExecute tcExecute)
        {
            return base._Instalar(tcExecute);
        }

        /// <summary>
        /// Método para realizar apertura un addon
        /// </summary>
        /// <param name="tcEjerAnt">ejercicio anterior</param>
        /// <param name="tcEjerActual">ejercicio actual</param>
        /// <returns>true si ha sido correcto</returns>
        public override bool _Apertura(string tcEjerAnt, string tcEjerActual)
        {
            return base._Apertura(tcEjerAnt, tcEjerActual);
        }

        #endregion MÉTODOS
    }
}
