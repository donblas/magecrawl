﻿using System;
using System.Collections.Generic;
using libtcodWrapper;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine.Interfaces
{
    public interface IWeapon : ITargetablePoints
    {
        DiceRoll Damage
        {
            get;
        }

        string Name
        {
            get;
        }

        bool PositionInTargetablePoints(Point pointOfInterest);
        float EffectiveStrengthAtPoint(Point pointOfInterest);
    }
}
