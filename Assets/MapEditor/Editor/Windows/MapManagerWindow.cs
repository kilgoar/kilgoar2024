using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using RustMapEditor.UI;
using RustMapEditor.Variables;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System;
using static TerrainManager;

public class MapManagerWindow : EditorWindow
{
    #region Values
    int mainMenuOptions = 0;
	// mapToolsOptions = 0, heightMapOptions = 0, conditionalPaintOptions = 0;
    float offset = 0f, heightSet = 500f;
	//, heightLow = 450f, heightHigh = 750f;

    bool clampOffset = true, autoUpdate = false;
    float normaliseLow = 450f, normaliseHigh = 1000f;
    Conditions conditions = new Conditions() 
    { 
        GroundConditions = new GroundConditions(TerrainSplat.Enum.Grass), BiomeConditions = new BiomeConditions(TerrainBiome.Enum.Temperate), TopologyConditions = new TopologyConditions(TerrainTopology.Enum.Beach)
    };
    Layers layers = new Layers() { Ground = TerrainSplat.Enum.Grass, Biome = TerrainBiome.Enum.Temperate, Topologies = TerrainTopology.Enum.Field};

    //int texture = 0, smoothPasses = 0;
    Vector2 scrollPos = new Vector2(0, 0);
    Selections.Objects rotateSelection;
    //float terraceErodeFeatureSize = 150f, terraceErodeInteriorCornerWeight = 1f, blurDirection = 0f, filterStrength = 1f;

	

	float zOffset = 0f;
	GeologyPreset activePreset = new GeologyPreset();
	public static GeologyItem geoItem = new GeologyItem();
	int generatorOptions = 0;
	int presetIndex = 0;
	int breakerIndex = 0;
	int replacerPresetIndex = 0;
	int macroIndex = 0;
	int customPrefabIndex = 0;
	string macroTitle = "";
	string mark = "";
	float tttWeight = .7f;
	float zBreaker = 100f;
	bool destroy = false;

	bool previewCurves = true;

	
	[SerializeField] TreeViewState breakerState;
	BreakerTreeView breakerTree;
	BreakerPreset breakerPreset = new BreakerPreset();
	BreakingData breakingFragment = new BreakingData();
	int oldID = -1;
	FragmentPair pair;
	
	IconTextures icons;
	
	string [] breakerList = SettingsManager.GetPresetTitles("Presets/Breaker/");
	string [] geologyList = SettingsManager.GetPresetTitles("Presets/Geology/");
	string [] replacerList = SettingsManager.GetPresetTitles("Presets/Replacer/");
	string [] macroList = SettingsManager.GetPresetTitles("Presets/Geology/Macros/");
	string [] customPrefabList  = SettingsManager.GetDirectoryTitles("Custom/");

	int layerIndex = (int)TerrainManager.CurrentLayerType;
	int prefabIndex= 0, thicc = 3;
    bool aboveTerrain = false;
		
	Layers sourceLayers = new Layers() { Ground = TerrainSplat.Enum.Grass, Biome = TerrainBiome.Enum.Temperate, Topologies = TerrainTopology.Enum.Field };
	SlopesInfo slopesInfo = new SlopesInfo() { SlopeLow = 40f, SlopeHigh = 60f, SlopeBlendLow = 25f, SlopeBlendHigh = 75f, BlendSlopes = false };

	CurvesInfo curvesInfo = new CurvesInfo() { CurveLow = 40f, CurveHigh = 60f, CurveBlendLow = 25f, CurveBlendHigh = 75f, BlendCurves = false };

    HeightsInfo heightsInfo = new HeightsInfo() { HeightLow = 400f, HeightHigh = 600f, HeightBlendLow = 300f, HeightBlendHigh = 700f, BlendHeights = false };

	CrazingPreset crazing = new CrazingPreset();
	PerlinSplatPreset perlinSplat = new PerlinSplatPreset();
	OceanPreset ocean = new OceanPreset();
	RipplePreset ripple = new RipplePreset();
	TerracingPreset terracing = new TerracingPreset();
	PerlinPreset perlin = new PerlinPreset();
	ReplacerPreset replacer = new ReplacerPreset();
	RustCityPreset city = new RustCityPreset();

	Vector3 prefabsOffset = new Vector3();
	
	string macroDisplay;
	
	int mapSize = 3000;
	float landHeight = 505f;
	
    #endregion

	public void OnEnable()
	{

		geoItem.emphasis = 1;
		activePreset.biomeLayer = TerrainBiome.Enum.Arid;
		breakerTree = new BreakerTreeView(breakerState);
		SettingsManager.LoadFragmentLookup();
		

		geologyList = SettingsManager.GetPresetTitles("Presets/Geology/");
		activePreset.title = geologyList[presetIndex];
		SettingsManager.LoadGeologyPreset(activePreset.title);
		activePreset = SettingsManager.geology;
		
		

		icons = new IconTextures(
			(Texture2D)Resources.Load("Textures/Icons/gears"),
			(Texture2D)Resources.Load("Textures/Icons/scrap"),
			(Texture2D)Resources.Load("Textures/Icons/stop"),
			(Texture2D)Resources.Load("Textures/Icons/tarp"),
			(Texture2D)Resources.Load("Textures/Icons/trash"));

	}
	
