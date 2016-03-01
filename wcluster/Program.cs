using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Logging;
using System.Threading;
using Net.Astropenguin.Linq;

namespace wcluster
{
    class Program
    {
        private static readonly string ID = typeof( Program ).Name;

        public const LogType Plain = LogType.R39;

        static void Main( string[] args )
        {
            ConsoleControl.EnableQuickEditMode();

            Logger.OnLog += Logger_OnLog;
            Logger.Log( ID, "Working Directory: " + Directory.GetCurrentDirectory(), LogType.DEBUG );

            RCluster CL = new RCluster();
            Servlet S = new Servlet( CL.Handler );
            S.Listen();

            string[] URIs = S.Listener.Prefixes.ToArray();
            try
            {
                S.Start();
            }
            catch( HttpListenerException ex )
            {
                Logger.Log( ID, ex.Message, LogType.ERROR );
                Logger.Log( ID, "Perhaps access is denied, please run the following command(s) with admin privilege:", LogType.INFO );

                string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                foreach( string Uri in URIs )
                {
                    Logger.Log( ID, string.Format( "    netsh http add urlacl url={0} user={1}", Uri, userName ), Plain );
                }

                Logger.Log( ID, "Press any key to exit", Plain );
                Console.ReadKey();
            }
            catch( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.ERROR );
            }
        }

        #region Logging
        static volatile Queue<LogArgs> LogQ = new Queue<LogArgs>();
        static volatile bool Printing = false;

        private static void Logger_OnLog( LogArgs LogArgs )
        {
            if( Printing )
            {
                LogQ.Enqueue( LogArgs );
                return;
            }

            Printing = true;

            switch ( LogArgs.Type )
            {
                case Plain:
                    Console.WriteLine( LogArgs.Message );
                    break;

                default:
                    string id = LogArgs.id;

                    string d = string.Format( "{0:MM-dd-yyyy HH:mm:ss}", LogArgs.timestamp );

                    // Write date
                    Console.Write( "[" + d + "]" );

                    WriteLevel( LogArgs.Type );

                    Console.WriteLine( "[{0}] {1}", LogArgs.id, LogArgs.Message );
                    break;
            }

            Printing = false;
            if ( 0 < LogQ.Count )
            {
                Logger_OnLog( LogQ.Dequeue() );
            }
        }

        private static void WriteLevel( LogType type )
        {
            Console.Write( "[" );
            switch( type )
            {
                case LogType.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogType.WARNING:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogType.INFO:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LogType.DEBUG:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }

            Console.Write( type.ToString() );

            Console.ResetColor();
            Console.Write( "]" );
        }
        #endregion
    }

}
