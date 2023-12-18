using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using RustMapEditor.Variables;
using static BreakerSerialization;

public static class SettingsManager
{
    #region Init
    [InitializeOnLoadMethod]
    private static void Init()
    {
        if (!File.Exists(SettingsPath))
            using (StreamWriter write = new StreamWriter(SettingsPath, false))
                write.Write(JsonUtility.ToJson(new EditorSettings(), true));

        LoadSettings();
    }
    #endregion

    public const string SettingsPath = "EditorSettings.json";
    public const string BundlePathExt = @"\Bundles\Bundles";
	
	public static bool style { get; set; }
    public static string RustDirectory { get; set; }
    public static float PrefabRenderDistance { get; set; }
    public static float PathRenderDistance { get; set; }
    public static float WaterTransparency { get; set; }
    public static bool LoadBundleOnLaunch { get; set; }
    public static bool TerrainTextureSet { get; set; }
	public static CrazingPreset crazing { get; set; }
	public static PerlinSplatPreset perlinSplat { get; set; }
	public static RipplePreset ripple { get; set; }
	public static OceanPreset ocean { get; set; }
	public static TerracingPreset terracing { get; set; }
	public static PerlinPreset perlin { get; set; }
	public static GeologyPreset geology { get; set; }
	public static ReplacerPreset replacer { get; set; }
	public static string[] breakerPresets { get; set; }
	public static string[] geologyPresets { get; set; }
    public static string[] PrefabPaths { get; private set; }
	public static GeologyPreset[] macro {get; set; }
	public static bool macroSources {get; set; }
 	public static RustCityPreset city {get; set; }
	public static BreakerPreset breaker {get; set;}
	public static FragmentLookup fragmentIDs {get; set;}
	public static BreakerSerialization breakerSerializer = new BreakerSerialization();

    /// <summary>Saves the current EditorSettings to a JSON file.</summary>
    public static void SaveSettings()
    {
        using (StreamWriter write = new StreamWriter(SettingsPath, false))
        {
            EditorSettings editorSettings = new EditorSettings
            (
                RustDirectory, PrefabRenderDistance, PathRenderDistance, WaterTransparency, LoadBundleOnLaunch
            );
            write.Write(JsonUtility.ToJson(editorSettings, true));
        }
    }

		public static Dictionary<string,uint> ListToDict(List<FragmentPair> fragmentPairs)
		{
			Dictionary<string,uint> namelist = new Dictionary<string,uint>();
			foreach(FragmentPair pair in fragmentPairs)
			{
				namelist.Add(pair.fragment, pair.id);
			}
			return namelist;
		}
		
	public static List<FragmentPair> DictToList(Dictionary<string,uint> fragmentNamelist)
		{
			List<FragmentPair> namePairs = new List<FragmentPair>();
			FragmentPair fragPair = new FragmentPair();
			foreach (KeyValuePair<string,uint> pair in fragmentNamelist)
			{
				fragPair.fragment = pair.Key;
				fragPair.id = pair.Value;
				namePairs.Add(fragPair);
			}
			return namePairs;
		}
	
	public static void SaveFragmentLookup()
	{
		using (StreamWriter write = new StreamWriter($"Presets/breakerFragments.json", false))
        {
            write.Write(JsonUtility.ToJson(fragmentIDs, true));
			fragmentIDs.Deserialize();
        }
	}

	public static void LoadFragmentLookup()
    {
		fragmentIDs  = new FragmentLookup();
		using (StreamReader reader = new StreamReader($"Presets/breakerFragments.json"))
			{
				fragmentIDs  = JsonUtility.FromJson<FragmentLookup>(reader.ReadToEnd());
				fragmentIDs.Deserialize();
			}
    }

	
	public static void SaveBreakerPreset(string filename)
    {
		breakerSerializer.breaker = breaker;
		breakerSerializer.Save($"Presets/Breaker/{filename}.breaker");
		/*       
	   using (StreamWriter write = new StreamWriter($"Presets/Breaker/{breaker.title}.breaker", false))
        {
            write.Write(JsonUtility.ToJson(breaker, true));
        }
		*/
    }
	
	public static void LoadBreakerPreset(string filename)
	{
		breaker = breakerSerializer.Load($"Presets/Breaker/{filename}.breaker");
		/*
		using (StreamReader reader = new StreamReader($"Presets/Breaker/{filename}.breaker"))
			{
				
				breaker = JsonUtility.FromJson<BreakerPreset>(reader.ReadToEnd());
			}
		*/
	}
	
