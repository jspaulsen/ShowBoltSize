using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShowBoltSize
{

    [ActionCategory(ActionCategory.ScriptControl)]
    internal class FsmHook : FsmStateAction
    {
        public Action Call;

        public override void OnEnter()
        {
            var call = Call;
            call?.Invoke();
            Finish();
        }
    }

    public enum SizeShowType
    {
        ExactNumber, DirectionDistance, Direction
    }

    public class ShowBoltSize : Mod
    {
        public override string ID => "ShowBoltSize";
        public override string Name => "ShowBoltSize";
        public override string Author => "cannibaljeebus (Original by Lex and wolf_vx)";
        public override string Version => "1.0.0"; 
        public override string Description => "Displays a helpful message if a bolt is too large or too small for a bolt"; 

        private static readonly float TotalDisplayTime = 0.75f;
        private FsmFloat WrenchSize;
        private FsmFloat BoltSize;
        private bool LoadedCallback = false;
        private SizeShowType ShowType;

        private FsmString GuiInteraction;
        private string DisplayString = "";
        private float RemainingDisplayTime = 0f;

        private static Settings featureShowDirectionDistance = new Settings("ShowBoltSizes_directionDistance", "Bolt Size Distance - Slightly/Way Too Big/Small", true);
        private static Settings featureShowBoltSize = new Settings("ShowBoltSizes_boltSize", "Exact Bolt Size", false);
        private static Settings featureShowDirection = new Settings("ShowBoltSizes_direction", "Bolt Size Direction - Too Big/Small", false);

        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
            SetupFunction(Setup.Update, Mod_Update);
        }

        public override void ModSettings()
        {
            Settings.AddCheckBox(this, featureShowDirectionDistance, "BoltSizeGroup");
            Settings.AddCheckBox(this, featureShowDirection, "BoltSizeGroup");
            Settings.AddCheckBox(this, featureShowBoltSize, "BoltSizeGroup");
        }

        private void Mod_OnLoad()
        {
            GameObject selectItem = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/SelectItem");

            // Configure the mod from settings
            ReadSettings();

            WrenchSize = PlayMakerGlobals.Instance.Variables.FindFsmFloat("ToolWrenchSize");
            GuiInteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");

            // Inject `LoadCallback` which when invoked for the first time
            // injects `CheckBoltCallback`; it's confusing, but it seems necessary
            // as it's not otherwise available or instantiated (?) prior to the `Tools` state.
            FsmInject(selectItem, "Tools", LoadCallback);
        }

        private void Mod_Update()
        {
            if (RemainingDisplayTime <= 0f)
                return;

            RemainingDisplayTime -= Time.deltaTime;

            if (RemainingDisplayTime <= 0f)
            {
                DisplayString = "";
            }

            GuiInteraction.Value = DisplayString;
        }

        private void ReadSettings()
        {
            bool showBoltSize = (bool)featureShowBoltSize.GetValue();
            bool showDirection = (bool)featureShowDirection.GetValue();
            bool showDirectionDistance = (bool)featureShowDirectionDistance.GetValue();

            // Configure Mod based on settings
            if (showBoltSize)
                ShowType = SizeShowType.ExactNumber;

            if (showDirection)
                ShowType = SizeShowType.Direction;

            if (showDirectionDistance)
                ShowType = SizeShowType.DirectionDistance;
        }

        private void CheckBoltCallback()
        {
            int wrenchSize = Mathf.RoundToInt(WrenchSize.Value * 10f);
            int boltSize = Mathf.RoundToInt(BoltSize.Value * 10f);

            // Hook is potentially called whenever swapping between 
            // empty hand and tool; wrenchSize at that point is 0
            if (wrenchSize == 0)
                return;

            // If the wrench already matches the bolt size, no need to 
            // display text
            if (wrenchSize == boltSize)
                return;

            DisplayString = GenerateDisplayString(ShowType, wrenchSize, boltSize);
            RemainingDisplayTime = TotalDisplayTime;
        }

        private void LoadCallback()
        {
            if (LoadedCallback)
                return;

            GameObject raycast = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera/2Spanner/Raycast");
            PlayMakerFSM component = GetPlayMakerFSMFromGameObject(raycast, "Check");
            Fsm fsm = component.Fsm;

            BoltSize = fsm.GetFsmFloat("BoltSize");

            // State 1 becomes active if the checked bolt size is (RED) or too large (?)
            FsmInject(raycast, "State 1", CheckBoltCallback);

            // State 2 becomes active if the checked bolt size is (YELLOW) or too small (?)
            FsmInject(raycast, "State 2", CheckBoltCallback);
            LoadedCallback = true;
        }

        private static string GenerateDisplayString(SizeShowType showType, int wrenchSize, int boltSize)
        {
            string ret = "";
            string sizing = boltSize > wrenchSize ? "large" : "small";

            switch (showType)
            {
                case SizeShowType.ExactNumber:
                    ret = $"Size {boltSize}";
                    break;
                case SizeShowType.Direction:
                    ret = $"Wrench is too {sizing}";
                    break;
                case SizeShowType.DirectionDistance:
                    int distance = Math.Abs(wrenchSize - boltSize);
                    string adjSizing;

                    if (distance < 3)
                        adjSizing = "slightly ";
                    else if (distance < 5)
                        adjSizing = " ";
                    else
                        adjSizing = "way ";

                    ret = $"Bolt is {adjSizing}too {sizing}";
                    break;
            }

            return ret;
        }

        // ModLoader provides this mechanism (albeit, in a different way) which was not functioning
        // so this was brought forward
        private static bool FsmInject(GameObject obj, string stateName, Action callback)
        {
            PlayMakerFSM[] components = obj.GetComponents<PlayMakerFSM>();
            bool ret = false;

            foreach (PlayMakerFSM component in components)
            {
                FsmState[] states = component.Fsm.States;

                if (states != null)
                    foreach (FsmState state in states)
                        if (state != null && state.Name == stateName)
                        {
                            state.Actions = new List<FsmStateAction>(state.Actions)
                            {
                                new FsmHook
                                {
                                    Call = callback
                                }
                            }.ToArray();
                        }
            }

            return ret;
        }

        private static PlayMakerFSM GetPlayMakerFSMFromGameObject(GameObject obj, string stateName)
        {
            PlayMakerFSM[] components = obj.GetComponents<PlayMakerFSM>();
            PlayMakerFSM ret = null;

            foreach (PlayMakerFSM component in components)
            {
                if (component.FsmName == "Check")
                {
                    ret = component;
                    break;
                }
            }

            return ret;
        }
    }
}


