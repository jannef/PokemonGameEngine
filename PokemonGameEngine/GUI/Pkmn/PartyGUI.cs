﻿using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.GUI.Interactive;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI.Pkmn
{
    internal sealed class PartyGUI
    {
        public enum Mode : byte
        {
            PkmnMenu,
            SelectDaycare,
            BattleSwitchIn
        }
        private sealed class GamePartyData
        {
            public readonly Party Party;

            public GamePartyData(Party party, List<PartyGUIMember> members, List<Sprite> sprites)
            {
                Party = party;
                foreach (PartyPokemon pkmn in party)
                {
                    members.Add(new PartyGUIMember(pkmn, sprites));
                }
            }
        }
        private sealed class BattlePartyData
        {
            public readonly SpritedBattlePokemonParty Party;

            public BattlePartyData(SpritedBattlePokemonParty party, List<PartyGUIMember> members, List<Sprite> sprites)
            {
                Party = party;
                foreach (PBEBattlePokemon pkmn in party.BattleParty)
                {
                    SpritedBattlePokemon sPkmn = party[pkmn]; // Use battle party's order
                    members.Add(new PartyGUIMember(sPkmn, sprites));
                }
            }
        }

        private readonly Mode _mode;
        private readonly bool _useGamePartyData;
        private readonly GamePartyData _gameParty;
        private readonly BattlePartyData _battleParty;
        private readonly List<PartyGUIMember> _members;
        private readonly List<Sprite> _sprites;

        private FadeColorTransition _fadeTransition;
        private Action _onClosed;

        private Window _textChoicesWindow;
        private TextGUIChoices _textChoices;
        private string _message;

        private int _selectionX;
        private int _selectionY;

        #region Open & Close GUI

        public unsafe PartyGUI(Party party, Mode mode, Action onClosed)
        {
            _mode = mode;
            _useGamePartyData = true;
            _sprites = new List<Sprite>();
            _members = new List<PartyGUIMember>(PkmnConstants.PartyCapacity);
            _gameParty = new GamePartyData(party, _members, _sprites);
            _members[0].SetBigBounce();

            if (mode == Mode.SelectDaycare)
            {
                SetDefaultSelectionToNone();
            }

            _onClosed = onClosed;
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeInParty);
            Game.Instance.SetRCallback(RCB_Fading);
        }
        public unsafe PartyGUI(SpritedBattlePokemonParty party, Mode mode, Action onClosed)
        {
            _mode = mode;
            _useGamePartyData = false;
            _sprites = new List<Sprite>();
            _members = new List<PartyGUIMember>(PkmnConstants.PartyCapacity);
            _battleParty = new BattlePartyData(party, _members, _sprites);
            _members[0].SetBigBounce();

            if (mode == Mode.BattleSwitchIn)
            {
                SetDefaultSelectionToNone();
            }

            _onClosed = onClosed;
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeInParty);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void ClosePartyMenu()
        {
            _fadeTransition = new FadeToColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeOutParty);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void CB_FadeInParty()
        {
            Sprite.DoCallbacks(_sprites);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                Game.Instance.SetCallback(CB_LogicTick);
                Game.Instance.SetRCallback(RCB_RenderTick);
            }
        }
        private unsafe void CB_FadeOutParty()
        {
            Sprite.DoCallbacks(_sprites);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _onClosed.Invoke();
                _onClosed = null;
            }
        }

        private unsafe void RCB_Fading(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RCB_RenderTick(bmpAddress, bmpWidth, bmpHeight);
            _fadeTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        }

        #endregion

        private int GetPartySize()
        {
            return _useGamePartyData ? _gameParty.Party.Count : _battleParty.Party.SpritedParty.Length;
        }
        private int SelectionCoordsToPartyIndex(int col, int row)
        {
            if (row == -1)
            {
                return -1;
            }
            int i = row * 2 + col;
            if (i >= GetPartySize())
            {
                return -1;
            }
            return i;
        }
        private void UpdateBounces(int oldCol, int oldRow)
        {
            int i = SelectionCoordsToPartyIndex(oldCol, oldRow);
            if (i != -1)
            {
                _members[i].SetSmallBounce();
            }
            i = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
            if (i != -1)
            {
                _members[i].SetBigBounce();
            }
        }
        private void SetDefaultSelectionToNone()
        {
            Game.Instance.Save.Vars[Var.SpecialVar_Result] = -1; // If you back out, the default selection is -1
        }

        private void Action_SelectPartyPkmn(int index)
        {
            switch (_mode)
            {
                case Mode.SelectDaycare:
                case Mode.BattleSwitchIn:
                {
                    Game.Instance.Save.Vars[Var.SpecialVar_Result] = (short)index;
                    CloseChoices();
                    ClosePartyMenu();
                    return;
                }
                default: throw new Exception();
            }
        }
        private void Action_BringUpSummary(int index)
        {

        }

        private void BringUpPkmnActions(int index)
        {
            string nickname;
            _textChoices = new TextGUIChoices(0, 0, backCommand: CloseChoicesThenGoToLogicTick, font: Font.Default, fontColors: Font.DefaultDark, selectedColors: Font.DefaultSelected);
            switch (_mode)
            {
                case Mode.PkmnMenu:
                {
                    PartyPokemon pkmn = _gameParty.Party[index];
                    nickname = pkmn.Nickname;
                    _textChoices.Add(new TextGUIChoice("Summary", () => Action_BringUpSummary(index)));
                    break;
                }
                case Mode.SelectDaycare:
                {
                    PartyPokemon pkmn = _gameParty.Party[index];
                    nickname = pkmn.Nickname;
                    if (!pkmn.IsEgg)
                    {
                        _textChoices.Add(new TextGUIChoice("Select", () => Action_SelectPartyPkmn(index)));
                    }
                    _textChoices.Add(new TextGUIChoice("Summary", () => Action_BringUpSummary(index)));
                    break;
                }
                case Mode.BattleSwitchIn:
                {
                    SpritedBattlePokemonParty party = _battleParty.Party;
                    SpritedBattlePokemon sPkmn = party[party.BattleParty[index]];
                    PartyPokemon pkmn = sPkmn.PartyPkmn;
                    nickname = pkmn.Nickname;
                    if (!pkmn.IsEgg && sPkmn.Pkmn.FieldPosition == PBEFieldPosition.None) // Cannot switch in if active already
                    {
                        _textChoices.Add(new TextGUIChoice("Switch In", () => Action_SelectPartyPkmn(index)));
                    }
                    _textChoices.Add(new TextGUIChoice("Summary", () => Action_BringUpSummary(index)));
                    break;
                }
                default: throw new Exception();
            }

            _textChoices.Add(new TextGUIChoice("Cancel", CloseChoicesThenGoToLogicTick));
            _textChoices.GetSize(out int width, out int height);
            _textChoicesWindow = new Window(0.6f, 0.3f, width, height, RenderUtils.Color(255, 255, 255, 255));
            RenderChoicesOntoWindow();
            _message = string.Format("Do what with {0}?", nickname);
            Game.Instance.SetCallback(CB_Choices);
        }
        private void CloseChoices()
        {
            _textChoicesWindow.Close();
            _textChoicesWindow = null;
            _textChoices.Dispose();
            _textChoices = null;
        }
        private void CloseChoicesThenGoToLogicTick()
        {
            CloseChoices();
            _message = null;
            Game.Instance.SetCallback(CB_LogicTick);
        }
        private unsafe void RenderChoicesOntoWindow()
        {
            _textChoicesWindow.ClearImage();
            Image i = _textChoicesWindow.Image;
            fixed (uint* bmpAddress = i.Bitmap)
            {
                _textChoices.Render(bmpAddress, i.Width, i.Height);
            }
        }

        private void CB_Choices()
        {
            Sprite.DoCallbacks(_sprites);
            int s = _textChoices.Selected;
            _textChoices.HandleInputs();
            if (_textChoicesWindow is null)
            {
                return; // Was just closed
            }
            if (s != _textChoices.Selected)
            {
                RenderChoicesOntoWindow();
            }
        }
        private void CB_LogicTick()
        {
            Sprite.DoCallbacks(_sprites);
            if (InputManager.IsPressed(Key.A))
            {
                if (_selectionY == -1)
                {
                    ClosePartyMenu();
                }
                else
                {
                    int i = SelectionCoordsToPartyIndex(_selectionX, _selectionY);
                    BringUpPkmnActions(i);
                }
                return;
            }
            if (InputManager.IsPressed(Key.B))
            {
                ClosePartyMenu();
                return;
            }
            if (InputManager.IsPressed(Key.Left))
            {
                if (_selectionX == 1)
                {
                    _selectionX = 0;
                    UpdateBounces(1, _selectionY);
                }
                return;
            }
            if (InputManager.IsPressed(Key.Right))
            {
                if (_selectionX == 0 && SelectionCoordsToPartyIndex(1, _selectionY) != -1)
                {
                    _selectionX = 1;
                    UpdateBounces(0, _selectionY);
                }
                return;
            }
            if (InputManager.IsPressed(Key.Down))
            {
                int oldY = _selectionY;
                if (oldY != -1)
                {
                    if (SelectionCoordsToPartyIndex(_selectionX, oldY + 1) == -1)
                    {
                        _selectionY = -1;
                    }
                    else
                    {
                        _selectionY++;
                    }
                    UpdateBounces(_selectionX, oldY);
                }
                return;
            }
            if (InputManager.IsPressed(Key.Up))
            {
                int oldY = _selectionY;
                if (oldY == -1)
                {
                    _selectionY = (GetPartySize() - 1) / 2;
                    UpdateBounces(_selectionX, oldY);
                }
                else if (oldY > 0)
                {
                    _selectionY--;
                    UpdateBounces(_selectionX, oldY);
                }
                return;
            }
        }

        private unsafe void RCB_RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            // Background
            RenderUtils.ThreeColorBackground(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(222, 50, 60, 255), RenderUtils.Color(190, 40, 50, 255), RenderUtils.Color(255, 180, 200, 255));

            for (int i = 0; i < _members.Count; i++)
            {
                int col = i % 2;
                int row = i / 2;
                int x = col == 0 ? bmpWidth / 40 : (bmpWidth / 2) + (bmpWidth / 40);
                int y = row * (bmpHeight / 4) + (bmpHeight / 20);
                _members[i].Render(bmpAddress, bmpWidth, bmpHeight, x, y, col == _selectionX && row == _selectionY);
            }

            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0.5f, 0.8f, 0.5f, 0.2f, _selectionY == -1 ? RenderUtils.Color(96, 48, 48, 255) : RenderUtils.Color(48, 48, 48, 255));
            Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 0.5f, 0.8f, "Back", Font.DefaultWhite);

            if (_message != null)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0, 0.8f, 0.5f, 0.2f, RenderUtils.Color(200, 200, 200, 255));
                Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 0, 0.8f, _message, Font.DefaultDark);
            }

            Game.Instance.RenderWindows(bmpAddress, bmpWidth, bmpHeight);
        }
    }
}
