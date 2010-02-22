﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Magecrawl.GameEngine.Armor;
using Magecrawl.GameEngine.Interfaces;
using Magecrawl.GameEngine.Items;
using Magecrawl.GameEngine.Magic;
using Magecrawl.GameEngine.SaveLoad;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine.Actors
{
    internal sealed class Player : Character, IPlayer, IXmlSerializable
    {
        public int CurrentMP { get; internal set; }

        public int MaxMP { get; internal set; }

        public IArmor ChestArmor { get; internal set; }
        public IArmor Headpiece { get; internal set; }
        public IArmor Gloves { get; internal set; }
        public IArmor Boots { get; internal set; }

        private List<Item> m_itemList;

        public Player() : base()
        {
            m_itemList = null;
            CurrentMP = 0;
            MaxMP = 0;
        }

        public Player(Point p) : base((string)Preferences.Instance["PlayerName"], p, 12, 6)
        {
            m_itemList = new List<Item>();
            CurrentMP = 10;
            MaxMP = 10;
            m_itemList.Add(CoreGameEngine.Instance.ItemFactory.CreateItem("Minor Health Potion"));
            m_itemList.Add(CoreGameEngine.Instance.ItemFactory.CreateItem("Minor Health Potion"));
            m_itemList.Add(CoreGameEngine.Instance.ItemFactory.CreateItem("Minor Mana Potion"));
            m_itemList.Add(CoreGameEngine.Instance.ItemFactory.CreateItem("Camp Supplies"));
            m_itemList.Add(CoreGameEngine.Instance.ItemFactory.CreateItem("Camp Supplies"));
            m_itemList.Add(CoreGameEngine.Instance.ItemFactory.CreateItem("Wand Of Magic Missile"));
            m_itemList.Add(CoreGameEngine.Instance.ItemFactory.CreateItem("Wand Of Sparks"));            
            Equip(CoreGameEngine.Instance.ItemFactory.CreateItem("Wooden Cudgel"));
            Equip(CoreGameEngine.Instance.ItemFactory.CreateItem("Robe"));
            Equip(CoreGameEngine.Instance.ItemFactory.CreateItem("Wool Cap"));
            Equip(CoreGameEngine.Instance.ItemFactory.CreateItem("Sandles"));
            Equip(CoreGameEngine.Instance.ItemFactory.CreateItem("Wool Gloves"));
        }

        public IEnumerable<ISpell> Spells
        {
            get 
            {
                return new List<ISpell>() 
                {
                    SpellFactory.CreateSpell("Heal"), SpellFactory.CreateSpell("Zap"), SpellFactory.CreateSpell("Lightning Bolt"),
                    SpellFactory.CreateSpell("Haste"), SpellFactory.CreateSpell("Earthen Armor"), SpellFactory.CreateSpell("Light"),
                    SpellFactory.CreateSpell("Cone Of Cold")
                    /* SpellFactory.CreateSpell("False Life"),
                    SpellFactory.CreateSpell("Poison Bolt"), SpellFactory.CreateSpell("Poison Touch"),
                    SpellFactory.CreateSpell("Blink"), SpellFactory.CreateSpell("Teleport"), SpellFactory.CreateSpell("Slow")*/
                };
            }
        }

        // Returns amount actually healed by
        public int HealMP(int toHeal)
        {
            int previousMP = CurrentMP;
            CurrentMP = Math.Min(CurrentMP + toHeal, MaxMP);
            return CurrentMP - previousMP;
        }

        public IEnumerable<IItem> Items
        {
            get
            {
                return m_itemList.ConvertAll<IItem>(new System.Converter<Item, IItem>(delegate(Item i) { return i as IItem; }));
            }
        }

        public IEnumerable<string> StatusEffects
        {
            get
            {
                return m_affects.Select(a => a.Name).ToList();
            }
        }

        internal override IItem Equip(IItem item)
        {
            if (item is ChestArmor)
            {
                IItem previousArmor = ChestArmor;
                ChestArmor = (IArmor)item;
                return previousArmor;
            }
            if (item is Headpiece)
            {
                IItem previousArmor = Headpiece;
                Headpiece = (IArmor)item;
                return previousArmor;
            }
            if (item is Gloves)
            {
                IItem previousArmor = Gloves;
                Gloves = (IArmor)item;
                return previousArmor;
            }
            if (item is Boots)
            {
                IItem previousArmor = Boots;
                Boots = (IArmor)item;
                return previousArmor;
            }

            return base.Equip(item);
        }

        internal override IItem Unequip(IItem item)
        {
            if (item is ChestArmor)
            {
                IItem previousArmor = ChestArmor;
                ChestArmor = null;
                return previousArmor;
            }
            if (item is Headpiece)
            {
                IItem previousArmor = Headpiece;
                Headpiece = null;
                return previousArmor;
            }
            if (item is Gloves)
            {
                IItem previousArmor = Gloves;
                Gloves = null;
                return previousArmor;
            }
            if (item is Boots)
            {
                IItem previousArmor = Boots;
                Boots = null;
                return previousArmor;
            }

            return base.Unequip(item);
        }

        internal void TakeItem(Item i)
        {
            m_itemList.Add(i);
        }

        internal void RemoveItem(Item i)
        {
            m_itemList.Remove(i);
        }

        public override DiceRoll MeleeDamage
        {
            get
            {
                return new DiceRoll(1, 2);
            }
        }
        
        public override double MeleeSpeed
        {
            get
            {
                return 1.0;
            }
        }

        #region SaveLoad

        public override void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();
            base.ReadXml(reader);

            CurrentMP = reader.ReadElementContentAsInt();
            MaxMP = reader.ReadElementContentAsInt();

            ChestArmor = (IArmor)Item.ReadXmlEntireNode(reader, this);
            Headpiece = (IArmor)Item.ReadXmlEntireNode(reader, this);
            Gloves = (IArmor)Item.ReadXmlEntireNode(reader, this);
            Boots = (IArmor)Item.ReadXmlEntireNode(reader, this);

            m_itemList = new List<Item>();
            ReadListFromXMLCore readDelegate = new ReadListFromXMLCore(delegate
            {
                string typeString = reader.ReadElementContentAsString();
                Item newItem = CoreGameEngine.Instance.ItemFactory.CreateItem(typeString); 
                newItem.ReadXml(reader);
                m_itemList.Add(newItem);
            });
            ListSerialization.ReadListFromXML(reader, readDelegate);
            reader.ReadEndElement();
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Player");
            base.WriteXml(writer);

            writer.WriteElementString("CurrentMagic", CurrentMP.ToString());
            writer.WriteElementString("MaxMagic", MaxMP.ToString());

            Item.WriteXmlEntireNode((Item)ChestArmor, "ChestArmor", writer);
            Item.WriteXmlEntireNode((Item)Headpiece, "Headpiece", writer);
            Item.WriteXmlEntireNode((Item)Gloves, "Gloves", writer);
            Item.WriteXmlEntireNode((Item)Boots, "Boots", writer);

            ListSerialization.WriteListToXML(writer, m_itemList, "Items");
            writer.WriteEndElement();
        }

        #endregion
    }
}
