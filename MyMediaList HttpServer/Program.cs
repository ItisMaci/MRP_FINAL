namespace MyMediaList_HttpServer
{
    internal static class Program
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // entry point                                                                                                      //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Main entry point of the application.</summary>
        /// <param name="args">Command line arguments.</param>
        static void Main(string[] args)
        {
            Server.HttpRestServer svr = new(8080);
            svr.RequestReceived += Handlers.Handler.HandleEvent;
            svr.Run();
        }
    }
}