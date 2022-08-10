using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public static readonly PlayerPrefOptionsBasis SCROLL_PREF = new PlayerPrefOptionsBasis("SCROLL_SENSITIVITY",60.0f);
    public static readonly PlayerPrefOptionsBasis MUSIC_PREF = new PlayerPrefOptionsBasis("MUSIC_VOLUME", 1.0f);
    public static readonly PlayerPrefOptionsBasis SFX_PREF = new PlayerPrefOptionsBasis("SFX_VOLUME",1.0f);
    public static readonly PlayerPrefOptionsBasis FULLSCREEN_PREF = new PlayerPrefOptionsBasis("FULLSCREEN",1.0f);

    //Variables
    float mouseValue;
    float scrollValue;
    float musicValue;
    float sfxValue;
    float fullscreenValue;

    void OnEnable()
    {
        CheckForPrefs();
    }

    void CheckForPrefs()
    {
        //Mouse
        CheckPref(MOUSE_PREF, ref mouseValue);
        PlayerLookController.sensitivity = Vector2.one * mouseValue;

        //Scroll
        CheckPref(SCROLL_PREF, ref scrollValue);
        PlayerWeaponHolding.scrollThreshold = mouseValue;

        //TODO: Music
        CheckPref(MUSIC_PREF, ref musicValue);

        //TODO: SFX
        CheckPref(SFX_PREF, ref sfxValue);

        //Fullscreen
        CheckPref(FULLSCREEN_PREF, ref fullscreenValue);
        Screen.fullScreen = fullscreenValue > 0;
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

    
}
