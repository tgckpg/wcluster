using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Net.Astropenguin.Logging;
using System.Text.RegularExpressions;

namespace wcluster
{
    class InvalidRequestException : Exception
    {
        public InvalidRequestException( string Mesg ) : base( Mesg ) { }
    }

    class RCluster
    {
        private static readonly string ID = typeof( RCluster ).Name;

        public Action<HttpListenerContext> Handler;

        public static string MainServer = "https://wcache.astropenguin.net/";

        private CacheStore CStore;

        private Regex PatternMD5 = new Regex( "^[\\dA-Fa-f]{32}$" );

        public RCluster( string CachePath = "Caches" )
        {
            Handler = RequestHandler;
            CStore = new CacheStore( CachePath );
        }

        private async void RequestHandler( HttpListenerContext Context )
        {
            try
            {
                TokenRequest Request = new TokenRequest( Context.Request );

                string id = Request.Query.Get( "q" );
                if ( string.IsNullOrEmpty( id ) )
                {
                    throw new InvalidRequestException( "Id is not defined" );
                }

                if ( !PatternMD5.IsMatch( id ) )
                {
                    throw new InvalidRequestException( "Invalid id" );
                }

                Logger.Log( ID, Context.Request.RemoteEndPoint.Address.ToString() + " - " + id, LogType.INFO );

                CacheStore.Cache C = CStore.InitCache( id );
                if ( !C.Exists || C.Expired )
                {
                    Logger.Log( ID, "Cache " + id + " does not exists or expired, requesting from remote", LogType.DEBUG );
                    await Request.ForwardRequest( C );
                }

                Context.Response.StatusCode = 200;

                if ( Context.Response.SendChunked = C.Exists )
                {
                    using ( Stream s = C.OpenStream() )
                    {
                        await s.CopyToAsync( Context.Response.OutputStream );
                    }
                }
                else
                {
                    byte[] ResponseData = Encoding.ASCII.GetBytes( "0" );
                    Context.Response.OutputStream.Write( ResponseData, 0, ResponseData.Length );
                }
            }
            catch( InvalidRequestException ex )
            {
                Context.Response.StatusCode = 400;
                byte[] bytes = Encoding.UTF8.GetBytes( ex.Message );
                Context.Response.OutputStream.Write( bytes, 0, bytes.Length );
            }
            catch ( Exception )
            {
                // Client disconnected or some other error
            }
            finally
            {
                Context.Response.Close();
            }
        }

        class TokenRequest
        {
            public NameValueCollection Query { get; private set; }

            public TokenRequest( HttpListenerRequest Request )
            {
                try
                {
                    using ( StreamReader reader
                        = new StreamReader( Request.InputStream, Request.ContentEncoding ) )
                    {
                        Query = HttpUtility.ParseQueryString( reader.ReadToEnd() );
                    }
                }
                catch( Exception ex )
                {
                    Logger.Log( ID, ex.Message, LogType.ERROR );
                }
            }

            public async Task ForwardRequest( CacheStore.Cache Cache )
            {
                WebRequest Request = WebRequest.Create( MainServer );
                Request.Method = "POST";
                Request.ContentType = "application/x-www-form-urlencoded";

                byte[] Data = Encoding.ASCII.GetBytes( "q=" + Cache.Id );

                Request.ContentLength = Data.Length;

                Stream stream = Request.GetRequestStream();

                stream.Write( Data, 0, Data.Length );
                stream.Close();

                WebResponse Response = Request.GetResponse();

                HttpStatusCode StatusCode = ( ( HttpWebResponse ) Response ).StatusCode;
                switch ( StatusCode )
                {
                    case HttpStatusCode.OK:
                        // Special < 3 byte return
                        if ( Response.ContentLength < 3 )
                        {
                            byte[] b = new byte[ 3 ];
                            Response.GetResponseStream().Read( b, 0, 3 );

                            Logger.Log( ID, "Server return: " + Encoding.ASCII.GetString( b ), LogType.WARNING );
                        }
                        else
                        {
                            await Cache.Save( Response.GetResponseStream() );
                        }
                        break;
                    default:
                        Logger.Log( ID, "Error getting cache: " + StatusCode, LogType.WARNING );
                        break;
                }
            }
        }
    }
}
