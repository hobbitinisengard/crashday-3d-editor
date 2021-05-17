using UnityEngine;
using UnityEngine.UI;
//Handles HeightSlider in BUILD mode.
//Tabs are added in Loader class
public class SliderCase : MonoBehaviour
{
  public Text title;
  public ScrollRect TabTemplate;
  public GameObject TileTemplate;
  public GameObject TilesetContainer;
  private string Current_tileset = Service.CheckpointString;
  private string[] Tilesets;

  public void InitializeSlider(string[] tilesets)
  {
    Tilesets = tilesets;
  }
  void Update()
  {
    // Don't allow switching tilesets when ctrl+mousewheel (mixing) is used
    if (Input.GetKey(KeyCode.LeftControl))
      return;
    if (Input.GetAxis("Mouse ScrollWheel") != 0)
    {
      HideCase(Current_tileset);
      if (Input.GetAxis("Mouse ScrollWheel") > 0)
        Current_tileset = GetNextTabName(Current_tileset);
      else
        Current_tileset = GetPreviousTabName(Current_tileset);

      ShowCase(Current_tileset);
      title.text = Current_tileset;
    }
  }
  public void SwitchToTileset(string tileset_name)
  {
    HideCase(Current_tileset);

    Current_tileset = tileset_name;
    
    ShowCase(Current_tileset);
    title.text = Current_tileset;
  }
  private string GetPreviousTabName(string tileset_name)
  {
    int n = TilesetContainer.transform.Find(tileset_name).GetSiblingIndex();
    if (n == 0)
      return TilesetContainer.transform.GetChild(Tilesets.Length - 1).name;
    else
      return TilesetContainer.transform.GetChild(n - 1).name;
  }

  private string GetNextTabName(string tileset_name)
  {
    int n = TilesetContainer.transform.Find(tileset_name).GetSiblingIndex();
    if (Tilesets.Length == n + 1)
      return TilesetContainer.transform.GetChild(0).name;
    else
      return TilesetContainer.transform.GetChild(n + 1).name;
  }

  private void ShowCase(string tileset_name)
  {
    TilesetContainer.transform.Find(tileset_name).gameObject.SetActive(true);
  }
  private void HideCase(string tileset_name)
  {
    TilesetContainer.transform.Find(tileset_name).gameObject.SetActive(false);
  }
}
