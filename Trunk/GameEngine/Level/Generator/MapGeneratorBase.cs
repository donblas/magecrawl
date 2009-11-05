﻿using System;
using System.Collections.Generic;
using libtcodWrapper;
using Magecrawl.GameEngine.Interfaces;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine.Level.Generator
{
    internal class MapGeneratorBase
    {
        protected TCODRandom m_random;

        internal MapGeneratorBase()
        {
            m_random = new TCODRandom();
        }    

        public void Dispose()
        {
            if (m_random != null)
                m_random.Dispose();
            m_random = null;
        }

        protected Point GetClearPoint(Map map)
        {
            List<Point> clearPointList = new List<Point>();
            
            bool[,] moveabilityGrid = PhysicsEngine.CalculateMoveablePointGrid(map);

            for (int i = 0; i < map.Width; ++i)
            {
                for (int j = 0; j < map.Height; ++j)
                {
                    if (moveabilityGrid[i, j])
                        clearPointList.Add(new Point(i, j));
                }
            }
            int position = m_random.GetRandomInt(0, clearPointList.Count - 1);
            return clearPointList[position];
        }

        protected void ResetScratch(Map map)
        {
            for (int i = 0; i < map.Width; ++i)
            {
                for (int j = 0; j < map.Height; ++j)
                {
                    map.GetInternalTile(i, j).Scratch = 0;
                }
            }
        }

        protected void FloodFill(Map map, Point p, int scratchValue)
        {
            if (!map.IsPointOnMap(p))
                throw new System.ArgumentException("FloodFill with invalid point");

            if (map[p.X, p.Y].Terrain == TerrainType.Floor && map.GetInternalTile(p.X, p.Y).Scratch == 0)
            {
                FloodFill(map, p + new Point(1, 0), scratchValue);
                FloodFill(map, p + new Point(-1, 0), scratchValue);
                FloodFill(map, p + new Point(0, 1), scratchValue);
                FloodFill(map, p + new Point(0, -1), scratchValue);
            }       
        }

        // Make sure map has one connected area
        protected bool CheckConnectivity(Map map)
        {
            const int CheckConnectivityScratchValue = 42;

            // Find a clear point
            Point clearPoint = GetFirstClearPoint(map);

            // Flood fill all connected tiles
            FloodFill(map, clearPoint, CheckConnectivityScratchValue);

            // See if any floor tiles don't have our scratch, if so they are not connected
            for (int i = 0; i < map.Width; ++i)
            {
                for (int j = 0; j < map.Height; ++j)
                {
                    if (map[i,j].Terrain == TerrainType.Floor && map.GetInternalTile(i, j).Scratch != CheckConnectivityScratchValue)
                        return false;
                }
            }

            return true;
        }

        protected void FillAllSmallerUnconnectedRooms(Map map)
        {
            ResetScratch(map);
            int currentScratchNumber = 1;
            
            // First we walk the entire map, flood filling each 
            for (int i = 0; i < map.Width; ++i)
            {
                for (int j = 0; j < map.Height; ++j)
                {
                    if (map[i, j].Terrain == TerrainType.Floor && map.GetInternalTile(i,j).Scratch == 0)
                    {
                        FloodFill(map, new Point(i, j), currentScratchNumber);
                        currentScratchNumber++;
                    }
                }
            }

            // If we didn't scratch any tiles, the map must be all walls, bail
            if (currentScratchNumber == 1)
                throw new InvalidOperationException("FillAllSmallerUnconnectedRooms came to a level with all walls?");

            // Walk each tile, and count up the different groups.
            int[] numberOfTilesWithThatScratch = new int[currentScratchNumber];
            numberOfTilesWithThatScratch.Initialize();

            for (int i = 0; i < map.Width; ++i)
            {
                for (int j = 0; j < map.Height; ++j)
                {
                    numberOfTilesWithThatScratch[map.GetInternalTile(i, j).Scratch]++;
                }
            }

            if (numberOfTilesWithThatScratch[0] != 0)
                throw new System.InvalidOperationException("Some valid tiles didn't get a scratch during FillAllSmallerRooms.");
            
            // Find the largest collection
            int biggestNumber = 1;
            for (int i = 2; i < currentScratchNumber; ++i)
            {
                if (numberOfTilesWithThatScratch[i] > numberOfTilesWithThatScratch[biggestNumber])
                    biggestNumber = i;
            }

            // Now walk level, and turn every floor tile without that scratch into a wall
            for (int i = 0; i < map.Width; ++i)
            {
                for (int j = 0; j < map.Height; ++j)
                {
                    if (map[i, j].Terrain == TerrainType.Floor && map.GetInternalTile(i, j).Scratch != biggestNumber)
                        map.GetInternalTile(i, j).Terrain = TerrainType.Wall;

                    //And reset the scratch while we're here
                    map.GetInternalTile(i, j).Scratch = 0;
                }
            }

            if (!CheckConnectivity(map))
                throw new InvalidOperationException("FillAllSmallerUnconnectedRooms produced a non-connected map.");
        }

        private Point GetFirstClearPoint(Map map)
        {
            for (int i = 0; i < map.Width; ++i)
            {
                for (int j = 0; j < map.Height; ++j)
                {
                    if (map[i, j].Terrain == TerrainType.Floor)
                        return new Point(i, j);
                }
            }
            throw new System.ApplicationException("GetFirstClearPoint found no clear points");
        }

        private static List<Point> SurroundingOneSquareList = new List<Point> { new Point(-1,-1), new Point(-1,0), new Point(-1,1),
            new Point(0,-1), new Point(0,1), new Point(1,-1), new Point(1,0), new Point(1,1), new Point(0,0)};

        private static List<Point> SurroundingTwoSquareList = new List<Point> { new Point(-1,-2),
            new Point(0,-2), new Point(1,-2), new Point(-2,-1), new Point(2,-1), new Point(-2,0), new Point(2,0), 
            new Point(-2,1), new Point(2,1), new Point(-1,2), new Point(0,2), new Point(1,2), new Point(-1,-1), new Point(-1,0), new Point(-1,1),
            new Point(0,-1), new Point(0,1), new Point(1,-1), new Point(1,0), new Point(1,1), new Point(0,0)};

        public static int CountNumberOfSurroundingFloorTilesOneStepAway(Map map, int x, int y)
        {
            return CountNumberOfSurroundingTilesCore(SurroundingOneSquareList, map, x, y, TerrainType.Floor);
        }

        public static int CountNumberOfSurroundingFloorTilesTwoStepAway(Map map, int x, int y)
        {
            return CountNumberOfSurroundingTilesCore(SurroundingTwoSquareList, map, x, y, TerrainType.Floor);
        }

        public static int CountNumberOfSurroundingWallTilesOneStepAway(Map map, int x, int y)
        {
            return CountNumberOfSurroundingTilesCore(SurroundingOneSquareList, map, x, y, TerrainType.Wall);
        }

        public static int CountNumberOfSurroundingWallTilesTwoStepAway(Map map, int x, int y)
        {
            return CountNumberOfSurroundingTilesCore(SurroundingTwoSquareList, map, x, y, TerrainType.Wall);
        }

        private static int CountNumberOfSurroundingTilesCore(List<Point> tileList, Map map, int x, int y, TerrainType typeToLookFor)
        {
            int numberOfFloorTileSurrounding = 0;

            foreach (Point p in tileList)
            {
                int newX = x + p.X;
                int newY = y + p.Y;
                if (map.IsPointOnMap(new Point(newX, newY)))
                {
                    if (map[newX, newY].Terrain == typeToLookFor)
                        numberOfFloorTileSurrounding++;
                }
            }
            return numberOfFloorTileSurrounding;
        }
    }
}
