using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.Logging;
using System.Threading;

namespace wcluster
{
    class Program
    {
        private static readonly string ID = typeof( Program ).Name;

        static void Main( string[] args )
        {
            Logger.OnLog += Logger_OnLog;
            Logger.Log( ID, "Working Directory: " + Directory.GetCurrentDirectory(), LogType.DEBUG );

            RCluster CL = new RCluster();
            Servelet S = new Servelet( CL.Handler );
            S.Listen();
            S.Start();
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

            string id = LogArgs.id;

            string d = string.Format( "{0:MM-dd-yyyy HH:mm:ss}", LogArgs.timestamp );

            // Write date
            Console.Write( "[" + d + "]" );

            WriteLevel( LogArgs.Type );

            Console.WriteLine( "[{0}] {1}", LogArgs.id, LogArgs.Message );

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
