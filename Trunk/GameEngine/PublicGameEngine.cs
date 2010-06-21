using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Magecrawl.GameEngine.Actors;
using Magecrawl.GameEngine.Effects;
using Magecrawl.Interfaces;
using Magecrawl.GameEngine.Items;
using Magecrawl.GameEngine.Magic;
using Magecrawl.GameEngine.MapObjects;
using Magecrawl.GameEngine.Skills;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine
{
    // So in the current archtecture, each public method should do the action requested,
    // and _then_ call the CoreTimingEngine somehow to let others have their time slice before returning
    // This is very synchronous, but easy to do.
    [Export (typeof(IGameEngine))]
    public class PublicGameEngine : IGameEngine
    {
        private CoreGameEngine m_engine;

        public event TextOutputFromGame TextOutputEvent;
        public event PlayerDied PlayerDiedEvent;
        public event RangedAttack RangedAttackEvent;

        public PublicGameEngine()
        {
            SetupCoreGameEngine();
        }

        public void CreateNewWorld(string playerName)
        {
            m_engine.CreateNewWorld(playerName);
        }

        public void LoadSaveFile(string saveGameName)
        {
            m_engine.LoadSaveFile(saveGameName);
        }

        private void SetupCoreGameEngine()
        {
            // This is a singleton accessable from anyone in GameEngine, but stash a copy since we use it alot
            m_engine = new CoreGameEngine();
            m_engine.TextOutputEvent += new TextOutputFromGame(s => TextOutputEvent(s));
            m_engine.PlayerDiedEvent += new PlayerDied(() => PlayerDiedEvent());
            m_engine.RangedAttackEvent += new RangedAttack((a, type, d, targetAtEnd) => RangedAttackEvent(a, type, d, targetAtEnd));
        }

        public void Dispose()
        {
            if (m_engine != null)
                m_engine.Dispose();
            m_engine = null;
        }

        public Point TargetSelection
        {
            get;
            set;
        }

        public bool SelectingTarget
        {
            get;
            set;
        }

        public IPlayer Player
        {
            get
            {
                return m_engine.Player;
            }
        }

        public IMap Map
        {
            get
            {
                return m_engine.Map;
            }
        }

        public int CurrentLevel
        {
            get
            {
                return m_engine.CurrentLevel;
            }
        }

        public int TurnCount 
        {
            get
            {
                return m_engine.TurnCount;
            }
        }

        public bool MovePlayer(Direction direction)
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.Move(m_engine.Player, direction);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public bool Operate(Point pointToOperateAt)
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.Operate(m_engine.Player, pointToOperateAt);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public bool PlayerWait()
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.Wait(m_engine.Player);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public bool PlayerAttack(Point target)
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.Attack(m_engine.Player, target);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;            
        }

        public bool ReloadWeapon()
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.ReloadWeapon(m_engine.Player);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public bool PlayerCouldCastSpell(ISpell spell)
        {
            return m_engine.Player.CurrentMP >= ((Spell)spell).Cost;
        }

        public bool PlayerCastSpell(ISpell spell, Point target)
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.CastSpell(m_engine.Player, (Spell)spell, target);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public bool PlayerGetItem()
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.PlayerGetItem();
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;            
        }

        public bool PlayerGetItem(IItem item)
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.PlayerGetItem(item);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;      
        }

        public void Save()
        {
            m_engine.Save();
        }

        public List<Point> PlayerPathToPoint(Point dest)
        {
            return m_engine.PathToPoint(m_engine.Player, dest, true, true, false);
        }

        // For the IsPathable debugging mode, show if player could walk there.
        public bool[,] PlayerMoveableToEveryPoint()
        {
            return m_engine.PlayerMoveableToEveryPoint();
        }

        public List<Point> CellsInPlayersFOV()
        {
            return GenerateFOVListForCharacter(m_engine.Player);
        }

        private List<Point> GenerateFOVListForCharacter(ICharacter c)
        {
            List<Point> returnList = new List<Point>();

            m_engine.FOVManager.CalculateForMultipleCalls(m_engine.Map, c.Position, c.Vision);

            for (int i = 0; i < m_engine.Map.Width; ++i)
            {
                for (int j = 0; j < m_engine.Map.Height; ++j)
                {
                    Point currentPosition = new Point(i, j);
                    if (m_engine.FOVManager.Visible(currentPosition))
                    {
                        returnList.Add(currentPosition);
                    }
                }
            }
            return returnList;
        }

        public Dictionary<ICharacter, List<Point>> CellsInAllMonstersFOV()
        {
            Dictionary<ICharacter, List<Point>> returnValue = new Dictionary<ICharacter, List<Point>>();

            foreach (ICharacter c in m_engine.Map.Monsters)
            {
                returnValue[c] = GenerateFOVListForCharacter(c);
            }

            return returnValue;
        }

        public TileVisibility[,] CalculateTileVisibility()
        {
            return m_engine.CalculateTileVisibility();
        }

        public bool PlayerSwapPrimarySecondaryWeapons()
        {            
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.SwapPrimarySecondaryWeapons(m_engine.Player);
            if (didAnything)
            {
                m_engine.SendTextOutput("Weapons Swapped");
                m_engine.AfterPlayerAction();
            }
            return didAnything;
        }

        public List<ItemOptions> GetOptionsForInventoryItem(IItem item)
        {
            return m_engine.GetOptionsForInventoryItem(item as Item);
        }

        public List<ItemOptions> GetOptionsForEquipmentItem(IItem item)
        {
            return m_engine.GetOptionsForEquipmentItem(item as Item);
        }

        public TargetingInfo GetTargettingTypeForInventoryItem(IItem item, string action)
        {
            if (action == "Drop")
                return null;

            ItemWithEffects itemWithEffects = item as ItemWithEffects;
            if (itemWithEffects != null)
                return itemWithEffects.Spell.Targeting;

            return null;
        }

        public bool PlayerSelectedItemOption(IItem item, string option, object argument)
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.PlayerSelectedItemOption(item, option, argument);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public void FilterNotTargetablePointsFromList(List<EffectivePoint> pointList, bool needsToBeVisible)
        {
            m_engine.FilterNotTargetablePointsFromList(pointList, m_engine.Player.Position, m_engine.Player.Vision, needsToBeVisible);
        }

        public void FilterNotVisibleBothWaysFromList(List<EffectivePoint> pointList, bool savePlayerPositionFromList)
        {
            if (savePlayerPositionFromList)
                m_engine.FilterNotVisibleBothWaysFromList(Player.Position, pointList, CoreGameEngine.Instance.Player.Position);
            else
                m_engine.FilterNotVisibleBothWaysFromList(Player.Position, pointList, Point.Invalid);
        }

        public List<Point> TargettedDrawablePoints(object targettingObject, Point target)
        {
            return m_engine.TargettedDrawablePoints(targettingObject, target);
        }

        public bool IsRangedPathBetweenPoints(Point x, Point y)
        {
            return m_engine.IsRangedPathBetweenPoints(x, y);
        }

        public bool PlayerMoveDownStairs()
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.PlayerMoveDownStairs();
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public bool PlayerMoveUpStairs()
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.PlayerMoveUpStairs();
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public StairMovmentType IsStairMovementSpecial(bool headingUp)
        {
            Stairs s = m_engine.Map.MapObjects.OfType<Stairs>().Where(x => x.Position == m_engine.Player.Position).SingleOrDefault();
            if (s != null)
            {
                if (s.Type == MapObjectType.StairsUp && m_engine.CurrentLevel == 0 && headingUp)
                    return StairMovmentType.QuitGame;
                else if (s.Type == MapObjectType.StairsDown && m_engine.CurrentLevel == (m_engine.NumberOfLevels - 1) && !headingUp)
                    return StairMovmentType.WinGame;
            }
            return StairMovmentType.None;
        }

        public bool DangerInLOS()
        {
            return m_engine.DangerPlayerInLOS();
        }

        public bool CurrentOrRecentDanger()
        {
            return m_engine.CurrentOrRecentDanger();
        }

        public List<ICharacter> MonstersInPlayerLOS()
        {
            return m_engine.MonstersInPlayerLOS();
        }

        public List<string> GetDescriptionForTile(Point p)
        {
            if (!m_engine.FOVManager.VisibleSingleShot(m_engine.Map, m_engine.Player.Position, m_engine.Player.Vision, p))
                return new List<string>();

            List<string> descriptionList = new List<string>();
            if (m_engine.Player.Position == p)
                descriptionList.Add(m_engine.Player.Name);

            ICharacter monsterAtLocation = m_engine.Map.Monsters.Where(monster => monster.Position == p).FirstOrDefault();
            if (monsterAtLocation != null)
                descriptionList.Add(monsterAtLocation.Name);

            IMapObject mapObjectAtLocation = m_engine.Map.MapObjects.Where(mapObject => mapObject.Position == p).FirstOrDefault();
            if (mapObjectAtLocation != null)
                descriptionList.Add(mapObjectAtLocation.Name);

            foreach (Pair<IItem, Point> i in m_engine.Map.Items.Where(i => i.Second == p))
                descriptionList.Add(i.First.DisplayName);

            if (descriptionList.Count == 0)
                descriptionList.Add(m_engine.Map.GetTerrainAt(p).ToString());
            return descriptionList;
        }

        public ISkill GetSkillFromName(string name)
        {
            return SkillFactory.CreateSkill(name);
        }

        public void AddSkillToPlayer(ISkill skill)
        {
            if (m_engine.Player.SkillPoints < skill.Cost)
                throw new System.InvalidOperationException("AddSkillToPlayer without enough SP");
            m_engine.Player.SkillPoints -= skill.Cost;
            m_engine.Player.AddSkill(skill);
        }

        public void DismissEffect(string effectName)
        {
            StatusEffect effectInQuestion = m_engine.Player.Effects.FirstOrDefault(e => e.DisplayName == effectName);
            if (effectInQuestion == null)
                throw new System.InvalidOperationException("Trying to DismissEffect " + effectName + " and can not find.");
            if (!effectInQuestion.IsPositiveEffect)
                throw new System.InvalidOperationException("Trying to DismissEffect a non-positive effect");
            m_engine.BeforePlayerAction();
            effectInQuestion.Dismiss();
            m_engine.Wait(m_engine.Player); // Waiting passes time, which we want dismissing effects to take
            m_engine.AfterPlayerAction();
        }

        // This is a catch all debug request interface, used for debug menus.
        // While I could provide a nice interface typesafe and all for all requests,
        // this is easier to do. What was that about the cobbler's children again?
        public object DebugRequest(string request, object argument)
        {
            switch (request)
            {
                case "GetAllItemList":
                    return m_engine.ItemFactory.GetAllDropableItemsListForDebug().OfType<INamedItem>().ToList();
                case "SpawnItem":
                    m_engine.Map.AddItem(new Pair<Item, Point>(m_engine.ItemFactory.CreateItem((string)argument), m_engine.Player.Position));
                    return null;
                case "GetAllMonsterList":
                    return m_engine.MonsterFactory.GetAllMonsterListForDebug().OfType<INamedItem>().ToList();
                case "SpawnMonster":
                {
                    Point playerPos = m_engine.Player.Position;
                    for (int i = -1; i <= 1; ++i)
                    {
                        for (int j = -1; j <= 1; ++j)
                        {
                            if (i == 0 && j == 0)
                                continue;
                            Point newPosition = playerPos + new Point(i, j);
                            if (m_engine.Map.IsPointOnMap(newPosition))
                            {
                                if (m_engine.PathToPoint(m_engine.Player, newPosition, false, true, false) != null && 
                                    m_engine.Map.Monsters.Where(m => m.Position == newPosition).Count() == 0)
                                {
                                    m_engine.Map.AddMonster(m_engine.MonsterFactory.CreateMonster((string)argument, newPosition));
                                    return null;
                                }
                            }
                        }
                    }
                    return null;
                }
                case "AddSkillPoints":
                    m_engine.Player.SkillPoints += (int)argument;
                    return null;
                case "KillMonstersOnFloor":
                    m_engine.Map.ClearMonstersFromMap();
                    return null;
                default:
                    return null;
            }
        }
    }
}