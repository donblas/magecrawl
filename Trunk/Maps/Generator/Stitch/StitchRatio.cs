﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using libtcod;
using Magecrawl.Utilities;

namespace Magecrawl.Maps.Generator.Stitch
{
    internal sealed class StitchRatio : IDisposable
    {
        private int[,] m_ratioTable;
        private static TCODRandom m_random = new TCODRandom();
        private int m_wantedNumberOfNodes;
        private int m_generatedNumberOfNodes;

        internal StitchRatio(int wantedNumberOfNodes)
        {
            m_wantedNumberOfNodes = wantedNumberOfNodes;
            m_generatedNumberOfNodes = 0;

            int nodeTypeLength = Enum.GetValues(typeof(MapNodeType)).Length;
            m_ratioTable = new int[nodeTypeLength, nodeTypeLength];
            using (StreamReader inputFile = new StreamReader("Map" + Path.DirectorySeparatorChar + "DungeonChunkRatio.dat"))
            {
                // Pull off the initial comment line
                string currentLine = inputFile.ReadLine();

                for (int i = 0; i < nodeTypeLength; ++i)
                {
                    string[] splitString = inputFile.ReadLine().Split('\t');
                    IEnumerable<string> elements = splitString.Skip(1).Where(s => s != "\t" && s != " " && s != "");
                    int numberOfValues = 0;
                    foreach (string propString in elements)
                    {
                        m_ratioTable[i, numberOfValues] = int.Parse(propString, CultureInfo.InvariantCulture);
                        numberOfValues++;
                    }
                    if (numberOfValues != nodeTypeLength)
                        throw new System.ArgumentException("Invalid information in DungeonChunkRatio.dat");
                }
            }
        }

        public void Dispose()
        {
            m_random.Dispose();
        }

        // So we use the base table values, but modify based on how close we are to our wanted value
        public int GetGenerationChance(MapNodeType parent, MapNodeType current)
        {
            double percentageNodesUsed = (double)m_generatedNumberOfNodes / (double)m_wantedNumberOfNodes;

            double modifyAmount = Math.Abs((.5 - percentageNodesUsed) * 2) + 1;
            bool isAboveHalf = percentageNodesUsed > .5;
           
            switch (current)
            {
                case MapNodeType.None:
                    if (isAboveHalf)
                        return (int)(m_ratioTable[(int)parent, (int)current] * modifyAmount);
                    else
                        return (int)(m_ratioTable[(int)parent, (int)current] / modifyAmount);
                case MapNodeType.Hall:
                case MapNodeType.MainRoom:
                case MapNodeType.SideRoom:
                case MapNodeType.TreasureRoom:
                    if (isAboveHalf)
                        return (int)(m_ratioTable[(int)parent, (int)current] / modifyAmount);
                    else
                        return (int)(m_ratioTable[(int)parent, (int)current] * modifyAmount);
            }

            return m_ratioTable[(int)parent, (int)current];
        }

        public MapNodeType GetNewNode(MapNodeType parent)
        {
            List<MapNodeType> possibleList = new List<MapNodeType>();

            foreach (MapNodeType t in Enum.GetValues(typeof(MapNodeType)))
            {
                if (GetGenerationChance(parent, t) > 0)
                    possibleList.Add(t);
            }

            if (possibleList.Count == 0)
                throw new System.InvalidOperationException("StitchRatio::GetNewNode with all zero values: " + parent.ToString());

            int listLength = possibleList.Count;

            while (true)
            {
                MapNodeType attempt = possibleList[m_random.getInt(0, listLength - 1)];
                if (m_random.Chance(GetGenerationChance(parent, attempt)))
                {
                    if (attempt != MapNodeType.None)
                        m_generatedNumberOfNodes++;
                    return attempt;
                }
            }
        }
    }
}