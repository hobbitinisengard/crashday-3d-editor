using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TilesetMenu : MonoBehaviour
{
	public GameObject TilesetList;
	public GameObject EntryTemplate;
	public GameObject UpperPanel;
	public GameObject NotFoundText;
	public InputField SearchField;

	private bool is_focused = false;

	void Awake()
    {
		Populate_Menu();
	}
	void OnDisable()
    {
		SearchField.text = "";
		NotFoundText.SetActive(false);
    }
	void Update()
    {
		if (SearchField.isFocused)
			is_focused = true;
		else if (is_focused && Input.GetKeyDown(KeyCode.Return))
			AddTileset(SearchField.text);
		else
			is_focused = false;
	}

	private void Populate_Menu()
	{
		string[] mod_ids = TileManager.CustomTileSections.Keys.ToArray();
		for (int i = 0; i < mod_ids.Length; i++)
		{
			GameObject NewEntry = Instantiate(EntryTemplate, EntryTemplate.transform.parent);
			SwitchAppearance(NewEntry, mod_ids[i], TileManager.CustomTileSections[mod_ids[i]].Enabled);
			NewEntry.GetComponent<RectTransform>().anchoredPosition = new Vector2(
				NewEntry.GetComponent<RectTransform>().anchoredPosition.x,
				i * -(NewEntry.GetComponent<RectTransform>().rect.height + 5) - 5);
			NewEntry.name = mod_ids[i];
			NewEntry.SetActive(true);
		}
		TilesetList.GetComponent<RectTransform>().sizeDelta = new Vector2(
			TilesetList.GetComponent<RectTransform>().sizeDelta.x,
			mod_ids.Length * (EntryTemplate.GetComponent<RectTransform>().rect.height + 5) + 5);

		if (TilesetList.transform.childCount == 1)
			UpperPanel.SetActive(false);
	}

	public void UpdateTileset(GameObject Id_GO)
	{
		string mod_id = Id_GO.GetComponent<Text>().text;
		bool enabled = TileManager.CustomTileSections[mod_id].Enabled;
		GameObject Entry = TilesetList.transform.Find(mod_id).gameObject;

		// Remove this tileset and its custom tiles from the database 
		string[] to_remove_names = TileManager.TileListInfo.Where(tile => tile.Value.Custom_tileset_id == mod_id).Select(t => t.Key).ToArray();
		foreach (var name in to_remove_names)
			TileManager.TileListInfo.Remove(name);
		TileManager.CustomTileSections.Remove(mod_id);

		// Remove folder in moddata
		Directory.Delete(IO.GetCrashdayPath() + "\\moddata\\" + mod_id + "\\", true);

		// Reload default tiles, as they could have been modified by the mod.
		TileManager.UpdateSpecificTiles(mod_id);

		// Unpack and load updated tileset
		PackageManager.LoadCPK(Directory.GetFiles(TileManager.CdWorkshopPath + mod_id).First(), mod_id);
		TileManager.LoadCustomTiles(mod_id, enabled);

		// Update tile sections in the menu
		List<string> sections = TileManager.CustomTileSections[mod_id].TileSections;
		Entry.transform.Find("Sets").GetComponent<Text>().text = string.Join(", ", sections.ToArray());
		Entry.transform.Find("button_update").gameObject.SetActive(false);
		Entry.transform.Find("button_update").gameObject.SetActive(true);
	}

	public void RemoveTileset(GameObject Id_GO)
	{
		string mod_id = Id_GO.GetComponent<Text>().text;
		GameObject Entry = TilesetList.transform.Find(mod_id).gameObject;

		// Remove this tileset and its custom tiles from the database 
		string[] to_remove_names = TileManager.TileListInfo.Where(tile => tile.Value.Custom_tileset_id == mod_id).Select(t => t.Key).ToArray();
		foreach (var name in to_remove_names)
			TileManager.TileListInfo.Remove(name);
		TileManager.CustomTileSections.Remove(mod_id);

		// Remove entry in tilesets.txt
		string[] lines_to_keep = File.ReadAllLines(Consts.tilesets_path)
			.Where(line => line != mod_id && line != "#" + mod_id).ToArray();
		File.WriteAllLines(Consts.tilesets_path, lines_to_keep);

		// Remove folder in moddata
		Directory.Delete(IO.GetCrashdayPath() + "\\moddata\\" + mod_id + "\\", true);

		// Reload default tiles, as they could have been modified by the mod.
		TileManager.UpdateSpecificTiles(mod_id);

		// Update tileset menu
		for (int i = Entry.transform.GetSiblingIndex() + 1; i < TilesetList.transform.childCount; i++)
		{
			GameObject ientry = TilesetList.transform.GetChild(i).gameObject;
			ientry.GetComponent<RectTransform>().anchoredPosition = new Vector2(ientry.GetComponent<RectTransform>().anchoredPosition.x,
				ientry.GetComponent<RectTransform>().anchoredPosition.y + ientry.GetComponent<RectTransform>().rect.height + 5);
		}
		TilesetList.GetComponent<RectTransform>().sizeDelta = new Vector2(TilesetList.GetComponent<RectTransform>().sizeDelta.x,
			TilesetList.GetComponent<RectTransform>().sizeDelta.y - Entry.GetComponent<RectTransform>().rect.height - 5);
		DestroyImmediate(Entry);

		// Disable the upper panel if we only have the invisible template remaining
		if (TilesetList.transform.childCount == 1)
			UpperPanel.SetActive(false);
	}

	public void ToggleTileset(GameObject Id_GO)
	{
		string mod_id = Id_GO.GetComponent<Text>().text;
		string[] mod_ids = File.ReadAllLines(Consts.tilesets_path).Select(x => x.Trim()).ToArray();
		bool enable = !TileManager.CustomTileSections[mod_id].Enabled;
		GameObject Entry = TilesetList.transform.Find(mod_id).gameObject;

		if (enable)
		{
			// Remove the prefix from the tileset ID
			mod_ids[Array.IndexOf(mod_ids, "#" + mod_id)] = mod_id;

			// Load the full tileset
			TileManager.LoadCustomTiles(mod_id, true);
			TileManager.CustomTileSections[mod_id].Enabled = true;
		}
		else
		{
			// Add the prefix to the tileset ID
			mod_ids[Array.IndexOf(mod_ids, mod_id)] = "#" + mod_id;

			// Remove custom tiles of this tileset
			string[] to_remove_names = TileManager.TileListInfo.Where(tile => tile.Value.Custom_tileset_id == mod_id).Select(t => t.Key).ToArray();
			foreach (var name in to_remove_names)
				TileManager.TileListInfo.Remove(name);

			// Reload default tiles, as they could have been modified by the mod.
			TileManager.UpdateSpecificTiles(mod_id);
			TileManager.CustomTileSections[mod_id].Enabled = false;
		}

		SwitchAppearance(Entry, mod_id, enable);
		Entry.transform.Find("button_toggle").gameObject.SetActive(false);
		Entry.transform.Find("button_toggle").gameObject.SetActive(true);

		// Update tilesets.txt
		File.WriteAllLines(Consts.tilesets_path, mod_ids);
	}

	public void UpdateAll()
	{
		for (int i = 0; i < TilesetList.transform.childCount; i++)
		{
			GameObject Entry = TilesetList.transform.GetChild(i).gameObject;
			if (Entry.activeSelf)
				UpdateTileset(Entry.transform.Find("Id").gameObject);
		}
		UpperPanel.transform.Find("button_update").gameObject.SetActive(false);
		UpperPanel.transform.Find("button_update").gameObject.SetActive(true);
	}

	public void RemoveAll()
	{
		for (int i = TilesetList.transform.childCount - 1; i >= 0; i--)
		{
			GameObject Entry = TilesetList.transform.GetChild(i).gameObject;
			if (Entry.activeSelf)
			{
				Debug.Log(23);
				string mod_id = Entry.transform.Find("Id").GetComponent<Text>().text;
				Directory.Delete(IO.GetCrashdayPath() + "\\moddata\\" + mod_id + "\\", true);
				DestroyImmediate(Entry);
			}
		}
		TileManager.TileListInfo.Clear();
		TileManager.CustomTileSections.Clear();
		TileManager.DefaultTiles.Clear();
		TileManager.LoadDefaultTiles();

		File.WriteAllLines(Consts.tilesets_path, new string[0]);

		UpperPanel.SetActive(false);
		TilesetList.GetComponent<RectTransform>().sizeDelta = new Vector2(TilesetList.GetComponent<RectTransform>().sizeDelta.x, 5f);
	}

	public void EnableAll()
	{
		for (int i = 0; i < TilesetList.transform.childCount; i++)
		{
			GameObject Entry = TilesetList.transform.GetChild(i).gameObject;
			string mod_id = Entry.transform.Find("Id").GetComponent<Text>().text;
			if (Entry.activeSelf && !TileManager.CustomTileSections[mod_id].Enabled)
			{
				string[] mod_ids = File.ReadAllLines(Consts.tilesets_path).Select(x => x.Trim()).ToArray();
				mod_ids[Array.IndexOf(mod_ids, "#" + mod_id)] = mod_id;
				File.WriteAllLines(Consts.tilesets_path, mod_ids);

				TileManager.LoadCustomTiles(mod_id, true);
				TileManager.CustomTileSections[mod_id].Enabled = true;

				SwitchAppearance(Entry, mod_id, true);
			}
		}
		UpperPanel.transform.Find("button_enable").gameObject.SetActive(false);
		UpperPanel.transform.Find("button_enable").gameObject.SetActive(true);
	}

	public void DisableAll()
    {
		for (int i = 0; i < TilesetList.transform.childCount; i++)
		{
			GameObject Entry = TilesetList.transform.GetChild(i).gameObject;
			string mod_id = Entry.transform.Find("Id").GetComponent<Text>().text;
			if (Entry.activeSelf && TileManager.CustomTileSections[mod_id].Enabled)
			{
				string[] mod_ids = File.ReadAllLines(Consts.tilesets_path).Select(x => x.Trim()).ToArray();
				mod_ids[Array.IndexOf(mod_ids, mod_id)] = "#" + mod_id;
				File.WriteAllLines(Consts.tilesets_path, mod_ids);

				string[] to_remove_names = TileManager.TileListInfo.Where(tile => tile.Value.Custom_tileset_id == mod_id).Select(t => t.Key).ToArray();
				foreach (string name in to_remove_names)
					TileManager.TileListInfo.Remove(name);

				TileManager.UpdateSpecificTiles(mod_id);
				TileManager.CustomTileSections[mod_id].Enabled = false;

				SwitchAppearance(Entry, mod_id, false);
			}
		}
		UpperPanel.transform.Find("button_disable").gameObject.SetActive(false);
		UpperPanel.transform.Find("button_disable").gameObject.SetActive(true);
	}

	public void AddTileset(string mod_id)
	{
		if (TileManager.CustomTileSections.ContainsKey(mod_id))
			NotFoundText.SetActive(false);

		else if (mod_id.Length != 0 && Directory.Exists(TileManager.CdWorkshopPath + mod_id + "\\")
			&& Directory.GetFiles(TileManager.CdWorkshopPath + mod_id + "\\").Length != 0)
		{
			try
			{
				PackageManager.LoadCPK(Directory.GetFiles(TileManager.CdWorkshopPath + mod_id).First(), mod_id);
				TileManager.LoadCustomTiles(mod_id, true);

				string text = File.ReadAllText(Consts.tilesets_path);
				File.WriteAllText(Consts.tilesets_path, text.Trim() + "\n" + mod_id);

				GameObject NewEntry = Instantiate(EntryTemplate, EntryTemplate.transform.parent);
				SwitchAppearance(NewEntry, mod_id, true);
				NewEntry.GetComponent<RectTransform>().anchoredPosition = new Vector2(
					NewEntry.GetComponent<RectTransform>().anchoredPosition.x,
					(TileManager.CustomTileSections.Count() - 1) * -(NewEntry.GetComponent<RectTransform>().rect.height + 5) - 5);
				NewEntry.name = mod_id;
				NewEntry.SetActive(true);
				TilesetList.GetComponent<RectTransform>().sizeDelta = new Vector2(TilesetList.GetComponent<RectTransform>().sizeDelta.x,
					TilesetList.GetComponent<RectTransform>().sizeDelta.y + NewEntry.GetComponent<RectTransform>().rect.height + 5);
				UpperPanel.SetActive(true);
			}
			catch
			{ }
			NotFoundText.SetActive(false);
		}
		else
			NotFoundText.SetActive(true);

		SearchField.text = "";
	}

	private void SwitchAppearance(GameObject Entry, string mod_id, bool enable)
	{
		Entry.transform.Find("Id").GetComponent<Text>().text = mod_id;
		Entry.transform.Find("Sets").GetComponent<Text>().text = string.Join(", ", TileManager.CustomTileSections[mod_id].TileSections.ToArray());

		if (enable)
		{
			Entry.transform.Find("Id").GetComponent<Text>().color = new Color32(255, 255, 255, 255);
			Entry.transform.Find("Sets").GetComponent<Text>().color = new Color32(255, 255, 255, 255);
			Entry.transform.Find("button_toggle").gameObject.transform.Find("Text").GetComponent<Text>().text = "Disable";
		}
		else
		{
			Entry.transform.Find("Id").GetComponent<Text>().color = new Color32(160, 160, 160, 160);
			Entry.transform.Find("Sets").GetComponent<Text>().color = new Color32(160, 160, 160, 160);
			Entry.transform.Find("button_toggle").gameObject.transform.Find("Text").GetComponent<Text>().text = "Enable";
		}
	}
}
