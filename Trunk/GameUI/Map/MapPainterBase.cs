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

        protected static char ConvertMapObjectToChar(MapObjectType t)
        {
            switch (t)
            {
                case MapObjectType.OpenDoor:
                    return ';';
                case MapObjectType.ClosedDoor:
                    return ':';
                default:
                    throw new System.ArgumentException("Unknown Type - ConvertMapObjectToChar");
            }
        }

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

        protected static bool IsDrawableTile(Point p)
        {
            bool xOk = p.X >= 1 && p.X < MapDrawnWidth;
            bool yOk = p.Y >= 1 && p.Y < MapDrawnHeight;
            return xOk && yOk;
        }
    }
}
