using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum OptionsType
{
    MOUSE,
    SCROLL,
    MUSIC,
    SFX,
    FULLSCREEN
}

public struct PlayerPrefOptionsBasis
{
    public string preferenceKey;
    public float defaultValue;
    public float min;
    public float max;

    public PlayerPrefOptionsBasis(string _key, float defaultValue, float min = float.NegativeInfinity, float max = float.PositiveInfinity)
    {
        preferenceKey = _key;
        this.defaultValue = defaultValue;
        this.min = min;
        this.max = max;
    }
}

public class OptionsPanel : MonoBehaviour
{
    //Keys for Player Preferences
    public static readonly PlayerPrefOptionsBasis      MOUSE_PREF = new PlayerPrefOptionsBasis( "MOUSE_SENSITIVITY", 200.0f);
    public static readonly PlayerPrefOptionsBasis     SCROLL_PREF = new PlayerPrefOptionsBasis("SCROLL_SENSITIVITY",  65.0f, min: 1);
    public static readonly PlayerPrefOptionsBasis      MUSIC_PREF = new PlayerPrefOptionsBasis(      "MUSIC_VOLUME", 100.0f);
    public static readonly PlayerPrefOptionsBasis        SFX_PREF = new PlayerPrefOptionsBasis(        "SFX_VOLUME", 100.0f);
    public static readonly PlayerPrefOptionsBasis FULLSCREEN_PREF = new PlayerPrefOptionsBasis(        "FULLSCREEN",   0.0f);

    //Variables
    float mouseValue;
    float scrollValue;
    float musicValue;
    float sfxValue;
    float fullscreenValue;

    //FMOD Global Parameters
    [FMODUnity.ParamRef]
    public string sfxVolumeParameter;
    [FMODUnity.ParamRef]
    public string musicVolumeParameter;

    //UI Components
    [SerializeField]
    Slider mouseSlider;
    [SerializeField]
    Slider scrollSlider;
    [SerializeField]
    Slider musicSlider;
    [SerializeField]
    Slider sfxSlider;
    [SerializeField]
    TextMeshProUGUI fullscreenText;

    void OnEnable()
    {
        CheckForPrefs();

        mouseSlider.onValueChanged.AddListener((v) => SetValue(v, ref mouseValue, OptionsType.MOUSE));
        scrollSlider.onValueChanged.AddListener((v) => SetValue(v, ref scrollValue, OptionsType.SCROLL));
        musicSlider.onValueChanged.AddListener((v) => SetValue(v, ref musicValue, OptionsType.MUSIC));
        sfxSlider.onValueChanged.AddListener((v) => SetValue(v, ref sfxValue, OptionsType.SFX));
    }

    void OnDisable()
    {
        Prefs_SetFloat(     MOUSE_PREF,      mouseValue);
        Prefs_SetFloat(    SCROLL_PREF,     scrollValue);
        Prefs_SetFloat(     MUSIC_PREF,      musicValue);
        Prefs_SetFloat(       SFX_PREF,        sfxValue);
        Prefs_SetFloat(FULLSCREEN_PREF, fullscreenValue);
    }

    private static void Prefs_SetFloat(PlayerPrefOptionsBasis pref, float value) => PlayerPrefs.SetFloat(pref.preferenceKey, Mathf.Clamp(value, pref.min, pref.max));

    void CheckForPrefs()
    {
        //Mouse
        CheckPref(MOUSE_PREF, ref mouseValue);
        UpdateForMouse();

        //Scroll
        CheckPref(SCROLL_PREF, ref scrollValue);
        UpdateForScroll();

        //TODO: Music
        CheckPref(MUSIC_PREF, ref musicValue);
        UpdateForMusic();

        //TODO: SFX
        CheckPref(SFX_PREF, ref sfxValue);
        UpdateForSfx();

        //Fullscreen
        CheckPref(FULLSCREEN_PREF, ref fullscreenValue);
        UpdateForFullscreen();
    }

    void CheckPref(PlayerPrefOptionsBasis basis, ref float value)
    {
        if (!PlayerPrefs.HasKey(basis.preferenceKey)) Prefs_SetFloat(basis, basis.defaultValue);
        
        value = Mathf.Clamp(PlayerPrefs.GetFloat(basis.preferenceKey), basis.min, basis.max);
    }

    void SetValue(float newValue, ref float toSet, OptionsType optionsType)
    {
        toSet = newValue;

        switch(optionsType)
        {
            case OptionsType.MOUSE:
                UpdateForMouse();
                break;
            case OptionsType.SCROLL:
                UpdateForScroll();
                break;
            case OptionsType.MUSIC:
                UpdateForMusic();
                break;
            case OptionsType.SFX:
                UpdateForSfx();
                break;
        }
    }

    void UpdateForMouse()
    {
        PlayerLookController.sensitivity = new Vector2(mouseValue,-mouseValue);
        mouseSlider.value = mouseValue;
    }

    void UpdateForScroll()
    {
        PlayerWeaponHolding.scrollThreshold = scrollValue;
        scrollSlider.value = scrollValue;
    }

    void UpdateForMusic()
    {
        musicSlider.value = musicValue;
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName(musicVolumeParameter, musicValue);
    }

    void UpdateForSfx()
    {
        sfxSlider.value = sfxValue;
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName(sfxVolumeParameter, sfxValue);
    }

    void UpdateForFullscreen()
    {
        Screen.fullScreen = fullscreenValue > 0;
        fullscreenText.text = "Fullscreen: " + (fullscreenValue > 0 ? "ON" : "OFF");
    }

    public void ToggleFullscreen()
    {
        fullscreenValue = (!Screen.fullScreen ? 1 : 0);
        UpdateForFullscreen();
    }
}
