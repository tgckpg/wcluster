using Microsoft.Deployment.Compression.Cab;
using Net.Astropenguin.Linq;
using Net.Astropenguin.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace wcluster
{
    class Packer
    {
        private static readonly string ID = typeof( Packer ).Name;

        public static int Threshold = 5;
        public static void Pack( CacheStore CStore )
        {
            var j = Task.Run( () =>
            {
                IEnumerable<FileInfo> Files = CacheStore.StagingArea.GetFiles();

                if ( Files.Count() < Threshold ) return;

                CabInfo CabStore = CStore.CabStore;

                if ( CabStore.Exists )
                {
                    IEnumerable<CabFileInfo> CInfos = CabStore
                        .GetFiles()
                        .Where( x => Files.Any( y => y.Name != x.Name ) );

                    string[] Unpacks = CInfos.Remap( x => x.Name ).ToArray();
                    Logger.Log( ID, "Unpacking " + Unpacks.Count() + " file(s)", LogType.DEBUG );
                    CabStore.UnpackFiles( Unpacks, CacheStore.StagingArea.FullName, Unpacks );
                }

                FileInfo[] Repacks = CacheStore.StagingArea.GetFiles().ToArray();
                string[] RepacksName = Repacks.Remap( x => x.Name ).ToArray();

                Logger.Log( ID, "Repacking " + Repacks.Count() + " file(s)", LogType.DEBUG );
                CabStore.PackFiles( CacheStore.StagingArea.FullName, RepacksName, RepacksName );

                foreach( FileInfo f in Repacks )
                {
                    f.Delete();
                }
            } );
        }
    }
}