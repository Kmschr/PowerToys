using System;
using Common.ComInterlop;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Common.Utilities;
using PreviewHandlerCommon;
using System.Collections.Generic;
using System.Drawing;

namespace Microsoft.PowerToys.ThumbnailHandler.Brs
{
    [Guid("35F2F3F7-14EB-45BF-B302-77151831BC6C")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class BrsThumbnailProvider : IInitializeWithStream, IThumbnailProvider
    {
        public IStream Stream { get; private set; }
        private const uint MaxThumbnailSize = 10000;
        private static readonly byte[] MAGIC = { (byte)'B', (byte)'R', (byte)'S' };

        public void GetThumbnail(uint cx, out IntPtr phbmp, out WTS_ALPHATYPE pdwAlpha)
        {
            phbmp = IntPtr.Zero;
            pdwAlpha = WTS_ALPHATYPE.WTSAT_UNKNOWN;

            if (cx == 0 || cx > MaxThumbnailSize)
            {
                return;
            }

            using (var stream = new ReadonlyStream(this.Stream as IStream))
            {
                using (var brs = new Reader(stream))
                {
                    byte[] magic = brs.ReadNBytes(3);
                    if (!Enumerable.SequenceEqual(magic, MAGIC))
                    {
                        return;
                    }

                    int brsVersion = brs.ReadShort();
                    if (brsVersion < 8)
                    {
                        return;
                    }
                    int gameVer = brs.ReadInt();

                    Reader h1 = brs.DecompressSection();
                    string map = h1.ReadString();
                    string author = h1.ReadString();
                    string description = h1.ReadString();
                    h1.SkipNBytes(16); // Author UUID  
                    h1.ReadString(); // Host Name
                    h1.SkipNBytes(16); // Host UUID
                    h1.SkipNBytes(8); // Time
                    int brickCount = h1.ReadInt();

                    Reader h2 = brs.DecompressSection();
                    List<string> mods = h2.Array(() => h2.ReadString());
                    List<string> brickAssets = h2.Array(() => h2.ReadString());
                    List<int> colors = h2.Array(() => h2.ReadColor());
                    List<string> materials = h2.Array(() => h2.ReadString());
                    int users = h2.ReadInt();
                    for (int i = 0; i < users; ++i)
                    {
                        h2.SkipNBytes(16); // UUID
                        h2.ReadString();
                        h2.ReadInt();
                    }
                    if (brsVersion >= 9)
                    {
                        List<string> physMats = h2.Array(() => h2.ReadString());
                    }

                    if (brs.ReadNBytes(1)[0] != 0)
                    {
                        int len = brs.ReadInt();
                        byte[] fileData = brs.ReadNBytes(len);
                        Image preview = Image.FromStream(new MemoryStream(fileData));
                        using (Bitmap thumbnail = new Bitmap(preview, (int)cx, (int)cx))
                        {
                            if (thumbnail.Size.Width > 0 && thumbnail.Size.Height > 0)
                            {
                                phbmp = thumbnail.GetHbitmap();
                                pdwAlpha = WTS_ALPHATYPE.WTSAT_RGB;
                            }
                        }
                    }
                }
            }
        }
        
        public void Initialize(IStream pstream, uint grfMode)
        {
            this.Stream = pstream;
        }
    }
}
