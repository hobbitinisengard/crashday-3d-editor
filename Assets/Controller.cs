using System.IO;
using SFB;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public TrackSavable Track;
    public P3DModel Model;
    public string CrashdayPath = "";

	public GUISkin Skin;

	void Start ()
	{
		CrashdayPath = IO.GetCrashdayPath();
		GetComponent<PackageManager>().LoadDefaultCPKs();
		GetComponent<TileManager>().LoadTiles();
	}

    void OnGUI()
    {
	    //GUI.skin = Skin;

        if (GUI.Button(new Rect(5, 5, 160, 35), "Load map"))
        {
            string[] path = StandaloneFileBrowser.OpenFilePanel("Open trk file", CrashdayPath + "/user/", "trk", false);
            if (path.Length != 0 && path[0].Length != 0)
            {
				PlayerPrefs.SetString("lastmappath", path[0]);
                Track = MapParser.ReadMap(path[0]);
                GetComponent<TrackManager>().LoadTrack(Track);
            }
        }

	    if (GUI.Button(new Rect(175, 5, 160, 35), "Save map"))
	    {
		    string path = StandaloneFileBrowser.SaveFilePanel("Save trk file", CrashdayPath + "/user/", "my_awesome_track", "trk");
		    if (path.Length != 0)
		    {
			    MapParser.SaveMap(GetComponent<TrackManager>().CurrentTrack, path);
		    }
	    }
    }

}
