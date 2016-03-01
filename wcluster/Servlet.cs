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
        public bool HasPublic { get; private set; }

        private Action<HttpListenerContext> RequestHandler;

        public Servlet( Action<HttpListenerContext> Handler )
        {
            Listener = new HttpListener();
            this.RequestHandler = Handler;
        }

        public void Listen( string host, int port )
        {
            if ( host == null ) host = "127.0.0.1";
            if ( port == 0 ) port = 5000;

            switch ( host )
            {
                case "localhost":
                case "127.0.0.1":
                    Logger.Log(
                        ID
                        , "You are listening on a localhost. Store apps restrictions may not allow request to be sent to this address."
                        , LogType.WARNING );
                    break;
                case "0.0.0.0":
                case "*":
                case "+":
                    Logger.Log(
                        ID
                        , "If you are listening on a public interface. Please ensure the appropriate firewall rule is added."
                        , LogType.INFO );
                    host = "*";
                    HasPublic = true;
                    break;
            }

            Listener.Prefixes.Add( "http://" + host + ":" + port + "/" );
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
