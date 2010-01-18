﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using libtcodWrapper;
using Magecrawl.GameEngine.Interfaces;
using Magecrawl.GameUI.ListSelection;
using Magecrawl.GameUI.ListSelection.Requests;
using Magecrawl.GameUI.Map.Requests;
using Magecrawl.GameUI.MapEffects;
using Magecrawl.Utilities;

namespace Magecrawl.Keyboard.Magic
{
    internal class MagicListKeyboardHandler : BaseKeystrokeHandler
    {
        private class SpellAnimationHelper
        {
            private Point m_point;
            private ISpell m_spell;
            private IGameEngine m_engine;
            private GameInstance m_gameInstance;

            internal SpellAnimationHelper(Point point, ISpell spell, IGameEngine engine, GameInstance gameInstance)
            {
                m_point = point;
                m_spell = spell;
                m_engine = engine;
                m_gameInstance = gameInstance;
            }

            internal void Invoke()
            {
                m_gameInstance.TextBox.AddText(string.Format("{0} casts {1}.", m_engine.Player.Name, m_spell.Name));
                m_engine.PlayerCastSpell(m_spell, m_point);
                m_gameInstance.ResetHandlerName();
                m_gameInstance.UpdatePainters();
            }
        }

        private IGameEngine m_engine;
        private GameInstance m_gameInstance;

        private Dictionary<string, string> m_spellAttributes;

        // When we're brought up, get the keystroke used to call us, so we can use it to select target(s)
        private NamedKey m_keystroke;

        public MagicListKeyboardHandler(IGameEngine engine, GameInstance instance)
        {
            m_engine = engine;
            m_gameInstance = instance;
            m_spellAttributes = new Dictionary<string, string>();
            LoadSpellAttributes();
        }

        public override void NowPrimaried(object objOne, object objTwo, object objThree, object objFour)
        {
            m_keystroke = (NamedKey)objOne;
            m_gameInstance.SendPaintersRequest(new DisableAllOverlays());
            ListItemShouldBeEnabled magicSpellEnabledDelegate = s => m_engine.PlayerCouldCastSpell((ISpell)s);
            m_gameInstance.SendPaintersRequest(new ShowListSelectionWindow(true, m_engine.Player.Spells.OfType<INamedItem>().ToList(), "Spellbook", magicSpellEnabledDelegate));
            m_gameInstance.UpdatePainters();
        }

        public override void HandleKeystroke(NamedKey keystroke)
        {
            MethodInfo action;
            m_keyMappings.TryGetValue(keystroke, out action);
            if (action != null)
            {
                action.Invoke(this, null);
            }
            else if (keystroke.Code == libtcodWrapper.KeyCode.TCODK_CHAR)
            {
                m_gameInstance.SendPaintersRequest(new ListSelectionItemSelectedByChar(keystroke.Character, new ListItemSelected(SpellSelectedDelegate)));
            }
        }

        private void SpellSelectedDelegate(INamedItem spellName)
        {
            ISpell spell = (ISpell)spellName;
            if (!m_engine.PlayerCouldCastSpell(spell))
                return;

            m_gameInstance.SendPaintersRequest(new ShowListSelectionWindow(false));
            
            Color color = GetColorOfSpellFromSchool(spell);

            if (spell.TargetType.StartsWith("Single Range"))
            {
                // Get targetable points.
                string[] targetParts = spell.TargetType.Split(':');
                int range = int.Parse(targetParts[1]);
                int tailLength = 1;
                if (targetParts.Length > 2)
                    tailLength = int.Parse(targetParts[2]);

                List<EffectivePoint> targetablePoints = PointListUtils.EffectivePointListFromBurstPosition(m_engine.Player.Position, range);
                m_engine.FilterNotTargetablePointsFromList(targetablePoints, true);

                // Now we need to filter any point that we don't have a GenerateBlastListOfPoints'able line.
                targetablePoints.RemoveAll(x => !m_engine.IsRangedPathBetweenPoints(m_engine.Player.Position, x.Position));

                // Setup delegate to do action on target
                OnTargetSelection selectionDelegate = new OnTargetSelection(s =>
                {
                    if (m_engine.IsValidTargetForSpell(spell, s))
                    {
                        // Since we want animation to go first, setup helper to run that
                        SpellAnimationHelper rangedHelper = new SpellAnimationHelper(s, spell, m_engine, m_gameInstance);
                        List<Point> spellPath = m_engine.SpellCastDrawablePoints(spell, s);
                        EffectDone onEffectDone = new EffectDone(rangedHelper.Invoke);
                        bool drawLastFrame = m_spellAttributes.ContainsKey(spell.Name) && m_spellAttributes[spell.Name].Contains("DrawLastFrame");
                        m_gameInstance.SetHandlerName("Effects", new ShowRangedBolt(onEffectDone, spellPath, color, drawLastFrame, tailLength));
                        m_gameInstance.UpdatePainters();
                        return true;
                    }
                    return false;
                });
                m_gameInstance.SetHandlerName("Target", targetablePoints, selectionDelegate, m_keystroke, TargettingKeystrokeHandler.TargettingType.Monster);
            }
            else if (spell.TargetType == "Self")
            {
                m_engine.PlayerCastSpell(spell, m_engine.Player.Position);
                m_gameInstance.ResetHandlerName();
                m_gameInstance.UpdatePainters();
            }
            else
            {
                throw new System.ArgumentException("Don't know how to cast things with target type: " + spell.TargetType);
            }
        }

        private Color GetColorOfSpellFromSchool(ISpell spell)
        {
            switch (spell.School)
            {
                case "Light":
                    return ColorPresets.Wheat;
                case "Darkness":
                    return ColorPresets.DarkGray;
                case "Fire":
                    return ColorPresets.Firebrick;
                case "Arcane":
                    return ColorPresets.DarkViolet;
                case "Air":
                    return ColorPresets.LightBlue;
                case "Earth":
                    return ColorPresets.SaddleBrown;
                default:
                    return TCODColorPresets.White;
            }
        }

        private void Select()
        {
            m_gameInstance.SendPaintersRequest(new ListSelectionItemSelected(new ListItemSelected(SpellSelectedDelegate)));          
        }

        private void Escape()
        {
            m_gameInstance.SendPaintersRequest(new ShowListSelectionWindow(false));
            m_gameInstance.UpdatePainters();
            m_gameInstance.ResetHandlerName();
        }

        private void HandleDirection(Direction direction)
        {
            m_gameInstance.SendPaintersRequest(new ChangeListSelectionPosition(direction));
            m_gameInstance.UpdatePainters();
        }

        private void North()
        {
            HandleDirection(Direction.North);
        }

        private void South()
        {
            HandleDirection(Direction.South);
        }

        private void LoadSpellAttributes()
        {
            m_spellAttributes = new Dictionary<string, string>();

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            XmlReader reader = XmlReader.Create(new StreamReader(Path.Combine("Resources", "Spells.xml")), settings);
            reader.Read();  // XML declaration
            reader.Read();  // Items element
            if (reader.LocalName != "Spells")
            {
                throw new System.InvalidOperationException("Bad spells file");
            }
            while (true)
            {
                reader.Read();
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "Spells")
                    break;

                if (reader.LocalName == "Spell")
                {
                    string name = reader.GetAttribute("Name");
                    string attributes = reader.GetAttribute("DrawingAttributes");

                    if (attributes != null)
                        m_spellAttributes.Add(name, attributes);
                }
            }
            reader.Close();
        }
    }
}
