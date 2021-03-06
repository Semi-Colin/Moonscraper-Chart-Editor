﻿// Copyright (c) 2016-2020 Alexander Ong
// See LICENSE in project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MSE.Input;

public static class GameSettings
{
    const string SECTION_NAME_SETTINGS = "Settings";
    const string SECTION_NAME_AUDIO = "Audio Volume";

    [System.Flags]
    public enum ClapToggle
    {
        NONE = 0,
        
        STRUM           = 1 << 0,
        HOPO            = 1 << 1,
        TAP             = 1 << 2,

        STARPOWER       = 1 << 3,
        CHARTEVENT      = 1 << 4,

        BPM             = 1 << 5,
        TS              = 1 << 6,
        EVENT           = 1 << 7,
        SECTION         = 1 << 8,
    }

    public enum NotePlacementMode
    {
        Default, LeftyFlip
    }

    public enum DrumModeOptions
    {
        Standard,
        ProDrums,
    }

    public static bool keysModeEnabled = false;
    public static bool metronomeActive = false;
    public static bool extendedSustainsEnabled = false;
    public static bool sustainGapEnabled { get; set; }
    public static bool resetAfterPlay = false;
    public static bool resetAfterGameplay = false;

    public static int audioCalibrationMS = 0;                     // Increase to start the audio sooner
    public static int clapCalibrationMS = 0;
    public static int customBgSwapTime;
    public static int targetFramerate = -1;
    public static int drumsLaneCount = 5;
    public static DrumModeOptions drumsModeOptions = DrumModeOptions.Standard;

    public static float hyperspeed = 5.0f;
    public static float highwayLength = 0;
    static float _sfxVolume = 1;
    public static float gameSpeed = 1;
    public static float gameplayStartDelayTime = 3.0f;
    public static float sfxVolume
    {
        get { return _sfxVolume; }
        set
        {
            if (value < 0)
                _sfxVolume = 0;
            else if (value > 1)
                _sfxVolume = 1;
            else
                _sfxVolume = value;
        }
    }
    public static float vol_master, vol_song, vol_guitar, vol_bass, vol_rhythm, vol_keys, vol_drums, vol_vocals, audio_pan, vol_crowd;

    public static Step snappingStep = new Step(16);
    public static int step { get { return snappingStep.value; } set { snappingStep.value = value; } }
    public static Step sustainGapStep;
    public static int sustainGap { get { return sustainGapStep.value; } set { sustainGapStep.value = value; } }

    public static bool clapEnabled = false;

    const int c_defaultClapVal = (int)(ClapToggle.STRUM | ClapToggle.HOPO | ClapToggle.TAP);
    public static ClapToggle clapProperties = (ClapToggle)c_defaultClapVal;
    public static NotePlacementMode notePlacementMode = NotePlacementMode.Default;

    public static SongValidate.ValidationOptions songValidatorModes = ~SongValidate.ValidationOptions.None;
    public static bool autoValidateSongOnSave = true;

    public static MSChartEditorInput.MSChartEditorActionContainer controls = new MSChartEditorInput.MSChartEditorActionContainer();

    public static bool GetBoolSetting(string identifier)
    {
        switch (identifier)
        {
            case ("keysModeEnabled"): return keysModeEnabled;
            case ("metronomeActive"): return metronomeActive;
            case ("extendedSustainsEnabled"): return extendedSustainsEnabled;
            case ("sustainGapEnabled"): return sustainGapEnabled;
            case ("resetAfterPlay"): return resetAfterPlay;
            case ("resetAfterGameplay"): return resetAfterGameplay;
            default: return false;
        }
    }

    static GameSettings()
    {
    }

