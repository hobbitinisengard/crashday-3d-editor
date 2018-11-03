using UnityEngine;

public static class STATIC
{
    public static TextAsset Kategorie { get; set; }
    public static TrackSavable TRACK { get; set; }
    public static TextAsset Flatters { get; set; }
    public static TextAsset Path { get; set; }
    public static TextAsset Pzeros { get; set; }
    public static TextAsset Rmcs { get; set; }
    public static bool Isloading { get; set; } = false;
    public static bool PlaygamePass { get; set; } = true;
    public static bool IsEditing { get; set; } = false;
    public static Element[,] Tiles { get; set; }
    public static string Nazwa_trasy { get; set; } = "Untitled";

}
