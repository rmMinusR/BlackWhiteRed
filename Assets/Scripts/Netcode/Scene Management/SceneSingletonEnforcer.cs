using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public sealed class SceneSingletonEnforcer : MonoBehaviour
{
    [SerializeField] private bool exitIfLoadedTwice = true;

    private void Awake()
    {
        if (FindObjectsOfType<SceneSingletonEnforcer>().Any(x => x != this && x.gameObject.scene == this.gameObject.scene))
        {
            string errMsg = "Scene "+gameObject.scene.name+" loaded multiple times!";
            ShowError(errMsg+"\nPlease report this to the devs.\nThe game will now exit.", "Fatal error");
            if (exitIfLoadedTwice) Application.Quit();
            else throw new InvalidProgramException(errMsg);
        }
    }

    private void ShowError(string text, string title)
    {
        try
        {
            MessageBox(GetActiveWindow(), text, title, (uint)(0x00000000L | 0x00000010L  | 0x00000000L           | 0x00000000L          | 0x00010000L));
                                                            //"OK" button | "Error" icon | Default button 1 (OK) | Block work in window | Set foreground
        }
        catch (Exception) { }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();
    
    [DllImport("user32.dll", SetLastError = true)]
    static extern int MessageBox(IntPtr hwnd, String lpText, String lpCaption, uint uType);
}
