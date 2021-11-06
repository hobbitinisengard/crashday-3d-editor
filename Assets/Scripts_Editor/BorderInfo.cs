using UnityEngine;
public class Restriction_info
{

}
/// <summary>
/// Contains info about restricted borders of tile in its current rotation
/// </summary>
public class BorderInfo : MonoBehaviour
{
	/// <summary>
	/// for instance RMC1x2H1V2
	/// </summary>
	public string info;
	private BorderInfo()
	{

	}
	public static void CreateComponent(GameObject where, string RMCname)
	{//RMC1x1H1V2V1 -> Restriction Info
		if (where.GetComponent<BorderInfo>() == null)
			where.AddComponent<BorderInfo>().info = RMCname;
		else
			where.GetComponent<BorderInfo>().info = RMCname;
	}
}