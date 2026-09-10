using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using sage._50;
using sage.ew.listados.Clases;
using sage.ew.functions;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Configuration;

namespace main
{
    static class Program
    {
        #region Propiedades
        /// <summary>
        /// Diccionario con los assemblys
        /// </summary>
        private static Dictionary<string, string> _dicAssemblys = new Dictionary<string, string>();
        #endregion Propiedades	
	
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Get assemblys
            GetAssemblys();

            // Attach custom event handler
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
			
			
            // copiar la libreria del add-on
            FUNCTIONS._CopiarLibreriasAddons(@"c:\sage50_last\sage50serv", @"C:\Sage50_Last\Sage50Term"); 

            // conexión a Sage 50
            main_s50.Connect(@"C:\Sage50_Last\Sage50Term", "SUPERVISOR", "1234"); 

            // presentación del desktop
            main_s50._Show();

            // Para acceder al diseñador de listados personalizables, puede quitar el comentario a las siguientes intrucciones
            // y comentar la carga de Sage50 en la línea anterior.            
            //ListadosPersonalizables loListados = new ListadosPersonalizables();
            //loListados._NuevoListado();
        }
		
        /// <summary>
        /// Get assemblys
        /// </summary>
        static void GetAssemblys()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string lcPath = Path.GetDirectoryName(config.FilePath);
            string lcPathConfig = Path.Combine(lcPath, "sage.50.exe.config");
            if (!File.Exists(lcPathConfig))
                return;

            var configXml = new XmlDocument();
            configXml.Load(lcPathConfig);

            // Navigate to the runtime/assemblyBinding section
            var runtimeNode = configXml.SelectSingleNode("configuration/runtime");
            if (runtimeNode == null)
                return;

            foreach (XmlNode node in runtimeNode.ChildNodes)
            {
                if (node.Name == "assemblyBinding")
                {
                    foreach (XmlNode node2 in node.ChildNodes)
                    {
                        if (node2.Name == "dependentAssembly")
                        {
                            string assemblyName = string.Empty;
                            string publicKeyToken = string.Empty;
                            string oldVersion = string.Empty;
                            string newVersion = string.Empty;

                            foreach (XmlNode node3 in node2.ChildNodes)
                            {
                                if (node3.Name == "assemblyIdentity")
                                {
                                    assemblyName = node3.Attributes["name"].Value;
                                    publicKeyToken = node3.Attributes["publicKeyToken"].Value;
                                }
                                else if (node3.Name == "bindingRedirect")
                                {
                                    oldVersion = node3.Attributes["oldVersion"].Value;
                                    newVersion = node3.Attributes["newVersion"].Value;
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(assemblyName) && !string.IsNullOrWhiteSpace(newVersion) && !_dicAssemblys.ContainsKey(assemblyName))
                                _dicAssemblys.Add(assemblyName, newVersion);
                        }
                    }
                }
            }
        }
		
        /// <summary>
        /// Load assembly
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly loAssembly = null;
            var requestedAssembly = new AssemblyName(args.Name);
            if (_dicAssemblys.ContainsKey(requestedAssembly.Name))
            {
                requestedAssembly.Version = new System.Version(_dicAssemblys[requestedAssembly.Name]);

                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;

                loAssembly = Assembly.Load(requestedAssembly);

                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

                return loAssembly;

            }

            return null;
        }		
		
    }
}