	public static void SaveGeologyPreset()
    {
        using (StreamWriter write = new StreamWriter($"Presets/Geology/{geology.title}.json", false))
        {
            write.Write(JsonUtility.ToJson(geology, true));
        }
    }
	
	public static void SaveReplacerPreset()
    {
        using (StreamWriter write = new StreamWriter($"Presets/Replacer/{replacer.title}.json", false))
        {
            write.Write(JsonUtility.ToJson(replacer, true));
        }
    }
	
	
	
	public static void LoadGeologyPreset(string filename)
	{
		using (StreamReader reader = new StreamReader($"Presets/Geology/{filename}.json"))
			{
				geology = JsonUtility.FromJson<GeologyPreset>(reader.ReadToEnd());
			}
	}
	
	public static GeologyPreset GetGeologyPreset(string filename)
	{
		if (File.Exists(filename))
			{
				using (StreamReader reader = new StreamReader(filename))
					{
						return JsonUtility.FromJson<GeologyPreset>(reader.ReadToEnd());
					}
			}
		else
			return new GeologyPreset("file not found");
	}

	
	public static void LoadReplacerPreset(string filename)
	{
		using (StreamReader reader = new StreamReader($"Presets/Replacer/{filename}.json"))
			{
				replacer = JsonUtility.FromJson<ReplacerPreset>(reader.ReadToEnd());
			}
	}
	
	public static void LoadGeologyMacro(string filename)
	{
			int length;
			using (StreamReader reader = new StreamReader($"Presets/Geology/Macros/{filename}.macro"))
			{
				string macroFile = reader.ReadToEnd();
				
				
				char[] delimiters = { '*'};
				string[] parse = macroFile.Split(delimiters);
				length = parse.Length-1;
				GeologyPreset[] newMacro = new GeologyPreset[length];
				
				for(int i = 0; i < length; i++)
					{
						newMacro[i] = JsonUtility.FromJson<GeologyPreset>(parse[i]);
					}
				macro = newMacro;
			}
			
			if(macroSources)
			{
				GeologyPreset[] fileMacro = new GeologyPreset[length];
				
				for(int i = 0; i < length; i++)
				{
					fileMacro[i] = GetGeologyPreset(macro[i].filename);
				}
				macro = fileMacro;
			}


	}
	
	
	public static void SaveGeologyMacro(string macroTitle)
    {
		string macroFile="";
		for (int i = 0; i < macro.Length; i++)
		{
			macroFile += JsonUtility.ToJson(macro[i], true) + "*";
		}
		
        using (StreamWriter write = new StreamWriter($"Presets/Geology/Macros/{macroTitle}.macro", false))
        {
            write.Write(macroFile);
        }
    }
	
	public static void RemovePreset(string macroTitle)
	{
		int newlength = macro.Length -1;
		
		
		if (newlength >= 0)
		{
			GeologyPreset[] newMacro = new GeologyPreset[newlength];
			
			for (int i = 0; i < newlength; i++)
			{	
					newMacro[i] = macro[i];
			}
			macro = newMacro;
			SaveGeologyMacro(macroTitle);
		}
		
	}
	
	
	public static void AddToMacro(string macroTitle)
	{
		int append  = 0;
		if (File.Exists($"Presets/Geology/Macros/{macroTitle}.macro"))
			{
				LoadGeologyMacro(macroTitle);
			}
		else
			{
				macro = new GeologyPreset[0];
			}

		if (macro != null)
		{
		 append = macro.Length;
		}
		
		GeologyPreset[] newMacro = new GeologyPreset[append+1];
		for (int i = 0; i < append; i++)
			{	
					newMacro[i] = macro[i];
			}
		
		newMacro[append] = geology;
		macro = newMacro;
		SaveGeologyMacro(macroTitle);
	
	}
	

