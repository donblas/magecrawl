﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Magecrawl;
using Magecrawl.Interfaces;
using Magecrawl.Utilities;
using MageCrawl.Silverlight.List;

namespace MageCrawl.Silverlight.KeyboardHandlers
{
    public class DefaultKeyboardHandler
    {
        private GameWindow m_window;
        private IGameEngine m_engine;

        public DefaultKeyboardHandler(GameWindow window, IGameEngine engine)
        {
            m_window = window;
            m_engine = engine;
        }

        public void OnKeyboardDown(MagecrawlKey key, Map map, GameWindow window, IGameEngine engine)
        {
            switch (key)
            {
                case MagecrawlKey.v:
                {
                    map.InTargettingMode = true;
                    TargettingModeKeyboardHandler handler = new TargettingModeKeyboardHandler();
                    window.SetKeyboardHandler(handler.OnKeyboardDown);
                    break;
                }
                case MagecrawlKey.A:
                {
                    map.InTargettingMode = true;
                    TargettingModeKeyboardHandler handler = new TargettingModeKeyboardHandler(OnRunTargetSelected);
                    window.SetKeyboardHandler(handler.OnKeyboardDown);
                    break;
                }
                case MagecrawlKey.i:
                {
                    ListSelection listSelection = new ListSelection(window, engine.Player.Items.OfType<INamedItem>(), "Inventory");
                    listSelection.SelectionDelegate = i =>
                    {
                        ItemSelection selection = new ItemSelection(engine, (IItem)i, OnItemSelected);
                        selection.ParentWindow = listSelection;
                        selection.Show();
                    };
                    listSelection.DismissOnSelection = false;
                    listSelection.Show();
                    break;
                }
                case MagecrawlKey.z:
                {
                    ListSelection listSelection = new ListSelection(window, engine.Player.Spells.OfType<INamedItem>(), "Spellbook");
                    listSelection.Show();
                    break;
                }
                case MagecrawlKey.E:
                {
                    ListSelection listSelection = new ListSelection(window, engine.Player.StatusEffects.OfType<INamedItem>(), "Dismiss Effect");
                    listSelection.SelectionDelegate = i => engine.Actions.DismissEffect(i.DisplayName);
                    listSelection.Show();
                    break;
                }
                case MagecrawlKey.Comma:
                {
                    List<INamedItem> itemsAtLocation = engine.Map.Items.Where(i => i.Second == engine.Player.Position).Select(i => i.First).OfType<INamedItem>().ToList();
                    if (itemsAtLocation.Count > 1)
                    {
                        ListSelection listSelection = new ListSelection(window, itemsAtLocation, "Pickup Item");
                        listSelection.SelectionDelegate = i => engine.Actions.GetItem((IItem)i);
                        listSelection.Show();
                    }
                    else
                    {
                        engine.Actions.GetItem();
                        window.UpdateWorld();
                    }
                    break;
                }
                case MagecrawlKey.Backquote:
                {
                    engine.Actions.SwapPrimarySecondaryWeapons();
                    window.UpdateWorld();
                    break;
                }
                case MagecrawlKey.Period:
                {
                    engine.Actions.Wait();
                    window.UpdateWorld();
                    break;
                }
                case MagecrawlKey.PageUp:
                {
                    window.MessageBox.PageUp();
                    break;
                }
                case MagecrawlKey.PageDown:
                {
                    window.MessageBox.PageDown();
                    break;
                }
                case MagecrawlKey.Backspace:
                {
                    window.MessageBox.Clear();
                    break;
                }
                case MagecrawlKey.Left:
                    HandleDirection(Direction.West, map, window, engine);
                    break;
                case MagecrawlKey.Right:
                    HandleDirection(Direction.East, map, window, engine);
                    break;
                case MagecrawlKey.Down:
                    HandleDirection(Direction.South, map, window, engine);
                    break;
                case MagecrawlKey.Up:
                    HandleDirection(Direction.North, map, window, engine);
                    break;
                case MagecrawlKey.Insert:
                    HandleDirection(Direction.Northwest, map, window, engine);
                    break;
                case MagecrawlKey.Delete:
                    HandleDirection(Direction.Southwest, map, window, engine);
                    break;
                case MagecrawlKey.Home:
                    HandleDirection(Direction.Northeast, map, window, engine);
                    break;
                case MagecrawlKey.End:
                    HandleDirection(Direction.Southeast, map, window, engine);
                    break;
            }
        }

