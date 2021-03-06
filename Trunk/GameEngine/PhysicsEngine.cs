using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Magecrawl.Actors;
using Magecrawl.EngineInterfaces;
using Magecrawl.GameEngine.Magic;
using Magecrawl.GameEngine.Physics;
using Magecrawl.Interfaces;
using Magecrawl.Items;
using Magecrawl.Maps;
using Magecrawl.Maps.MapObjects;
using Magecrawl.StatusEffects.Interfaces;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine
{
    internal sealed class PhysicsEngine
    {
        private CoreTimingEngine m_timingEngine;
        private FOVManager m_fovManager;
        private CombatEngine m_combatEngine;
        private MagicEffectsEngine m_magicEffects;

        // Fov FilterNotMovablePointsFromList
        private Dictionary<Point, bool> m_movableHash;

        // Cared and fed by CoreGameEngine, local copy for convenience
        private Player m_player;
        private Map m_map;

        public PhysicsEngine(Player player, Map map)
        {
            m_player = player;
            m_map = map;
            m_timingEngine = new CoreTimingEngine();
            m_fovManager = new FOVManager(this, map);
            m_combatEngine = new CombatEngine(this, player, map);
            m_movableHash = new Dictionary<Point, bool>(PointEqualityComparer.Instance);
            m_magicEffects = new MagicEffectsEngine(this, m_combatEngine);
            UpdatePlayerVisitedStatus();
        }

        internal CombatEngine CombatEngine
        {
            get { return m_combatEngine; }
        }

        internal FOVManager FOVManager
        {
            get
            {
                return m_fovManager;
            }
        }

        internal void NewMapPlayerInfo(Player player, Map map)
        {
            m_player = player;
            m_map = map;
            m_combatEngine.NewMapPlayerInfo(player, map);

            // We have a new map, recalc LOS with a new map
            m_fovManager.UpdateNewMap(this, m_map);
            UpdatePlayerVisitedStatus();
        }

        // This needs to be really _fast_. We're going to stick the not moveable points in a has table,
        // then compare each pointList to the terrian and if still good see if in hash table
        // Please keep in sync with Point version below
        public void FilterNotTargetablePointsFromList(List<EffectivePoint> pointList, Point characterPosition, int visionRange, bool needsToBeVisible)
        {
            if (pointList == null)
                return;

            m_fovManager.CalculateForMultipleCalls(m_map, characterPosition, visionRange);
            m_movableHash.Clear();

            foreach (MapObject obj in m_map.MapObjects)
            {
                if (obj.IsSolid)
                    m_movableHash[obj.Position] = true;
            }

            // Remove it if it's not on map, or is wall, or same square as something solid from above, is it's not visible.
            pointList.RemoveAll(point => 
                !m_map.IsPointOnMap(point.Position) || 
                m_map.GetTerrainAt(point.Position) == TerrainType.Wall || 
                m_movableHash.ContainsKey(point.Position) ||
                (needsToBeVisible && !m_fovManager.Visible(point.Position)));
        }

        // This needs to be really _fast_, and so we're going to violate a rule of duplication. 
        // Please keep in sync with EffectPoint version above
        public void FilterNotTargetablePointsFromList(List<Point> pointList, Point characterPosition, int visionRange, bool needsToBeVisible)
        {
            if (pointList == null)
                return;

            m_fovManager.CalculateForMultipleCalls(m_map, characterPosition, visionRange);
            m_movableHash.Clear();

            foreach (MapObject obj in m_map.MapObjects)
            {
                if (obj.IsSolid)
                    m_movableHash[obj.Position] = true;
            }

            // Remove it if it's not on map, or is wall, or same square as something solid from above, is it's not visible.
            pointList.RemoveAll(point =>
                !m_map.IsPointOnMap(point) ||
                m_map.GetTerrainAt(point) == TerrainType.Wall ||
                m_movableHash.ContainsKey(point) ||
                (needsToBeVisible && !m_fovManager.Visible(point)));
        }

        // This is a slow operation. It should not be called multiple times in a row!
        // Call CalculateMoveablePointGrid instead~!
        public bool IsMovablePointSingleShot(Map map, Point p)
        {
            // If it's not a floor, it's not movable
            if (map.GetTerrainAt(p) != TerrainType.Floor)
                return false;

            // If there's a map object there that is solid, it's not movable
            if (map.MapObjects.SingleOrDefault(m => m.Position == p && m.IsSolid) != null)
                return false;

            // If there's a monster there, it's not movable
            if (map.Monsters.SingleOrDefault(m => m.Position == p) != null)
                return false;

            // If the player is there, it's not movable
            if (m_player.Position == p)
                return false;

            return true;
        }

        public TileVisibility[,] CalculateTileVisibility()
        {
            TileVisibility[,] visibilityArray = new TileVisibility[m_map.Width, m_map.Height];

            m_fovManager.CalculateForMultipleCalls(m_map, m_player.Position, m_player.Vision);

            for (int i = 0; i < m_map.Width; ++i)
            {
                for (int j = 0; j < m_map.Height; ++j)
                {
                    Point p = new Point(i, j);
                    if (m_fovManager.Visible(p))
                    {
                        visibilityArray[i, j] = TileVisibility.Visible;
                    }
                    else
                    {
                        if (m_map.IsVisitedAt(p))
                            visibilityArray[i, j] = TileVisibility.Visited;
                        else
                            visibilityArray[i, j] = TileVisibility.Unvisited;
                    }
                }
            }
            return visibilityArray;
        }

        internal bool Move(Character c, Direction direction)
        {
            bool didAnything = false;
            Point newPosition = PointDirectionUtils.ConvertDirectionToDestinationPoint(c.Position, direction);
            if (m_map.IsPointOnMap(newPosition) && IsMovablePointSingleShot(m_map, newPosition))
            {
                c.Position = newPosition;
                m_timingEngine.ActorMadeMove(c);
                didAnything = true;
            }
            return didAnything;
        }

        internal bool WarpToPosition(Character c, Point p)
        {
            c.Position = p;
            return true;
        }

        internal List<Point> TargettedDrawablePoints(object targettingObject, Point target)
        {
            Spell asSpell = targettingObject as Spell;
            if (asSpell != null)
                return m_magicEffects.TargettedDrawablePoints(asSpell.Targeting, m_player.SpellStrength(asSpell.School), target);

            Item asItem = targettingObject as Item;
            if (asItem != null)
                return m_magicEffects.TargettedDrawablePoints(asItem.GetAttribute("InvokeSpellEffect"), int.Parse(asItem.GetAttribute("CasterLevel"), CultureInfo.InvariantCulture), target);

            return null;  
        }

        public bool IsRangedPathBetweenPoints(Point x, Point y)
        {
            return GenerateRangedAttackListOfPoints(m_map, x, y) != null;
        }

        internal List<Point> GenerateRangedAttackListOfPoints(Map map, Point attcker, Point target)
        {
            return RangedAttackPathfinder.RangedListOfPoints(map, attcker, target, false, false);
        }

        internal List<Point> GenerateBlastListOfPoints(Map map, Point caster, Point target, bool bounceOffWalls)
        {
            return RangedAttackPathfinder.RangedListOfPoints(map, caster, target, true, bounceOffWalls);
        }

        // So if we're previewing the position of a blast, and we can't see the wall we're going to bounce
        // off of, don't show the bounce at all...
        internal List<Point> GenerateBlastListOfPointsShowBounceIfSeeWall(Map map, ICharacter caster, Point target)
        {
            Point wallPosition = RangedAttackPathfinder.GetWallHitByBlast(map, caster.Position, target);
            if (m_fovManager.VisibleSingleShot(map, caster.Position, caster.Vision, wallPosition))
                return RangedAttackPathfinder.RangedListOfPoints(map, caster.Position, target, true, true);
            else
                return RangedAttackPathfinder.RangedListOfPoints(map, caster.Position, target, true, false);
        }

        private void UpdatePlayerVisitedStatus()
        {
            m_fovManager.CalculateForMultipleCalls(m_map, m_player.Position, m_player.Vision);

            // Only need Vision really, but to catch off by one errors and such, make it bigger
            // We're doing this instead of all cells for performance anyway
            int minX = m_player.Position.X - (m_player.Vision * 2);
            int minY = m_player.Position.Y - (m_player.Vision * 2);
            int maxX = m_player.Position.X + (m_player.Vision * 2);
            int maxY = m_player.Position.Y + (m_player.Vision * 2);

            for (int i = minX; i < maxX; ++i)
            {
                for (int j = minY; j < maxY; ++j)
                {
                    Point p = new Point(i, j);
                    if (m_map.IsPointOnMap(p) && m_fovManager.Visible(p))
                        m_map.SetVisitedAt(p, true);
                }
            }
        }

        public bool PlayerGetItem()
        {
            Pair<Item, Point> itemToPickup = m_map.InternalItems.Where(i => i.Second == m_player.Position).FirstOrDefault();
            if (itemToPickup != null)
            {
                m_map.RemoveItem(itemToPickup);
                m_player.TakeItem(itemToPickup.First);
                m_timingEngine.ActorDidAction(m_player);
                CoreGameEngine.Instance.SendTextOutput(string.Format("Picked up a {0}.", itemToPickup.First.DisplayName));
                return true;
            }

            return false;
        }

        public bool PlayerGetItem(IItem item)
        {
            List<Pair<Item, Point>> items = m_map.InternalItems.Where(i => i.Second == m_player.Position).ToList();
            Pair<Item, Point> itemPair = items.Where(i => i.First == item).FirstOrDefault();
            if (itemPair != null)
            {
                m_map.RemoveItem(itemPair);
                m_player.TakeItem(itemPair.First);
                m_timingEngine.ActorDidAction(m_player);
                CoreGameEngine.Instance.SendTextOutput(string.Format("Picked up a {0}.", itemPair.First.DisplayName));
                return true;
            }
            return false;
        }

        public bool PlayerDropItem(Item item)
        {
            if (m_player.Items.Contains(item))
            {
                m_map.AddItem(new Pair<Item, Point>(item, m_player.Position));
                m_player.RemoveItem(item);
                return true;
            }
            return false;
        }

        public bool DangerPlayerInLOS()
        {
            FOVManager.CalculateForMultipleCalls(m_map, m_player.Position, m_player.Vision);

            foreach (Monster m in m_map.Monsters)
            {
                if (FOVManager.Visible(m.Position))
                    return true;
            }
            
            return false;
        }

        public bool CurrentOrRecentDanger()
        {
            const int TurnsMonsterOutOfLOSToBeSafe = 8;
            bool dangerInLOS = DangerPlayerInLOS();
            if (dangerInLOS)
            {
                m_player.LastTurnSeenAMonster = CoreGameEngine.Instance.TurnCount;
                return true;
            }
            
            // This is wrong - BUG 225
            if (m_player.LastTurnSeenAMonster + TurnsMonsterOutOfLOSToBeSafe > CoreGameEngine.Instance.TurnCount)
                return true;

            if (m_player.Effects.Any(x => !x.IsPositiveEffect))
                return true;

            return false;
        }

        public bool UseItemWithEffect(Item item, Point targetedPoint)
        {
            if (m_player.Items.Contains(item))
            {
                bool itemUsedSucessfully = m_magicEffects.UseItemWithEffect(m_player, item, targetedPoint);
                if (itemUsedSucessfully)
                {
                    int currentCharges = int.Parse(item.GetAttribute("Charges"), CultureInfo.InvariantCulture) - 1;
                    if (currentCharges <= 0)
                    {
                        m_player.RemoveItem((Item)item);
                        if (item.GetAttribute("Type") == "Wand")
                            CoreGameEngine.Instance.SendTextOutput(string.Format("The {0} disintegrates as its last bit of magic is wrested from it.", item.DisplayName));
                    }
                    else
                    {
                        item.SetExistentAttribute("Charges", currentCharges.ToString());
                    }
                }
                return itemUsedSucessfully;
            }
            return false;
        }

        public bool Operate(Character characterOperating, Point pointToOperateAt)
        {
            // We can't operate if anyone is at that location.
            if (m_combatEngine.FindTargetAtPosition(pointToOperateAt) != null)
                return false;

            OperableMapObject operateObj = m_map.MapObjects.OfType<OperableMapObject>().SingleOrDefault(x => x.Position == pointToOperateAt);
            if (operateObj != null)
            {                
                operateObj.Operate(characterOperating);
                m_timingEngine.ActorDidAction(characterOperating);
                return true;
            }
            return false;
        }

        internal bool Wait(Character c)
        {
            m_timingEngine.ActorDidAction(c);
            return true;
        }

        internal bool Attack(Character attacker, Point target)
        {
            bool didAnything = m_combatEngine.Attack(attacker, target);
            if (didAnything)
                m_timingEngine.ActorDidWeaponAttack(attacker);
            return didAnything;
        }

        internal bool CastSpell(Player caster, Spell spell, Point target)
        {
            bool didAnything = m_magicEffects.CastSpell(caster, spell, target);
            if (didAnything)
                m_timingEngine.ActorDidAction(caster);
            return didAnything;
        }

        internal bool ReloadWeapon(Character character)
        {
            if (!character.CurrentWeapon.IsRanged)
                throw new InvalidOperationException("ReloadWeapon on non-ranged weapon?");
            ((Weapon)character.CurrentWeapon).LoadWeapon();
            m_timingEngine.ActorDidMinorAction(character);
            return true;
        }

        internal bool PlayerMoveUpStairs(Player player, Map map)
        {
            Stairs s = map.MapObjects.OfType<Stairs>().Where(x => x.Type == MapObjectType.StairsUp && x.Position == player.Position).SingleOrDefault();

            if (s != null)
            {
                // The position must come first, as changing levels checks FOV
                m_player.Position = StairsMapping.Instance.GetMapping(s.UniqueID);
                CoreGameEngine.Instance.CurrentLevel--;
                
                m_timingEngine.ActorMadeMove(m_player);
                return true;
            }
            return false;
        }

        internal bool PlayerMoveDownStairs(Player player, Map map)
        {
            Stairs s = map.MapObjects.OfType<Stairs>().Where(x => x.Type == MapObjectType.StairsDown && x.Position == player.Position).SingleOrDefault();
            if (s != null)
            {
                if (CoreGameEngine.Instance.CurrentLevel == CoreGameEngine.Instance.NumberOfLevels - 1)
                    throw new InvalidOperationException("Win dialog should have come up instead.");
                
                // The position must come first, as changing levels checks FOV
                m_player.Position = StairsMapping.Instance.GetMapping(s.UniqueID);
                CoreGameEngine.Instance.CurrentLevel++;

                m_timingEngine.ActorMadeMove(m_player);
                return true;
            }
            return false;
        }

        // Called by PublicGameEngine after any call that could pass time.
        internal void BeforePlayerAction(CoreGameEngine engine)
        {
            UpdatePlayerVisitedStatus();
        }

        // Called by PublicGameEngine after any call to CoreGameEngine which passes time.
        internal void AfterPlayerAction(CoreGameEngine engine)
        {
            UpdatePlayerVisitedStatus();

            // Regenerate health mana if out of combat for a bit.
            const int NumberOfRoundsUntilHealthFull = 20;
            if (!CurrentOrRecentDanger())
            {
                // Always heal at least 1 point if we're not topped off. Thanks integer math!
                int hpToHeal = m_player.MaxHP / NumberOfRoundsUntilHealthFull;               
                if (m_player.CurrentHP < m_player.MaxHP && hpToHeal <= 0)
                    hpToHeal = 1;
                int mpToHeal = m_player.MaxMP / NumberOfRoundsUntilHealthFull;
                if (m_player.CurrentMP < m_player.MaxMP && mpToHeal <= 0)
                    mpToHeal = 1;

                bool magicalHeal = m_player.HasAttribute("Regeneration");
                m_player.Heal(hpToHeal, magicalHeal);
                m_player.GainMP(mpToHeal);
            }
            
            // Until the player gets a turn
            while (true)
            {
                Character nextCharacter = m_timingEngine.GetNextActor(m_player, m_map);
                if (nextCharacter is Player)
                    return;

                Monster monster = nextCharacter as Monster; 
                monster.Action(CoreGameEngineInstance.Instance);
            }
        }

        internal ILongTermStatusEffect GetLongTermEffectSpellWouldProduce(string effectName)
        {
            return m_magicEffects.GetLongTermEffectSpellWouldProduce(effectName);
        }

        internal bool HandleItemAction(IItem item, string option, object argument)
        {
            // Some actions take longer than 1 turn, so spend the extra time here
            switch (option)
            {
                case "Equip":
                case "Equip as Secondary":
                case "Unequip":
                case "Unequip as Secondary":
                {
                    m_timingEngine.ActorDidAction(m_player);
                    m_timingEngine.ActorDidAction(m_player);
                    m_timingEngine.ActorDidAction(m_player);
                    break;
                }
            }

            switch (option)
            {
                case "Drop":
                {
                    return PlayerDropItem(item as Item);
                }
                case "Equip":
                {
                    // This probally should live in the player code
                    m_player.RemoveItem(item as Item);
                    Item oldWeapon = m_player.Equip(item) as Item;
                    if (oldWeapon != null)
                        m_player.TakeItem(oldWeapon);
                    return true;
                }
                case "Equip as Secondary":
                {
                    // This probally should live in the player code
                    m_player.RemoveItem(item as Item);
                    Item oldWeapon = m_player.EquipSecondaryWeapon(item as IWeapon) as Item;
                    if (oldWeapon != null)
                        m_player.TakeItem(oldWeapon);
                    return true;
                }
                case "Unequip":
                {
                    Item oldWeapon = m_player.Unequip(item) as Item;
                    if (oldWeapon != null)
                        m_player.TakeItem(oldWeapon);
                    return true;
                }
                case "Unequip as Secondary":
                {
                    Item oldWeapon = m_player.UnequipSecondaryWeapon() as Item;
                    if (oldWeapon != null)
                        m_player.TakeItem(oldWeapon);
                    return true;
                }
                case "Drink":
                case "Read":
                case "Zap":
                {
                    return UseItemWithEffect((Item)item, (Point)argument);
                }
                default:
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
