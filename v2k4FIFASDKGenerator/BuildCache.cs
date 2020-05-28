﻿using FrostySdk;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Frosty.OpenFrostyFiles;

namespace v2k4FIFASDKGenerator
{
    public class BuildCache
    {
		public bool LoadData(string FIFAVersion, string FIFALocation)
		{

			if (ProfilesLibrary.Initialize(FIFAVersion))
			{
				if (ProfilesLibrary.RequiresKey)
				{
					byte[] array;

					//array = NativeReader.ReadInStream(new FileStream(ProfilesLibrary.CacheName + ".key", FileMode.Open, FileAccess.Read));
					// change this so it reads the easy version of the key
					// 0B0E04030409080C010708010E0B0B02﻿
					array = NativeReader.ReadInStream(new FileStream("fifa20.key", FileMode.Open, FileAccess.Read));
					byte[] array2 = new byte[16];
					Array.Copy(array, array2, 16);
					KeyManager.Instance.AddKey("Key1", array2);
					if (array.Length > 16)
					{
						array2 = new byte[16];
						Array.Copy(array, 16, array2, 0, 16);
						KeyManager.Instance.AddKey("Key2", array2);
						array2 = new byte[16384];
						Array.Copy(array, 32, array2, 0, 16384);
						KeyManager.Instance.AddKey("Key3", array2);
					}

					TypeLibrary.Initialize();
					ILogger logger = new NullLogger();
					AssetManagerImportResult result = new AssetManagerImportResult();

					ClassesSdkCreator.FileSystem = new FileSystem(FIFALocation);
					foreach (FileSystemSource source in ProfilesLibrary.Sources)
					{
						ClassesSdkCreator.FileSystem.AddSource(source.Path, source.SubDirs);
					}
					byte[] key = KeyManager.Instance.GetKey("Key1");
					ClassesSdkCreator.FileSystem.Initialize(key);
					ClassesSdkCreator.ResourceManager = new ResourceManager(ClassesSdkCreator.FileSystem);
					ClassesSdkCreator.ResourceManager.SetLogger(logger);
					ClassesSdkCreator.ResourceManager.Initialize();
					ClassesSdkCreator.AssetManager = new AssetManager(ClassesSdkCreator.FileSystem, ClassesSdkCreator.ResourceManager);
					LegacyFileManager.AssetManager = ClassesSdkCreator.AssetManager;
					ClassesSdkCreator.AssetManager.RegisterCustomAssetManager("legacy", typeof(LegacyFileManager));
					ClassesSdkCreator.AssetManager.SetLogger(logger);
					ClassesSdkCreator.AssetManager.Initialize(additionalStartup: true, result);
					return true;
				}
			}
			return false;
		}
	}
}