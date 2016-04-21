using UnityEngine;
using System.Collections;
using System.Xml.Serialization;
using System;
using System.IO;
using AngryRain.Multiplayer;
using AngryRain;
using XInputDotNetPure;

public class OptionManager : MonoBehaviour 
{
    public static Options currentOptions
    {
        get
        {
            if (_currentOptions == null)
            {
                _currentOptions = LoadOptions();
                return _currentOptions;
            }
            else
            {
                return _currentOptions;
            }
        }
        set
        {
            _currentOptions = value;
        }
    }
    private static Options _currentOptions;

    [System.Serializable]
    public class Options
    {
        [NonSerialized]
        public int selectionResolution;

        public int resolutionWidth = 1280;
        public int resolutionHeight = 720;
        public bool resolutionFullscreen = false;

        public int qualitySettings;

        public float fieldOfView = 55;
        public RenderingPath renderingPath = RenderingPath.DeferredShading;

        public int postProcessing = 1;

        public PlayerInputSettings[] playerInputSettings = new PlayerInputSettings[4];

        [System.Serializable]
        public class PlayerInputSettings
        {
            public InputType inputType = InputType.MouseAndKeyboard;

            public PlayerIndex controllerIndex = PlayerIndex.One;

            //Rotation Input
            public bool reverseRotationX = false;
            public bool reverseRotationY = false;

            //Keycboard Movement
            public KeyCode[] keyMovement = new KeyCode[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };

            //Keyboard Actions, Reference InputName enum for the index meanings
            public KeyCode[] keyboardActions = new KeyCode[] { KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Space, KeyCode.LeftShift, KeyCode.R, KeyCode.F, KeyCode.N };

            //Controller Actions
            public ControllerInput[] controllerActions = new ControllerInput[] { ControllerInput.RightTrigger, ControllerInput.LeftTrigger, ControllerInput.A, ControllerInput.LeftThumbButton, ControllerInput.X, ControllerInput.X, ControllerInput.DPadUp };

            public bool GetButton(int index)
            {
                switch (inputType)
                {
                    case InputType.MouseAndKeyboard:
                        return Input.GetKey(keyboardActions[index]);
                    case InputType.Controller:
                        return InputManager.allGamePads[(int)controllerIndex].currentXboxInput[(int)controllerActions[index]];
                }

                return false;
            }

            public bool GetButtonDown(int index)
            {
                switch (inputType)
                {
                    case InputType.MouseAndKeyboard:
                        return Input.GetKeyDown(keyboardActions[index]);
                    case InputType.Controller:
                        return InputManager.allGamePads[(int)controllerIndex].currentXboxInputDown[(int)controllerActions[index]];
                }

                return false;
            }

            public bool GetButtonUp(int index)
            {
                switch (inputType)
                {
                    case InputType.MouseAndKeyboard:
                        return Input.GetKeyUp(keyboardActions[index]);
                    case InputType.Controller:
                        return InputManager.allGamePads[(int)controllerIndex].currentXboxInputUp[(int)controllerActions[index]];
                }

                return false;
            }
        }
    }

    public static Options LoadOptions()
    {
        string targetFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/lineoffire";
        string targetFile = "config.xml";
        try
        {
            if (File.Exists(targetFolder +"/"+ targetFile))
            {
                Options op = XMLSerializer.Load<Options>(targetFolder + "/" + targetFile);
                ApplyOptions(op);
                return op;
            }
            else
            {
                Directory.CreateDirectory(targetFolder);
                Options op = new Options() { resolutionFullscreen = Screen.fullScreen, resolutionHeight = Screen.height, resolutionWidth = Screen.width, qualitySettings = QualitySettings.GetQualityLevel() };
                op.playerInputSettings = new Options.PlayerInputSettings[] { new Options.PlayerInputSettings(), new Options.PlayerInputSettings(), new Options.PlayerInputSettings(), new Options.PlayerInputSettings() };
                op.playerInputSettings[0].reverseRotationX = true;
                SaveOptions(op);
                return op;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
            Directory.CreateDirectory(targetFolder);
            Options op = new Options() { resolutionFullscreen = Screen.fullScreen, resolutionHeight = Screen.height, resolutionWidth = Screen.width, qualitySettings = QualitySettings.GetQualityLevel() };
            op.playerInputSettings = new Options.PlayerInputSettings[] { new Options.PlayerInputSettings(), new Options.PlayerInputSettings(), new Options.PlayerInputSettings(), new Options.PlayerInputSettings() };
            op.playerInputSettings[0].reverseRotationX = true;
            SaveOptions(op);
            return op;
        }
    }

    public static void SaveOptions(Options options)
    {
        XMLSerializer.Save<Options>(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/lineoffire/config.xml", options);
    }

    public static void ApplyOptions(Options options)
    {
        Screen.SetResolution(options.resolutionWidth, options.resolutionHeight, options.resolutionFullscreen);
        for(int i = 0; i < Screen.resolutions.Length; i++)
        {
            if(Screen.resolutions[i].width < options.resolutionWidth)
                options.selectionResolution = i;
        }

        QualitySettings.SetQualityLevel(options.qualitySettings);

        if (TNManager.isConnected)
        {
            PlayerCamera cam = LocalPlayerManager.localPlayers[0].playerCamera;
            if (cam != null)
            {
                cam.camera.renderingPath = options.renderingPath;

                /*cam.GetComponent<SENaturalBloomAndDirtyLens>().enabled = options.postProcessing >= 1;
                cam.GetComponent<OptVignette>().enabled = true;
                cam.GetComponent<AdvancedCA>().enabled = true;*/
            }
        }
    }
}

public class XMLSerializer
{
    public static void Save<T>(string FileName, T targetObject)
    {
        using (var writer = new System.IO.StreamWriter(FileName))
        {
            var serializer = new XmlSerializer(targetObject.GetType());
            serializer.Serialize(writer, targetObject);
            writer.Flush();
        }
    }

    public static T Load<T>(string FileName)
    {
        using (var stream = System.IO.File.OpenRead(FileName))
        {
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(stream);
        }
    }
}