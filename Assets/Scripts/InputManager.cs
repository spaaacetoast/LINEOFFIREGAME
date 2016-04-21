using UnityEngine;
using System.Collections;
using XInputDotNetPure;
using TNet;
using AngryRain;

public class InputManager : MonoBehaviour
{
    #region Input Managing

    public static ControllerState[] allGamePads = new ControllerState[] { new ControllerState(PlayerIndex.One), new ControllerState(PlayerIndex.Two), new ControllerState(PlayerIndex.Three), new ControllerState(PlayerIndex.Four) };

    public static void UpdateInputControls()
    {
        allGamePads[0].UpdateState();
        allGamePads[1].UpdateState();
        allGamePads[2].UpdateState();
        allGamePads[3].UpdateState();
    }

    public class ControllerState
    {
        public ControllerState(PlayerIndex index)
        {
            this.controllerIndex = index;
        }

        public GamePadState state
        {
            get { return GamePad.GetState(controllerIndex, GamePadDeadZone.None); }
        }
        public bool isConnected
        {
            get { return state.IsConnected; }
        }

        public PlayerIndex controllerIndex;

        /// <summary>
        /// This contains the previous frame boolean, Reference ControllerInput for index numbers
        /// </summary>
        bool[] lastXboxInput = new bool[16];
        public bool[] currentXboxInput = new bool[16];
        public bool[] currentXboxInputDown = new bool[16];
        public bool[] currentXboxInputUp = new bool[16];

        public void UpdateState()
        {
            if (!isConnected)
                return;

            currentXboxInput[0] = state.Buttons.A == ButtonState.Pressed;
            currentXboxInput[1] = state.Buttons.B == ButtonState.Pressed;
            currentXboxInput[2] = state.Buttons.X == ButtonState.Pressed;
            currentXboxInput[3] = state.Buttons.Y == ButtonState.Pressed;
            currentXboxInput[4] = state.Buttons.LeftStick == ButtonState.Pressed;
            currentXboxInput[5] = state.Buttons.RightStick == ButtonState.Pressed;
            currentXboxInput[6] = state.Buttons.LeftShoulder == ButtonState.Pressed;
            currentXboxInput[7] = state.Buttons.RightShoulder == ButtonState.Pressed;
            currentXboxInput[8] = state.Triggers.Left > 0.9f;
            currentXboxInput[9] = state.Triggers.Right > 0.9f;
            currentXboxInput[10] = state.Buttons.Start == ButtonState.Pressed;
            currentXboxInput[11] = state.Buttons.Back == ButtonState.Pressed;
            currentXboxInput[12] = state.DPad.Up == ButtonState.Pressed;
            currentXboxInput[13] = state.DPad.Down == ButtonState.Pressed;
            currentXboxInput[14] = state.DPad.Left == ButtonState.Pressed;
            currentXboxInput[15] = state.DPad.Right == ButtonState.Pressed;

            for (int i = 0; i < 16; i++)
                CheckDifference(i);
        }

        void CheckDifference(int index)
        {
            currentXboxInputDown[index] = currentXboxInput[index] != lastXboxInput[index] && currentXboxInput[index];

            currentXboxInputUp[index] = currentXboxInput[index] != lastXboxInput[index] && !currentXboxInput[index];

            lastXboxInput[index] = currentXboxInput[index];
        }
    }

    #endregion

    public static bool GetButton(InputName input, LocalPlayer lPlayer)
    {
        return OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].GetButton((int)input);
    }

    public static bool GetButtonDown(InputName input, LocalPlayer lPlayer)
    {
        return OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].GetButtonDown((int)input);
    }

    public static bool GetButtonUp(InputName input, LocalPlayer lPlayer)
    {
        return OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].GetButtonUp((int)input);
    }

    public static bool GetButton(ControllerInput input, LocalPlayer lPlayer)
    {
        return allGamePads[(int)OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].controllerIndex].currentXboxInput[(int)input];
    }

    public static bool GetButtonDown(ControllerInput input, LocalPlayer lPlayer)
    {
        return allGamePads[(int)OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].controllerIndex].currentXboxInputDown[(int)input];
    }

    public static bool GetButtonUp(ControllerInput input, LocalPlayer lPlayer)
    {
        return allGamePads[(int)OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].controllerIndex].currentXboxInputUp[(int)input];
    }

    public static Vector2 GetInputDirection(LocalPlayer lPlayer)
    {
        Vector2 dir = new Vector3(0,0);

        if (OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].inputType == InputType.Controller)
        {
            dir.x = allGamePads[(int)OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].controllerIndex].state.ThumbSticks.Left.X;
            dir.y = allGamePads[(int)OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].controllerIndex].state.ThumbSticks.Left.Y;
        }
        else
        {
            if (Input.GetKey(OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].keyMovement[0]))
                dir.y += 1;
            if (Input.GetKey(OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].keyMovement[1]))
                dir.x -= 1;
            if (Input.GetKey(OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].keyMovement[2]))
                dir.y -= 1;
            if (Input.GetKey(OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].keyMovement[3]))
                dir.x += 1;
        }

        return dir;
    }

    public static Vector2 GetAxis(LocalPlayer lPlayer)
    {
        Vector2 dir = new Vector3(0, 0);

        if (OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].inputType == InputType.Controller)
        {
            GamePadThumbSticks thumb = allGamePads[(int)OptionManager.currentOptions.playerInputSettings[lPlayer.playerIndex].controllerIndex].state.ThumbSticks;

            float x = thumb.Right.Y;
            float y = thumb.Right.X;

            if (x < -0.1f || x > 0.1f)
                dir.x = x;
            if (y < -0.1f || y > 0.1f)
                dir.y = y;
        }
        else
        {
            dir.x = Input.GetAxisRaw("Mouse Y");
            dir.y = Input.GetAxisRaw("Mouse X");
        }

        return dir;
    }

    public static Vector2 GetAxis(int index)
    {
        LocalPlayer lPlayer = LocalPlayerManager.localPlayers[index];
        return GetAxis(lPlayer);
    }
}

public enum ControllerInput
{
    A = 0,
    B = 1,
    X = 2,
    Y = 3,
    LeftThumbButton = 4,
    RightThumbButton = 5,
    LeftBumper = 6,
    RightBumper = 7,
    LeftTrigger = 8,
    RightTrigger = 9,
    Start = 10,
    Back = 11,
    DPadUp = 12,
    DPadDown = 13,
    DPadLeft = 14,
    DPadRight = 15,
    None = 16
}

public enum InputType
{
    MouseAndKeyboard,
    Controller
}

public enum InputName
{
    Fire = 0,
    Aim = 1,
    Jump = 2,
    Run = 3,
    Reload = 4,
    Action = 5,
    LevelEditorSwitch = 6
}