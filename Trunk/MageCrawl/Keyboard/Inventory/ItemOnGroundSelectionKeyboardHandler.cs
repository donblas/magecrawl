﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Magecrawl.GameEngine.Interfaces;
using Magecrawl.GameUI.Inventory.Requests;
using Magecrawl.GameUI.ListSelection;
using Magecrawl.GameUI.ListSelection.Requests;
using Magecrawl.GameUI.Map.Requests;

namespace Magecrawl.Keyboard.Inventory
{
    internal class ItemOnGroundSelectionKeyboardHandler : BaseKeystrokeHandler
    {
        private IGameEngine m_engine;
        private GameInstance m_gameInstance;

        public ItemOnGroundSelectionKeyboardHandler(IGameEngine engine, GameInstance instance)
        {
            m_engine = engine;
            m_gameInstance = instance;
        }

        public override void NowPrimaried(object request)
        {
            List<INamedItem> itemsAtLocation = (List<INamedItem>)request;
            m_gameInstance.SendPaintersRequest(new DisableAllOverlays());
            m_gameInstance.SendPaintersRequest(new ShowListSelectionWindow(true, itemsAtLocation, "Items On Ground"));
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
                m_gameInstance.SendPaintersRequest(new ListSelectionItemSelectedByChar(keystroke.Character, new ListItemSelected(ItemSelectedDelegate)));
            }
        }

        private void ItemSelectedDelegate(INamedItem item)
        {
            if (item == null)
                return;

            m_engine.PlayerGetItem((IItem)item);
            Escape();
        }

        private void Select()
        {
            m_gameInstance.SendPaintersRequest(new ListSelectionItemSelected(ItemSelectedDelegate));          
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
    }
}