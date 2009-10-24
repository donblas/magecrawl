﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Magecrawl.GameEngine.Actors;
using Magecrawl.GameEngine.Interfaces;

namespace Magecrawl.GameEngine.Magic
{
    internal abstract class SpellBase
    {
        internal abstract void Cast(Character caster, CoreGameEngine gameEngine, CombatEngine combatEngine);

        internal virtual int Damage
        {
            get
            {
                return 0;
            }
        }

        internal virtual int MagicCost
        {
            get
            {
                return 0;
            }
        }
    }
}
