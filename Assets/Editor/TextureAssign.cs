using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

//Note: only the metal/rough shader is tested at the moment. The spec/gloss shader texture assign logic might fail
//Only jpg and png textures are supported at the moment.



public class TextureAssign : EditorWindow
{
	//Albedo texture: RGB = albedo, alpha = transparency.
	//Metal/rough texture: R (from RGB) = metallic, alpha = smoothness.
	//Specular/gloss texture: RGB = specular, alpha = smoothness. 

	int maxTextureSize = 4096;
	bool replaceRedundantTextures = true;
	bool processOpacity = true;

	int redundantTextureAmount = 0;
	int assignedTextureAmount = 0;

	//Supported file formats.
	//Add more of this if you want to support more texture file formats, and also add the GetFiles code below.
	string pngExtension = "*.png";
	string jpgExtension = "*.jpg";

	//This is to set the textures of the material using material.SetTexture()
	//The source of this is in UnityStandardInput.cginc which is in the Unity build in shaders (separate download).
	string albedoTextureID = "_MainTex";
	string metallicTextureID = "_MetallicGlossMap";
	string specularTextureID = "_SpecGlossMap";
	string normalTextureID = "_BumpMap";
	string heightTextureID = "_ParallaxMap";
	string occlusionTextureID = "_OcclusionMap";
	string emissionTextureID = "_EmissionMap";

	//These are the single color parameters of the material.
	string albedoColorID = "_Color";
	string specularColorID = "_SpecColor";
	string emissionColorID = "_EmissionColor";

	//These are the material sliders.
	string metallicSliderID = "_Metallic";
	string smoothnessSliderID = "_Glossiness";

	//These are the names added to the textures when exported by Substance Painter.
	const string albedoExtension = "_AlbedoTransparency";
	const string metallicExtension = "_MetallicSmoothness";
	const string specularExtension = "_SpecularSmoothness";
	const string normalExtension = "_Normal";
	const string heightExtension = "_Height"; //This texture has to be manually added to the export preset in SP, so make sure the name is correct.
	const string occlusionExtension = "_AO"; //This texture has to be manually added to the export preset in SP, so make sure the name is correct.
	const string emissionExtension = "_Emission";

	//Texture keywords.
	string metallicKeyword = "_METALLICGLOSSMAP";
	string specularKeyword = "_SPECGLOSSMAP";
	string normalKeyword = "_NORMALMAP";
	string heightKeyword = "_PARALLAXMAP";
	string emissionKeyword = "_EMISSION";

	[MenuItem("Window/TextureAssign")]
	static void Init()
	//public static void ShowWindow()
	{
		// Get existing open window or if none, make a new one:
		TextureAssign textureAssignWindow = (TextureAssign)EditorWindow.GetWindow(typeof(TextureAssign));
		textureAssignWindow.position = new Rect(100, 200, 300, 180);

		//Change the window title here.
		GUIContent titleContent = new GUIContent("TextureAssign");
		textureAssignWindow.titleContent = titleContent;
	}

