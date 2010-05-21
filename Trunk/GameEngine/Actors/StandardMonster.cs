﻿using System;
using System.Collections.Generic;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine.Actors
{
    internal class StandardMonster : Monster
    {
        public StandardMonster(string name, Point p, int maxHP, int vision, DiceRoll damage, double evade, double ctIncreaseModifer, double ctMoveCost, double ctActCost, double ctAttackCost)
            : base(name, p, maxHP, vision, damage, evade, ctIncreaseModifer, ctMoveCost, ctActCost, ctAttackCost)
        {
        }

        public override void Action(CoreGameEngine engine)
        {
            DefaultAction(engine);
        }
    }
}
