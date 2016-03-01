using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Net.Astropenguin.Logging;

using NDesk.Options;

namespace wcluster
{
    class Program
    {
        private static readonly string ID = typeof( Program ).Name;

        public const LogType Plain = LogType.R39;

        static void Main( string[] args )
        {
            ConsoleControl.EnableQuickEditMode();

            string host = null;
            int port = 0;
            bool help = false;

            bool @default = true;

            Action<string> SetLog = v =>
            {
                switch ( v )
                {
                    case "vv":
                        Logger.LogFilter.Add( LogType.DEBUG );
                        goto case "v";
                    case "v":
                        Logger.LogFilter.Add( LogType.INFO );
                        break;

                    default:
                        goto case "v";
                }

                Logger.LogFilter.Add( LogType.WARNING );
                Logger.LogFilter.Add( LogType.ERROR );
                Logger.LogFilter.Add( Plain );

                @default = false;
            };

            OptionSet OptSet = new OptionSet()
            {
                { "h|host=", "Listening host, default 127.0.0.1"
                ,  v => {
                    host = v;
                    @default = false;
                } }
                , { "p|port=", "Listening port, default 5000"
                , v => {
                    int.TryParse( v, out port );
                    @default = false;
                } }
                , { "v|vv", "Verbosity level", SetLog }
                , { "help", "Show this message and exit", v => { help = true; } }
            };

            try
            {
                OptSet.Parse( args );
            }
            catch( OptionException e )
            {
                Console.WriteLine( "Invalid argument: " + e.Message );
                help = true;
            }

            if( help )
            {
                OptSet.WriteOptionDescriptions( Console.Out );
                return;
            }

            if( Logger.LogFilter.Count == 0 )
            {
#if DEBUG
                SetLog( "vv" );
#else
                SetLog( null );
#endif
            }

            Logger.OnLog += Logger_OnLog;

            if ( @default )
            {
                Logger.Log( ID, "Not options specified, running in default mode.", LogType.INFO );
            }

            Logger.Log( ID, "Working Directory: " + Directory.GetCurrentDirectory(), LogType.DEBUG );

            RCluster CL = new RCluster();
            Servlet S = new Servlet( CL.Handler );

            S.Listen( host, port );

            string[] URIs = S.Listener.Prefixes.ToArray();
            try
            {
                S.Start();
            }
            catch( HttpListenerException ex )
            {
                switch ( ex.ErrorCode )
                {
                    case 183:
                        Logger.Log( ID, "Address is already been used", LogType.ERROR );
                        break;
                    case 5:
                        Logger.Log( ID, "Access is denied. Please run the following command(s):", LogType.ERROR );

                        string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                        foreach ( string Uri in URIs )
                        {
                            Logger.Log( ID, string.Format( "    netsh http add urlacl url={0} user={1}", Uri, userName ), Plain );
                        }
                        break;

                    default:
                        Logger.Log( ID, ex.Message, LogType.ERROR );
                        break;
                }

#if DEBUG
                Thread.Sleep( 1500 );
                Logger.Log( ID, "Press any key to exit", Plain );
                Console.Read();
#endif
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
