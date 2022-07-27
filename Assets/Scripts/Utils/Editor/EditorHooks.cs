using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

[InitializeOnLoad]
public class EditorHooks
{
    private static readonly string PREFIX = "["+nameof(EditorHooks)+"] ";

    static EditorHooks()
    {
        //Defer until editor functions are available
        EditorApplication.update -= Hook;
        EditorApplication.update += Hook;
    }

    private static List<IImmediateEditorHook> hooks = new List<IImmediateEditorHook>();

    private static void Hook()
    {
        //Run only once per assembly load
        EditorApplication.update -= Hook;
        Debug.Log(PREFIX+"Registering hooks");

        //Hook into build system
        //NOTE: This will completely break if anything else calls it
        BuildPlayerWindow.RegisterBuildPlayerHandler(Build);

        //Capture hooks
        foreach (Type t in TypeCache.GetTypesDerivedFrom<IImmediateEditorHook>())
        {
            try
            {
                IImmediateEditorHook h = (IImmediateEditorHook) t.GetConstructors()[0].Invoke(new object[0]);
                h.Init();
                hooks.Add(h);
            } catch(Exception e)
            {
                Debug.LogError(PREFIX+"Could not hook "+t+":");
                Debug.LogException(e);
            }
        }
    }

    public static event Action<BuildPlayerOptions> OnPreBuild;

    private static void Build(BuildPlayerOptions options)
    {
        //Execute pre-build logic
        try { OnPreBuild(options); }
        catch (Exception e) { throw new BuildFailedException(e); }

        //TODO Defer actual build until next editor frame?
        //Default build behaviour
        BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
    }
}

public interface IImmediateEditorHook
{
    public void Init();
}