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
    public class Servlet 
    {
        private static readonly string ID = typeof( Servlet ).Name;

        public HttpListener Listener { get; private set; }

        private Action<HttpListenerContext> RequestHandler;

        public Servlet( Action<HttpListenerContext> Handler )
        {
            Listener = new HttpListener();
            this.RequestHandler = Handler;
        }

        public void Listen( string Uri = "http://*:8082/" )
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
                }
            }
        }
    }
}
