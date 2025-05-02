using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using System.Threading.Tasks;

[ExecuteInEditMode]
public class TerrainTexturing : MonoBehaviour
{
    // Serialized fields for prefab persistence
    [SerializeField] public float terrainMaxDimension;
    [SerializeField] public int textureResolution;
    [SerializeField] public float pixelSize;

    // Runtime data
    public float[] shoreDistances;
    public Vector3[] shoreVectors;
    public Texture2D shoreVectorTexture;
    public RenderTexture baseDiffuseTexture;
    public RenderTexture baseNormalTexture;
    public RenderTexture heightSlopeTexture;

    // State
    public bool isInitialized;
    public bool debugFoliageDisplacement;
    public bool previousFoliageDebugState;
    public TextureState baseTextureState = TextureState.Initializing;
    public TextureState heightSlopeState = TextureState.Initializing;
    public enum TextureState { Skipped, Initializing, Uncached, CachedRaw }
    private static TerrainTexturing instance;

    // Shader property IDs
    private int[,] shaderPropertyIds;
    private static readonly int NaNProperty = Shader.PropertyToID("_NaN");
    private static readonly int Control0Property = Shader.PropertyToID("Terrain_Control0");
    private static readonly int Control1Property = Shader.PropertyToID("Terrain_Control1");
    private static readonly int TextureArray0Property = Shader.PropertyToID("Terrain_TextureArray0");
    private static readonly int TextureArray1Property = Shader.PropertyToID("Terrain_TextureArray1");
    private static readonly int HeightSlopeProperty = Shader.PropertyToID("Terrain_HeightSlope");
    private static readonly int ShoreVectorProperty = Shader.PropertyToID("Terrain_ShoreVector");
    private static readonly int PositionProperty = Shader.PropertyToID("Terrain_Position");
    private static readonly int SizeProperty = Shader.PropertyToID("Terrain_Size");
    private static readonly int RcpSizeProperty = Shader.PropertyToID("Terrain_RcpSize");
    private static readonly int TexelSizeProperty = Shader.PropertyToID("Terrain_TexelSize");
    private static readonly int TexelSize0Property = Shader.PropertyToID("Terrain_TexelSize0");
    private static readonly int TexelSize1Property = Shader.PropertyToID("Terrain_TexelSize1");
    private static readonly int ParallaxProperty = Shader.PropertyToID("_TerrainParallax");
    private static readonly int DetailTextureProperty = Shader.PropertyToID("Terrain_PotatoDetailTexture");
    private static readonly int DetailScaleProperty = Shader.PropertyToID("Terrain_PotatoDetailWorldUVScale");
    private static readonly int DetailTextureInputProperty = Shader.PropertyToID("_PotatoDetailTexture");
    private static readonly int DetailScaleInputProperty = Shader.PropertyToID("_PotatoDetailWorldUVScale");
    private static readonly int UVMixMultProperty = Shader.PropertyToID("_UVMIXMult");
    private static readonly int UVMixStartProperty = Shader.PropertyToID("_UVMIXStart");
    private static readonly int UVMixDistProperty = Shader.PropertyToID("_UVMIXDist");
    private static readonly int FallbackAlbedoProperty = Shader.PropertyToID("_LayerFallback_Albedo");
    private static readonly int FallbackMetallicProperty = Shader.PropertyToID("_LayerFallback_Metallic");
    private static readonly int FallbackSmoothnessProperty = Shader.PropertyToID("_LayerFallback_Smoothness");

    public int Resolution => textureResolution;
    public Vector3[] ShoreVectors => shoreVectors;
    public static TerrainTexturing Instance => instance;

    private void Awake()
    {
        InitializeSingleton();
        if (!isInitialized && TerrainManager.Land != null)
        {
            CheckAndInitializeBasicData();
        }
    }

/*
    private void OnEnable()
    {
        InitializeSingleton();
        if (!isInitialized && TerrainManager.Land != null)
        {
            CheckAndInitializeBasicData();
        }
    }
*/

