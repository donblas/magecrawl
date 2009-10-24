﻿using System;
using System.Collections.Generic;
using Magecrawl.GameEngine.Interfaces;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine.Weapons
{
    internal class SimpleBow : WeaponBase
    {   
        internal SimpleBow(ICharacter owner)
        {
            m_owner = owner;
        }

        public override DiceRoll Damage
        {
            get 
            {
                return new DiceRoll(1, 2, 1);
            }
        }

        public override string Name
        {
            get 
            {
                return "Bow";
            }
        }

        public override List<WeaponPoint> CalculateTargetablePoints()
        {
            List<WeaponPoint> targetablePoints = new List<WeaponPoint>();

            const int SimpleBowRange = 6;
            for (int i = -SimpleBowRange; i <= SimpleBowRange; ++i)
            {
                for (int j = -SimpleBowRange; j <= SimpleBowRange; ++j)
                {
                    int distance = System.Math.Abs(i) + Math.Abs(j);
                    bool allowable = (distance <= SimpleBowRange) && (distance > 2);
                    float weaponStrength = 1.0f - (Math.Max(distance - 4, 0) * .25f);
                    if (allowable)
                        targetablePoints.Add(new WeaponPoint(new Point(m_owner.Position.X + i, m_owner.Position.Y + j), weaponStrength));
                }
            }

            CoreGameEngine.Instance.FilterNotTargetablePointsFromList(targetablePoints, m_owner.Position, m_owner.Vision);
            
            return targetablePoints;
        }
    }
}