    public static void Load(string configFilepath, string controllerBindingsFilepath)
    {
        INIParser iniparse = new INIParser();

        try
        {
            Debug.Log("Loading game settings");
         
            iniparse.Open(configFilepath);

            // Check for valid fps values
            targetFramerate = iniparse.ReadValue(SECTION_NAME_SETTINGS, "Framerate", 120);
            hyperspeed = (float)iniparse.ReadValue(SECTION_NAME_SETTINGS, "Hyperspeed", 5.0f);
            highwayLength = (float)iniparse.ReadValue(SECTION_NAME_SETTINGS, "Highway Length", 0);
            audioCalibrationMS = iniparse.ReadValue(SECTION_NAME_SETTINGS, "Audio calibration", 0);
            clapCalibrationMS = iniparse.ReadValue(SECTION_NAME_SETTINGS, "Clap calibration", 0);
            clapProperties = (ClapToggle)iniparse.ReadValue(SECTION_NAME_SETTINGS, "Clap", c_defaultClapVal);
            extendedSustainsEnabled = iniparse.ReadValue(SECTION_NAME_SETTINGS, "Extended sustains", false);
            clapEnabled = false;
            sustainGapEnabled = iniparse.ReadValue(SECTION_NAME_SETTINGS, "Sustain Gap", false);
            sustainGapStep = new Step((int)iniparse.ReadValue(SECTION_NAME_SETTINGS, "Sustain Gap Step", (int)16));
            notePlacementMode = (NotePlacementMode)iniparse.ReadValue(SECTION_NAME_SETTINGS, "Note Placement Mode", (int)NotePlacementMode.Default);
            gameplayStartDelayTime = (float)iniparse.ReadValue(SECTION_NAME_SETTINGS, "Gameplay Start Delay", 3.0f);
            resetAfterPlay = iniparse.ReadValue(SECTION_NAME_SETTINGS, "Reset After Play", false);
            resetAfterGameplay = iniparse.ReadValue(SECTION_NAME_SETTINGS, "Reset After Gameplay", false);
            customBgSwapTime = iniparse.ReadValue(SECTION_NAME_SETTINGS, "Custom Background Swap Time", 30);
            drumsLaneCount = iniparse.ReadValue(SECTION_NAME_SETTINGS, "Drums Lane Count", 5);
            drumsModeOptions = (DrumModeOptions)iniparse.ReadValue(SECTION_NAME_SETTINGS, "Drums Mode", (int)DrumModeOptions.Standard);
            if (!System.Enum.IsDefined(typeof(DrumModeOptions), drumsModeOptions))
            {
                drumsModeOptions = DrumModeOptions.Standard;
            }
            songValidatorModes = (SongValidate.ValidationOptions)iniparse.ReadValue(SECTION_NAME_SETTINGS, "Song Validator Modes", (int)(~SongValidate.ValidationOptions.None));
            autoValidateSongOnSave = iniparse.ReadValue(SECTION_NAME_SETTINGS, "Auto Validate Song On Save", true);

            gameplayStartDelayTime = Mathf.Clamp(gameplayStartDelayTime, 0, 3.0f);
            gameplayStartDelayTime = (float)(System.Math.Round(gameplayStartDelayTime * 2.0f, System.MidpointRounding.AwayFromZero) / 2.0f); // Check that the gameplay start delay time is a multiple of 0.5 and is

            // Audio levels
            vol_master = (float)iniparse.ReadValue(SECTION_NAME_AUDIO, "Master", 0.5f);
            vol_song = (float)iniparse.ReadValue(SECTION_NAME_AUDIO, "Music Stream", 1.0f);
            vol_guitar = (float)iniparse.ReadValue(SECTION_NAME_AUDIO, "Guitar Stream", 1.0f);
            vol_bass = (float)iniparse.ReadValue(SECTION_NAME_AUDIO, "Bass Stream", 1.0f);
            vol_rhythm = (float)iniparse.ReadValue(SECTION_NAME_AUDIO, "Rhythm Stream", 1.0f);
			vol_keys = (float)iniparse.ReadValue(SECTION_NAME_AUDIO, "Keys Stream", 1.0f);
			vol_vocals = (float)iniparse.ReadValue(SECTION_NAME_AUDIO, "Vocals Stream", 1.0f);
            vol_drums = (float)iniparse.ReadValue(SECTION_NAME_AUDIO, "Drum Stream", 1.0f);
            vol_crowd = (float)iniparse.ReadValue(SECTION_NAME_AUDIO, "Crowd Stream", 1.0f);
            audio_pan = (float)iniparse.ReadValue(SECTION_NAME_AUDIO, "Audio Pan", 0.0f);
            sfxVolume = (float)iniparse.ReadValue(SECTION_NAME_AUDIO, "SFX", 1.0f);
			
            // Need to fix old config values
            if ((int)clapProperties > (((int)ClapToggle.SECTION << 1) - 1))
            {
                clapProperties = (ClapToggle)c_defaultClapVal;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error encountered when trying to load game settings. " + e.Message);
        }
        finally
        {
            iniparse.Close();
        }

        try
        {
            // Populate with all default controls first
            controls.LoadFromSaveData(InputManager.Instance.defaultControls);

            if (File.Exists(controllerBindingsFilepath))
            {
                // Override with custom controls if they exist.
                Debug.Log("Loading input settings");
                string controlsJson = File.ReadAllText(controllerBindingsFilepath);
                controls.LoadFromSaveData(JsonUtility.FromJson<MSChartEditorInput.MSChartEditorActionContainer>(controlsJson));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Unable to load saved controls. " + e.Message);
        }
    }

    public static void Save(string configFilepath, string controllerBindingsFilepath)
    {
        INIParser iniparse = new INIParser();

        try
        {
            Debug.Log("Saving game settings");

            iniparse.Open(configFilepath);

            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Framerate", targetFramerate);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Hyperspeed", hyperspeed);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Highway Length", highwayLength);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Audio calibration", audioCalibrationMS);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Clap calibration", clapCalibrationMS);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Clap", (int)clapProperties);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Extended sustains", extendedSustainsEnabled);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Sustain Gap", sustainGapEnabled);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Sustain Gap Step", sustainGap);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Note Placement Mode", (int)notePlacementMode);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Gameplay Start Delay", gameplayStartDelayTime);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Reset After Play", resetAfterPlay);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Reset After Gameplay", resetAfterGameplay);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Custom Background Swap Time", customBgSwapTime);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Drums Lane Count", drumsLaneCount);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Drums Mode", (int)drumsModeOptions);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Song Validator Modes", (int)songValidatorModes);
            iniparse.WriteValue(SECTION_NAME_SETTINGS, "Auto Validate Song On Save", autoValidateSongOnSave);

            // Audio levels
            iniparse.WriteValue(SECTION_NAME_AUDIO, "Master", vol_master);
            iniparse.WriteValue(SECTION_NAME_AUDIO, "Music Stream", vol_song);
            iniparse.WriteValue(SECTION_NAME_AUDIO, "Guitar Stream", vol_guitar);
            iniparse.WriteValue(SECTION_NAME_AUDIO, "Bass Stream", vol_bass);
            iniparse.WriteValue(SECTION_NAME_AUDIO, "Rhythm Stream", vol_rhythm);
			iniparse.WriteValue(SECTION_NAME_AUDIO, "Keys Stream", vol_keys);
			iniparse.WriteValue(SECTION_NAME_AUDIO, "Vocals Stream", vol_vocals);
            iniparse.WriteValue(SECTION_NAME_AUDIO, "Drum Stream", vol_drums);
            iniparse.WriteValue(SECTION_NAME_AUDIO, "Crowd Stream", vol_crowd);
            iniparse.WriteValue(SECTION_NAME_AUDIO, "Audio Pan", audio_pan);
            iniparse.WriteValue(SECTION_NAME_AUDIO, "SFX", sfxVolume);			
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error encountered when trying to save game settings. " + e.Message);
        }
        finally
        {
            iniparse.Close();
        }

        controls.UpdateSaveData();
        var controlsJson = JsonUtility.ToJson(controls, true);
        try
        {
            Debug.Log("Saving input settings");

            // Save to file
            File.WriteAllText(controllerBindingsFilepath, controlsJson, System.Text.Encoding.UTF8);
        }
        catch (System.Exception e)
        {
            Logger.LogException(e, "Error encountered while saving control bindings. " + e.Message);
        }
    }
}

