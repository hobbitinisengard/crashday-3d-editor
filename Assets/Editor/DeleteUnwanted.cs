using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
//Programik do robienia prostej powtarzalnej roboty w edytorze
class DeleteUnwanted : EditorWindow {
	[MenuItem ("Window/DeleteUnwanted")]
	public static void  ShowWindow () {
		EditorWindow.GetWindow(typeof(DeleteUnwanted));
	}
	void OnGUI () {
		Object[] prefabs;
		//MeshRenderer [] meshes;
		prefabs  = Resources.LoadAll("prefabs/");
        StreamWriter writer = new StreamWriter("C:\\Users\\Kuba\\Desktop\\dims.txt", true);        
        foreach (GameObject prefab in prefabs){
            writer.WriteLine(prefab.name + " " + prefab.tag);
			//meshes = prefab.GetComponentsInChildren<MeshRenderer> ();
            //prefab.transform.localRotation = Quaternion.Euler(new Vector3(0, 180, 0));
			//foreach (MeshRenderer mesh in meshes) {
			//if(mesh.name.Contains("main.") || mesh.name.Contains("decal") || mesh.name.Contains("dest") || mesh.name.Contains("metl"))
			//    mesh.enabled = false;
			//}
			//Texture2D img = Resources.Load("tiles/"+prefab.name) as Texture2D;
			//if(img.width == 64){
			//	if(img.height == 64){
			//		prefab.tag = "1x1";
			//	}else if(img.height == 128){
			//		prefab.tag = "1x2";
			//	}
			//}else if(img.width == 128){
			//	if(img.height == 64){
			//		prefab.tag = "2x1";
			//	}else if(img.height == 128){
			//		prefab.tag = "2x2";
			//	}
			//}

		}
        writer.Close();
	}
}