using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;



// Token: 0x02000006 RID: 6
public static class HarmonyLoader
{
    private static string GetHarmonyDllPath()
    {
		string directoryName = Path.GetDirectoryName(typeof(HarmonyLoader).Assembly.Location);
        return Path.Combine(directoryName, "0Harmony.dll");
    }

	public static void DeleteLog(){
				// Clear the harmony_log.txt file explicitly
			string logFilePath = "harmony_log.txt"; // Matches HARMONY_LOG_FILE default
			try
			{
				File.WriteAllText(logFilePath, ""); // Overwrite with empty content
				Debug.Log("Cleared harmony_log.txt file.");
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to clear harmony_log.txt: " + ex.Message);
			}
	}


public static string LoadHarmonyMods(string modsFolderPath)
{
    System.Text.StringBuilder logBuilder = new System.Text.StringBuilder();
    int modsLoadedCount = 0;
    int potentialModsFound = 0;

    if (string.IsNullOrEmpty(modsFolderPath))
    {
        logBuilder.AppendLine("Harmony mods folder path cannot be null or empty / not available in-editor");
        return logBuilder.ToString();
    }

    try
    {
        HarmonyLoader.modPath = modsFolderPath;

        string harmonyLoadResult = HarmonyLoader.EnsureHarmonyIsLoaded();
        logBuilder.AppendLine(harmonyLoadResult);

        // Check if Harmony loaded successfully before proceeding
        if (HarmonyLoader.harmonyAssembly == null)
        {
            logBuilder.AppendLine("Aborting mod loading: Harmony initialization failed.");
            return logBuilder.ToString();
        }

        try
        {
            HarmonyLoader.Harmony.DEBUG = true;
            logBuilder.AppendLine("Debug mode enabled");
        }
        catch (Exception ex)
        {
            logBuilder.AppendLine("Failed to enable Harmony debug mode: " + ex.Message);
            return logBuilder.ToString();
        }

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HARMONY_LOG_FILE")))
        {
            Environment.SetEnvironmentVariable("HARMONY_LOG_FILE", "harmony_log.txt");
            logBuilder.AppendLine("Set HARMONY_LOG_FILE to 'harmony_log.txt'.");
        }

        try
        {
            HarmonyLoader.FileLog.Reset();
            logBuilder.AppendLine("Harmony file log reset.");
        }
        catch (Exception ex)
        {
            logBuilder.AppendLine("Warning: Failed to reset Harmony file log: " + ex.Message);
        }

        if (!Directory.Exists(modsFolderPath))
        {
            try
            {
                Directory.CreateDirectory(modsFolderPath);
                logBuilder.AppendLine("Created Harmony mods directory at: '" + Path.GetFullPath(modsFolderPath) + "'");
            }
            catch (Exception ex)
            {
                logBuilder.AppendLine("Failed to create Harmony mods directory: " + ex.Message);
            }
        }
        
        if (Directory.Exists(modsFolderPath))
        {

            try
            {
                HarmonyLoader.AssemblyResolver.AddSearchDirectory(modsFolderPath);
                logBuilder.AppendLine("Mods directory: '" + Path.GetFullPath(modsFolderPath) + "'");
            }
            catch (Exception ex)
            {
                logBuilder.AppendLine("Failed to set assembly resolver directories: " + ex.Message);
                return logBuilder.ToString();
            }

            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args)
            {
                logBuilder.AppendLine("Trying to load assembly: " + args.Name);
                string text2 = args.Name.Split(',', StringSplitOptions.None)[0];
                if (text2.StartsWith("MonoMod."))
                {
                    return HarmonyLoader.harmonyAssembly;
                }
                Assembly result;
                if (HarmonyLoader.assemblyNames.TryGetValue(text2, out result))
                {
                    return result;
                }
                AssemblyName assemblyName = new AssemblyName(text2);
                string text3 = Path.Combine(modsFolderPath, assemblyName.Name + ".dll");
                if (!File.Exists(text3))
                {
                    return null;
                }
                return HarmonyLoader.LoadAssembly(text3);
            };

            string[] dllFiles = Directory.EnumerateFiles(modsFolderPath, "*.dll").ToArray();
            if (dllFiles.Length == 0)
            {
                logBuilder.AppendLine("No mods found");
            }
            else
            {
                foreach (string text in dllFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(text);
                    if (!string.IsNullOrEmpty(text) && !HarmonyLoader.IsKnownDependency(fileName))
                    {
                        potentialModsFound++;
                        if (HarmonyLoader.TryLoadMod(text))
                        {
                            string modName = fileName;
                            logBuilder.AppendLine($"Successfully loaded mod: '{modName}'");
                            modsLoadedCount++;
                        }
                    }
                    else if (HarmonyLoader.IsKnownDependency(fileName))
                    {
                        logBuilder.AppendLine($"Skipped '{fileName}.dll' - identified as a known dependency.");
                    }
                }

                if (potentialModsFound == 0)
                {
                    logBuilder.AppendLine("No valid Harmony mods found (all DLLs were known dependencies).");
                }
                else if (modsLoadedCount == 0)
                {
                    logBuilder.AppendLine($"No Harmony mods were successfully loaded out of {potentialModsFound} potential mods.");
                }
                else
                {
                    logBuilder.AppendLine($"Harmony mod loading completed. Total mods loaded: {modsLoadedCount} out of {potentialModsFound} potential mods.");
                }
            }
        }
        else
        {
            logBuilder.AppendLine("Harmony mods directory does not exist or was not created.");
        }
    }
    catch (Exception exception)
    {
        logBuilder.AppendLine("Exception occurred: " + exception.ToString());
    }
    finally
    {
        try
        {
            HarmonyLoader.FileLog.FlushBuffer();
            logBuilder.AppendLine("Harmony file log flushed.");
        }
        catch (Exception ex)
        {
            logBuilder.AppendLine("Failed to flush Harmony file log: " + ex.Message);
        }
    }

