﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Magecrawl.GameEngine.Actors;
using Magecrawl.GameEngine.Interfaces;

namespace Magecrawl.GameEngine.Magic
{
    internal sealed class Spell : ISpell
    {
        private string m_name;
        private string m_effectType;
        private int m_cost;
        private string m_school;
        private TargetingInfo.TargettingType m_targettingType;
        private int m_range;

        internal Spell(string name, string school, string effectType, int cost, TargetingInfo.TargettingType targettingType, int range)
        {
            m_name = name;
            m_effectType = effectType;
            m_cost = cost;
            m_school = school;
            m_targettingType = targettingType;
            m_range = range;
        }

        public string Name
        {
            get
            {
                return m_name;
            }
        }

        public string DisplayName
        {
            get
            {
                return Name + '\t' + m_school + '\t' + "Mp: " + m_cost.ToString();
            }
        }

        public string School
        {
            get
            {
                return m_school;
            }
        }

        internal string EffectType
        {
            get
            {
                return m_effectType;
            }
        }

        internal int Cost
        {
            get
            {
                return m_cost;
            }
        }

        public TargetingInfo Targeting
        {
            get 
            {
                return new TargetingInfo(m_targettingType, m_range);
            }
        }
    }
}
