using System.Collections.Generic;
using System.DrawingCore;
using UnityEngine;

public static class STATIC {
    /// <summary>
    /// Czy jesteśmy w trakcie procesu ładowania terenu i obiektów
    /// </summary>
    private static bool _isloading = false;
    private static bool _canOpenEditor = true;
    /// <summary>
    /// Edytujemy tor. Edytor załadował mapę pomyślnie
    /// </summary>
	private static bool _isEditing = false;
    private static Element[,] _tiles;
    private static string _nazwa_trasy = "Untitled";
    private static TextAsset _flatters;
    private static TextAsset _path;
    private static TextAsset _pzeros;
    private static TextAsset _rmcs;
    private static TextAsset _kategorie;
    private static TrackSavable _track;

    public static TextAsset kategorie
    {
        get
        {
            return _kategorie;
        }
        set
        {
            _kategorie = value;
        }
    }

    public static TrackSavable TRACK
    {
        get
        {
            return _track;
        }
        set
        {
            _track = value;
        }
    }

    public static TextAsset flatters
    {
        get
        {
            return _flatters;
        }
        set
        {
            _flatters = value;
        }
    }
    public static TextAsset path
    {
        get
        {
            return _path;
        }
        set
        {
            _path = value;
        }
    }
    public static TextAsset pzeros
    {
        get
        {
            return _pzeros;
        }
        set
        {
            _pzeros = value;
        }
    }
    public static TextAsset rmcs
    {
        get
        {
            return _rmcs;
        }
        set
        {
            _rmcs = value;
        }
    }
    public static bool isloading
    {
        get
        {
            return _isloading;
        }
        set
        {
            _isloading = value;
        }
    }
    public static bool playgamePass
    {
        get
        {
            return _canOpenEditor;
        }
        set
        {
            _canOpenEditor = value;
        }
    }
    public static bool isEditing
    {
        get
        {
            return _isEditing;
        }
        set
        {
            _isEditing = value;
        }
    }
    public static Element[,] tiles
    {
        get
        {
            return _tiles;
        }
        set
        {
            _tiles = value;
        }
    }
    
    public static string nazwa_trasy
    {
        get
        {
            return _nazwa_trasy;
        }
        set
        {
            _nazwa_trasy = value;
        }
    }
   
}