    private void OnDisable()
    {
        CleanupResources();
    }

    private void InitializeSingleton()
    {
        instance = instance ?? this;
    }

    private void CheckAndInitializeBasicData()
    {
		    if (TerrainManager.Land == null)
			{
				Debug.LogError("[TerrainTexturing] TerrainManager.Land is null. Cannot initialize.");
				return;
			}
			if (TerrainManager.Land.terrainData == null)
			{
				Debug.LogError("[TerrainTexturing] TerrainManager.Land.terrainData is null. Cannot initialize.");
				return;
			}
		
        if (terrainMaxDimension == 0 || textureResolution == 0 || shoreDistances == null)
        {
            terrainMaxDimension = Mathf.Max(TerrainManager.Land.terrainData.size.x, TerrainManager.Land.terrainData.size.z);
            textureResolution = Mathf.ClosestPowerOfTwo(TerrainManager.Land.terrainData.heightmapResolution) >> 1;
            pixelSize = terrainMaxDimension / textureResolution;
            InitializeShoreData();
            isInitialized = true;
            Debug.Log("[TerrainTexturing] Basic data initialized.");
        }

        if (heightSlopeTexture == null) heightSlopeState = TextureState.Uncached;
        if (baseDiffuseTexture == null || baseNormalTexture == null) baseTextureState = TextureState.Uncached;
    }

    private void InitializeBaseTextures()
    {
        baseDiffuseTexture = CreateRenderTexture("Terrain-DiffuseBase", 4096, false);
        baseNormalTexture = CreateRenderTexture("Terrain-NormalBase", 4096, false);
        baseTextureState = TextureState.Uncached;
    }

    private void InitializeShoreData()
    {
        int totalPixels = textureResolution * textureResolution;
        shoreDistances = new float[totalPixels];
        shoreVectors = new Vector3[totalPixels];
        for (int i = 0; i < totalPixels; i++)
        {
            shoreDistances[i] = 10000f;
            shoreVectors[i] = Vector3.one;
        }
    }

    private void UpdateBaseTextures()
    {
        if (baseTextureState == TextureState.Uncached)
        {
            if (baseDiffuseTexture == null || baseNormalTexture == null) InitializeBaseTextures();
            GenerateBaseTextures();
        }
        else if (baseTextureState == TextureState.CachedRaw && IsBaseTextureLost())
        {
            Debug.Log("[TerrainTexturing] Base textures lost. Rebuilding...");
            CleanupBaseTextures();
            InitializeBaseTextures();
            needsRefresh = true;
        }
    }

    public void Refresh()
    {
        CleanupResources();
        CheckAndInitializeBasicData();
        InitializeBaseTextures();
        //GenerateShoreVectors(out shoreDistances, out shoreVectors); 
		//CreateShoreVectorTexture(textureResolution);
		shoreVectorTexture = null; // Explicitly set to null
        UpdateShaderProperties();
        UpdateMaterialProperties();
        previousFoliageDebugState = debugFoliageDisplacement;
        isInitialized = true;
        needsRefresh = true;
    }