        private void OnItemSelected(IItem item, string optionName)
        {
            TargetingInfo targetInfo = m_engine.Targetting.GetTargettingTypeForInventoryItem(item, optionName);
            if (targetInfo == null)
            {
                m_engine.Actions.SelectedItemOption(item, optionName, m_engine.Player.Position);
                m_window.UpdateWorld();
            }
            else
            {
                switch (targetInfo.Type)
                {
                    case TargetingInfo.TargettingType.Stream:
                    {
                        //List<EffectivePoint> targetablePoints = PointListUtils.EffectivePointListOneStepAllDirections(m_engine.Player.Position);
                        //HandleRangedSinglePointInvoke(invokingObject, targetablePoints, onInvoke, invokeKey);
                        //return;
                        throw new NotImplementedException();
                    }
                    case TargetingInfo.TargettingType.RangedSingle:
                    case TargetingInfo.TargettingType.RangedBlast:
                    case TargetingInfo.TargettingType.RangedExplodingPoint:
                    {
                        //List<EffectivePoint> targetablePoints = PointListUtils.EffectivePointListFromBurstPosition(m_engine.Player.Position, targetInfo.Range);
                        //HandleRangedSinglePointInvoke(invokingObject, targetablePoints, onInvoke, invokeKey);
                        //return;
                        throw new NotImplementedException();
                    }
                    case TargetingInfo.TargettingType.Cone:
                    {
                        //Point playerPosition = m_engine.Player.Position;
                        //List<EffectivePoint> targetablePoints = GetConeTargetablePoints(playerPosition);
                        //OnTargetSelection selectionDelegate = new OnTargetSelection(s =>
                        //{
                            //if (s != m_engine.Player.Position)
                                //onInvoke(s);
                            //return false;
                        //});
                        //m_gameInstance.SetHandlerName("Target", new TargettingKeystrokeRequest(targetablePoints, selectionDelegate,
                                //NamedKey.Invalid, TargettingKeystrokeHandler.TargettingType.Monster,
                                //p => m_engine.Targetting.TargettedDrawablePoints(invokingObject, p)));
                            //return;
                        throw new NotImplementedException();
                    }
                    case TargetingInfo.TargettingType.Self:
                    {
                        m_engine.Actions.SelectedItemOption(item, optionName, m_engine.Player.Position);
                        m_window.UpdateWorld();
                        break;
                    }
                    default:
                        throw new System.InvalidOperationException("InvokingKeystrokeHandler - HandleInvoke, don't know how to handle: " + targetInfo.Type.ToString());
                }
            }
        }

        private static void OnRunTargetSelected(GameWindow window, IGameEngine engine, Point point)
        {
            window.Map.InTargettingMode = false;
            RunningKeyboardHandler runner = new RunningKeyboardHandler(window, engine);
            runner.StartRunning(point);
        }

        private static void HandleDirection(Direction direction, Map map, GameWindow window, IGameEngine engine)
        {
            if (!map.InTargettingMode)
            {
                if (Keyboard.Modifiers != ModifierKeys.Shift)
                {
                    engine.Actions.Move(direction);
                    window.UpdateWorld();
                }
                else
                {
                    RunningKeyboardHandler runner = new RunningKeyboardHandler(window, engine);
                    runner.StartRunning(direction);
                }
            }
            else
            {
                throw new InvalidOperationException("DefaultKeyboardHandler and Map disagree on current state");
            }
        }
    }
}
