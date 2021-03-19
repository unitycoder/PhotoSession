﻿using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.ScreenCapture;

namespace Rowlan.PhotoSession
{
	/// <summary>
	/// Create a screenshot and save it in the <see cref="ScreenshotsFolder"/>.
	/// Inside the editor the folder is parallel to the Assets folder. In build mode it's parallel to the data folder.
	/// </summary>
	public class Screenshot
	{
		private readonly string ScreenshotsFolder = "Screenshots";
		private string screenshotPath;

		public int SuperSize { get; set; } = 1;
		public bool StereoEnabled { get; set; } = false;
		public StereoScreenCaptureMode StereoScreenCaptureMode { get; set; } = StereoScreenCaptureMode.BothEyes;

		/// <summary>
		/// Screenshots path is parallel to the Assets path in edit mode or parallel to the data folder in build mode
		/// </summary>
		/// <returns></returns>
		public string GetPath()
		{
			string dataPath = Application.dataPath;

			string parentPath = Path.GetDirectoryName(dataPath);

			string screenshotPath = Path.Combine(parentPath, ScreenshotsFolder);

			return screenshotPath;
		}

		/// <summary>
		/// Create a filename using pattern "<scene name> - 2021.03.16 - 16.04.22.46.png"
		/// </summary>
		/// <returns></returns>
		private string GetFilename()
		{
			string sceneName = SceneManager.GetActiveScene().name;

			return string.Format("{0} - {1:yyyy.MM.dd - HH.mm.ss.ff}.png", sceneName, DateTime.Now);
		}

		/// <summary>
		/// Create a screenshot and save it in <see cref="screenshotPath"/>.
		/// This is using Unity's internal mechanism which isn't suited for higher than screen size resolutions.
		/// Even with super size setting the result is blurry.
		/// </summary>
		public void Capture()
		{
			string filepath = Path.Combine(this.screenshotPath, GetFilename());

			if (StereoEnabled)
			{
				ScreenCapture.CaptureScreenshot(filepath, this.StereoScreenCaptureMode);
			}
			else
			{
				ScreenCapture.CaptureScreenshot(filepath, this.SuperSize);
			}

			Debug.Log(string.Format("[<color=blue>Screenshot</color>]Screenshot captured\n<color=grey>{0}</color>", filepath));
		}

		/// <summary>
		/// Create a screenshot using the specified camera with resolution of the current screen size. 
		/// </summary>
		/// <param name="camera"></param>
		public void Capture(Camera camera)
		{
			Capture(camera, -1, -1);
		}

		/// <summary>
		/// Create a custom resolution screenshot using the specified camera. 
		/// The resolution can be higher than the screen size.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void Capture(Camera camera, int width, int height)
		{

			if (width <= 0 || height <= 0)
			{
				width = Screen.width;
				height = Screen.height;
			}

			// save data which we'll modify
			RenderTexture prevRenderTexture = RenderTexture.active;
			RenderTexture prevCameraTargetTexture = camera.targetTexture;
			bool prevCameraEnabled = camera.enabled;

			// create rendertexture
			int msaaSamples = 1;
			RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, msaaSamples);

			try
			{
				// disabling the camera is important, otherwise you get e. g. a blurry image with different focus than the one the camera displays
				// see https://docs.unity3d.com/ScriptReference/Camera.Render.html
				camera.enabled = false;

				// set rendertexture into which the camera renders
				camera.targetTexture = renderTexture;

				// render a single frame
				camera.Render();

				// create image using the camera's render texture
				RenderTexture.active = camera.targetTexture;

				Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
				screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
				screenShot.Apply();

				// save the image
				byte[] bytes = screenShot.EncodeToPNG();

				string filepath = Path.Combine(this.screenshotPath, GetFilename());

				System.IO.File.WriteAllBytes(filepath, bytes);

				Debug.Log(string.Format("[<color=blue>Screenshot</color>]Screenshot captured\n<color=grey>{0}</color>", filepath));

			}
			catch (Exception ex)
			{
				Debug.LogError("Screenshot capture exception: " + ex);
			}
			finally
			{
				RenderTexture.ReleaseTemporary(renderTexture);

				// restore modified data
				RenderTexture.active = prevRenderTexture;
				camera.targetTexture = prevCameraTargetTexture;
				camera.enabled = prevCameraEnabled;

			}

		}

		/// <summary>
		/// Ensure the screenshot path exists, create it if it doesn't exist yet.
		/// </summary>
		public void SetupPath()
		{
			this.screenshotPath = GetPath();

			if (!Directory.Exists(this.screenshotPath))
			{
				Directory.CreateDirectory(this.screenshotPath);
			}

		}

	}
}