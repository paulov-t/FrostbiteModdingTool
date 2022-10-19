﻿using Frosty.Hash;
using FrostySdk;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.Frostbite
{
	/// <summary>
	/// A Build / Load Cache Data via FrostySDK
	/// </summary>
    public class BuildCache : ILogger
    {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="GameVersion"></param>
		/// <param name="GameLocation"></param>
		/// <param name="logger">ILogger</param>
		/// <param name="forceDeleteOfOld">Force the deletion of old Cache to rebuild it again</param>
		/// <param name="loadSDK">If you have already built the SDK, then just use the one you have</param>
		/// <returns></returns>
		public bool LoadData(string GameVersion, string GameLocation, ILogger logger = null, bool forceDeleteOfOld = false, bool loadSDK = false)
		{
			var result = LoadDataAsync(GameVersion, GameLocation, logger, forceDeleteOfOld, loadSDK).Result;
			return result;
		}
		public async Task<bool> LoadDataAsync(string GameVersion, string GameLocation, ILogger logger = null, bool forceDeleteOfOld = false, bool loadSDK = false)
		{
			Debug.WriteLine($"[DEBUG] BuildCache::LoadDataAsync({GameVersion},{GameLocation})");
			if (ProfilesLibrary.Initialize(GameVersion))
			{
				if (File.Exists(ProfilesLibrary.CacheName + ".cache") && forceDeleteOfOld)
					File.Delete(ProfilesLibrary.CacheName + ".cache");

				if (File.Exists(ProfilesLibrary.CacheName + ".CachingSBData.cache") && forceDeleteOfOld)
					File.Delete(ProfilesLibrary.CacheName + ".CachingSBData.cache");


				return await Task.Run(() => {

					if (ProfilesLibrary.RequiresKey)
					{
						KeyManager.ReadInKeys();
					}

					Debug.WriteLine($"[DEBUG] LoadDataAsync::Initialising Type Library");

					if (TypeLibrary.Initialize(loadSDK))
					{
						if (logger == null)
							logger = this;

						logger.Log("Loaded Type Library SDK");
						new FileSystem(GameLocation);
						new ResourceManager(logger);
						logger.Log("Initialised File & Resource System");
						new AssetManager(logger);
						AssetManager.Instance.RegisterLegacyAssetManager();
                        AssetManager.Instance.SetLogger(logger);
                        AssetManager.Instance.Initialize(additionalStartup: true);

						logger.Log("Initialised Asset Manager");

						return true;
					}
					return false;
				});
			}
			else
            {
				logger.LogError("Profile does not exist");
				Debug.WriteLine($"[ERROR] Failed to initialise");
			}
			return false;
		}

		private string LastMessage = null;

		public void Log(string text, params object[] vars)
        {
			if(!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(LastMessage))
			{
				Debug.WriteLine(text);
            }
			LastMessage = text;
        }

        public void LogError(string text, params object[] vars)
        {
        }

        public void LogWarning(string text, params object[] vars)
        {
        }
    }
}