    private void UpdateShaderProperties()
    {
		
	    if (TerrainManager._config == null)
		{
			Debug.LogError("[TerrainTexturing] TerrainManager._config is null! Cannot update shader properties.");
			return;
		}
		
		if (TerrainManager.Land == null || !TerrainManager.Land.gameObject.activeInHierarchy)
		{
			Debug.LogError("TerrainManager.Land is null or its GameObject is inactive!");
			return;
		}
		if (!TerrainManager.Land.enabled)
		{
			Debug.LogError("Terrain component is disabled!");
			TerrainManager.Land.enabled = true;
		}
		
        Shader.SetGlobalTexture(Control0Property, TerrainManager.Land.terrainData.alphamapTextures[0]);
        Shader.SetGlobalTexture(Control1Property, TerrainManager.Land.terrainData.alphamapTextures[1]);
        Shader.SetGlobalFloat(NaNProperty, float.NaN);
        Shader.SetGlobalTexture(TextureArray0Property, TerrainManager._config.AlbedoArray);
        Shader.SetGlobalTexture(TextureArray1Property, TerrainManager._config.NormalArray);
        Shader.SetGlobalTexture(HeightSlopeProperty, TerrainManager.HeightSlopeTexture);
        Shader.SetGlobalTexture(ShoreVectorProperty, null);

        Vector3 position = TerrainManager.Land.gameObject.transform.position;
        Vector3 size = TerrainManager.Land.terrainData.size;
        Shader.SetGlobalVector(PositionProperty, position);
        Shader.SetGlobalVector(SizeProperty, size);
        Shader.SetGlobalVector(RcpSizeProperty, TerrainManager.TerrainSizeInverse);
		

        Vector2 texelSize = new Vector2(1f / TerrainManager._config.GetTextureArrayWidth(), 1f / TerrainManager._config.GetTextureArrayWidth());
        float[] layerSizes = TerrainManager._config.GetSplatTilings();
        Vector3[] layerParams = TerrainManager._config.GetUVMIXParameters();
        Vector4 texelSize0 = new Vector4(1f / layerSizes[0], 1f / layerSizes[1], 1f / layerSizes[2], 1f / layerSizes[3]);
        Vector4 texelSize1 = new Vector4(1f / layerSizes[4], 1f / layerSizes[5], 1f / layerSizes[6], 1f / layerSizes[7]);
		


        Shader.SetGlobalVector(TexelSizeProperty, texelSize);
        Shader.SetGlobalVector(TexelSize0Property, texelSize0);
        Shader.SetGlobalVector(TexelSize1Property, texelSize1);
		

        InitializeShaderPropertyIds();
        Color[] aridColors = TerrainManager._config.GetAridColors();
        Color[] temperateColors = TerrainManager._config.GetTemperateColors();
        Color[] tundraColors = TerrainManager._config.GetTundraColors();
        Color[] arcticColors = TerrainManager._config.GetArcticColors();
		Color[] jungleColors = TerrainManager._config.GetJungleColors();

        Color[] aridOverlayColors; Vector4[] aridOverlayParams;
        TerrainManager._config.GetAridOverlayData(out aridOverlayColors, out aridOverlayParams);

        Color[] temperateOverlayColors; Vector4[] temperateOverlayParams;
        TerrainManager._config.GetTemperateOverlayData(out temperateOverlayColors, out temperateOverlayParams);

        Color[] tundraOverlayColors; Vector4[] tundraOverlayParams;
        TerrainManager._config.GetTundraOverlayData(out tundraOverlayColors, out tundraOverlayParams);

        Color[] arcticOverlayColors; Vector4[] arcticOverlayParams;
        TerrainManager._config.GetArcticOverlayData(out arcticOverlayColors, out arcticOverlayParams);
		
		Color[] jungleOverlayColors; Vector4[] jungleOverlayParams;
        TerrainManager._config.GetJungleOverlayData(out jungleOverlayColors, out jungleOverlayParams);

        float maxUVMix = layerParams[0].x;
        float minUVMixStart = layerParams[0].y;
        float maxUVMixDist = layerParams[0].z;

        for (int i = 0; i < 8; i++)
        {
			Shader.SetGlobalVector(shaderPropertyIds[0, i], new Vector3(layerParams[i].x, layerParams[i].y, layerParams[i].z));
            Shader.SetGlobalColor(shaderPropertyIds[1, i], aridColors[i]);
            Shader.SetGlobalColor(shaderPropertyIds[2, i], temperateColors[i]);
            Shader.SetGlobalColor(shaderPropertyIds[3, i], tundraColors[i]);
            Shader.SetGlobalColor(shaderPropertyIds[4, i], arcticColors[i]);
			
            Shader.SetGlobalColor(shaderPropertyIds[5, i], aridOverlayColors[i]);
            Shader.SetGlobalColor(shaderPropertyIds[6, i], temperateOverlayColors[i]);
            Shader.SetGlobalColor(shaderPropertyIds[7, i], tundraOverlayColors[i]);
            Shader.SetGlobalColor(shaderPropertyIds[8, i], arcticOverlayColors[i]);
			
            Shader.SetGlobalVector(shaderPropertyIds[9, i], aridOverlayParams[i]);
            Shader.SetGlobalVector(shaderPropertyIds[10, i], temperateOverlayParams[i]);
            Shader.SetGlobalVector(shaderPropertyIds[11, i], tundraOverlayParams[i]);
            Shader.SetGlobalVector(shaderPropertyIds[12, i], arcticOverlayParams[i]);
			
			Shader.SetGlobalColor(shaderPropertyIds[16, i], jungleColors[i]);
            Shader.SetGlobalColor(shaderPropertyIds[17, i], jungleOverlayColors[i]);
            Shader.SetGlobalVector(shaderPropertyIds[18, i], jungleOverlayParams[i]);

            maxUVMix = Mathf.Max(maxUVMix, layerParams[i].x);
            minUVMixStart = Mathf.Min(minUVMixStart, layerParams[i].y);
            maxUVMixDist = Mathf.Max(maxUVMixDist, layerParams[i].z);
        }

        Shader.SetGlobalFloat(UVMixMultProperty, maxUVMix);
        Shader.SetGlobalFloat(UVMixStartProperty, minUVMixStart);
        Shader.SetGlobalFloat(UVMixDistProperty, maxUVMixDist);
    }

