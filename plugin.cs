using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GivingPyroNightmares;

public struct FrameData 
{
	public int yRow;
	public int[] xRow;
}

public struct Frame 
{
	public int FrameNum;
	public FrameData[] data;
}

[BepInPlugin("tairasoul.vaproxy.badapple", "bad apple", "1.0.0")]
public class Plugin : BaseUnityPlugin 
{
	internal static ManualLogSource Log;
	internal static Frame[] frameData = [];
	
	public void Awake() 
	{
		Log = Logger;
		string resource = "bad-apple.resources.frames";
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
		MemoryStream memStream = new();
		// frames resource is gzip compressed
		GZipStream gzip = new(stream, CompressionMode.Decompress);
		gzip.CopyTo(memStream);
		byte[] bytes = memStream.ToArray();
		string frameData = Encoding.UTF8.GetString(bytes);
		// frames are separated by ;
		string[] frames = frameData.Split(';');
		for (int frame = 0; frame < frames.Length; frame++) 
			ProcessFrame(frames[frame], frame);
		SceneManager.activeSceneChanged += SceneChanged;
	}
	
	public void ProcessFrame(string frame, int frameNumber) 
	{
		Log.LogInfo($"Processing frame {frameNumber}");
		Frame frameData = new()
		{
			FrameNum = frameNumber,
			data = []
		};
		// rows are separated by &
		string[] rows = frame.Split('&');
		for (int y = 0; y < rows.Length; y++) 
		{
			FrameData data = new()
			{
				yRow = y
			};
			int[] parsedNums = [];
			foreach (char num in rows[y]) 
			{
				// 0 represents inactive pixel (or f5 in this case)
				if (num == '0')
					parsedNums = [ .. parsedNums, 0 ];
				// 1 represents active pixel
				else
					parsedNums = [ .. parsedNums, 1 ];
			}
			data.xRow = parsedNums;
			frameData.data = [ ..frameData.data, data ];
		}
		Plugin.frameData = [ .. Plugin.frameData, frameData ];
	}
	
	public void SceneChanged(Scene old, Scene active) 
	{
		Logger.LogInfo(active.buildIndex);
		BadApple.currentFrame = -1;
		if (active.buildIndex == 2) 
		{
			GameObject apple = new("BadApple");
			apple.AddComponent<BadApple>();
		}
	}
}