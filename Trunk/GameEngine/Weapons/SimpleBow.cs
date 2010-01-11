﻿using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using Magecrawl.GameEngine.Interfaces;
using Magecrawl.GameEngine.Items;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine.Weapons
{
    internal class SimpleBow : RangedWeaponBase
    {
        public SimpleBow(string name, DiceRoll damage, double ctCost, string description, string flavorText)
        {
            m_itemDescription = description;
            m_flavorText = flavorText;
            m_owner = null;
            m_name = name;
            m_damage = damage;
            m_ctCostToAttack = ctCost;
        }

        public override List<EffectivePoint> CalculateTargetablePoints()
        {
            const int SimpleBowRange = 10;
            const int SimpleBowMinRange = 3;
            const int SimpleBowFalloffStart = 4;
            const float SimpleBowFalloffAmount = .25f;

            List<EffectivePoint> targetablePoints = GenerateRangedTargetablePoints(SimpleBowRange, SimpleBowMinRange, SimpleBowFalloffStart, SimpleBowFalloffAmount);

            CoreGameEngine.Instance.FilterNotTargetablePointsFromList(targetablePoints, m_owner.Position, m_owner.Vision, true);

            return targetablePoints;
        }
    }
}
