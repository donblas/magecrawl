using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Magecrawl.Actors;
using Magecrawl.EngineInterfaces;
using Magecrawl.Interfaces;
using Magecrawl.Items;
using Magecrawl.Utilities;
using Magecrawl.Maps.MapObjects;

namespace Magecrawl.Maps
{
    public class Map : IMapCore, IXmlSerializable
    {
        private int m_width;
        private int m_height;
        private MapTile[,] m_map;
        private List<IMapObjectCore> m_mapObjects;
        private List<Monster> m_monsterList;
        private List<Pair<Item, Point>> m_items;

        public Map()
        {
            m_width = -1;
            m_height = -1;
        }

        public Map(int width, int height)
        {
            m_mapObjects = new List<IMapObjectCore>();
            m_monsterList = new List<Monster>();
            m_items = new List<Pair<Item, Point>>();
            m_width = width;
            m_height = height;
            m_map = new MapTile[m_width, m_height];
        }

        internal void ClearMap()
        {
            for (int i = 0; i < m_width; ++i)
            {
                for (int j = 0; j < m_height; ++j)
                {
                    m_map[i, j].Terrain = TerrainType.Wall;
                }
            }
            m_mapObjects.Clear();
            m_monsterList.Clear();
            m_items.Clear();
        }

        // Doesn't implement all of ICloneable, just copies mapTiles
        internal void CopyMap(Map sourceMap)
        {
            if (m_width != sourceMap.m_width || m_height != sourceMap.m_height)
                throw new InvalidOperationException("CopyMap of different size");

            for (int i = 0; i < m_width; ++i)
            {
                for (int j = 0; j < m_height; ++j)
                {
                    m_map[i, j].Terrain = sourceMap.m_map[i, j].Terrain;
                }
            }
        }

        // This assumes that all creatures/tiles/items are in that range and that's all that's in map
        internal void TrimToSubset(Point upperLeft, Point lowerRight)
        {
            m_width = lowerRight.X - upperLeft.X + 1;
            m_height = lowerRight.Y - upperLeft.Y + 1;

            int tempI = 0;
            for (int i = upperLeft.X; i <= lowerRight.X; ++i)
            {
                int tempJ = 0;
                for (int j = upperLeft.Y; j <= lowerRight.Y; ++j)
                {
                    m_map[tempI, tempJ].Terrain = m_map[i, j].Terrain;
                    tempJ++;
                }
                tempI++;
            }

            m_mapObjects.ForEach(o => o.Position -= upperLeft);

            List<Pair<Item, Point>> tempItemList = new List<Pair<Item, Point>>();
            foreach (var item in m_items)
            {
                tempItemList.Add(new Pair<Item, Point>(item.First, item.Second - upperLeft));
            }
            m_items = tempItemList;
            
            m_monsterList.ForEach(m => m.Position -= upperLeft);
        }

        public void AddMonster(Monster m)
        {
            m_monsterList.Add(m);
        }

        public bool RemoveMonster(Monster m)
        {
            return m_monsterList.Remove(m);
        }

        public void ClearMonstersFromMap()
        {
            m_monsterList.Clear();
        }

        public void AddMapItem(IMapObjectCore item)
        {
            m_mapObjects.Add(item);
        }

        public bool RemoveMapItem(IMapObjectCore item)
        {
            return m_mapObjects.Remove(item);
        }

        public void AddItem(Pair<Item, Point> item)
        {
            m_items.Add(item);
        }

        public bool RemoveItem(Pair<Item, Point> item)
        {
            return m_items.Remove(item);
        }

        public int Width
        {
            get
            {
                return m_width;
            }
        }

        public int Height
        {
            get
            {
                return m_height;
            }
        }

        public IEnumerable<IMapObject> MapObjects
        {
            get 
            {
                return m_mapObjects.OfType<IMapObject>();
            }
        }

        public IEnumerable<ICharacter> Monsters
        {
            get 
            {
                return m_monsterList.OfType<ICharacter>();
            }
        }

        public IEnumerable<Pair<IItem, Point>> Items
        {
            get 
            {
                foreach (Pair<Item, Point> itemPair in m_items)
                {
                    yield return new Pair<IItem, Point>(itemPair.First, itemPair.Second);
                }
            }
        }

        public IList<Pair<Item, Point>> InternalItems
        {
            get
            {
                return m_items;
            }
        }

        public TerrainType GetTerrainAt(int x, int y)
        {
            return m_map[x, y].Terrain;
        }

        public TerrainType GetTerrainAt(Point p)
        {
            return m_map[p.X, p.Y].Terrain;
        }

        public void SetTerrainAt(int x, int y, TerrainType t)
        {
            m_map[x, y].Terrain = t;
        }

        public void SetTerrainAt(Point p, TerrainType t)
        {
            m_map[p.X, p.Y].Terrain = t;
        }

        public byte GetScratchAt(Point p)
        {
            return m_map[p.X, p.Y].Scratch;
        }

        public byte GetScratchAt(int x, int y)
        {
            return m_map[x, y].Scratch;
        }

        public void SetScratchAt(Point p, byte scratch)
        {
            m_map[p.X, p.Y].Scratch = scratch;
        }

        public void SetScratchAt(int x, int y, byte scratch)
        {
            m_map[x, y].Scratch = scratch;
        }

        public bool IsVisitedAt(Point p)
        {
            return m_map[p.X, p.Y].Visited;
        }

        public void SetVisitedAt(Point p, bool visitedStatus)
        {
            m_map[p.X, p.Y].Visited = visitedStatus;
        }

