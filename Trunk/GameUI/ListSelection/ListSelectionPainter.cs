using System.Collections.Generic;
using libtcod;
using Magecrawl.Interfaces;
using Magecrawl.Utilities;

namespace Magecrawl.GameUI.ListSelection
{
    public delegate void ListItemSelected(INamedItem item);
    public delegate bool ListItemShouldBeEnabled(INamedItem item);

    // This code is scary, I admit it. It looks complex, but it has to be. 
    // Scrolling inventory right, when it might be lettered, is hard.
    internal class ListSelectionPainter : PainterBase
    {
        private bool m_enabled;                         // Are we showing the inventory
        private IList<INamedItem> m_itemList;           // Items to display
        private int m_lowerRange;                       // If we're scrolling, the loweset number item to show
        private int m_higherRange;                      // Last item to show
        private bool m_isScrollingNeeded;               // Do we need to scroll at all?
        private int m_cursorPosition;                   // What item is the cursor on
        private bool m_useCharactersNextToItems;        // Should we put letters next to each letter
        private bool m_shouldNotResetCursorPosition;    // If set, the next time we show the inventory window, we don't reset the position.
        private ListItemShouldBeEnabled m_shouldBeSelectedDelegate; // Called for each item to determine if we should enable it if not null
        private string m_title;

        private DialogColorHelper m_dialogColorHelper;

        private const int ScrollAmount = 8;
        private const int InventoryWindowOffset = 5;
        private const int InventoryItemWidth = UIHelper.ScreenWidth - 10;
        private const int InventoryItemHeight = UIHelper.ScreenHeight - 10;
        private const int NumberOfLinesDisplayable = InventoryItemHeight - 2;

        internal ListSelectionPainter()
        {
            m_dialogColorHelper = new DialogColorHelper();
            m_enabled = false;
            m_shouldNotResetCursorPosition = false;
        }

        public override void UpdateFromNewData(IGameEngine engine, Point mapUpCorner, Point cursorPosition)
        {
            m_shouldNotResetCursorPosition = false;
        }

        public override void DrawNewFrame(TCODConsole screen)
        {
            if (m_enabled)
            {
                m_higherRange = m_isScrollingNeeded ? m_lowerRange + NumberOfLinesDisplayable : m_itemList.Count;
                screen.printFrame(InventoryWindowOffset, InventoryWindowOffset, InventoryItemWidth, InventoryItemHeight, true, TCODBackgroundFlag.Set, m_title);
                
                // Start lettering from our placementOffset.
                char currentLetter = 'a';

                if (m_useCharactersNextToItems)
                {
                    for (int i = 0; i < m_lowerRange; ++i)
                        currentLetter = IncrementLetter(currentLetter);
                }

                int positionalOffsetFromTop = 0;
                m_dialogColorHelper.SaveColors(screen);

                int farRightPaddingAmount = DetermineFarRightPaddingForMagicList();

                for (int i = m_lowerRange; i < m_higherRange; ++i)
                {
                    string displayString = m_itemList[i].DisplayName;
                    m_dialogColorHelper.SetColors(screen, i == m_cursorPosition, m_shouldBeSelectedDelegate(m_itemList[i]));
                    if (displayString.Contains('\t'.ToString()))
                    {
                        // This is the case for Tab Seperated Spaces, used for magic lists and such
                        string[] sectionArray = displayString.Split(new char[] { '\t' }, 3);

                        screen.print(InventoryWindowOffset + 1, InventoryWindowOffset + 1 + positionalOffsetFromTop, currentLetter + " - " + sectionArray[0]);
                        if (sectionArray.Length > 1)
                        {
                            screen.print(InventoryWindowOffset + (InventoryItemWidth / 2), InventoryWindowOffset + 1 + positionalOffsetFromTop, sectionArray[1]);
                            if (sectionArray.Length > 2)
                            {
                                screen.printEx(InventoryWindowOffset - 2 + InventoryItemWidth, InventoryWindowOffset + 1 + positionalOffsetFromTop, TCODBackgroundFlag.Set, TCODAlignment.RightAlignment, sectionArray[2].PadRight(farRightPaddingAmount));
                            }
                        }
                    }
                    else
                    {
                        string printString;
                        if (m_useCharactersNextToItems)
                            printString = string.Format("{0} - {1}", currentLetter, displayString);
                        else
                            printString = " - " + displayString;
                        screen.print(InventoryWindowOffset + 1, InventoryWindowOffset + 1 + positionalOffsetFromTop, printString);
                    }

                    currentLetter = IncrementLetter(currentLetter);
                    positionalOffsetFromTop++;
                }
                m_dialogColorHelper.ResetColors(screen);
            }
        }

