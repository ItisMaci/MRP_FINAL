using MyMediaList_HttpServer.System.Session;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace MyMediaList_HttpServer.Server
{
    public class HttpRestEventArgs : EventArgs
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // constructors                                                                                                     //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Creates a new instance of this class.</summary>
        /// <param name="context">HTTP listener context.</param>
        public HttpRestEventArgs(HttpListenerContext context)
        {
            Context = context;

            Method = HttpMethod.Parse(context.Request.HttpMethod);
            Path = context.Request.Url?.AbsolutePath ?? string.Empty;

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Received: {Method} {Path}");

            if (context.Request.HasEntityBody)
            {
                using Stream input = context.Request.InputStream;
                using StreamReader re = new(input, context.Request.ContentEncoding);
                Body = re.ReadToEnd();

                try
                {
                    Content = JsonNode.Parse(Body)?.AsObject() ?? new JsonObject();
                }
                catch
                {
                    Content = new JsonObject();
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(Body);
            }
            else
            {
                Body = string.Empty;
                Content = new JsonObject();
            }
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public properties                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Gets the underlying HTTP listener context object.</summary>
        public HttpListenerContext Context { get; }

        /// <summary>Gets the HTTP method for the request.</summary>
        public HttpMethod Method { get; }

        /// <summary>Gets the path for the request.</summary>
        public string Path { get; }

        /// <summary>Gets the request body.</summary>
        public string Body { get; }

        /// <summary>Gets the request JSON content.</summary>
        public JsonObject Content { get; }

        /// <summary>Gets the session from the request.</summary>
        public Session? Session
        {
            get
            {
                string token = Context.Request.Headers["Authorization"] ?? string.Empty;

                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = token.Substring("Bearer ".Length).Trim();
                }
                else
                {
                    return null;
                }

                // Uses the Manager to retrieve the session
                return SessionManager.GetSession(token);
            }
        }

        /// <summary>Gets or sets if the request has been responded to.</summary>
        public bool Responded
        {
            get; set;
        } = false;


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Sends a response to the request.</summary>
        /// <param name="statusCode">HTTP status code.</param>
        /// <param name="content">Response message JSON content.</param>
        public void Respond(HttpStatusCode statusCode, JsonObject? content)
        {
            if (Responded) { return; }

            HttpListenerResponse response = Context.Response;
            response.StatusCode = (int)statusCode;
            string rstr = content?.ToString() ?? string.Empty;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Responding: {statusCode}: {rstr}\n\n");

            byte[] buf = Encoding.UTF8.GetBytes(rstr);
            response.ContentLength64 = buf.Length;
            response.ContentType = "application/json; charset=UTF-8";

            try
            {
                using Stream output = response.OutputStream;
                output.Write(buf, 0, buf.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending response: {ex.Message}");
            }
            finally
            {
                try { response.Close(); } catch { }
            }

            Responded = true;
        }
    }
}
