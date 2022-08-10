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

    public PlayerPrefOptionsBasis(string _key, float _value)
    {
        preferenceKey = _key;
        defaultValue = _value;
    }
}

public class OptionsPanel : MonoBehaviour
{
    //Keys for Player Preferences
    public static readonly PlayerPrefOptionsBasis MOUSE_PREF = new PlayerPrefOptionsBasis("MOUSE_SENSITIVITY", 10.0f);
    public static readonly PlayerPrefOptionsBasis SCROLL_PREF = new PlayerPrefOptionsBasis("SCROLL_SENSITIVITY",65.0f);
    public static readonly PlayerPrefOptionsBasis MUSIC_PREF = new PlayerPrefOptionsBasis("MUSIC_VOLUME", 1.0f);
    public static readonly PlayerPrefOptionsBasis SFX_PREF = new PlayerPrefOptionsBasis("SFX_VOLUME",1.0f);
    public static readonly PlayerPrefOptionsBasis FULLSCREEN_PREF = new PlayerPrefOptionsBasis("FULLSCREEN",0.0f);

    //Variables
    float mouseValue;
    float scrollValue;
    float musicValue;
    float sfxValue;
    float fullscreenValue;

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
        PlayerPrefs.SetFloat(MOUSE_PREF.preferenceKey, mouseValue);
        PlayerPrefs.SetFloat(SCROLL_PREF.preferenceKey, scrollValue);
        PlayerPrefs.SetFloat(MUSIC_PREF.preferenceKey, musicValue);
        PlayerPrefs.SetFloat(SFX_PREF.preferenceKey, sfxValue);
        PlayerPrefs.SetFloat(FULLSCREEN_PREF.preferenceKey, fullscreenValue);
    }

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
        if (PlayerPrefs.HasKey(basis.preferenceKey))
        {
            value = PlayerPrefs.GetFloat(basis.preferenceKey);
        }
        else
        {
            PlayerPrefs.SetFloat(basis.preferenceKey, basis.defaultValue);
        }
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
        PlayerLookController.sensitivity = Vector2.one * mouseValue;
        mouseSlider.value = mouseValue;
    }

    void UpdateForScroll()
    {
        PlayerWeaponHolding.scrollThreshold = scrollValue;
        scrollSlider.value = scrollValue;
    }

    void UpdateForMusic()
    {
        //TODO
        musicSlider.value = musicValue;
    }

    void UpdateForSfx()
    {
        //TODO
        sfxSlider.value = sfxValue;
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
