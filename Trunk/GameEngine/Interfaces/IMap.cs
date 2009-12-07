﻿using System;
using System.Collections.Generic;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine.Interfaces
{
    public enum TerrainType : byte
    {
        Wall, Floor
    }

    public interface IMap
    {
        int Width
        {
            get;
        }

        int Height
        {
            get;
        }

        IList<IMapObject> MapObjects
        {
            get;
        }

        IList<ICharacter> Monsters
        {
            get;
        }

        IList<Pair<IItem, Point>> Items
        {
            get;
        }

        TerrainType GetTerrainAt(Point p);
        bool IsPointOnMap(Point p);
    }
}