    return logBuilder.ToString();
}


	private static string EnsureHarmonyIsLoaded()
	{
		if (HarmonyLoader.harmonyAssembly != null)
		{
			return "Harmony assembly already loaded"; // Success case if already loaded
		}

		string harmonyPath = HarmonyLoader.GetHarmonyDllPath();
		// Log the path being checked for debugging
		HarmonyLoader.LogError("Checking Harmony DLL at: " + Path.GetFullPath(harmonyPath));

		if (!File.Exists(harmonyPath))
		{
			string error = "Failed to find 0Harmony.dll at '" + Path.GetFullPath(harmonyPath) + "'";
			HarmonyLoader.LogError(error);
			return error;
		}

		try
		{
			HarmonyLoader.harmonyAssembly = Assembly.Load(File.ReadAllBytes(harmonyPath));
			HarmonyLoader.ReflectionFields.Instance = new HarmonyLoader.ReflectionFields();
			return "Harmony found and loaded successfully";
		}
		catch (Exception ex)
		{
			string error = "Failed to load 0Harmony.dll: " + ex.Message;
			HarmonyLoader.LogError(error);
			return error;
		}
	}

    public static bool TryLoadMod(string dllName)
    {
        HarmonyLoader.EnsureHarmonyIsLoaded();
        string text = Path.GetFileName(dllName);
        if (text.EndsWith(".dll"))
        {
            text = text.Substring(0, text.Length - 4);
        }
        HarmonyLoader.TryUnloadMod(text);
        string text2 = Path.Combine(HarmonyLoader.modPath, text + ".dll");
        string text3 = "com.facepunch.rust_dedicated." + text;
        HarmonyLoader.Log(text3, "Loading from " + text2);
        try
        {
            Assembly assembly = HarmonyLoader.LoadAssembly(text2);
            if (assembly == null)
            {
                HarmonyLoader.LogError(text3, string.Concat(new string[]
                {
                    "Failed to load harmony mod '",
                    text,
                    ".dll' from '",
                    HarmonyLoader.modPath,
                    "'"
                }));
                return false;
            }
            HarmonyLoader.HarmonyMod harmonyMod = new HarmonyLoader.HarmonyMod();
            harmonyMod.Assembly = assembly;
            harmonyMod.AllTypes = assembly.GetTypes();
            harmonyMod.Name = text;
            foreach (Type type in harmonyMod.AllTypes)
            {
                if (typeof(IHarmonyModHooks).IsAssignableFrom(type))
                {
                    try
                    {
                        IHarmonyModHooks harmonyModHooks = Activator.CreateInstance(type) as IHarmonyModHooks;
                        if (harmonyModHooks == null)
                        {
                            HarmonyLoader.LogError(harmonyMod.Name, "Failed to create hook instance: Is null");
                        }
                        else
                        {
                            harmonyMod.Hooks.Add(harmonyModHooks);
                        }
                    }
                    catch (Exception arg)
                    {
                        HarmonyLoader.LogError(harmonyMod.Name, string.Format("Failed to create hook instance {0}", arg));
                    }
                }
            }
            harmonyMod.Harmony = new HarmonyLoader.Harmony(text3);
            harmonyMod.HarmonyId = text3;
            try
            {
                harmonyMod.Harmony.PatchAll(assembly);
            }
            catch (Exception arg2)
            {
                HarmonyLoader.LogError(harmonyMod.Name, string.Format("Failed to patch all hooks: {0}", arg2));
                return false;
            }
            foreach (IHarmonyModHooks harmonyModHooks2 in harmonyMod.Hooks)
            {
                try
                {
                    harmonyModHooks2.OnLoaded(new OnHarmonyModLoadedArgs());
                }
                catch (Exception arg3)
                {
                    HarmonyLoader.LogError(harmonyMod.Name, string.Format("Failed to call hook 'OnLoaded' {0}", arg3));
                }
            }
            HarmonyLoader.loadedMods.Add(harmonyMod);
            HarmonyLoader.Log(text3, "Loaded harmony mod '" + text3 + "'");
            return true; // Return true on successful load
        }
        catch (Exception e)
        {
            HarmonyLoader.LogError(text3, "Failed to load: " + text2);
            HarmonyLoader.ReportException(text3, e);
            return false;
        }
    }

	// Token: 0x0600000A RID: 10 RVA: 0x000025E0 File Offset: 0x000007E0
	public static bool TryUnloadMod(string name)
	{
		HarmonyLoader.EnsureHarmonyIsLoaded();
		HarmonyLoader.HarmonyMod mod = HarmonyLoader.GetMod(name);
		if (mod == null)
		{
			HarmonyLoader.LogWarning("Couldn't unload mod '" + name + "': not loaded");
			return false;
		}
		foreach (IHarmonyModHooks harmonyModHooks in mod.Hooks)
		{
			try
			{
				harmonyModHooks.OnUnloaded(new OnHarmonyModUnloadedArgs());
			}
			catch (Exception arg)
			{
				HarmonyLoader.LogError(mod.Name, string.Format("Failed to call hook 'OnUnloaded' {0}", arg));
			}
		}
		HarmonyLoader.UnloadMod(mod);
		return true;
	}

	// Token: 0x0600000B RID: 11 RVA: 0x0000268C File Offset: 0x0000088C
	private static void UnloadMod(HarmonyLoader.HarmonyMod mod)
	{
		HarmonyLoader.Log(mod.Name, "Unpatching hooks...");
		mod.Harmony.UnpatchAll(mod.HarmonyId);
		HarmonyLoader.loadedMods.Remove(mod);
		HarmonyLoader.Log(mod.Name, "Unloaded mod");
	}

	// Token: 0x0600000C RID: 12 RVA: 0x000026CC File Offset: 0x000008CC
	private static HarmonyLoader.HarmonyMod GetMod(string name)
	{
		return HarmonyLoader.loadedMods.FirstOrDefault((HarmonyLoader.HarmonyMod x) => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
	}

	// Token: 0x0600000D RID: 13 RVA: 0x000026FC File Offset: 0x000008FC
	private static Assembly LoadAssembly(string assemblyPath)
	{
		if (!File.Exists(assemblyPath))
		{
			return null;
		}
		byte[] array = File.ReadAllBytes(assemblyPath);
		string name;
		using (MemoryStream memoryStream = new MemoryStream(array))
		{
			using (MemoryStream memoryStream2 = new MemoryStream())
			{
				ReaderParameters parameters = new ReaderParameters
				{
					ReadSymbols = false,
					AssemblyResolver = HarmonyLoader.AssemblyResolver,
					MetadataResolver = new MetadataResolver(HarmonyLoader.AssemblyResolver)
				};
				AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(memoryStream, parameters);
				name = assemblyDefinition.Name.Name;
				string name2 = name + "_" + Guid.NewGuid().ToString("N");
				assemblyDefinition.Name = new AssemblyNameDefinition(name2, assemblyDefinition.Name.Version);
				WriterParameters parameters2 = new WriterParameters
				{
					WriteSymbols = false
				};
				assemblyDefinition.Write(memoryStream2, parameters2);
				array = memoryStream2.ToArray();
			}
		}
		Assembly assembly = Assembly.Load(array);
		HarmonyLoader.assemblyNames[name] = assembly;
		return assembly;
	}

	// Token: 0x0600000E RID: 14 RVA: 0x0000280C File Offset: 0x00000A0C
	private static bool IsKnownDependency(string assemblyName)
	{
		return assemblyName.StartsWith("System.", StringComparison.InvariantCultureIgnoreCase) || assemblyName.StartsWith("Microsoft.", StringComparison.InvariantCultureIgnoreCase) || assemblyName.StartsWith("Newtonsoft.", StringComparison.InvariantCultureIgnoreCase) || assemblyName.StartsWith("UnityEngine.", StringComparison.InvariantCultureIgnoreCase);
	}

	// Token: 0x0600000F RID: 15 RVA: 0x00002848 File Offset: 0x00000A48
	private static void ReportException(string harmonyId, Exception e)
	{
		HarmonyLoader.LogError(harmonyId, e);
		ReflectionTypeLoadException ex = e as ReflectionTypeLoadException;
		if (ex != null)
		{
			HarmonyLoader.LogError(harmonyId, string.Format("Has {0} LoaderExceptions:", ex.LoaderExceptions));
			foreach (Exception e2 in ex.LoaderExceptions)
			{
				HarmonyLoader.ReportException(harmonyId, e2);
			}
		}
		if (e.InnerException != null)
		{
			HarmonyLoader.LogError(harmonyId, "Has InnerException:");
			HarmonyLoader.ReportException(harmonyId, e.InnerException);
		}
	}

	// Token: 0x06000010 RID: 16 RVA: 0x000028BB File Offset: 0x00000ABB
	private static void Log(string harmonyId, object message)
	{
		Debug.Log(string.Format("[HarmonyLoader {0}] {1}", harmonyId, message));
	}

	// Token: 0x06000011 RID: 17 RVA: 0x000028CE File Offset: 0x00000ACE
	private static void LogError(string harmonyId, object message)
	{
		Debug.LogError(string.Format("[HarmonyLoader {0}] {1}", harmonyId, message));
	}

	// Token: 0x06000012 RID: 18 RVA: 0x000028E1 File Offset: 0x00000AE1
	private static void LogError(object message)
	{
		Debug.LogError(string.Format("[HarmonyLoader] {0}", message));
	}

	// Token: 0x06000013 RID: 19 RVA: 0x000028F3 File Offset: 0x00000AF3
	private static void LogWarning(object message)
	{
		Debug.LogWarning(string.Format("[HarmonyLoader] {0}", message));
	}

	// Token: 0x04000003 RID: 3
	private static string modPath;

	// Token: 0x04000004 RID: 4
	private static Assembly harmonyAssembly;

	// Token: 0x04000005 RID: 5
	private static List<HarmonyLoader.HarmonyMod> loadedMods = new List<HarmonyLoader.HarmonyMod>();

	// Token: 0x04000006 RID: 6
	private static DefaultAssemblyResolver AssemblyResolver = new DefaultAssemblyResolver();

	// Token: 0x04000007 RID: 7
	private static Dictionary<string, Assembly> assemblyNames = new Dictionary<string, Assembly>();

	// Token: 0x02000009 RID: 9
	private class ReflectionFields
	{
		// Token: 0x06000017 RID: 23 RVA: 0x00002994 File Offset: 0x00000B94
		public ReflectionFields()
		{
			this.type_FileLog = Type.GetType("HarmonyLib.FileLog, 0Harmony", true);
			this.type_Harmony = Type.GetType("HarmonyLib.Harmony, 0Harmony", true);
			this.field_DEBUG = this.type_Harmony.GetField("DEBUG", BindingFlags.Static | BindingFlags.Public);
			this.Harmony_PatchAll = this.type_Harmony.GetMethod("PatchAll", BindingFlags.Instance | BindingFlags.Public, null, new Type[]
			{
				typeof(Assembly)
			}, null);
			this.Harmony_UnpatchAll = this.type_Harmony.GetMethod("UnpatchAll", BindingFlags.Instance | BindingFlags.Public, null, new Type[]
			{
				typeof(string)
			}, null);
			this.FileLog_Reset = this.type_FileLog.GetMethod("Reset", BindingFlags.Static | BindingFlags.Public);
			this.FileLog_FlushBuffer = this.type_FileLog.GetMethod("FlushBuffer", BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x0400000A RID: 10
		public static HarmonyLoader.ReflectionFields Instance;

		// Token: 0x0400000B RID: 11
		public readonly Type type_Harmony;

		// Token: 0x0400000C RID: 12
		public readonly Type type_FileLog;

		// Token: 0x0400000D RID: 13
		public readonly FieldInfo field_DEBUG;

		// Token: 0x0400000E RID: 14
		public readonly MethodInfo Harmony_PatchAll;

		// Token: 0x0400000F RID: 15
		public readonly MethodInfo Harmony_UnpatchAll;

		// Token: 0x04000010 RID: 16
		public readonly MethodInfo FileLog_Reset;

		// Token: 0x04000011 RID: 17
		public readonly MethodInfo FileLog_FlushBuffer;
	}

	// Token: 0x0200000A RID: 10
	private class HarmonyMod
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000018 RID: 24 RVA: 0x00002A6B File Offset: 0x00000C6B
		// (set) Token: 0x06000019 RID: 25 RVA: 0x00002A73 File Offset: 0x00000C73
		public string Name { get; set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600001A RID: 26 RVA: 0x00002A7C File Offset: 0x00000C7C
		// (set) Token: 0x0600001B RID: 27 RVA: 0x00002A84 File Offset: 0x00000C84
		public string HarmonyId { get; set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600001C RID: 28 RVA: 0x00002A8D File Offset: 0x00000C8D
		// (set) Token: 0x0600001D RID: 29 RVA: 0x00002A95 File Offset: 0x00000C95
		public HarmonyLoader.Harmony Harmony { get; set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600001E RID: 30 RVA: 0x00002A9E File Offset: 0x00000C9E
		// (set) Token: 0x0600001F RID: 31 RVA: 0x00002AA6 File Offset: 0x00000CA6
		public Assembly Assembly { get; set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000020 RID: 32 RVA: 0x00002AAF File Offset: 0x00000CAF
		// (set) Token: 0x06000021 RID: 33 RVA: 0x00002AB7 File Offset: 0x00000CB7
		public Type[] AllTypes { get; set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000022 RID: 34 RVA: 0x00002AC0 File Offset: 0x00000CC0
		public List<IHarmonyModHooks> Hooks { get; } = new List<IHarmonyModHooks>();
	}

	// Token: 0x0200000B RID: 11
	private static class FileLog
	{
		// Token: 0x06000024 RID: 36 RVA: 0x00002ADB File Offset: 0x00000CDB
		public static void Reset()
		{
			HarmonyLoader.ReflectionFields.Instance.FileLog_Reset.Invoke(null, null);
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002AEF File Offset: 0x00000CEF
		public static void FlushBuffer()
		{
			HarmonyLoader.ReflectionFields.Instance.FileLog_FlushBuffer.Invoke(null, null);
		}
	}

	// Token: 0x0200000C RID: 12
	private class Harmony
	{
		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000026 RID: 38 RVA: 0x00002B03 File Offset: 0x00000D03
		// (set) Token: 0x06000027 RID: 39 RVA: 0x00002B1A File Offset: 0x00000D1A
		public static bool DEBUG
		{
			get
			{
				return (bool)HarmonyLoader.ReflectionFields.Instance.field_DEBUG.GetValue(null);
			}
			set
			{
				HarmonyLoader.ReflectionFields.Instance.field_DEBUG.SetValue(null, value);
			}
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002B32 File Offset: 0x00000D32
		public Harmony(string id)
		{
			this.harmonyObject = Activator.CreateInstance(HarmonyLoader.ReflectionFields.Instance.type_Harmony, new object[]
			{
				id
			});
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002B59 File Offset: 0x00000D59
		public void PatchAll(Assembly assembly)
		{
			HarmonyLoader.ReflectionFields.Instance.Harmony_PatchAll.Invoke(this.harmonyObject, new object[]
			{
				assembly
			});
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002B7B File Offset: 0x00000D7B
		public void UnpatchAll(string harmonyId)
		{
			HarmonyLoader.ReflectionFields.Instance.Harmony_UnpatchAll.Invoke(this.harmonyObject, new object[]
			{
				harmonyId
			});
		}

		// Token: 0x04000018 RID: 24
		private object harmonyObject;
	}
}
