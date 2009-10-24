﻿using System.Collections.Generic;
using libtcodWrapper;
using Magecrawl.GameEngine.Interfaces;

namespace Magecrawl.GameUI
{
    public class CharacterInfo
    {
        public CharacterInfo()
        {
        }

        private const int StartingX = 51;
        private const int InfoWidth = 29;
        private const int InfoHeight = 60;
        private const int ScreenCenter = StartingX + (InfoWidth / 2);
        public void Draw(Console screen, IPlayer player)
        {
            screen.DrawFrame(StartingX, 0, InfoWidth, InfoHeight, true);
            screen.PrintLine(player.Name, ScreenCenter, 1, LineAlignment.Center);

            string hpString = string.Format("HP {0}/{1}", player.CurrentHP, player.MaxHP);
            screen.PrintLine(hpString, StartingX + 2, 2, LineAlignment.Left);
            string magicString = string.Format("Magic {0}/{1}", player.CurrentMagic, player.MaxMagic);
            screen.PrintLine(magicString, StartingX + 2, 3, LineAlignment.Left);
            
            string fps = TCODSystem.FPS.ToString();
            screen.PrintLine(fps, 52, 58, LineAlignment.Left);
        }
    }
}