   public void OnGUI()
    {
		
		
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);
        GUIContent[] mainMenu = new GUIContent[6];
        mainMenu[0] = new GUIContent("File");
        mainMenu[1] = new GUIContent("Settings");
        mainMenu[2] = new GUIContent("Prefabs");
		mainMenu[3] = new GUIContent("Layers");
        mainMenu[4] = new GUIContent("Generator");
        mainMenu[5] = new GUIContent("Advanced");


		EditorGUI.BeginChangeCheck();

        mainMenuOptions = GUILayout.Toolbar(mainMenuOptions, mainMenu, EditorStyles.toolbarButton);
		
		if (EditorGUI.EndChangeCheck())
					{
						if (generatorOptions !=1 || mainMenuOptions !=4)
							TerrainManager.HideLandMask();
						else if (activePreset.preview)
							GenerativeManager.MakeCliffMap(activePreset);
					}
		

		Functions.SaveSettings();

	
		
        #region Menu
        switch (mainMenuOptions)
        {
            #region File
            case 0:
				Functions.EditorIO();
				Functions.NewMapOptions(ref mapSize, ref landHeight, ref layers);
				Functions.MapInfo();
				Functions.CustomPrefab();
                break;
            #endregion
            #region Prefabs
			case 1:
				Functions.EditorSettings();
				Functions.EditorInfo();
                Functions.EditorLinks();
				break;
            case 2:
			
			
				GUIContent[] prefabsMenu = new GUIContent[3];
				prefabsMenu[0] = new GUIContent("Batch Replacer");
				prefabsMenu[1] = new GUIContent("Prefab Breaker");
				prefabsMenu[2] = new GUIContent("Advanced");
				
				prefabIndex = GUILayout.Toolbar(prefabIndex, prefabsMenu, EditorStyles.toolbarButton);
				
				switch(prefabIndex)
				{
					case 0:
						Functions.Replacer(ref replacer, ref replacerPresetIndex, ref replacerList);
						break;
					case 1:
						breakerPreset = SettingsManager.breaker;
						Functions.BreakerHierarchy(ref breakerPreset, ref breakerTree, ref breakingFragment, ref oldID, ref icons, ref breakerList, ref breakerIndex, ref pair);
						break;
					case 2:
						Functions.refabs(ref prefabsOffset, ref mark);
						break;
				}
				
				
				break;
            #endregion
            case 3:
			
			
            GUIContent[] layersMenu = new GUIContent[4];
            layersMenu[0] = new GUIContent("Ground");
            layersMenu[1] = new GUIContent("Biome");
            layersMenu[2] = new GUIContent("Alpha");
			layersMenu[3] = new GUIContent("Topology");

			EditorGUI.BeginChangeCheck();
			
			layerIndex = GUILayout.Toolbar(layerIndex, layersMenu, EditorStyles.toolbarButton);
			
			if (EditorGUI.EndChangeCheck())
			{
				Functions.SetLayer((LayerType)layerIndex, TerrainTopology.TypeToIndex((int)layers.Topologies));
			}
			

			
				switch (TerrainManager.CurrentLayerType)
				{
					case LayerType.Ground:
						Functions.TextureSelect((LayerType)layerIndex, ref layers);

						Functions.LayerTools(TerrainManager.CurrentLayerType, TerrainSplat.TypeToIndex((int)layers.Ground));


						
						crazing = SettingsManager.crazing;
						crazing.splatLayer = TerrainTopology.TypeToIndex((int)layers.Ground);
						Functions.Crazing(ref crazing);
						
						perlinSplat = SettingsManager.perlinSplat;
						perlinSplat.splatLayer = TerrainTopology.TypeToIndex((int)layers.Ground);
						Functions.PerlinSplat(ref perlinSplat);
						
						Functions.HeightTools(TerrainManager.CurrentLayerType, TerrainSplat.TypeToIndex((int)layers.Ground), ref heightsInfo);
						Functions.SlopeTools(TerrainManager.CurrentLayerType, TerrainSplat.TypeToIndex((int)layers.Ground), ref slopesInfo);

						Functions.CurvesTools(TerrainManager.CurrentLayerType, TerrainSplat.TypeToIndex((int)layers.Ground), ref curvesInfo, ref previewCurves);
						

						Functions.RiverTools(TerrainManager.CurrentLayerType, TerrainSplat.TypeToIndex((int)layers.Ground), ref aboveTerrain);
						
						break;
					case LayerType.Biome:
						Functions.TextureSelect((LayerType)layerIndex, ref layers);

						Functions.LayerTools(TerrainManager.CurrentLayerType, TerrainBiome.TypeToIndex((int)layers.Biome));

						Functions.DitherTool(TerrainManager.CurrentLayerType);

						
						Functions.HeightTools(TerrainManager.CurrentLayerType, TerrainBiome.TypeToIndex((int)layers.Biome), ref heightsInfo);
						Functions.SlopeTools(TerrainManager.CurrentLayerType, TerrainBiome.TypeToIndex((int)layers.Biome), ref slopesInfo);
						break;
					case LayerType.Alpha:
					
						Functions.LayerTools(TerrainManager.CurrentLayerType, 0, 1);

						break;
					case LayerType.Topology:
					
						Functions.TopologyTools();
						
						Functions.TopologyLayerSelect(ref layers);

						Functions.LayerTools(TerrainManager.CurrentLayerType, 0, 1, TerrainTopology.TypeToIndex((int)layers.Topologies));


						Functions.HeightTools(TerrainManager.CurrentLayerType, 0, ref heightsInfo, 1, TerrainTopology.TypeToIndex((int)layers.Topologies));
						Functions.SlopeTools(TerrainManager.CurrentLayerType, 0, ref slopesInfo, 1, TerrainTopology.TypeToIndex((int)layers.Topologies));
						
						Functions.Combinator(ref layers, ref sourceLayers, ref tttWeight, ref thicc);
						
						
						Functions.RiverTools(TerrainManager.CurrentLayerType, 0, ref aboveTerrain, 1, TerrainTopology.TypeToIndex((int)layers.Topologies));
						Functions.LakeOcean(ref layers);

						break;
				}
				
			

			/*
            switch (mapToolsOptions)
            {
				
=======

            switch (mapToolsOptions)
            {
				/*
>>>>>>> origin/master
                #region HeightMap
                case 0:
                    GUIContent[] heightMapMenu = new GUIContent[2];
                    heightMapMenu[0] = new GUIContent("Heights");
                    heightMapMenu[1] = new GUIContent("Filters");
                    heightMapOptions = GUILayout.Toolbar(heightMapOptions, heightMapMenu, EditorStyles.toolbarButton);
            
                    switch (heightMapOptions)
                    {
                        case 0:
                            Elements.BoldLabel(ToolTips.heightsLabel);
                            Functions.OffsetMap(ref offset, ref clampOffset);
                            Functions.SetHeight(ref heightSet);
                            Functions.ClampHeight(ref heightLow, ref heightHigh);
                            Elements.BoldLabel(ToolTips.miscLabel);
                            Functions.InvertMap();
                            break;
                        case 1:
                            Functions.NormaliseMap(ref normaliseLow, ref normaliseHigh, ref autoUpdate);
                            Functions.SmoothMap(ref filterStrength, ref blurDirection, ref smoothPasses);
                            Functions.TerraceMap(ref terraceErodeFeatureSize, ref terraceErodeInteriorCornerWeight);
                            break;
                    }
                    break;
                #endregion
				
                #region Textures
                case 0:
                    Functions.ConditionalPaint(ref conditionalPaintOptions, ref texture, ref conditions, ref layers);
                    break;
                #endregion

                #region Misc
                case 2:
                    Functions.RotateMap(ref rotateSelection);
                    break;
                    #endregion
<<<<<<< HEAD
					
            }
			*/

            break;
		case 4:
			GUIContent[] generatorMenu = new GUIContent[3];
				generatorMenu[0] = new GUIContent("Heightmap");
				generatorMenu[1] = new GUIContent("Geology");
				generatorMenu[2] = new GUIContent("Monuments");
				

				EditorGUI.BeginChangeCheck();
				
					generatorOptions = GUILayout.Toolbar(generatorOptions, generatorMenu, EditorStyles.toolbarButton);
					
				if (EditorGUI.EndChangeCheck())
					{
						if (generatorOptions !=1)
							TerrainManager.HideLandMask();
						else if (activePreset.preview)
							GenerativeManager.MakeCliffMap(activePreset);
					}
				

				switch (generatorOptions)
				{
						case 0:
							Functions.SetHeight(ref heightSet);
							Functions.OffsetMap(ref offset, ref clampOffset);
						
							perlin = SettingsManager.perlin;
							Functions.PerlinTerrain(ref perlin);
							
							Functions.NormaliseMap(ref normaliseLow, ref normaliseHigh, ref autoUpdate);
							
							ocean = SettingsManager.ocean;
							Functions.Ocean(ref ocean);
							
							ripple = SettingsManager.ripple;
							Functions.Ripple(ref ripple);
							
							terracing = SettingsManager.terracing;
							Functions.RandomTerracing(ref terracing);
							
						break;

						case 1:
								Functions.Geology(ref activePreset, ref presetIndex, ref geologyList, ref macroIndex, ref macroList, ref macroTitle, ref macroDisplay, ref layers, ref geoItem,
								ref customPrefabList, ref customPrefabIndex);
						break;
						
						case 2:
								city = SettingsManager.city;
								Functions.RustCity(ref city, ref zBreaker, ref destroy);
						break;

				
				}
		break;
		
		case 5:
			Functions.Merger(ref zOffset);
		break;
		
        }

        #endregion
        EditorGUILayout.EndScrollView();
    }
}