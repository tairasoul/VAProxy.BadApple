using System.Collections;
using System.Linq;
using PixelCrushers.DialogueSystem.Wrappers;
using UnityEngine;

namespace GivingPyroNightmares;

public class BadApple : MonoBehaviour
{
	public static int width = 32;
	public static int height = 24;
	// how far away on the Y axis does each f5 have to be
	public static float YDifference = 4.11f;
	// how far away on the X axis does each f5 have to be
	public static float XDifference = 4.788f;
	public static Vector3 StartPos = new(4733.434f, 10511.81f+height*YDifference, 1221.011f);
	public static GameObject F5;
	public static GameObject PixelStorage;
	public static GameObject F5Storage;
	public static bool Finished = false;
	public static Bounds startBounds = new(
		new Vector3(4785.659f, 10508.88f, 993.5815f),
		new Vector3(37.7827f, 23.2773f, 55.8336f)
	);
	public static int currentFrame = -1;
	public void Awake() 
	{
		// reset finished and currentFrame
		Finished = false;
		currentFrame = -1;
		// set f5 object for cloning
		F5 = GameObject.Find("World").transform.Find("Areas").transform.Find("IronFactory").transform.Find("Lod0").transform.Find("F5").gameObject;
		// storage for pixel updaters
		PixelStorage = new("PixelStorage");
		// storage for f5 objects
		F5Storage = new("F5Storage");
		StartPos = new(4733.434f, 10511.81f+height*YDifference, 1221.011f);
		// start the frame rendering coroutine (starts when you're in the bounds)
		StartCoroutine(StartFrameRendering());
		// setup the f5 pixels
		StartCoroutine(SetupPixels());
	}
	
	public GameObject GetClonedF5() 
	{
		GameObject clone = GameObject.Instantiate(F5);
		// get object containing cloth
		GameObject Cloth = clone.transform.Find("RobotArmature").transform.Find("Body").transform.Find("Torso").transform.Find("Chest").transform.Find("Neck").transform.Find("Hhead").transform.Find("Corrupt").transform.Find("BeegHat").transform.Find("SM_Prop_Medical_Scanner_01 (1)").transform.Find("DA_Skirt (1)").gameObject;
		// disable the cloth component
		Cloth.GetComponent<Cloth>().enabled = false;
		// disable other components that aren't needed
		clone.GetComponent<Animator>().enabled = false;
		clone.GetComponent<CorruptF5>().enabled = false;
		clone.GetComponent<DialogueSystemTrigger>().enabled = false;
		clone.GetComponent<Dialoger>().enabled = false;
		clone.GetComponent<DialogueSystemEvents>().enabled = false;
		// parent it to f5 storage
		clone.transform.SetParent(F5Storage.transform);
		// disable f5
		clone.SetActive(false);
		return clone;
	}
	
	public IEnumerator StartFrameRendering() 
	{
		GameObject S105 = GameObject.Find("S-105.1");
		while (true) 
		{
			// wait until sen is in bounds
			if (startBounds.Contains(S105.transform.position)) 
				break;
			yield return new WaitForEndOfFrame();
		}
		// disable ais
		GameObject.Find("World").transform.Find("Ais").gameObject.SetActive(false);
		// wait a second for all ais to be properly disabled
		yield return new WaitForSeconds(1f);
		while (true) 
		{
			// 60 fps rendering
			yield return new WaitForSeconds(0.01667f);
			currentFrame++;
			// when we're beyond the last frame, delete everything and re-enable ais
			if (currentFrame == Plugin.frameData.Length) 
			{
				GameObject.Destroy(PixelStorage);
				GameObject.Destroy(F5Storage);
				GameObject.Find("World").transform.Find("Ais").gameObject.SetActive(true);
				Finished = true;
				GameObject.Destroy(gameObject);
				break;
			}
			Plugin.Log.LogInfo($"Displaying frame {currentFrame}/{Plugin.frameData.Length}");
		}
	}
	
	private IEnumerator SetupPixels() 
	{
		for (int x = 0; x < width; x++) 
		{
			StartCoroutine(SetupX(x));
		}
		yield return null;
	}
	
	private IEnumerator SetupX(int x) 
	{
		for (int y = 0; y < height; y++) 
		{
			StartCoroutine(SetupPixel(x, y));
		}
		yield return null;
	}
	
	private IEnumerator SetupPixel(int x, int y) 
	{
		Plugin.Log.LogInfo($"Creating F5 representing pixel at {x} {y}");
		Vector3 offset = new(XDifference*x, (-YDifference)*y);
		// get the f5's position offset from the start position
		Vector3 res = StartPos + offset;
		// clone f5
		GameObject clone = GetClonedF5();
		clone.transform.position = res;
		// create containre for pixel component
		GameObject pixelObj = new($"pixel{x}-{y}");
		pixelObj.transform.SetParent(PixelStorage.transform);
		// the pixel updater
		BadApplePixel pixel = pixelObj.AddComponent<BadApplePixel>();
		// set the pixel's x value
		pixel.x = x;
		// set the pixel's y value
		pixel.y = y;
		// set the object representing this pixel
		pixel.pixelObj = clone;
		yield return null;
	}
}

public class BadApplePixel: MonoBehaviour 
{
	public int x;
	public int y;
	public GameObject pixelObj;
	
	public void Update() 
	{
		// if bad apple is finished, destroy the f5 this represents and the object containing this component
		if (BadApple.Finished) 
		{
			GameObject.Destroy(pixelObj);
			GameObject.Destroy(gameObject);
		}
		// if we're not rendering, disable
		if (BadApple.currentFrame == -1) 
		{
			pixelObj.SetActive(false);
			return;
		}
		// get current frame
		Frame currentFrame = Plugin.frameData[BadApple.currentFrame];
		// get the row for this pixel
		FrameData ourRow = currentFrame.data.First((v) => v.yRow == y);
		// get the pixel
		int pixelData = ourRow.xRow[x];
		// if pixel is active, set our f5 to active
		pixelObj.SetActive(pixelData == 1);
	}
}