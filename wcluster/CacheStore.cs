using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Deployment.Compression.Cab;

namespace wcluster
{
    class CacheStore
    {
        private CabInfo Store;

        private static DirectoryInfo Root;
        private static DirectoryInfo StagingArea;

        public CacheStore( string Path )
        {
            Root = Directory.CreateDirectory( Path );
            StagingArea = Directory.CreateDirectory( Root.FullName + "\\Staging" );

            Store = new CabInfo( Root.FullName + "\\Archives.cab" );
        }

        public Cache InitCache( string id )
        {
            Cache C = new Cache( Store );
            C.SetId( id );

            return C;
        }

        public class Cache
        {
            private CabInfo Store;
            private MixedInfo FInfo;
            private string _token;

            public bool Exists { get { return FInfo != null; } }

            public bool Expired
            {
                get
                {
                    return false;
                }
            }

            public string Id { get; private set; }

            public string Token
            {
                get
                {
                    if ( string.IsNullOrEmpty( _token ) )
                    {
                        _token = StagingArea.FullName + '\\' + Id;
                    }
                    return _token;
                }
            }

            internal Cache( CabInfo Store )
            {
                this.Store = Store;
            }

            internal void SetId( string id )
            {
                Id = id;
                if ( Store.Exists )
                {
                    FInfo = new MixedInfo( Store.GetFiles().FirstOrDefault( x => x.Name == id ) );
                }
                else if ( File.Exists( Token ) )
                {
                    FInfo = new MixedInfo( new FileInfo( Token ) );
                }
            }

            public Stream OpenStream()
            {
                return FInfo.OpenRead();
            }

            public async Task Save()
            {
                await Task.Run( () =>
                {
                    string[] s = Token.Split( '\\' );

                    // string SrcFileName = s.Last();

                    // string Dir = Token.Substring( 0, Token.Length - SrcFileName.Length );
                } );
            }

            public async Task Save( Stream ReadStream )
            {
                using ( ReadStream )
                {
                    using ( FileStream WriteStream = File.OpenWrite( Token ) )
                    {
                        await ReadStream.CopyToAsync( WriteStream );
                    }
                }
                await Save();
            }
        }

        private class MixedInfo
        {
            private FileInfo FInfo;
            private CabFileInfo CInfo;

            public MixedInfo( FileInfo FInfo )
            {
                this.FInfo = FInfo;
            }

            public MixedInfo( CabFileInfo CInfo )
            {
                this.CInfo = CInfo;
            }

            public Stream OpenRead()
            {
                if( FInfo != null )
                {
                    return FInfo.OpenRead();
                }
                else if( CInfo != null )
                {
                    return CInfo.OpenRead();
                }

                throw new InvalidOperationException();
            }
        }
    }
}