    private void UpdateMaterialProperties()
    {
        Material material = TerrainManager.Land.materialTemplate;
        Shader.SetGlobalFloat(ParallaxProperty, material.GetFloat(ParallaxProperty));
        Shader.SetGlobalTexture(DetailTextureProperty, material.GetTexture(DetailTextureInputProperty));
        Shader.SetGlobalFloat(DetailScaleProperty, material.GetFloat(DetailScaleInputProperty));
        Shader.SetGlobalColor(FallbackAlbedoProperty, material.GetColor(FallbackAlbedoProperty));
        Shader.SetGlobalFloat(FallbackMetallicProperty, material.GetFloat(FallbackMetallicProperty));
        Shader.SetGlobalFloat(FallbackSmoothnessProperty, material.GetFloat(FallbackSmoothnessProperty));

        for (int i = 0; i < 8; i++)
        {
            Shader.SetGlobalFloat(shaderPropertyIds[13, i], material.GetFloat(shaderPropertyIds[13, i]));
            Shader.SetGlobalFloat(shaderPropertyIds[14, i], material.GetFloat(shaderPropertyIds[14, i]));
            Shader.SetGlobalFloat(shaderPropertyIds[15, i], material.GetFloat(shaderPropertyIds[15, i]));
        }

    // Disable Shore Wetness Layer (already done, but ensure for redundancy)
    Shader.SetGlobalFloat("Terrain_ShoreWetnessLayer_Range", 0f);
    Shader.SetGlobalFloat("Terrain_ShoreWetnessLayer_BlendFactor", 0f);
    Shader.SetGlobalFloat("Terrain_ShoreWetnessLayer_BlendFalloff", 0f);
    Shader.SetGlobalFloat("Terrain_ShoreWetnessLayer_WetAlbedoScale", 0f);
    Shader.SetGlobalFloat("Terrain_ShoreWetnessLayer_WetSmoothness", 0f);

    // Disable Puddle Layer by setting metallic and smoothness to 0
    Shader.SetGlobalFloat("_LayerFallback_Metallic", 0f);
    Shader.SetGlobalFloat("_LayerFallback_Smoothness", 0f);
    // Optionally, set a neutral albedo to avoid a dark "wet" tint
    Shader.SetGlobalColor("_LayerFallback_Albedo", new Color(0.5f, 0.5f, 0.5f, 1f)); // Neutral gray

    // Disable Wetness Layer
    Shader.SetGlobalFloat("_WetnessLayer_Wetness", 0f);
    Shader.SetGlobalFloat("_WetnessLayer_WetAlbedoScale", 0f);
    Shader.SetGlobalFloat("_WetnessLayer_WetSmoothness", 0f);
    // Set the mask to a black texture to ensure no wetness is applied
    Texture2D blackTexture = new Texture2D(1, 1);
    blackTexture.SetPixel(0, 0, Color.black);
    blackTexture.Apply();
    Shader.SetGlobalTexture("_WetnessLayer_Mask", blackTexture);
	
	Shader.SetGlobalFloat("_Cutoff", 1.0f); // Force fully opaque rendering
    Shader.SetGlobalFloat("_CutoffRange", 0f); // Disable distance-based cutoff
	
    }

