using System.Collections;
using System.Linq;
using PixelCrushers.DialogueSystem.Wrappers;
using UnityEngine;

namespace GivingPyroNightmares;

public class BadApple : MonoBehaviour
{
	public static int width = 32;
	public static int height = 24;
	public static float YDifference = 4.11f;
	public static float XDifference = 4.788f;
	public Vector3 StartPos = new(4727.203f, 10506.96f+height*YDifference, 1102.274f);
	public static GameObject F5;
	public static GameObject PixelStorage;
	public static GameObject F5Storage;
	public static Bounds startBounds = new(
		new Vector3(4785.659f, 10514.88f, 993.5815f),
		new Vector3(37.7827f, 23.2773f, 55.8336f)
	);
	public static int currentFrame = -1;
	public void Awake() 
	{
		F5 = GameObject.Find("World").transform.Find("Areas").transform.Find("IronFactory").transform.Find("Lod0").transform.Find("F5").gameObject;
		PixelStorage = new("PixelStorage");
		F5Storage = new("F5Storage");
		GameObject.Find("World").transform.Find("Ais").gameObject.SetActive(false);
		StartCoroutine(StartFrameRendering());
		SetupPixels();
	}
	
	public GameObject GetClonedF5() 
	{
		GameObject clone = GameObject.Instantiate(F5);
		GameObject Cloth = clone.transform.Find("RobotArmature").transform.Find("Body").transform.Find("Torso").transform.Find("Chest").transform.Find("Neck").transform.Find("Hhead").transform.Find("Corrupt").transform.Find("BeegHat").transform.Find("SM_Prop_Medical_Scanner_01 (1)").transform.Find("DA_Skirt (1)").gameObject;
		Cloth.GetComponent<Cloth>().enabled = false;
		clone.GetComponent<Animator>().enabled = false;
		clone.GetComponent<CorruptF5>().enabled = false;
		clone.GetComponent<DialogueSystemTrigger>().enabled = false;
		clone.GetComponent<Dialoger>().enabled = false;
		clone.GetComponent<DialogueSystemEvents>().enabled = false;
		clone.transform.SetParent(F5Storage.transform);
		return clone;
	}
	
	public IEnumerator StartFrameRendering() 
	{
		GameObject S105 = GameObject.Find("S-105.1");
		while (true) 
		{
			if (startBounds.Contains(S105.transform.position)) 
				break;
			yield return new WaitForEndOfFrame();
		}
		while (true) 
		{
			yield return new WaitForSeconds(0.01667f);
			if (currentFrame + 1 == Plugin.frameData.Length)
				break;
			currentFrame++;
			Plugin.Log.LogInfo($"Displaying frame {currentFrame}/{Plugin.frameData.Length}");
		}
	}
	
	public void SetupPixels() 
	{
		for (int x = 0; x < width; x++) 
		{
			for (int y = 0; y < height; y++) 
			{
				Plugin.Log.LogInfo($"Creating F5 representing pixel at {x} {y}");
				Vector3 offset = new(XDifference*x, (-YDifference)*y);
				Vector3 res = StartPos + offset;
				GameObject clone = GetClonedF5();
				clone.transform.position = res;
				GameObject pixelObj = new($"pixel{x}-{y}");
				pixelObj.transform.SetParent(PixelStorage.transform);
				BadApplePixel pixel = pixelObj.AddComponent<BadApplePixel>();
				pixel.x = x;
				pixel.y = y;
				pixel.pixelObj = clone;
			}
		}
	}
}

public class BadApplePixel: MonoBehaviour 
{
	public int x;
	public int y;
	public GameObject pixelObj;
	
	public void Update() 
	{
		if (BadApple.currentFrame == -1) 
		{
			pixelObj.SetActive(false);
			return;
		}
		Frame currentFrame = Plugin.frameData[BadApple.currentFrame];
		FrameData ourRow = currentFrame.data.First((v) => v.yRow == y);
		int pixelData = ourRow.xRow[x];
		pixelObj.SetActive(pixelData == 1);
	}
}