	void OnGUI()
	{
		GUILayout.Space(20);

		GUILayout.BeginHorizontal();
		replaceRedundantTextures = EditorGUILayout.Toggle("Replace redundant", replaceRedundantTextures);
		GUILayout.Space(20);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		processOpacity = EditorGUILayout.Toggle("Process opacity", processOpacity);
		GUILayout.Space(20);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Assign textures"))
		{
			float timeBegin = Time.realtimeSinceStartup;

			List<string> skipMaterialList = new List<string>();

			redundantTextureAmount = 0;
			assignedTextureAmount = 0;

			//Get all textures in the entire project.
			List<string> texturePathsList = GetTextures();

			Debug.Log(texturePathsList.Count + " textures found.");

			bool[] uniqueTextures = TagUniqueTextures(texturePathsList);

			//Get all the game objects in the scene.
			GameObject[] gameObjects = FindObjectsOfType(typeof(GameObject)) as GameObject[];

			//Loop through the game objects
			for (int i = 0; i < gameObjects.Length; i++)
			{
				if (gameObjects[i].GetComponent<Renderer>() != null)
				{
					//Get the materials on a mesh (can be more than one).
					Material[] materials = gameObjects[i].GetComponent<Renderer>().sharedMaterials;

					for(int matIndex = 0; matIndex < materials.Length; matIndex++)
					{ 
						Material material = materials[matIndex];

						if (material != null)
						{
							//Has the material been processed before and is it unique?
							bool skipMaterial = IsSkipMaterial(skipMaterialList, material);

							if (!skipMaterial)
							{
								//Find the matching texture folder for the object.
								string folder = FindObjectTextureFolder(gameObjects[i]);

								//A folder with textures for the object is found.
								if (folder != "")
								{
									//Longest execution time.
									bool textureFound = AssignFolderTexturesToObject(gameObjects[i], folder);

									if (textureFound)
									{
										//Are all textures for the object unique?
										bool unique = AreAllObjectTexturesUnique(gameObjects[i], folder, texturePathsList, uniqueTextures);

										if (unique)
										{
											//This material has been processed and its textures are unique, so skip it next time.
											skipMaterialList.Add(material.name);
										}

										else
										{
											//Do this after assigning the textures because making the material unique changes the
											//name, causing name matching to fail.
											MakeMaterialUnique(gameObjects[i]);
										}
									}

									//The textures for the object are not found in the folder, so they might be elsewhere in the project.
									//In this case it is safe to assume the textures are unique.
									else
									{
										//Assign the textures to the material of the object, trying all textures in the project.
										AssignTexturePathListToObject(gameObjects[i], material, matIndex, texturePathsList);
										skipMaterialList.Add(material.name);
									}
								}

								//Texture folder not found.
								//In this case it is safe to assume the textures are unique.
								else
								{
									//Assign the textures to the material of the object, trying all textures in the project.
									AssignTexturePathListToObject(gameObjects[i], material, matIndex, texturePathsList);
									skipMaterialList.Add(material.name);
								}
							}
						}
					}
				}		
			}

			Debug.Log(redundantTextureAmount + " redundant textures replaced with a color or value.");
			Debug.Log(assignedTextureAmount + " textures assigned.");

			float timeEnd = Time.realtimeSinceStartup;
			PrintTimeDifference(timeBegin, timeEnd);
		}
		GUILayout.EndHorizontal();


		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Assign to selected object"))
		{
			float timeBegin = Time.realtimeSinceStartup;

			redundantTextureAmount = 0;
			assignedTextureAmount = 0;
			GameObject selectedObject = null;

			List<string> texturePathsList = GetTextures();

			//Get the selected game objects.
			UnityEngine.Object[] objects = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.TopLevel);

			//An object is selected and textures are available.
			if ((objects.Length != 0) && (texturePathsList.Count >= 1))
            {
				Debug.Log(texturePathsList.Count + " textures found.");

				//Get the selected object.
				if(objects[0] is GameObject) 
				{
					selectedObject = (GameObject)objects[0];

					Material[] materials = selectedObject.GetComponent<Renderer>().sharedMaterials;

					for(int matIndex = 0; matIndex < materials.Length; matIndex++)
					{ 
						Material material = materials[matIndex];

						if(material != null)
						{
							bool textureFound = AssignTexturePathListToObject(selectedObject, material, matIndex, texturePathsList);

							if (textureFound)
							{
								//Do this after assigning the textures because making the material unique changes the
								//name, causing name matching to fail.
								MakeMaterialUnique(selectedObject);
							}

							else
							{
								Debug.Log("No matching textures found for material " + material.name);
							}
						}
					}
				}
			}

			if (objects.Length == 0)
			{
				Debug.Log("Select an object first.");
			}

			Debug.Log(redundantTextureAmount + " redundant textures replaced with a color or value.");
			Debug.Log(assignedTextureAmount + " textures assigned.");

			float timeEnd = Time.realtimeSinceStartup;
			PrintTimeDifference(timeBegin, timeEnd);
		}

		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Make material unique"))
		{
			//Get the selected game object.
			UnityEngine.Object[] objects = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.TopLevel);

			//An object is selected.
			if (objects.Length != 0)
			{
				if(objects[0] is GameObject) 
				{
					//Get the selected object.
					GameObject selectedObject = (GameObject)objects[0];

					MakeMaterialUnique(selectedObject);
				}
			}

