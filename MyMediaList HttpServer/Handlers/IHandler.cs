using MyMediaList_HttpServer.Server;

namespace MyMediaList_HttpServer.Handlers
{
    /// <summary>Classes capable of handling request implement this interface.</summary>
    public interface IHandler
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Handles a request if possible.</summary>
        /// <param name="e">Event arguments.</param>
        void Handle(HttpRestEventArgs e);
    }
}
