using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Net.Astropenguin.Logging;

namespace wcluster
{
    public class Servelet 
    {
        private static readonly string ID = typeof( Servelet ).Name;

        private Action<HttpListenerContext> RequestHandler;
        private HttpListener Listener;

        public Servelet( Action<HttpListenerContext> Handler )
        {
            Listener = new HttpListener();
            this.RequestHandler = Handler;
        }

        public void Listen( string Uri = "http://localhost:8081/" )
        {
            Listener.Prefixes.Add( Uri );
        }

        public void Start()
        {
            Logger.Log( ID, "Listening on: " + string.Join( ", ", Listener.Prefixes ), LogType.INFO );

            Listener.Start();

            while ( true )
            {
                try
                {
                    HttpListenerContext context = Listener.GetContext();
                    ThreadPool.QueueUserWorkItem( o => RequestHandler( context ) );
                }
                catch ( Exception )
                {
                    // Ignored for this example
                }
            }
        }
    }
}
