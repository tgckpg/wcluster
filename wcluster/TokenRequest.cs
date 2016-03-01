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

namespace wcluster
{
    class TokenRequest
    {
        private static readonly string ID = typeof( TokenRequest ).Name;

        public string RawQuery { get; private set; }
        public NameValueCollection Query { get; private set; }

#if DEBUG
        public static string MainServer = "https://botanical.astropenguin.net/";
#else
        public static string MainServer = "https://wcache.astropenguin.net/";
#endif

        public TokenRequest( HttpListenerRequest Request )
        {
            try
            {
                using ( StreamReader reader
                    = new StreamReader( Request.InputStream, Request.ContentEncoding ) )
                {
                    RawQuery = reader.ReadToEnd();
                    Query = HttpUtility.ParseQueryString( RawQuery );
                }
            }
            catch ( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.ERROR );
            }
        }

        public async Task ForwardRequest( TokenRequest IncRequest, CacheStore.Cache Cache )
        {
            WebRequest Request = WebRequest.Create( MainServer );
            Request.Method = "POST";
            Request.ContentType = "application/x-www-form-urlencoded";

            byte[] Data = Encoding.ASCII.GetBytes( IncRequest.RawQuery );

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

                        string Code = Encoding.ASCII.GetString( b );
                        Logger.Log( ID, "Server return: " + Code, LogType.WARNING );

                        await Cache.Save( Response.ContentType, b );
                    }
                    else
                    {
                        await Cache.Save( Response.ContentType, Response.GetResponseStream() );
                    }
                    break;
                default:
                    Logger.Log( ID, "Error getting cache: " + StatusCode, LogType.WARNING );
                    break;
            }
        }
    }
}
