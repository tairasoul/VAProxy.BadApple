using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
		string rawData = Encoding.UTF8.GetString(bytes);
		string size = rawData.Split('-')[0];
		ProcessSize(size);
		string frameData = rawData.Split('-')[1];
		// frames are separated by ;
		string[] frames = frameData.Split(';');
		int coroutineCount = 32;
		int framesPerCoroutine = frames.Length / coroutineCount;
		Task[] tasks = [];
		
		for (int i = 0; i < coroutineCount; i++)
		{
			int startFrame = i * framesPerCoroutine;
			int endFrame = (i == coroutineCount - 1) ? frames.Length : startFrame + framesPerCoroutine;
			Task task = Task.Run(() => ProcessFramesCoroutine(frames, startFrame, endFrame));
			tasks = [ .. tasks, task ];
		}
		
		Task.Run(() => FinalizeFrames(frames.Length));
		
		SceneManager.activeSceneChanged += SceneChanged;
	}
	
	private void ProcessSize(string size) 
	{
		string[] split = size.Split('x');
		string x = split[0];
		string y = split[1];
		BadApple.width = int.Parse(x);
		BadApple.height = int.Parse(y);
		Log.LogInfo($"Image dimensions are {size}.");
	}

	private async void ProcessFramesCoroutine(string[] frames, int start, int end)
	{
		await Task.Delay(1);
		List<Frame> processed = [];
		for (int frame = start; frame < end; frame++) 
		{
			Frame frameData = ProcessFrame(frames[frame], frame);
			if (frameData.data.Length != 0)
				processed.Add(frameData);
		}
		lock(frameData) 
		{
			frameData = [ .. frameData, .. processed ];
		}
	}
	
	public Frame ProcessFrame(string frame, int frameNumber) 
	{
		//Log.LogDebug($"Processing frame {frameNumber}");
		Frame frameData = new()
		{
			FrameNum = frameNumber,
			data = []
		};
		// rows are separated by &
		string[] rows = frame.Split('&');
		for (int y = rows.Length - 1; y >= 0; y--) 
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
		return frameData;
	}

	private async void FinalizeFrames(int frames)
	{
		while (true) 
		{
			if (frameData.Length == frames)
				break;
			await Task.Delay(1);
		}
		Log.LogInfo($"Sorting {frames} frames");
		List<Frame> list = [.. frameData];
		list.Sort((f1, f2) => f1.FrameNum.CompareTo(f2.FrameNum));
		frameData = [ .. list];
		Log.LogInfo($"Sorted {frames} frames");
	}
	
	public void SceneChanged(Scene old, Scene active) 
	{
		if (active.buildIndex == 2) 
		{
			GameObject apple = new("BadApple");
			apple.AddComponent<BadApple>();
		}
	}
}