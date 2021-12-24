using UnityEngine;
using UnityEngine.UI;

//Tabs are added in Loader class
public class SliderCase : MonoBehaviour
{
	public Text title;
	public ScrollRect TabTemplate;
	public GameObject TileDescription;
	public GameObject TileTemplate;
	public GameObject TilesetContainer;
	public GameObject HelpPanel;
	private string Current_tileset = Consts.CHKPOINTS_STR;
	private string[] Tilesets;
	private static bool hide = false;

	public void InitializeSlider(string[] tilesets)
	{
		Tilesets = tilesets;
	}

	void Update()
	{
		// Don't allow switching tilesets when ctrl+mousewheel (mixing) is used
		if (Input.GetKey(KeyCode.LeftControl))
			return;
		// Don't allow when in deleting or selecting mode
		if (Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.C) || HelpPanel.activeSelf)
			return;
		if (Input.GetKeyUp(KeyCode.G))
			hide = !hide;
		if (Input.GetAxis("Mouse ScrollWheel") != 0)
			SwitchTileset();
		if (hide && Current_tileset == Consts.DefaultTilesetName)
			SwitchTileset();
	}

	private void SwitchTileset()
    {
		HideTileDescription();
		HideCase();

		if (Input.GetAxis("Mouse ScrollWheel") < 0)
			Current_tileset = GetPreviousTabName();
		else
			Current_tileset = GetNextTabName();

		ShowCase();
		MoveTileDescription();
		title.text = Current_tileset;
	}

	public void SwitchToTileset(string tileset_name)
	{
		HideCase();

		Current_tileset = tileset_name;

		ShowCase();
		MoveTileDescription();
		title.text = Current_tileset;
	}

	private string GetPreviousTabName()
	{
		int n = TilesetContainer.transform.Find(Current_tileset).GetSiblingIndex();
		if (n == 0)
			return TilesetContainer.transform.GetChild(Tilesets.Length - 1).name;
		else
			return TilesetContainer.transform.GetChild(n - 1).name;
	}

	private string GetNextTabName()
	{
		int n = TilesetContainer.transform.Find(Current_tileset).GetSiblingIndex();
		if (Tilesets.Length == n + 1)
			return TilesetContainer.transform.GetChild(0).name;
		else
			return TilesetContainer.transform.GetChild(n + 1).name;
	}

	private void ShowCase()
	{
		TilesetContainer.transform.Find(Current_tileset).gameObject.SetActive(true);
	}

	private void HideCase()
	{
		try
		{
			TilesetContainer.transform.Find(Current_tileset).gameObject.SetActive(false);
		}
		catch
		{
			Debug.LogWarning("No tileset with name " + Current_tileset);
		}
	}

	public void ShowTileDescription(string tile_name)
	{
		TileDescription.GetComponent<Text>().text = TileManager.TileListInfo[tile_name].Description;
	}

	public void HideTileDescription()
	{
		TileDescription.GetComponent<Text>().text = "";
	}

	public void MoveTileDescription()
    {
		GameObject Tileset = TilesetContainer.transform.Find(Current_tileset).gameObject;
		Transform Content = Tileset.transform.GetChild(0).transform.GetChild(0);
		TileDescription.GetComponent<RectTransform>().anchoredPosition = new Vector2(
			TileDescription.GetComponent<RectTransform>().anchoredPosition.x,
			Mathf.Clamp(TilesetContainer.GetComponent<RectTransform>().anchoredPosition.y - Mathf.Ceil((float)Content.childCount / 6f) * 64f - 40f,
			-998, float.MaxValue));
	}
}
