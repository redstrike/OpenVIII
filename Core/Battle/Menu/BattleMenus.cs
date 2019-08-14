﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using OpenVIII.Encoding.Tags;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenVIII
{
    /// <summary>
    /// Menu holds a menu for each character.
    /// </summary>
    public partial class BattleMenus : Menus
    {

        #region Fields

        private int _player = 0;
        private Dictionary<Mode, Action> DrawActions;
        private Dictionary<Mode, Func<bool>> InputFunctions;
        private Dictionary<Mode, Action> ReturnAction;
        private Dictionary<Mode, Func<bool>> UpdateFunctions;
        private Module_main_menu_debug.MainMenuStates lastmenu;
        private int lastgamestate;
        private ushort lastmusic;
        private bool lastmusicplaying;

        #endregion Fields

        #region Enums

        public enum Mode : byte
        {
            Starting,
            Battle,
            Victory,
            GameOver,
        }

        private enum SectionName : byte
        {
            HP
        }
        #endregion Enums

        #region Methods

        public override void Draw()
        {

            if (DrawActions.ContainsKey((Mode)GetMode()))
                DrawActions[(Mode)GetMode()]();
        }


        public override bool Inputs()
        {
            bool ret = false;
            if (InputFunctions.ContainsKey((Mode)GetMode()))
                ret = InputFunctions[(Mode)GetMode()]() && ret;
            if (Input2.DelayedButton(Keys.D1))
            {
                SetMode(Mode.Victory);
            }
            else if (Input2.DelayedButton(Keys.D2))
            {
                SetMode(Mode.GameOver);
            }
            return ret;
        }

        public override void Refresh()
        {
            if (Memory.State?.Characters != null)
            {
                IEnumerable<KeyValuePair<int, Characters>> party = Memory.State.Party.Select((element, index) => new { element, index }).ToDictionary(m => m.index, m => m.element).Where(m => !m.Value.Equals(Characters.Blank));
                int count = party.Count();
                menus = new List<Menu>(count);
                foreach (KeyValuePair<int, Characters> m in party)
                {
                    BattleMenu tmp = new BattleMenu(Memory.State.PartyData[m.Key], m.Value);
                    tmp.Hide();
                    menus.Add(tmp);
                }
                menus.Add(new VictoryMenu());
                SetMode(Mode.Battle);
                UpdateFunctions = new Dictionary<Mode, Func<bool>>()
                {
                    {Mode.Starting, UpdateStartingFunction},
                    {Mode.Battle, UpdateBattleFunction},
                    {Mode.Victory, UpdateVictoryFunction},
                    {Mode.GameOver, UpdateGameOverFunction},
                };
                DrawActions = new Dictionary<Mode, Action>()
                {
                    {Mode.Starting, DrawStartingAction},
                    {Mode.Battle, DrawBattleAction},
                    {Mode.Victory, DrawVictoryAction},
                    {Mode.GameOver, DrawGameOverAction},
                };
                InputFunctions = new Dictionary<Mode, Func<bool>>()
                {
                    //{Mode.Starting, InputStartingFunction},
                    {Mode.Battle, InputBattleFunction},
                    //{Mode.Victory, InputVictoryFunction},
                    //{Mode.GameOver, InputGameOverFunction},
                };
                ReturnAction = new Dictionary<Mode, Action>()
                {
                    {Mode.Starting, ReturnStartingFunction},
                    {Mode.Battle, ReturnBattleFunction},
                    {Mode.Victory, ReturnVictoryFunction},
                    {Mode.GameOver, ReturnGameOverFunction},
                };
                menus?.ForEach(m => m.Show());
            }
            Victory_Menu?.Refresh(10000,1000,new Saves.Item(10,100), new Saves.Item(20, 65), new Saves.Item(28, 54));
            base.Refresh();
        }

        public void  CameFrom()
        {
            lastmenu = Module_main_menu_debug.State;
            lastgamestate = Memory.module;
            lastmusic = Memory.MusicIndex;
            lastmusicplaying = init_debugger_Audio.MusicPlaying;
        }
        public void ReturnTo()
        {
            Module_main_menu_debug.State = lastmenu;
            Memory.module = lastgamestate;
            if (lastmusicplaying)
                init_debugger_Audio.PlayMusic(lastmusic);
            else
                init_debugger_Audio.StopMusic();
        }
        private void ReturnGameOverFunction() { }
        private void ReturnVictoryFunction() { }
        private void ReturnBattleFunction() { }
        private void ReturnStartingFunction() { }

        public override bool Update()
        {
            bool ret = false;
            if (UpdateFunctions.TryGetValue((Mode)GetMode(),out Func<bool> u))
            {
                ret = u();
            }
                
            ret = base.Update() || ret;
            return ret;
        }

        protected override void Init()
        {
            Size = new Vector2 { X = 881, Y = 636 };
            Data = new Dictionary<Enum, IGMData>()
            {
                //{SectionName.HP, new IGMData_HP(new Rectangle((int)(Size.X-389),507,389,126))}
            };
            base.Init();
        }

        private void DrawBattleAction()
        {
            StartDraw();
            //Had to split up the HP and Commands drawing. So that Commands would draw over HP. 
            menus?.Where(m => m.GetType().Equals(typeof(BattleMenu))).ForEach(m => ((BattleMenu)m).DrawData(BattleMenu.SectionName.HP));
            menus?.Where(m => m.GetType().Equals(typeof(BattleMenu))).ForEach(m => ((BattleMenu)m).DrawData(BattleMenu.SectionName.Commands));
            EndDraw();
        }

        private void DrawGameOverAction() {}

        private void DrawStartingAction() { }

        private void DrawVictoryAction() => Victory_Menu.Draw();

        public VictoryMenu Victory_Menu => (VictoryMenu)(menus?.Where(m => m.GetType().Equals(typeof(VictoryMenu))).First());
        private bool InputBattleFunction()
        {
            bool ret = false;
            foreach (var m in menus.Where(m => m.GetType().Equals(typeof(BattleMenu)) && (BattleMenu.Mode)m.GetMode() == BattleMenu.Mode.YourTurn))
            {
                ret = m.Inputs() || ret;
                if (ret) return ret;
            }
            if (Input2.DelayedButton(FF8TextTagKey.Cancel))
            {
                switch ((BattleMenu.Mode)menus[_player].GetMode())
                {
                    case BattleMenu.Mode.YourTurn:
                        menus[_player].SetMode(BattleMenu.Mode.ATB_Charged);
                        break;
                }
                if (++_player > 2) _player = 0;
                menus[_player].SetMode(BattleMenu.Mode.YourTurn);
                if(((BattleMenu)menus[_player]).CrisisLevel)
                    init_debugger_Audio.PlaySound(94);
                else
                    init_debugger_Audio.PlaySound(14);
                switch ((BattleMenu.Mode)menus[_player].GetMode())
                {
                    case BattleMenu.Mode.ATB_Charged:
                        menus[_player].SetMode(BattleMenu.Mode.YourTurn);
                        break;
                }
            }
            return ret;
        }

        private bool InputGameOverFunction() => false;
        private bool InputStartingFunction() => false;

        private bool InputVictoryFunction() => throw new NotImplementedException();
        private bool UpdateBattleFunction()
        {
            menus?[_player].SetMode(BattleMenu.Mode.YourTurn);
            bool ret = false;
            foreach (var m in menus?.Where(m => m.GetType().Equals(typeof(BattleMenu))))
            {
                ret = m.Update() || ret;
            }
            return ret;
        }
        
        private bool UpdateGameOverFunction()
        {
            Memory.module = Memory.MODULE_FIELD_DEBUG;
            Memory.FieldHolder.FieldID = 75; //gover
            init_debugger_Audio.PlayMusic(0);
            Module_main_menu_debug.State = Module_main_menu_debug.MainMenuStates.MainLobby;
            return true;
        }

        private bool UpdateStartingFunction() => throw new NotImplementedException();

        private bool UpdateVictoryFunction()
        {
            init_debugger_Audio.PlayMusic(1);
            return Victory_Menu.Update();
        }
        #endregion Methods

    }
}