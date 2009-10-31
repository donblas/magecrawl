﻿using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using Magecrawl.GameEngine.Interfaces;
using Magecrawl.GameEngine.Items;
using Magecrawl.GameEngine.Magic;
using Magecrawl.GameEngine.SaveLoad;
using Magecrawl.GameEngine.Weapons;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine.Actors
{
    internal sealed class Player : Character, Interfaces.IPlayer, IXmlSerializable
    {
        private IWeapon m_equipedWeapon;
        private List<Item> m_itemList;

        public Player() : base()
        {
            m_equipedWeapon = null;
            m_itemList = null;
        }

        public Player(int x, int y) : base(x, y, 10, 10, 6, 10, 10, "Donblas")
        {
            m_itemList = new List<Item>();
        }

        internal IWeapon EquipWeapon(IWeapon weapon)
        {
            if (weapon == null)
                throw new System.ArgumentException("EquipWeapon - Null weapon");
            WeaponBase currentWeapon = weapon as WeaponBase;
            currentWeapon.Owner = this;

            IWeapon oldWeapon = UnequipWeapon();
            m_equipedWeapon = currentWeapon;
            return oldWeapon;
        }

        internal IWeapon UnequipWeapon()
        {
            if (m_equipedWeapon == null)
                return null;
            WeaponBase oldWeapon = m_equipedWeapon as WeaponBase;
            oldWeapon.Owner = null;
            return oldWeapon;
        }

        public override IWeapon CurrentWeapon               
        {
            get
            {
                if (m_equipedWeapon == null)
                    return new MeleeWeapon(this);
                return m_equipedWeapon;
            }
        }

        public IList<ISpell> Spells
        {
            get 
            {
                return new List<ISpell>() { SpellFactory.CreateSpell("Heal"), SpellFactory.CreateSpell("Blast"), SpellFactory.CreateSpell("Zap") };
            }
        }

        public IList<IItem> Items
        {
            get
            {
                return m_itemList.ConvertAll<IItem>(new System.Converter<Item, IItem>(delegate(Item i) { return i as IItem; }));
            }
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

        #region SaveLoad

        public override void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();
            base.ReadXml(reader);

            reader.ReadStartElement();
            string equipedWeaponTypeString = reader.ReadElementString();
            if (equipedWeaponTypeString == "None")
            {
                m_equipedWeapon = null;
            }
            else
            {
                Item loadedWeapon = CoreGameEngine.Instance.ItemFactory.CreateItem(equipedWeaponTypeString);
                loadedWeapon.ReadXml(reader);
                m_equipedWeapon = (IWeapon)loadedWeapon;
            }
            reader.ReadEndElement();

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

            writer.WriteStartElement("EquipedWeapon");
            Item itemToSave = m_equipedWeapon as Item;
            if (itemToSave != null)
                itemToSave.WriteXml(writer);
            else
                writer.WriteElementString("Type", "None");
            writer.WriteEndElement();

            ListSerialization.WriteListToXML(writer, m_itemList, "Items");
            writer.WriteEndElement();
        }

        #endregion
    }
}