			else
			{
				Debug.Log("Select an object first.");
			}
		}
		GUILayout.EndHorizontal();


		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Crunch"))
		{
			float timeBegin = Time.realtimeSinceStartup;

			List<string> texturePathsList = GetTextures();

			//Loop through all the textures.
			for (int i = 0; i < texturePathsList.Count; i++)
			{
				TextureImporter importer = AssetImporter.GetAtPath(texturePathsList[i]) as TextureImporter;

				importer.crunchedCompression = true;
				importer.compressionQuality = 100;

				//Apply the texture importer settings.
				AssetDatabase.WriteImportSettingsIfDirty(texturePathsList[i]);
				UnityEditor.AssetDatabase.SaveAssets();
				UnityEditor.AssetDatabase.Refresh();
			}

			float timeEnd = Time.realtimeSinceStartup;
			PrintTimeDifference(timeBegin, timeEnd);
		}
		GUILayout.Space(20);
		GUILayout.EndHorizontal();


	}



	//Has the material been processed before and is it unique?
	bool IsSkipMaterial(List<string> skipMaterialList, Material material)
	{
		//Loop through the list
		for(int i = 0; i < skipMaterialList.Count; i++)
		{
			if(material.name == skipMaterialList[i])
			{
				return true;
			}
        }

		return false;
    }

	bool IsTextureUnique(List<string> texturePathsList, bool[] uniqueTextures, string texturePath)
	{
		//Loop through all textures.
		for(int i = 0; i < texturePathsList.Count; i++)
		{
			//Is this the texture we are looking for?
			if(texturePath == texturePathsList[i])
			{
				//Is this texture unique?
				if (uniqueTextures[i] == true)
				{
					return true;
				}

				else
				{
					return false;
				}
			}
		}

		return false;
    }

	bool AssignFolderTexturesToObject(GameObject selectedObject, string folder)
	{
		List<string> texturePathsList = new List<string>();
		bool textureFound = false;

		//Get the texture paths in the selected folder.
		string[] pngPaths = Directory.GetFiles(folder, pngExtension, SearchOption.AllDirectories);
		string[] jpgPaths = Directory.GetFiles(folder, jpgExtension, SearchOption.AllDirectories);

		//Add the texture paths to a list.
		texturePathsList.AddRange(pngPaths);
		texturePathsList.AddRange(jpgPaths);

		Material[] materials = selectedObject.GetComponent<Renderer>().sharedMaterials;

		for(int matIndex = 0; matIndex < materials.Length; matIndex++)
		{ 
			Material material = materials[matIndex];

			if(material != null)
			{
				textureFound = AssignTexturePathListToObject(selectedObject, material, matIndex, texturePathsList);
			}
		}

		return textureFound;
    }

	bool AssignTexturePathListToObject(GameObject selectedObject, Material material, int matIndex, List<string> texturePathsList)
	{
		bool textureFound = false;

		//Loop through all the textures found in the folder.
		for (int i = 0; i < texturePathsList.Count; i++)
		{
			Texture2D texture;
			string texturePart;
			string textureExtension;

			bool valid = GetTextureData(out texture, out texturePart, out textureExtension, texturePathsList[i]);

			if (valid)
			{
				if (selectedObject.GetComponent<Renderer>() != null)
				{
					if (material != null)
					{
						string matName = material.name;

						matName = matName.Replace(" (Instance)", "");

						//Do the names match up?
						//if (textureName == materialName)
						if (matName == texturePart)
						{
							//Assign the texture to the material of the object.
							AssignTexture(selectedObject, material, matIndex, texture, texturePathsList[i], textureExtension);

							textureFound = true;
						}
					}

					else
					{
						Debug.Log("Select an object with a material.");
					}					
				}

				else
				{
					Debug.Log("Select an object with a material.");
				}
			}
		}

		return textureFound;
    }

	bool AreAllObjectTexturesUnique(GameObject selectedObject, string folder, List<string> allTexturesList, bool[] uniqueTextures)
	{
		bool unique = true;

        List<string> texturePathsList = new List<string>();

		//Get the texture paths in the selected folder.
		string[] pngPaths = Directory.GetFiles(folder, pngExtension, SearchOption.AllDirectories);
		string[] jpgPaths = Directory.GetFiles(folder, jpgExtension, SearchOption.AllDirectories);

		//Add the texture paths to a list.
		texturePathsList.AddRange(pngPaths);
		texturePathsList.AddRange(jpgPaths);

		//Loop through all the textures found in the folder.
		for (int i = 0; i < texturePathsList.Count; i++)
		{
			Texture2D texture;
			string texturePart;
			string textureExtension;

			bool valid = GetTextureData(out texture, out texturePart, out textureExtension, texturePathsList[i]);

			//Found.
			if (valid)
			{
				string[] parts = texturePart.Split('_');
				string textureName = parts[0];

				//Does the selected object name contain the texture name?
				if (selectedObject.name.Contains(textureName))
				{
					//Is the texture unique?
					bool thisTextureUnique = IsTextureUnique(allTexturesList, uniqueTextures, texturePathsList[i]);

					if(!thisTextureUnique)
					{
						unique = false;
						break;
                    }
                }
			}
		}

		return unique;
	}

	string FindObjectTextureFolder(GameObject selectedObject)
	{
		bool found = false;

		//Get all folders in the project.
		string[] directories = Directory.GetDirectories("Assets\\", "*", SearchOption.AllDirectories);

		Transform parent = selectedObject.transform.parent;

		string folder = "";

		//Loop through all the selected object parents.
		while (parent != null)
		{
			//Loop through all directories.
			for (int i = 0; i < directories.Length; i++)
			{
				string[] parts = directories[i].Split('\\');
				string folderName = parts[parts.Length - 1];

				string meshPartName = GetMeshPartName(parent.gameObject);

				if (meshPartName == folderName)
				{
					folder = directories[i];
					found = true;
					break;
				}
			}

			if (found)
			{
				//Bail out.
				parent = null;
			}

			else
			{
				//Find the next parent up the tree.
				parent = parent.transform.parent;
			}
		}

		return folder;
	}

	string GetMeshPartName(GameObject selectedObject)
	{
		string meshPartName = "";
		char splitChar = '.';
		bool found = false;
		string name = selectedObject.name;

		//Loop through the characters of the name string.
		for(int i = 0; i < name.Length; i++)
		{
			//Look for the first split character.
			if((name[i] == '.') || (name[i] == ':'))
			{
				splitChar = name[i];
				found = true;
				break;
            }
        }

		if (found)
		{
			string[] parts = name.Split(splitChar);
			meshPartName = parts[0];
		}

		else
		{
			meshPartName = selectedObject.name;
        }		

		return meshPartName;
	}

	List<string> GetTextures()
	{
		List<string> texturePathsList = new List<string>();
		string[] pngPaths = null;
		string[] jpgPaths = null;

		//Get the selected folders.
		UnityEngine.Object[] assets = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets);

		//One or more folders are selected.
		if (assets.Length != 0)
		{
			for (int i = 0; i < assets.Length; i++)
			{
				//Extract the path from the selected folder.
				string selectedFolderPath = AssetDatabase.GetAssetPath(assets[i]);

				//Get the texture paths in the selected folder.
				pngPaths = Directory.GetFiles(selectedFolderPath, pngExtension, SearchOption.AllDirectories);
				jpgPaths = Directory.GetFiles(selectedFolderPath, jpgExtension, SearchOption.AllDirectories);

				//Add the texture paths to a list.
				texturePathsList.AddRange(pngPaths);
				texturePathsList.AddRange(jpgPaths);
			}			
		}

		//If no folders are selected or no textures are found in the selected folder, get all textures in the project.
		if ((assets.Length == 0) || (texturePathsList.Count == 0))
		{
			//Get all the textures in the project.
			pngPaths = Directory.GetFiles("Assets\\", pngExtension, SearchOption.AllDirectories);
			jpgPaths = Directory.GetFiles("Assets\\", jpgExtension, SearchOption.AllDirectories);

			//Add the texture paths to a list.
			texturePathsList.AddRange(pngPaths);
			texturePathsList.AddRange(jpgPaths);
		}

		return texturePathsList;
    }

	void MakeMaterialUnique(GameObject inputObject)
	{
		Material newMat = null;
		int matNumber = 0;
		bool inputMaterialExists = true;
		string newMatName = "";

		if (inputObject.GetComponent<Renderer>() != null)
		{
			//Get the object material.
			Material sharedMaterial = inputObject.GetComponent<Renderer>().sharedMaterial;

			if (sharedMaterial != null)
			{
				string inputMaterialName = sharedMaterial.name;

				if (sharedMaterial == null)
				{
					inputMaterialName = "new material";
					newMatName = inputMaterialName;
					inputMaterialExists = false;
				}

				string newMaterialFolder = "Assets/Materials";

				//Create a folder if it doesn't already exist.
				bool pathExists = AssetDatabase.IsValidFolder(newMaterialFolder);
				if (pathExists == false)
				{
					AssetDatabase.CreateFolder("Assets", "Materials");
				}

				//Check if a material with the new name exists because in that case creating a material clone won't work.
				matNumber++;
				bool materialExists = DoesMaterialExist(inputMaterialName);

				//Create another name if it already exists.
				while (materialExists)
				{
					matNumber++;
					newMatName = GetNewMatName(inputMaterialName, matNumber);
					materialExists = DoesMaterialExist(newMatName);
				}

				//Create a path for the new material.
				string newMaterialPath = newMaterialFolder + "/" + newMatName + ".mat";

				//Copy an existing material.
				if (inputMaterialExists)
				{
					//Get the input material path.
					string inputMaterialPath = GetMaterialPath(inputMaterialName);

					AssetDatabase.CopyAsset(inputMaterialPath, newMaterialPath);

					//Load the new material into memory.
					newMat = (Material)(AssetDatabase.LoadAssetAtPath(newMaterialPath, typeof(Material)));
				}

				//Crate a new material.
				else
				{
					newMat = new Material(Shader.Find("Standard"));
					AssetDatabase.CreateAsset(newMat, newMaterialPath);
				}

				//Assign the new material to the input object.
				inputObject.GetComponent<Renderer>().material = newMat;
			}

			else
			{
				Debug.Log("Select an object with a material.");
			}
		}

		else
		{
			Debug.Log("Select an object with a material.");
		}
    }

	string GetNewMatName(string matName, int number)
	{
		string newMatName = "";

		//If the last part is a number, just increase the number.
		string[] parts = matName.Split('_');
		string lastPart = parts[parts.Length - 1];

		int n;
		bool isNumber = int.TryParse(lastPart, out n);

		if (isNumber)
		{
			int lastIndex = matName.LastIndexOf('_');
			newMatName = matName.Substring(0, lastIndex + 1);
			newMatName = newMatName + number;
		}

		else
		{
			newMatName = matName + "_" + number;
		}

		return newMatName;
	}

	bool[] TagUniqueTextures(List<string> texturePathsList)
	{
		bool[] uniqueTextures = new bool[texturePathsList.Count];
		string fileNameA;
		string fileNameB;

		//Initialize array.
		for(int i = 0; i < uniqueTextures.Length; i++)
		{
			uniqueTextures[i] = true;
		}

		for (int i = 0; i < texturePathsList.Count; i++)
		{
			fileNameA = Path.GetFileName(texturePathsList[i]);

			for (int e = 0; e < texturePathsList.Count; e++)
			{
				//Don't compare the file with itself.
				if(i != e)
				{
					fileNameB = Path.GetFileName(texturePathsList[e]);

					if(fileNameA == fileNameB)
					{
						uniqueTextures[i] = false;

						//This only breaks the inner loop.
						break;
					}
				}				
			}
		}

		return uniqueTextures;
	}

	//Rendering mode code can be found in StandardShaderGUI.cs
	void SetTransparent(Material mat)
	{
		float mode = mat.GetFloat("_Mode");

		if (mode != 3f)
		{
			//Set the inspector GUI.
			mat.SetFloat("_Mode", 3f);

			mat.SetOverrideTag("RenderType", "Transparent");
			mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			mat.SetInt("_ZWrite", 0);
			mat.DisableKeyword("_ALPHATEST_ON");
			mat.DisableKeyword("_ALPHABLEND_ON");
			mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
			mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
		}
	}

	//Rendering mode code can be found in StandardShaderGUI.cs
	void SetOpaque(Material mat)
	{
		float mode = mat.GetFloat("_Mode");

		if (mode != 0f)
		{
			//Set the inspector GUI.
			mat.SetFloat("_Mode", 0f);

			mat.SetOverrideTag("RenderType", "");
			mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			mat.SetInt("_ZWrite", 1);
			mat.DisableKeyword("_ALPHATEST_ON");
			mat.DisableKeyword("_ALPHABLEND_ON");
			mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			mat.renderQueue = -1;
		}
	}

	bool DoesMaterialExist(string materialName)
	{
		bool materialExists = false;

		string[] guids = AssetDatabase.FindAssets(materialName);

		for (int i = 0; i < guids.Length; i++)
		{
			string path = AssetDatabase.GUIDToAssetPath(guids[i]);

			string extension = Path.GetExtension(path);

			if (extension == ".mat")
			{
				materialExists = true;

				//Bail out.
				break;
			}
		}

		return materialExists;
	}

	string GetMaterialPath(string materialName)
	{
		string materialPath = "";

		string[] guids = AssetDatabase.FindAssets(materialName);

		for (int i = 0; i < guids.Length; i++)
		{
			string path = AssetDatabase.GUIDToAssetPath(guids[i]);

			string extension = Path.GetExtension(path);

			if (extension == ".mat")
			{
				materialPath = path;

				//Bail out.
				break;
			}
		}

		return materialPath;
	}

	void PrintTimeDifference(float timeBegin, float timeEnd)
	{
		float minutesDecimal = (timeEnd - timeBegin) / 60.0f;
		float minutes = Mathf.Floor(minutesDecimal);
		float seconds = minutesDecimal - minutes;
		seconds *= 60.0f;
		seconds = Mathf.Floor(seconds);

		Debug.Log("Completed in " + minutes + " minutes and " + seconds + " seconds.");
	}

	bool GetTextureData(out Texture2D texture, out string texturePart, out string textureExtension, string texturePath)
	{
		bool valid = true;
		texturePart = "";
		textureExtension = "";

		//Load the texture into memory.
		texture = (Texture2D)(AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)));

		if (texture != null)
		{
			//Get the texture name.
			string textureName = texture.name;

			//Find the last underscore character.
			int lastIndex = FindLastChar(textureName, '_');

			//Found.
			if (lastIndex != -1)
			{
				//Get the textureExtension
				textureExtension = textureName.Substring(lastIndex);

				//Get the second underscore, starting from the end.
				int secondLastIndex = textureName.LastIndexOf('_', lastIndex -1);

				//Found.
				if(secondLastIndex != -1) 
				{
					int length = (lastIndex - secondLastIndex) - 1;

					//Get the name without the textureExtension and mesh name.
					texturePart = textureName.Substring(secondLastIndex + 1, length);
				}

				//Not found.
				else
				{
					//Get the name without the textureExtension and mesh name.
					texturePart = textureName.Substring(0, lastIndex);
				}
			}

			else 
			{
				valid = false;
			}

			

			//Get the amount of underscores in the texture name.
			//int underscoreAmount = textureName.Split('_').Length - 1;





		}
		
		return valid;
	}

	static int FindLastChar(string arr, char value)
	{
		for (int i = (arr.Length-1); i >= 0; i--)
		{
			if (arr[i] == value)
			{
				return i;
			}
		}
		return -1;
	}

	//Returns rgbSame true if all RGB colors are the same, false otherwise.
	//The color output is the detected color.
	void IsRGBASame(out bool rgbaSame, out Color32 color, Color32[] pixelArray)
	{
		rgbaSame = true;
		color = pixelArray[0];

		for (int i = 0; i < pixelArray.Length; i++)
		{
			//Check if the current color is not the same as the first color in the array.
			if ((pixelArray[i].r != pixelArray[0].r) || (pixelArray[i].g != pixelArray[0].g) || (pixelArray[i].b != pixelArray[0].b) || (pixelArray[i].a != pixelArray[0].a))
			{
				rgbaSame = false;
				color = pixelArray[i];

				//Bail out.
				break;
			}
		}
	}

	//Returns metallicSame true if the metallic color (red channel) is the same, false otherwise.
	//The float output it the detected metal value.
	void IsMetallicSame(out bool metallicSame, out float metallic, Color32[] pixelArray)
	{
		metallicSame = true;
		metallic = pixelArray[0].r / 255.0f;

		for (int i = 0; i < pixelArray.Length; i++)
		{
			//Check if the current color is not the same as the first color in the array.
			if (pixelArray[i].r != pixelArray[0].r)
			{
				metallicSame = false;
				metallic = pixelArray[i].r / 255.0f;

				//Bail out.
				break;
			}
		}
	}

	//Returns smoothnessSame true if the smoothness color (alpha channel) is the same, false otherwise.
	//The float output it the detected smoothness value.
	void IsSmoothnessSame(out bool smoothnessSame, out float smoothness, Color32[] pixelArray)
	{
		smoothnessSame = true;
		smoothness = pixelArray[0].a / 255.0f;

		for (int i = 0; i < pixelArray.Length; i++)
		{
			//Check if the current color is not the same as the first color in the array.
			if (pixelArray[i].a != pixelArray[0].a)
			{
				smoothnessSame = false;
				smoothness = pixelArray[i].a / 255.0f;

				//Bail out.
				break;
			}
		}
	}

	//Returns alphaPresent true if any pixel has an alpha value of less then 255.
	bool IsAlphaPresent(Color32[] pixelArray)
	{
		bool alphaPresent = false;

		for (int i = 0; i < pixelArray.Length; i++)
		{
			if (pixelArray[i].a < 255)
			{
				alphaPresent = true;

				//Bail out.
				break;
			}
		}

		return alphaPresent;
	}


	void AssignTexture(GameObject selectedObject, Material material, int matIndex, Texture2D texture, string texturePath, string textureExtension)
	{
		Color32[] pixelArray = null;
        bool rgbaSame = false;
		bool metalSame = false;
		bool smoothnessSame = false;
		Color32 rgbaColor;
		float metallicValue;
		float smoothnessValue;

		TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;

		//Set the maximum texture size.
		importer.maxTextureSize = maxTextureSize;

		if (replaceRedundantTextures || processOpacity)
		{
			//Set the texture to readable to enable GetPixels. Note that the texture will consume much more memory 
			//because of this. Set it to non-readable when done.
			importer.isReadable = true;

			//Apply the texture importer settings.
			AssetDatabase.WriteImportSettingsIfDirty(texturePath);
			UnityEditor.AssetDatabase.SaveAssets();
			UnityEditor.AssetDatabase.Refresh();

			//Convert the texture to an array of colors.
			pixelArray = texture.GetPixels32();
		}

		switch (textureExtension)
		{
			case albedoExtension:

				if(replaceRedundantTextures == true)
				{
					IsRGBASame(out rgbaSame, out rgbaColor, pixelArray);

					if (rgbaSame)
					{
						//Remove texture.
						material.SetTexture(albedoTextureID, null);

						//Set a uniform color.
						material.SetColor(albedoColorID, rgbaColor);

						redundantTextureAmount++;
					}
				}

				if ((replaceRedundantTextures == false) || ((replaceRedundantTextures == true) && (rgbaSame == false)) )
				{
					//Assign the texture
					material.SetTexture(albedoTextureID, texture);

					//This is important, otherwise the texture will look wrong if a texture is used).
					material.SetColor(albedoColorID, Color.white);

					assignedTextureAmount++;
				}

				if (processOpacity)
				{
					bool alphaPresent = IsAlphaPresent(pixelArray);

					if (alphaPresent)
					{
						SetTransparent(material);
					}

					else
					{
						SetOpaque(material);
					}
				}							

				break;

			case metallicExtension:

				if(replaceRedundantTextures == true)
				{
					//The metallic texture contains both a metallic and smoothness value.
					IsMetallicSame(out metalSame, out metallicValue, pixelArray);
					IsSmoothnessSame(out smoothnessSame, out smoothnessValue, pixelArray);

					if (metalSame && smoothnessSame)
					{
						//Remove texture.
						material.SetTexture(metallicTextureID, null);

						//Set the metal HeightSlider.
						material.SetFloat(metallicSliderID, metallicValue);

						//Set the smoothness HeightSlider.
						material.SetFloat(smoothnessSliderID, smoothnessValue);

						material.DisableKeyword(metallicKeyword);

						redundantTextureAmount++;
					}
				}

				if ((replaceRedundantTextures == false) || ((replaceRedundantTextures == true) && !(metalSame && smoothnessSame)))
				{
					material.EnableKeyword(metallicKeyword);
					material.SetTexture(metallicTextureID, texture);
					assignedTextureAmount++;
				}

				break;

			case specularExtension:

				if (replaceRedundantTextures == true)
				{
					//The specular texture contains both a specular and smoothness value.
					IsRGBASame(out rgbaSame, out rgbaColor, pixelArray);
					IsSmoothnessSame(out smoothnessSame, out smoothnessValue, pixelArray);

					if (rgbaSame && smoothnessSame)
					{
						//Remove texture.
						material.SetTexture(specularTextureID, null);

						//Set the specular color.
						material.SetColor(specularColorID, rgbaColor);

						//Set the smoothness HeightSlider.
						material.SetFloat(smoothnessSliderID, smoothnessValue);

						material.DisableKeyword(specularKeyword);

						redundantTextureAmount++;
					}
				}

				if ((replaceRedundantTextures == false) || ((replaceRedundantTextures == true) && !(rgbaSame && smoothnessSame)))
				{
					material.EnableKeyword(specularKeyword);
					material.SetTexture(specularTextureID, texture);
					assignedTextureAmount++;
				}

				break;

			case normalExtension:

				if (replaceRedundantTextures == true)
				{
					IsRGBASame(out rgbaSame, out rgbaColor, pixelArray);

					if (rgbaSame)
					{
						//Remove texture.
						material.SetTexture(normalTextureID, null);
						material.DisableKeyword(normalKeyword);
						redundantTextureAmount++;
					}
				}

				if ((replaceRedundantTextures == false) || ((replaceRedundantTextures == true) && (rgbaSame == false)))
				{
					material.EnableKeyword(normalKeyword);

					//A normal map should be marked as a normal map.					
					importer.textureType = TextureImporterType.NormalMap;

					AssetDatabase.WriteImportSettingsIfDirty(texturePath);

					material.SetTexture(normalTextureID, texture);
					assignedTextureAmount++;

					UnityEditor.AssetDatabase.SaveAssets();
					UnityEditor.AssetDatabase.Refresh();
				}

				break;

			case heightExtension:

				if (replaceRedundantTextures == true)
				{
					IsRGBASame(out rgbaSame, out rgbaColor, pixelArray);

					if (rgbaSame)
					{
						//Remove texture.
						material.SetTexture(heightTextureID, null);
						material.DisableKeyword(heightKeyword);
						redundantTextureAmount++;
					}
				}

				if ((replaceRedundantTextures == false) || ((replaceRedundantTextures == true) && (rgbaSame == false)))
				{
					material.EnableKeyword(heightKeyword);
					material.SetTexture(heightTextureID, texture);
					assignedTextureAmount++;
				}

				break;

			case occlusionExtension:

				if (replaceRedundantTextures == true)
				{
					IsRGBASame(out rgbaSame, out rgbaColor, pixelArray);

					if (rgbaSame)
					{
						//Remove texture.
						material.SetTexture(occlusionTextureID, null);
						redundantTextureAmount++;
					}
				}

				if ((replaceRedundantTextures == false) || ((replaceRedundantTextures == true) && (rgbaSame == false)))
				{
					material.SetTexture(occlusionTextureID, texture);
					assignedTextureAmount++;
				}
				
				break;

			case emissionExtension:

				if (replaceRedundantTextures == true)
				{
					IsRGBASame(out rgbaSame, out rgbaColor, pixelArray);

					if (rgbaSame)
					{
						//The emission shader keyword has to be enabled even if no texture is used,
						//otherwise the shader won't refresh.
						material.EnableKeyword(emissionKeyword);

						//Remove texture.
						material.SetTexture(emissionTextureID, null);
						material.SetColor(emissionColorID, rgbaColor);
						redundantTextureAmount++;
					}
				}

				if ((replaceRedundantTextures == false) || ((replaceRedundantTextures == true) && (rgbaSame == false)))
				{
					material.EnableKeyword(emissionKeyword);
					material.SetTexture(emissionTextureID, texture);
					assignedTextureAmount++;

					//This is important, otherwise the texture will look wrong if a texture is used).
					material.SetColor(emissionColorID, Color.white);
				}

				break;

			default:
			break;
		}

		selectedObject.GetComponent<Renderer>().sharedMaterials[matIndex] = material;

		if (replaceRedundantTextures || processOpacity)
		{
			//Set the texture to non-readable again, to free up memory.
			importer.isReadable = false;
			AssetDatabase.WriteImportSettingsIfDirty(texturePath);
			UnityEditor.AssetDatabase.SaveAssets();
			UnityEditor.AssetDatabase.Refresh();
		}
		
	}
}
 