    /// <summary>Loads and sets the current EditorSettings from a JSON file.</summary>
    public static void LoadSettings()
    {
        using (StreamReader reader = new StreamReader(SettingsPath))
        {
            EditorSettings editorSettings = JsonUtility.FromJson<EditorSettings>(reader.ReadToEnd());
            RustDirectory = editorSettings.rustDirectory;
            PrefabRenderDistance = editorSettings.prefabRenderDistance;
            PathRenderDistance = editorSettings.pathRenderDistance;
            WaterTransparency = editorSettings.waterTransparency;
            LoadBundleOnLaunch = editorSettings.loadbundleonlaunch;
            PrefabPaths = editorSettings.prefabPaths;
			style = editorSettings.style;
			crazing = editorSettings.crazing;
			perlinSplat = editorSettings.perlinSplat;
			ripple = editorSettings.ripple;
			ocean = editorSettings.ocean;
			terracing = editorSettings.terracing;
			perlin = editorSettings.perlin;
			geology = editorSettings.geology;
			replacer = editorSettings.replacer;
			city = editorSettings.city;
			macroSources = editorSettings.macroSources;
        }

		LoadPresets();
		LoadMacros();
    }
	
	public static void LoadPresets()
	{
		string[] geologyPresets = Directory.GetFiles("Presets/Geology/");
		string[] breakerPresets = Directory.GetFiles("Presets/Breaker/");
	}
	
	public static void LoadMacros()
	{
		string[] geologyPresets = Directory.GetFiles("Presets/Geology/Macros/");
	}
	
	public static string[] GetPresetTitles(string path)
	{
		char[] delimiters = { '/', '.'};
		string[] geologyPresets = Directory.GetFiles(path);
		string[] parse;
		string[] filenames = new string [geologyPresets.Length];
		int filenameID;
		for(int i = 0; i < geologyPresets.Length; i++)
		{
			parse = geologyPresets[i].Split(delimiters);
			filenameID = parse.Length - 2;
			filenames[i] = parse[filenameID];
		}
		return filenames;
	}
	
	public static string[] GetDirectoryTitles(string path)
	{
		
			return Directory.GetDirectories(path);

	}

    /// <summary> Sets the EditorSettings back to default values.</summary>
    public static void SetDefaultSettings()
    {
        RustDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Rust";
        ToolTips.rustDirectoryPath.text = RustDirectory;
        PrefabRenderDistance = 700f;
        PathRenderDistance = 250f;
        WaterTransparency = 0.2f;
        LoadBundleOnLaunch = false;
        Debug.Log("Default Settings set.");
    }
	
}

[Serializable]
public struct EditorSettings
{
    public string rustDirectory;
    public float prefabRenderDistance;
    public float pathRenderDistance;
    public float waterTransparency;
    public bool loadbundleonlaunch;
    public bool terrainTextureSet;
	public bool style;
	public CrazingPreset crazing;
	public PerlinSplatPreset perlinSplat;
	public RipplePreset ripple;
	public OceanPreset ocean;
	public TerracingPreset terracing;
	public PerlinPreset perlin;
	public GeologyPreset geology;
	public ReplacerPreset replacer;
	public string[] prefabPaths;
	public RustCityPreset city;
	public BreakerPreset breaker;
	public bool macroSources;

    public EditorSettings
    (
        string rustDirectory = @"C:\Program Files (x86)\Steam\steamapps\common\Rust", float prefabRenderDistance = 700f, float pathRenderDistance = 200f, 
        float waterTransparency = 0.2f, bool loadbundleonlaunch = false, bool terrainTextureSet = false, bool style = true, CrazingPreset crazing = new CrazingPreset(), PerlinSplatPreset perlinSplat = new PerlinSplatPreset(),
		RipplePreset ripple = new RipplePreset(), OceanPreset ocean = new OceanPreset(), TerracingPreset terracing = new TerracingPreset(), PerlinPreset perlin = new PerlinPreset(), GeologyPreset geology = new GeologyPreset(), 
		ReplacerPreset replacer = new ReplacerPreset(), RustCityPreset city = new RustCityPreset(), BreakerPreset breaker = new BreakerPreset(), bool macroSources = true
	)
        {
            this.rustDirectory = rustDirectory;
            this.prefabRenderDistance = prefabRenderDistance;
            this.pathRenderDistance = pathRenderDistance;
            this.waterTransparency = waterTransparency;
            this.loadbundleonlaunch = loadbundleonlaunch;
            this.terrainTextureSet = terrainTextureSet;
			this.style = style;
			this.crazing = crazing;
			this.perlinSplat = perlinSplat;
            this.prefabPaths = SettingsManager.PrefabPaths;
			this.ripple = ripple;
			this.ocean = ocean;
			this.terracing = terracing;
			this.perlin = perlin;
			this.geology = geology;
			this.replacer = replacer;
			this.city = city;
			this.breaker = breaker;
			this.macroSources = macroSources;
        }
}