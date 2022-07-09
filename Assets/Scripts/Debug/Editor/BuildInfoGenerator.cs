using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

public class BuildInfoGenerator : IImmediateEditorHook
{
    private static readonly string PREFIX = "["+nameof(BuildInfoGenerator)+"] ";

    private static string FILEPATH;

    public void Init()
    {
        FILEPATH = null;

        string search = "Assets";
        
        //Search for file
        FILEPATH = Directory.EnumerateFiles(search, "*.cs", SearchOption.AllDirectories)
                            .First(f => File.ReadAllText(f).Contains("class " + nameof(BuildInfo)));

        Debug.Log(PREFIX+"Located "+nameof(BuildInfo)+" file: "+FILEPATH);

        //Hook into build scripting
        EditorHooks.OnPreBuild -= OnPreBuild;
        EditorHooks.OnPreBuild += OnPreBuild;
    }

    private void OnPreBuild(BuildPlayerOptions options) => RegenerateBuildInfo();

    //private static readonly Regex varPattern = new Regex(@"([A-Za-z0-9@_]+?)\s*?=\s([^;]+?);", RegexOptions.Multiline);
    private static readonly string setExpr = @"\s*?=\s([^;]+?);";

    public static void RegenerateBuildInfo()
    {
        if (FILEPATH != null)
        {
            Debug.Log(PREFIX+"Regenerating "+FILEPATH);
            string content = File.ReadAllText(FILEPATH);
            
            //Helper function
            void set(string key, string newValue)
            {
                //Locate relevant line
                Match m = new Regex(key+setExpr).Match(content);
                if (m.Success)
                {
                    content = content.Replace(m.Value, key+" = "+newValue+";");
                }
                else Debug.LogWarning(PREFIX+"Cannot locate "+key);
            }

            //Do replacement
            //set(nameof(BuildInfo.BUILD_NUMBER  ), BuildInfo.BUILD_NUMBER+1);
            set(nameof(BuildInfo.BUILD_TIME    ), $"new DateTime({DateTime.Now.Year}, {DateTime.Now.Month}, {DateTime.Now.Day}, {DateTime.Now.Hour}, {DateTime.Now.Minute}, {DateTime.Now.Second})");
            
            //Save changes
            File.WriteAllText(FILEPATH, content);
        }
    }
}