        // There are many times we want to know what cells are movable into, for FOV or Pathfinding for example
        // This calculates them in batch much much more quickly. True means you can walk there.
        public bool[,] CalculateMoveablePointGrid(bool monstersBlockPath)
        {
            return CalculateMoveablePointGridCore(new Point(0, 0), Width, Height, monstersBlockPath);
        }

        public bool[,] CalculateMoveablePointGrid(bool monstersBlockPath, Point characterPosition)
        {
            bool[,] returnValue = CalculateMoveablePointGridCore(new Point(0, 0), Width, Height, monstersBlockPath);

            returnValue[characterPosition.X, characterPosition.Y] = false;

            return returnValue;
        }

        private bool[,] CalculateMoveablePointGridCore(Point upperLeftCorner, int width, int height, bool monstersBlockPath)
        {
            bool[,] returnValue = new bool[Width, Height];

            for (int i = upperLeftCorner.X; i < upperLeftCorner.X + width; ++i)
            {
                for (int j = upperLeftCorner.Y; j < upperLeftCorner.Y + height; ++j)
                {
                    returnValue[i, j] = m_map[i, j].Terrain == TerrainType.Floor;
                }
            }

            foreach (MapObject obj in MapObjects.Where(x => x.IsSolid))
            {
                returnValue[obj.Position.X, obj.Position.Y] = false;
            }

            if (monstersBlockPath)
            {
                foreach (Monster m in Monsters)
                {
                    returnValue[m.Position.X, m.Position.Y] = false;
                }
            }

            return returnValue;
        }

        // This is a debugging tool. It prints out a map to Console Out. Usefor for visualizing a map.
        internal void PrintMapToStdOut()
        {
            for (int j = 0; j < Height; ++j)
            {
                for (int i = 0; i < Width; ++i)
                    System.Console.Out.Write(m_map[i, j].Terrain == TerrainType.Wall ? '#' : '.');
                System.Console.Out.WriteLine();
            }
            System.Console.Out.WriteLine();
        }

        // This is a debugging tool. It prints out a map to Console Out with its scratch values
        internal void PrintScratchMapToStdOut()
        {
            for (int j = 0; j < Height; ++j)
            {
                for (int i = 0; i < Width; ++i)
                    System.Console.Out.Write(ConvertScratchToDebugSymbol(m_map[i, j].Scratch));
                System.Console.Out.WriteLine();
            }
            System.Console.Out.WriteLine();
        }

        private string ConvertScratchToDebugSymbol(int scratch)
        {
            if (scratch == -1)
                return '*'.ToString();
            else if (scratch < 9)
                return scratch.ToString();
            else
                return ((char)((int)'a' + scratch - 9)).ToString();
        }

        public bool IsPointOnMap(Point p)
        {
            return (p.X >= 0) && (p.Y >= 0) && (p.X < Width) && (p.Y < Height);
        }

        public bool IsPointOnMap(int x, int y)
        {
            return (x >= 0) && (y >= 0) && (x < Width) && (y < Height);
        }

        public Point CoercePointOntoMap(Point p)
        {
            if (p.X < 0)
                p.X = 0;
            if (p.Y < 0)
                p.Y = 0;
            if (p.X >= Width)
                p.X = Width - 1;
            if (p.Y >= Height)
                p.Y = Height - 1;
            return p;
        }

        #region SaveLoad
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();

            m_width = reader.ReadElementContentAsInt();
            m_height = reader.ReadElementContentAsInt();
            m_map = new MapTile[m_width, m_height];

            for (int i = 0; i < m_width; ++i)
            {
                for (int j = 0; j < m_height; ++j)
                {
                    m_map[i, j] = new MapTile();
                    m_map[i, j].ReadXml(reader);
                }
            }

            // Read Map Features
            m_mapObjects = new List<IMapObjectCore>();

            ReadListFromXMLCore readDelegate = new ReadListFromXMLCore(delegate
            {
                string typeString = reader.ReadElementContentAsString();
                MapObject newObj = MapObjectFactory.Instance.CreateMapObject(typeString);
                newObj.ReadXml(reader);
                m_mapObjects.Add(newObj);
            });
            ListSerialization.ReadListFromXML(reader, readDelegate);

            // Read Monsters
            m_monsterList = new List<Monster>();

            readDelegate = new ReadListFromXMLCore(delegate
            {
                string typeString = reader.ReadElementContentAsString();
                int baseLevel = reader.ReadElementContentAsInt();
                Monster newObj = MonsterFactory.Instance.CreateMonster(typeString, baseLevel);
                newObj.ReadXml(reader);
                m_monsterList.Add(newObj);
            });
            ListSerialization.ReadListFromXML(reader, readDelegate);

            // Read Items
            m_items = new List<Pair<Item, Point>>();

            readDelegate = new ReadListFromXMLCore(delegate
            {
                string typeString = reader.ReadElementContentAsString();
                Item newItem = ItemFactory.Instance.CreateBaseItem(typeString);
                newItem.ReadXml(reader);
                Point position = new Point();
                position = position.ReadXml(reader);
                m_items.Add(new Pair<Item, Point>(newItem, position));
            });
            ListSerialization.ReadListFromXML(reader, readDelegate);

            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Map");
            writer.WriteElementString("Width", m_width.ToString());
            writer.WriteElementString("Height", m_height.ToString());

            for (int i = 0; i < m_width; ++i)
            {
                for (int j = 0; j < m_height; ++j)
                {
                    m_map[i, j].WriteXml(writer);
                }
            }

            ListSerialization.WriteListToXML(writer, m_mapObjects, "MapObjects");

            ListSerialization.WriteListToXML(writer, m_monsterList, "Monsters");

            ListSerialization.WriteListToXML(writer, m_items, "Items");

            writer.WriteEndElement();
        }

        #endregion
    }
}
