﻿using System.Text;
using System.Collections.Generic;
using libtcodWrapper;
using Magecrawl.GameEngine.Interfaces;

namespace Magecrawl.GameUI
{
    public class CharacterInfo
    {
        public CharacterInfo()
        {
        }

        private const int StartingX = UIHelper.MapWidth;
        private const int InfoWidth = UIHelper.CharInfoWidth;
        private const int InfoHeight = UIHelper.CharInfoHeight;

        private const int ScreenCenter = StartingX + (InfoWidth / 2);
        public void Draw(Console screen, IPlayer player)
        {
            screen.DrawFrame(StartingX, 0, InfoWidth, InfoHeight, true);
            screen.PrintLine(player.Name, ScreenCenter, 1, LineAlignment.Center);

            string hpString = string.Format("HP: {0}/{1}", player.CurrentHP, player.MaxHP);
            screen.PrintLine(hpString, StartingX + 2, 2, LineAlignment.Left);
            string magicString = string.Format("Magic {0}/{1}", player.CurrentMP, player.MaxMP);
            screen.PrintLine(magicString, StartingX + 2, 3, LineAlignment.Left);

            string weaponString = string.Format("Weapon: {0}", player.CurrentWeapon.Name);
            screen.PrintLine(weaponString, StartingX + 2, 4, LineAlignment.Left);

            weaponString = string.Format("Damage: {0} damage", player.CurrentWeapon.Damage.ToString());
            screen.PrintLine(weaponString, StartingX + 2, 5, LineAlignment.Left);

            screen.PrintLine("Status Effects:", StartingX + 2, 7, LineAlignment.Left);
            StringBuilder statusEffects = new StringBuilder();
            foreach (string s in player.StatusEffects)
            {
                statusEffects.Append(s + " ");
            }

            // TODO - What happens if this is more then 2 lines worth?
            screen.PrintLineRect(statusEffects.ToString(), StartingX + 2, 8, InfoWidth - 4, 2, LineAlignment.Left);
            
            string fps = TCODSystem.FPS.ToString();
            screen.PrintLine(fps, 52, 58, LineAlignment.Left);
        }
    }
}