        // A "magic" list is one that embeds '\t' in it for columns. If we have 3 columns, figure out how much we should pad the right
        // most one so it stays lined up nice
        private int DetermineFarRightPaddingForMagicList()
        {
            int farRightPaddingAmount = 0;
            for (int i = 0; i < m_itemList.Count; ++i)
            {
                string displayString = m_itemList[i].DisplayName;
                if (displayString.Contains('\t'.ToString()))
                {
                    // This is the case for Tab Seperated Spaces, used for magic lists and such
                    string[] sectionArray = displayString.Split(new char[] { '\t' }, 3);
                    if (sectionArray.Length > 2)
                        farRightPaddingAmount = System.Math.Max(farRightPaddingAmount, sectionArray[2].Length);
                }
            }
            return farRightPaddingAmount;
        }

        public bool IsEnabled(INamedItem item)
        {
            return m_shouldBeSelectedDelegate != null ? m_shouldBeSelectedDelegate(item) : true; 
        }

        internal INamedItem CurrentSelection
        {
            get 
            {
                if (m_itemList.Count <= m_cursorPosition)
                    return null;
                return m_itemList[m_cursorPosition]; 
            }
        }

        internal void SelectionFromChar(char toSelect, ListItemSelected onSelect)
        {
            if (m_useCharactersNextToItems)
            {
                List<char> listOfLettersUsed = GetListOfLettersUsed();
                if (listOfLettersUsed.Contains(toSelect))
                {
                    m_cursorPosition = listOfLettersUsed.IndexOf(toSelect);

                    onSelect(m_itemList[m_cursorPosition]);
                }
            }
        }

        internal void Enable(List<INamedItem> data, string title, bool useLetters, ListItemShouldBeEnabled shouldBeSelectedDelegate)
        {
            if (!m_shouldNotResetCursorPosition)
            {
                m_cursorPosition = 0;
                m_lowerRange = 0;
                m_higherRange = 0;
            }
            else
            {
                m_shouldNotResetCursorPosition = false;
            }
            
            // This gets set before UpdateFromNewData in case we say we want letters but have too many items
            m_useCharactersNextToItems = useLetters;

            UpdateFromNewData(data);
            m_shouldBeSelectedDelegate = shouldBeSelectedDelegate;
            m_title = title;

            m_enabled = true;
        }

        internal void Disable()
        {
            m_enabled = false;
        }

        internal bool SaveSelectionPosition
        {
            get 
            { 
                return m_shouldNotResetCursorPosition; 
            }
            set 
            {
                m_shouldNotResetCursorPosition = value; 
            }
        }

        private void UpdateFromNewData(List<INamedItem> data)
        {
            m_itemList = data;
            m_isScrollingNeeded = m_itemList.Count > NumberOfLinesDisplayable;

            // If we're going to run out of letters, don't show em.
            if (m_itemList.Count > 26 * 2)
                m_useCharactersNextToItems = false;
        }

        private List<char> GetListOfLettersUsed()
        {
            if (!m_useCharactersNextToItems)
                throw new System.ArgumentException("GetListOfLettersUsed can't be called when not using letters next to names");

            List<char> returnList = new List<char>();
            char elt = 'a';
            while (elt != MapSelectionOffsetToLetter(m_itemList.Count))
            {
                returnList.Add(elt);
                elt = IncrementLetter(elt);
            }
            return returnList;
        }

        private static char MapSelectionOffsetToLetter(int offset)
        {
            if (offset > 25)
                return (char)('A' + (char)(offset - 26));
            else
                return (char)('a' + (char)offset);
        }

        private static char IncrementLetter(char letter)
        {
            if (letter == 'Z')
                return 'a';
            else if (letter == 'z')
                return 'A';
            else
                return (char)(((int)letter) + 1);
        }

        internal void MoveInventorySelection(Direction cursorDirection)
        {
            if (cursorDirection == Direction.North)
            {
                if (m_cursorPosition > 0)
                {
                    if (m_isScrollingNeeded && (m_cursorPosition == m_lowerRange))
                    {
                        m_lowerRange -= ScrollAmount;
                        if (m_lowerRange < 0)
                            m_lowerRange = 0;
                    }
                    m_cursorPosition--;
                }
            }
            if (cursorDirection == Direction.South && m_cursorPosition < m_itemList.Count - 1)
            {
                // If we need scrolling and we're pointed at the end of the list and there's more to show.
                if (m_isScrollingNeeded && (m_cursorPosition == (m_lowerRange - 1 + NumberOfLinesDisplayable)) && (m_lowerRange + NumberOfLinesDisplayable < m_itemList.Count))
                {
                    m_lowerRange += ScrollAmount;
                    if ((m_lowerRange + NumberOfLinesDisplayable) > m_itemList.Count)
                        m_lowerRange = m_itemList.Count - NumberOfLinesDisplayable;

                    m_cursorPosition++;
                }
                else
                {
                    if ((m_cursorPosition + 1) < m_itemList.Count)
                        m_cursorPosition++;
                }
            }
        }
    }
}
