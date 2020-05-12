using System.DrawingCore;
using System.IO;
using SFB;
using UnityEngine;

public class IO
{
  public static string GetCrashdayPath()
  {
    string crashdayPath = null;

    if (PlayerPrefs.HasKey("crashpath"))
    {
      crashdayPath = PlayerPrefs.GetString("crashpath");
      if (!Directory.Exists(crashdayPath))
      {
        crashdayPath = StandaloneFileBrowser.OpenFolderPanel("Select crashday folder", "", false)[0];
        if (crashdayPath == null)
          Application.Quit();
        PlayerPrefs.SetString("crashpath", crashdayPath);
      }
    }
    else
    {
      string[] data = StandaloneFileBrowser.OpenFolderPanel("Select crashday folder", "", false);
      if (data.Length == 0)
        Application.Quit();
      else
      {
        crashdayPath = data[0];
        PlayerPrefs.SetString("crashpath", crashdayPath);
      }
    }

    if (!File.Exists(crashdayPath + "/crashday.exe"))
    {
      PlayerPrefs.DeleteKey("crashpath");
      crashdayPath = GetCrashdayPath();
    }
    return crashdayPath;
  }

  public static void RemoveCrashdayPath()
  {
    PlayerPrefs.DeleteKey("crashpath");
  }
  /// <summary>
  /// Remove a comment in a file and remove all trailing spaces
  /// </summary>
  /// <param name="input">String to be edited</param>
  /// <returns>new string with removed comments and spaces</returns>
  public static string RemoveComment(string input)
  {
    return input.IndexOf('#') > 0 ? input.Remove(input.IndexOf('#')).Trim() : input.Trim();
  }

  

  
}
