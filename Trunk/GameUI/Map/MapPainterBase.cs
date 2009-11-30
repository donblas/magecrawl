﻿using System.Collections.Generic;
using libtcodWrapper;
using Magecrawl.GameEngine.Interfaces;
using Magecrawl.Utilities;

namespace Magecrawl.GameUI
{
    internal abstract class MapPainterBase : PainterBase, System.IDisposable
    {
        public const int MapDrawnWidth = UIHelper.MapWidth - 1;
        public const int MapDrawnHeight = UIHelper.MapHeight - 1;
        public static Point ScreenCenter = new Point((MapDrawnWidth - 1) / 2, (MapDrawnHeight - 2) / 2);

        protected const int OffscreenWidth = MapDrawnWidth + 1;
        protected const int OffscreenHeight = MapDrawnHeight + 1;

        // If you change this, update HelpPainter.cs
        protected static char ConvertMapObjectToChar(MapObjectType t)
        {
            switch (t)
            {
                case MapObjectType.OpenDoor:
                    return ';';
                case MapObjectType.ClosedDoor:
                    return ':';
                case MapObjectType.TreasureChest:
                    return '+';
                case MapObjectType.Cosmetic:
                    return '_';
                default:
                    throw new System.ArgumentException("Unknown Type - ConvertMapObjectToChar");
            }
        }

        // If you change this, update HelpPainter.cs
        protected static char ConvertTerrianToChar(TerrainType t)
        {
            switch (t)
            {
                case TerrainType.Floor:
                    return ' ';
                case TerrainType.Wall:
                    return '#';
                default:
                    throw new System.ArgumentException("Unknown Type - ConvertTerrianToChar");
            }
        }

        // Normally this'd take a Point, but we don't want people allocating Points()
        // all over in GUI redraw loops (slow stuff down).
        protected static bool IsDrawableTile(int x, int y)
        {
            bool xOk = x >= 1 && x < MapDrawnWidth;
            bool yOk = y >= 1 && y < MapDrawnHeight;
            return xOk && yOk;
        }

        internal virtual void DisableAllOverlays() 
        { 
        }
    }
}
