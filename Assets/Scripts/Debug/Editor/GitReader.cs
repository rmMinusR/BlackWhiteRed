using System;
using System.IO;
using System.Linq;

public static class GitReader
{
    private static string __gitFolder;
    private static string GitFolder
    {
        get
        {
            if (__gitFolder != null) return __gitFolder;

            DirectoryInfo dir;
            for (dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
            {
                if (dir.EnumerateDirectories().Any(i => i.Name == ".git")) break;
            }

            __gitFolder = Path.Combine(dir.FullName, ".git");
            return __gitFolder;
        }
    }

    public static string GetHead() => File.ReadLines(Path.Combine(GitFolder, "HEAD")).First();

    public static string GetBranch()
    {
        string @ref = GetHead();
        if (@ref.StartsWith("ref: "))
        {
            @ref = @ref.Substring("ref: refs/".Length); //Strip start
            @ref = @ref.Substring(@ref.IndexOf('/')+1); //Strip heads/, remotes/, tags/
            return @ref;
        }
        else return "(detached)";
    }

    public static string GetCommit()
    {
        string val = GetHead();
        if (val.StartsWith("ref: "))
        {
            //If it's a ref, read the ref file to find commit
            string refFilePath = Path.Combine(GitFolder, Path.Combine(val.Replace("ref: ", "").Split('/')));
            val = File.ReadLines(refFilePath).First();
            return val;
        }
        else
        {
            //It's already a commit
            return val;
        }
    }

    public static string GetInfoString()
    {
        return GetBranch() + "@" + GetCommit();
    }

}