    private void GenerateShoreVectors(out float[] distances, out Vector3[] vectors)
    {
        float stepSize = terrainMaxDimension / textureResolution;
        Vector3 terrainPosition = TerrainManager.TerrainPosition;
        int terrainLayer = LayerMask.NameToLayer("Terrain");
        int layerMask = LayerMask.GetMask("Terrain", "Object");

        NativeArray<RaycastHit> hits = new NativeArray<RaycastHit>(
            textureResolution * textureResolution, Allocator.TempJob, NativeArrayOptions.ClearMemory);
        NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(
            textureResolution * textureResolution, Allocator.TempJob, NativeArrayOptions.ClearMemory);

        for (int y = 0; y < textureResolution; y++)
        {
            for (int x = 0; x < textureResolution; x++)
            {
                Vector3 origin = terrainPosition + new Vector3(
                    (x + 0.5f) * stepSize,
                    1000f,
                    (y + 0.5f) * stepSize
                );
                commands[y * textureResolution + x] = new RaycastCommand(origin, Vector3.down, float.MaxValue, layerMask, 1);
            }
        }

        RaycastCommand.ScheduleBatch(commands, hits, 1, default).Complete();

        byte[] shoreMap = new byte[textureResolution * textureResolution];
        distances = new float[textureResolution * textureResolution];
        vectors = new Vector3[textureResolution * textureResolution];

        int index = 0;
        for (int y = 0; y < textureResolution; y++)
        {
            for (int x = 0; x < textureResolution; x++)
            {
                RaycastHit hit = hits[index];
                bool isTerrain = hit.collider?.gameObject.layer == terrainLayer;
                if (isTerrain && hit.point.y <= 0f) isTerrain = false;

                shoreMap[index] = isTerrain ? byte.MaxValue : (byte)0;
                distances[index] = isTerrain ? 256f : 0f;
                index++;
            }
        }

        byte threshold = 127;
        TerrainProcessingUtility.ComputeSignedDistanceField(textureResolution, threshold, shoreMap, ref distances);
        TerrainProcessingUtility.ApplyGaussianBlur(textureResolution, distances, 1);
        TerrainProcessingUtility.ComputeNormals(textureResolution, distances, ref vectors);

        hits.Dispose();
        commands.Dispose();
    }

    private void CreateShoreVectorTexture(int resolution)
    {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBAHalf, false, true)
        {
            name = "Terrain_ShoreVector",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[resolution * resolution];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(shoreVectors[i].x, shoreVectors[i].y, shoreDistances[i]);
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        shoreVectorTexture = texture;
    }


    private void GenerateBaseTextures()
    {
        Material material = new Material(AssetManager.LoadAsset<Shader>("assets/content/shaders/resources/terrain/renderbase.shader"))
        {
            hideFlags = HideFlags.DontSave
        };

        UpdateShaderProperties();
        UpdateMaterialProperties();

        float texelOffset = 0.00024414062f;
        material.SetVector("_viewport", new Vector4(1f, -1f, 0f, 1f));
        material.SetVector("_offsets", new Vector4(-0.5f * texelOffset, 0.5f * texelOffset, 0f, 0f));
		
        GL.sRGBWrite = true;
        Graphics.Blit(null, baseDiffuseTexture, material, 0);
        GL.sRGBWrite = false;
        Graphics.Blit(null, baseNormalTexture, material, 1);

        DestroyObject(ref material);
        baseTextureState = TextureState.CachedRaw;
    }

