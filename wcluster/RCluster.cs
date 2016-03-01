using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Net.Astropenguin.Logging;

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
                    await Request.ForwardRequest( Request, C );
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

    }
}
