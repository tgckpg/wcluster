﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Deployment.Compression.Cab;

using Net.Astropenguin.Logging;

namespace wcluster
{
    class CacheStore
    {
        public CabInfo CabStore { get; private set; }

        public static DirectoryInfo Root { get; private set; }
        public static DirectoryInfo StagingArea { get; private set; }

        public CacheStore( string Path )
        {
            Root = Directory.CreateDirectory( Path );
            StagingArea = Directory.CreateDirectory( Root.FullName + "\\Staging" );

            CabStore = new CabInfo( Root.FullName + "\\Archives.cab" );
        }

        public Cache InitCache( string id )
        {
            Cache C = new Cache( CabStore );
            C.SetId( id );

            return C;
        }

        public class Cache
        {
            private static readonly string ID = typeof( Cache ).Name;

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

            public string ContentType { get; private set; }

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

                // Files in staging area is always newer than archived one
                if ( File.Exists( Token ) )
                {
                    FInfo = new MixedInfo( new FileInfo( Token ) );
                }
                else if ( Store.Exists )
                {
                    CabFileInfo C = Store.GetFiles().FirstOrDefault( x => x.Name == id );
                    if ( C != null )
                    {
                        FInfo = new MixedInfo( C );
                    }
                }
            }

            public Stream OpenStream()
            {
                Stream s = FInfo.OpenRead();

                byte[] typeDat = new byte[ 1024 ];

                bool IsCR = false;
                int i = 0;
                while ( s.CanRead )
                {
                    byte b = ( byte ) s.ReadByte();

                    if( b == 0xD ) IsCR = true;
                    else if( b == 0xA && IsCR ) break;
                    else IsCR = false;

                    typeDat[ i++ ] = b;
                }

                ContentType = Encoding.ASCII.GetString( typeDat, 0, i - 1 );

                return s;
            }

            public async Task Save( string ContentType, byte[] bytes )
            {
                try
                {
                    this.ContentType = ContentType;
                    using ( FileStream WriteStream = File.OpenWrite( Token ) )
                    {
                        byte[] b = Encoding.ASCII.GetBytes( ContentType );
                        await WriteStream.WriteAsync( b, 0, b.Length );
                        await WriteStream.WriteAsync( new byte[] { 0xD, 0xA }, 0, 2 );
                        await WriteStream.WriteAsync( bytes, 0, bytes.Length );
                    }

                    FInfo = new MixedInfo( new FileInfo( Token ) );
                }
                catch( Exception ex )
                {
                    Logger.Log( ID, ex.Message, LogType.ERROR );
                }
            }

            public async Task Save( string ContentType, Stream ReadStream )
            {
                try
                {
                    this.ContentType = ContentType;
                    using ( ReadStream )
                    {
                        using ( FileStream WriteStream = File.OpenWrite( Token ) )
                        {
                            byte[] b = Encoding.ASCII.GetBytes( ContentType );
                            await WriteStream.WriteAsync( b, 0, b.Length );
                            await WriteStream.WriteAsync( new byte[] { 0xD, 0xA }, 0, 2 );
                            await ReadStream.CopyToAsync( WriteStream );
                        }
                    }

                    FInfo = new MixedInfo( new FileInfo( Token ) );
                }
                catch ( Exception ex )
                {
                    Logger.Log( ID, ex.Message, LogType.ERROR );
                }
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