    private bool IsHeightSlopeTextureLost() => heightSlopeTexture != null && !heightSlopeTexture.IsCreated();
    private bool IsBaseTextureLost() => (baseDiffuseTexture != null && !baseDiffuseTexture.IsCreated()) || (baseNormalTexture != null && !baseNormalTexture.IsCreated());

    private void CleanupResources()
    {
        CleanupBaseTextures();
        CleanupHeightSlopeTexture();
        CleanupShoreData();
        isInitialized = false;
    }

    private void CleanupBaseTextures() { DestroyObject(ref baseDiffuseTexture); DestroyObject(ref baseNormalTexture); }
    private void CleanupHeightSlopeTexture() { DestroyObject(ref heightSlopeTexture); }
    private void CleanupShoreData() { shoreDistances = null; shoreVectors = null; DestroyObject(ref shoreVectorTexture); }

    private void DestroyObject<T>(ref T obj) where T : UnityEngine.Object
    {
        if (obj != null)
        {
            UnityEngine.Object.DestroyImmediate(obj);
            obj = null;
        }
    }

    private RenderTexture CreateRenderTexture(string name, int size, bool linear)
    {
        RenderTextureReadWrite readWrite = linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB;
        RenderTexture rt = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, readWrite)
        {
            name = name,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = true,
            autoGenerateMips = true
        };
        rt.Create();
        return rt;
    }

    private void InitializeShaderPropertyIds()
    {
        if (shaderPropertyIds != null) return;

        shaderPropertyIds = new int[19, 8];
        for (int i = 0; i < 8; i++)
        {
            shaderPropertyIds[0, i] = Shader.PropertyToID($"Splat{i}_UVMIX");
            shaderPropertyIds[1, i] = Shader.PropertyToID($"Splat{i}_AridColor");
            shaderPropertyIds[2, i] = Shader.PropertyToID($"Splat{i}_TemperateColor");
            shaderPropertyIds[3, i] = Shader.PropertyToID($"Splat{i}_TundraColor");
            shaderPropertyIds[4, i] = Shader.PropertyToID($"Splat{i}_ArcticColor");
            shaderPropertyIds[5, i] = Shader.PropertyToID($"Splat{i}_AridOverlayColor");
            shaderPropertyIds[6, i] = Shader.PropertyToID($"Splat{i}_TemperateOverlayColor");
            shaderPropertyIds[7, i] = Shader.PropertyToID($"Splat{i}_TundraOverlayColor");
            shaderPropertyIds[8, i] = Shader.PropertyToID($"Splat{i}_ArcticOverlayColor");
            shaderPropertyIds[9, i] = Shader.PropertyToID($"Splat{i}_AridOverlayParam");
            shaderPropertyIds[10, i] = Shader.PropertyToID($"Splat{i}_TemperateOverlayParam");
            shaderPropertyIds[11, i] = Shader.PropertyToID($"Splat{i}_TundraOverlayParam");
            shaderPropertyIds[12, i] = Shader.PropertyToID($"Splat{i}_ArcticOverlayParam");
            shaderPropertyIds[13, i] = Shader.PropertyToID($"_Layer{i}_Metallic");
            shaderPropertyIds[14, i] = Shader.PropertyToID($"_Layer{i}_Factor");
            shaderPropertyIds[15, i] = Shader.PropertyToID($"_Layer{i}_Falloff");			
			shaderPropertyIds[16, i] = Shader.PropertyToID($"Splat{i}_JungleColor");
            shaderPropertyIds[17, i] = Shader.PropertyToID($"Splat{i}_JungleOverlayColor");
            shaderPropertyIds[18, i] = Shader.PropertyToID($"Splat{i}_JungleOverlayParam");
        }
    }

    public bool needsRefresh;


}