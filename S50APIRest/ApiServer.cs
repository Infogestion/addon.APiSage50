using Newtonsoft.Json;
using sage.addons.S50APIRest.Negocio.Clases;
using sage.ew.cliente;
using sage.ew.listados.Listados;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace sage.addons.S50APIRest
{
    public class ApiServer
    {
        private HttpListener _listener;
        private Thread _thread;
        private bool _running;

        public void Start(string prefix = "http://+:9000/")
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(prefix);
                _listener.Start();
                _running = true;

                _thread = new Thread(Listen);
                _thread.IsBackground = true;
                _thread.Start();
            }
            catch (Exception ex)
            {
                //log
            }
        }

        public void Stop()
        {
            _running = false;
            _listener?.Stop();
        }

        private void Listen()
        {
            while (_running)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch
                {
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            var path = context.Request.Url.AbsolutePath.ToLower();
            var method = context.Request.HttpMethod.ToUpper();

            string response = "";
            context.Response.ContentType = "application/json";

            try
            {
                if (method == "GET" && path == "/api/clientes")
                {
                    response = GetClientes();
                } else if (method == "GET" && path == "/swagger")
                {
                    context.Response.ContentType = "text/html";
                    response = GetSwaggerHtml();
                } else if (method == "GET" && path == "/swagger/docs")
                {
                    response = GetSwaggerJson();
                } else if (method == "POST" && path == "/api/auth/login")
                {
                    var body = new StreamReader(context.Request.InputStream).ReadToEnd();
                    response = Login(body);
                } else
                {
                    context.Response.StatusCode = 404;
                    response = JsonConvert.SerializeObject(new { error = "Not found" });
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                response = JsonConvert.SerializeObject(new { error = ex.Message });
            }

            var buffer = Encoding.UTF8.GetBytes(response);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        private string Login(string body)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<dynamic>(body);
                string usuario = request.usuario;
                string password = request.password;

                bool isValid = ValidadUsuario(usuario, password);
                if (!isValid)
                {
                    return JsonConvert.SerializeObject(new { error = "Usuario o contraseña incorrecto" });
                }

                var jwt = new JwtService();
                var token = jwt.GenerarToken(usuario);

                return JsonConvert.SerializeObject(new { token });
            } catch (Exception ex) { 
                return JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }

        private bool ValidadUsuario(string usuario, string password)
        {
            return true;
        }

        private string GetSwaggerHtml()
        {
            return @"<!DOCTYPE html>
                <html>
                <head>
                <title>Sage API</title>
                <meta charset='utf-8'/>
                <link rel='stylesheet' type='text/css' href='https://unpkg.com/swagger-ui-dist@5/swagger-ui.css'>
                </head>
                <body>
                <div id='swagger-ui'></div>
                <script src='https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js'></script>
                <script src='https://unpkg.com/swagger-ui-dist@5/swagger-ui-standalone-preset.js'></script>
                <script>
                    SwaggerUIBundle({
                        url: '/swagger/docs',
                        dom_id: '#swagger-ui',
                        presets: [
                            SwaggerUIBundle.presets.apis,
                            SwaggerUIStandalonePreset
                        ],
                        plugins: [SwaggerUIBundle.plugins.DownloadUrl],
                        layout: 'StandaloneLayout'
                    })
                </script>
                </body>
                </html>";
        }

        private string GetSwaggerJson()
        {
            var spec = new
            {
                openapi = "3.0.0",
                info = new { title = "Sage 50 API", version = "v1" },
                components = new
                {
                    securitySchemes = new Dictionary<string, object>
                    {
                        {
                            "Bearer", new
                            {
                                type = "http",
                                scheme = "bearer",
                                bearerFormat = "JWT"
                            }
                        }
                    }
                        },
                        paths = new Dictionary<string, object>
                {
                    {
                        "/api/auth/login", new
                        {
                            post = new
                            {
                                summary = "Obtener token de acceso",
                                tags = new[] { "Auth" },
                                requestBody = new
                                {
                                    required = true,
                                    content = new Dictionary<string, object>
                                    {
                                        {
                                            "application/json", new
                                            {
                                                schema = new
                                                {
                                                    type = "object",
                                                    properties = new
                                                    {
                                                        usuario = new { type = "string" },
                                                        password = new { type = "string" }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                },
                                responses = new Dictionary<string, object>
                                {
                                    { "200", new { description = "Token generado correctamente" } },
                                    { "401", new { description = "Usuario o contraseña incorrectos" } }
                                }
                            }
                        }
                    },
                    {
                        "/api/clientes", new
                        {
                            get = new
                            {
                                summary = "Obtener todos los clientes",
                                tags = new[] { "Clientes" },
                                security = new[] { new Dictionary<string, object> { { "Bearer", new string[] { } } } },
                                responses = new Dictionary<string, object>
                                {
                                    { "200", new { description = "Lista de clientes obtenida correctamente" } },
                                    { "401", new { description = "No autorizado" } }
                                }
                            }
                        }
                    }
                }
            };
            return JsonConvert.SerializeObject(spec);
        }

        private string GetClientes()
        {        
            Clientes clientes = new Clientes();
            List<object> listClientes = new List<object>();

            DataTable dtClientes = clientes._DataTable();
            DataColumn paisColumn = new DataColumn("pais", typeof(string));
            dtClientes.Columns.Add(paisColumn);

            foreach (DataRow r in dtClientes.Rows)
            {
                Cliente c = new Cliente(r["codigo"].ToString());
                r["pais"] = c._Pais;
            }

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Error = (sender, args) =>
                {
                    args.ErrorContext.Handled = true;
                }
            };

            return JsonConvert.SerializeObject(dtClientes, Formatting.Indented);
        }

    }
}
