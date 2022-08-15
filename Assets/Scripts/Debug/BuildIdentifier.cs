#if UNITY_EDITOR

using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;

public class BuildIdentifier
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        string idFilePath = Path.Combine(Path.GetDirectoryName(pathToBuiltProject), "BUILD INFO.txt");
        if (File.Exists(idFilePath)) File.Delete(idFilePath);

        using StreamWriter idFile = File.CreateText(idFilePath);

        //Write build info header
        idFile.WriteLine("Black White Red v" + Application.version + " for " + target + " with framework v" + Application.unityVersion);
        idFile.WriteLine("Build #"+BuildInfo.BUILD_NUMBER+" "+BuildInfo.BUILD_TIME.ToString("MMM dd HH:mm:ss")+" on "+BuildInfo.COMMIT);
    }

    private static string Hash(string filePath)
    {
        using SHA384 hasher = SHA384.Create();
        using FileStream stream = File.OpenRead(filePath);
        byte[] hash = hasher.ComputeHash(stream);
        return BitConverter.ToString(hash);
    }
}

#endif