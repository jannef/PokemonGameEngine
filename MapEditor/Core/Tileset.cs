﻿using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.MapEditor.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    internal sealed class Tileset : IDisposable
    {
        public sealed class Tile
        {
            public readonly Tileset Parent;
            public readonly int Id;
            public readonly uint[][] Colors;

            public Tile(Tileset parent, int id, uint[][] colors)
            {
                Parent = parent;
                Id = id;
                Colors = colors;
            }
        }

        public const int BitmapNumTilesX = 8;
        public readonly WriteableBitmap Bitmap;

        private static readonly IdList _ids = new IdList(Path.Combine(Program.AssetPath, "Tileset", "TilesetIds.txt"));

        public readonly int Id;
        public readonly Tile[] Tiles;

        private unsafe Tileset(string name, int id)
        {
            uint[][][] t = RenderUtil.LoadSpriteSheet(Path.Combine(Program.AssetPath, "Tileset", name + ".png"), 8, 8);
            Tiles = new Tile[t.Length];
            for (int i = 0; i < Tiles.Length; i++)
            {
                Tiles[i] = new Tile(this, i, t[i]);
            }
            Id = id;
            // Draw
            int numTilesY = (Tiles.Length / BitmapNumTilesX) + (Tiles.Length % BitmapNumTilesX != 0 ? 1 : 0);
            const int bmpWidth = BitmapNumTilesX * 8;
            int bmpHeight = numTilesY * 8;
            Bitmap = new WriteableBitmap(new PixelSize(bmpWidth, bmpHeight), new Vector(96, 96), PixelFormat.Bgra8888);
            using (ILockedFramebuffer l = Bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                RenderUtil.TransparencyGrid(bmpAddress, bmpWidth, bmpHeight, 4, 4);
                int x = 0;
                int y = 0;
                for (int i = 0; i < Tiles.Length; i++, x++)
                {
                    if (x >= BitmapNumTilesX)
                    {
                        x = 0;
                        y++;
                    }
                    RenderUtil.Draw(bmpAddress, bmpWidth, bmpHeight, x * 8, y * 8, Tiles[i].Colors, false, false);
                }
                for (; x < BitmapNumTilesX; x++)
                {
                    RenderUtil.DrawCrossUnchecked(bmpAddress, bmpWidth, x * 8, y * 8, 8, 8, 0xFFFF0000);
                }
            }
        }
        ~Tileset()
        {
            Dispose(false);
        }

        private static readonly List<WeakReference<Tileset>> _loadedTilesets = new List<WeakReference<Tileset>>();
        public static Tileset LoadOrGet(string name)
        {
            int id = _ids[name];
            if (id == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            return LoadOrGet(name, id);
        }
        public static Tileset LoadOrGet(int id)
        {
            string name = _ids[id];
            if (name == null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            return LoadOrGet(name, id);
        }
        private static Tileset LoadOrGet(string name, int id)
        {
            Tileset t;
            if (id >= _loadedTilesets.Count)
            {
                t = new Tileset(name, id);
                _loadedTilesets.Add(new WeakReference<Tileset>(t));
                return t;
            }
            if (_loadedTilesets[id].TryGetTarget(out t))
            {
                return t;
            }
            t = new Tileset(name, id);
            _loadedTilesets[id].SetTarget(t);
            return t;
        }

        public void Dispose()
        {
            Dispose(true);
        }
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
            Bitmap.Dispose();
        }
    }
}
