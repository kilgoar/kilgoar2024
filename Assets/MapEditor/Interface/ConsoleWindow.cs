using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using RustMapEditor.Variables;


public class ConsoleWindow : MonoBehaviour
{
    public InputField consoleInput;
    public VerticalLayoutGroup consoleOutputLayout;
    public Text textTemplate;
    public ScrollRect consoleScrollRect;
	
	private List<string> commandHistory = new List<string>();
	private int historyIndex = -1;
	
	private Dictionary<string, object> consoleVariables = new Dictionary<string, object>();

	public static ConsoleWindow Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)        
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
						//create objects necessary for using accessible methods
			InitializeConsoleVariables();
			
			// Assign listener for InputField's submit event
			consoleInput.onEndEdit.AddListener(OnInputEndEdit);
			textTemplate.gameObject.SetActive(false);
			Startup();		
        }
        else        
        {
            Destroy(gameObject);
        }
    }
	
	 
	
	// New Startup method to run a startup script
    public void Startup()
    {
        const string startupScriptName = "startup.rmml";
        List<string> commands = SettingsManager.GetScriptCommands(startupScriptName);
        
        if (commands.Count > 0)
        {
            Post($"{startupScriptName}");
            foreach (string cmd in commands)
            {
                string trimmedCmd = cmd.Trim().ToLower();
                if (trimmedCmd.StartsWith("run"))
                {
                    Post($"> {cmd}");
                    Post("Nested 'run' commands are not allowed in startup script.");
                    continue;
                }
                ExecuteCommand(cmd);
            }
            Post($"Startup script {startupScriptName} completed.");
        }
        else
        {
            Post($"No startup script found ({startupScriptName})");
        }
    }

    private void Start()
    {


    }

    private void OnInputEndEdit(string text)
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            OnSubmit();
        }
    }
	
	public void PostMultiLine(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            Post(""); // Handle empty input with a single empty post
            return;
        }

        // Split the message by line breaks (\n, \r\n, or \r) and filter out empty lines if desired
        string[] lines = message.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

        foreach (string line in lines)
        {
            // Post each line separately; you can trim or skip empty lines if preferred
            Post(line);
        }
    }

	public void Post(string message)
	{
		// Create a new text object for the message
		Text newText = Instantiate(textTemplate, consoleOutputLayout.transform);
		newText.gameObject.SetActive(true);
		newText.text = message;
		Canvas.ForceUpdateCanvases();
		// Scroll to the bottom of the ScrollRect
		consoleScrollRect.verticalNormalizedPosition = 0f;
	}

    private void Update()
    {
        if (consoleInput.isFocused)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                NavigateHistory(true); // Up arrow
            }
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                NavigateHistory(false); // Down arrow
            }
        }
    }

    public void OnSubmit()
    {
        if (!string.IsNullOrEmpty(consoleInput.text))
        {
            commandHistory.Add(consoleInput.text);
            historyIndex = commandHistory.Count;
            ExecuteCommand(consoleInput.text);
        }

        ActivateConsole();
    }
	
	private void NavigateHistory(bool up)
    {
        if (commandHistory.Count == 0) return;

        if (up)
        {
            historyIndex = Mathf.Max(-1, historyIndex - 1);
        }
        else
        {
            historyIndex = Mathf.Min(commandHistory.Count - 1, historyIndex + 1);
        }

        if (historyIndex >= 0)
        {
            consoleInput.text = commandHistory[historyIndex];
            consoleInput.caretPosition = consoleInput.text.Length;
        }
        else
        {
            consoleInput.text = "";
        }
    }
	
	private void ExecuteCommand(string command)
    {
 		// Echo the command entered
		Post("> " + command);

		string[] parts = command.Split(new char[] { ' ' }, 2);
		
		
			// Check if the command is a variable name or help command
		if (parts[0].Trim().ToLower() == "help")
			{
				if (parts.Length == 2)
				{
					ListMethods(parts[1]);
				}
				if (parts.Length == 1)
				{
					ListMethods();
				}
				ActivateConsole();
				return;
			}
			
		if (parts[0].Trim().ToLower() == "loadmods")
		{		
			PostMultiLine(HarmonyLoader.LoadHarmonyMods(Path.Combine(SettingsManager.AppDataPath(), "HarmonyMods")));
			ActivateConsole();
			return;
		}

		// Handle 'dir' command for listing .rmml files
		if (parts[0].Trim().ToLower() == "dir")
		{
			Post("");
			List<string> scriptFiles = SettingsManager.GetScriptFiles();
			if (scriptFiles.Count == 0)
			{
				Post("No scripts (.rmml) found in AppData/Roaming/RustMapper/Presets/Scripts/");
			}
			else
			{
				Post("AppData/Roaming/RustMapper/Presets/Scripts/");
				foreach (string file in scriptFiles)
				{
					Post($"  {file}");
				}
			}
			
			ActivateConsole();
			return;
		}
		
	if (parts[0].Trim().ToLower() == "echo")
	{
		string trimmedCommand = command;
		trimmedCommand = command.Substring(5);

		Post(trimmedCommand);
		ActivateConsole();
		return;
	}
	
	if (parts[0].Trim().ToLower() == "run")
	{
		if (parts.Length < 2)
		{
			Post("Usage: run <scriptname>.rmml");
			ActivateConsole();
			return;
		}

		System.Diagnostics.Stopwatch batchWatch = System.Diagnostics.Stopwatch.StartNew();
		string scriptName = parts[1].Trim();
		if (!scriptName.EndsWith(".rmml", StringComparison.OrdinalIgnoreCase))
		{
			scriptName += ".rmml"; // Append extension if missing
		}

		List<string> commands = SettingsManager.GetScriptCommands(scriptName);
		if (commands.Count == 0)
		{
			Post($"No commands found in {scriptName}.");
			ActivateConsole();
			return;
		}

		Post($"Executing {scriptName}");
		foreach (string cmd in commands)
		{
			// Check if the command is "run" and skip it to prevent nesting
			string trimmedCmd = cmd.Trim().ToLower();
			if (trimmedCmd.StartsWith("run"))
			{
				Post($"> {cmd}");
				Post("Nested 'run' commands are not allowed.");
				continue; // Skip to the next command
			}

			ExecuteCommand(cmd); // Execute it
		}

		batchWatch.Stop();
		Post($"Script {scriptName} completed in {batchWatch.ElapsedMilliseconds}ms.");
		ActivateConsole();
		return;
	}
	
	if (parts[0].Trim().ToLower() == "list" && parts.Length > 1)
	{
		string scriptName = parts[1].Trim();
		if (!scriptName.EndsWith(".rmml", StringComparison.OrdinalIgnoreCase))
		{
			scriptName += ".rmml"; // Ensure extension if omitted
		}

		List<string> commands = SettingsManager.GetScriptCommands(scriptName);
		if (commands.Count == 0)
		{
			Post($"No commands found in {scriptName}.");
		}
		else
		{
			Post($"Commands in {scriptName}:");
			foreach (string cmd in commands)
			{
				Post($"  {cmd}");
			}
		}

		ActivateConsole();
		return;
	}

		// Measure the time of execution
		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

		try
			{

				string[] subParts = parts[0].Split('.');
				
					object variableInstance = GetStructInstance(subParts[0]);

					if (variableInstance!=null) 
					{
						//display variables and fields when they're named in console
						if (parts.Length == 1){	
							if(subParts.Length == 1){ PostVariableFields(variableInstance);	}
							else if (subParts.Length == 2) {	PostVariableField(variableInstance, subParts[1]); }
							
							ActivateConsole();
							return;
						}
						else if (parts.Length == 2){
							if(subParts.Length == 2){ //only allow the modifications of variable fields
								ModifyVariableField(variableInstance, subParts[1], parts[1]);
								ActivateConsole();
								return;
							}
						}
					}
					else { //now look for methods
						
							string prefix = subParts[0].ToLower();
							string actualMethodName = subParts[1];
							string parameters = parts[1];

							Type targetClass = prefix switch
							{
								"t" => typeof(TerrainManager),
								"g" => typeof(GenerativeManager),
								"p" => typeof(PrefabManager),
								"m" => typeof(MapManager),
								_ => null
							};

							if (targetClass == null)
							{
								Post($"Unknown command prefix: {prefix}");
								ActivateConsole();
								return;
							}
							
							// Use reflection to find and execute the method
							System.Reflection.MethodInfo method = targetClass.GetMethod(actualMethodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
							if (method == null)
							{
								Post($"Method '{actualMethodName}' not found in {targetClass.Name}.");
								ActivateConsole();
								return;
							}

							// Convert string parameters to the method's expected parameter types
							object[] convertedParams = parameters.Length > 0 ? ConvertParameters(method.GetParameters(), parameters.Split(',')) : new object[0];

							// Execute method
							method.Invoke(null, convertedParams);
							stopwatch.Stop();

							// Post success message with elapsed time
							Post($"Method '{prefix}.{actualMethodName}' executed successfully in {stopwatch.ElapsedMilliseconds}ms.");
							
							ActivateConsole();
							return;
							
					}

			}
			catch (System.FormatException)
				{
					Post($"Parameter conversion failed. Check parameter types.");
					stopwatch.Stop();
					ActivateConsole();
					return;
				}
			catch (System.Reflection.TargetParameterCountException)
				{
					Post($"Incorrect number of parameters for the method.");
					stopwatch.Stop();
					ActivateConsole();
					return;
				}
			catch (System.Reflection.TargetInvocationException e)
				{
					Post($"An error occurred while executing the method: {e.InnerException?.Message}");
					stopwatch.Stop();
					ActivateConsole();
					return;
				}
			catch (Exception e)
				{
					Post($"An unexpected error occurred: {e.Message}");
					stopwatch.Stop();
					ActivateConsole();
					return;
				}
			
		Post("Arglebargle glop-glyf?");
		ActivateConsole();
    }
	
	private void OnEnable(){
		consoleInput.ActivateInputField();
	}
	
	private void ActivateConsole(){
		consoleInput.text = "";
		consoleInput.ActivateInputField();
	}

	private void PostVariableFields(object variable)
	{
		Type type = variable.GetType();
		foreach (var field in type.GetFields())
		{
			Post($"{field.Name}: {field.GetValue(variable)}");
		}
	}

	private void PostVariableField(object variable, string fieldName)
	{
		Type type = variable.GetType();
		FieldInfo field = type.GetField(fieldName);
		if (field != null)
		{
			Post($"{fieldName}: {field.GetValue(variable)}");
		}
		else
		{
			Post($"Field '{fieldName}' not found in {type.Name}.");
		}
	}

	private void ModifyVariableField(object variable, string fieldName, string value)
	{
		Type type = variable.GetType();
		FieldInfo field = type.GetField(fieldName);
		if (field != null)
		{
			try
			{
				object convertedValue = Convert.ChangeType(value, field.FieldType);
				field.SetValue(variable, convertedValue);
				Post($"Field '{fieldName}' in {type.Name} set to '{value}'.");
			}
			catch (FormatException)
			{
				Post($"Cannot convert '{value}' to type {field.FieldType.Name} for field '{fieldName}'.");
			}
			catch (Exception e)
			{
				Post($"An error occurred: {e.Message}");
			}
		}
		else
		{
			Post($"Field '{fieldName}' not found in {type.Name}.");
		}
	}

	// Utility method to create Layers object based on keywords
	private Layers CreateLayerFromKeyword(string keyword)
	{
		Layers layer = new Layers();
		keyword = keyword.Trim();
		// Map keywords to TerrainSplat (Ground)
		switch (keyword)
		{
			case "dirt":
				layer.Ground = TerrainSplat.Enum.Dirt;
				layer.Layer = TerrainManager.LayerType.Ground;
				break;
			case "snow":
				layer.Ground = TerrainSplat.Enum.Snow;
				layer.Layer = TerrainManager.LayerType.Ground;
				break;
			case "sand":
				layer.Ground = TerrainSplat.Enum.Sand;
				layer.Layer = TerrainManager.LayerType.Ground;
				break;
			case "rock":
				layer.Ground = TerrainSplat.Enum.Rock;
				layer.Layer = TerrainManager.LayerType.Ground;
				break;
			case "grass":
				layer.Ground = TerrainSplat.Enum.Grass;
				layer.Layer = TerrainManager.LayerType.Ground;
				break;
			case "forest":
				layer.Ground = TerrainSplat.Enum.Forest;
				layer.Layer = TerrainManager.LayerType.Ground;
				break;
			case "stones":
				layer.Ground = TerrainSplat.Enum.Stones;
				layer.Layer = TerrainManager.LayerType.Ground;
				break;
			case "gravel":
				layer.Ground = TerrainSplat.Enum.Gravel;
				layer.Layer = TerrainManager.LayerType.Ground;
				break;

			// Map keywords to TerrainBiome
			case "arid":
				layer.Biome = TerrainBiome.Enum.Arid;
				layer.Layer = TerrainManager.LayerType.Biome;
				break;
			case "temperate":
				layer.Biome = TerrainBiome.Enum.Temperate;
				layer.Layer = TerrainManager.LayerType.Biome;
				break;
			case "tundra":
				layer.Biome = TerrainBiome.Enum.Tundra;
				layer.Layer = TerrainManager.LayerType.Biome;
				break;
			case "arctic":
				layer.Biome = TerrainBiome.Enum.Arctic;
				layer.Layer = TerrainManager.LayerType.Biome;
				break;

			// Map keywords to TerrainTopology (full list from /layers)
			case "field":
				layer.Topologies = TerrainTopology.Enum.Field;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "cliff":
				layer.Topologies = TerrainTopology.Enum.Cliff;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "summit":
				layer.Topologies = TerrainTopology.Enum.Summit;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "beach":
				layer.Topologies = TerrainTopology.Enum.Beach;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "foresttopo": // To avoid conflict with TerrainSplat.Forest
				layer.Topologies = TerrainTopology.Enum.Forest;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "ocean":
				layer.Topologies = TerrainTopology.Enum.Ocean;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "oceanside":
				layer.Topologies = TerrainTopology.Enum.Oceanside;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "riverside":
				layer.Topologies = TerrainTopology.Enum.Riverside;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "lakeside":
				layer.Topologies = TerrainTopology.Enum.Lakeside;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "roadside":
				layer.Topologies = TerrainTopology.Enum.Roadside;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "railside":
				layer.Topologies = TerrainTopology.Enum.Railside;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "swamp":
				layer.Topologies = TerrainTopology.Enum.Swamp;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "river":
				layer.Topologies = TerrainTopology.Enum.River;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "lake":
				layer.Topologies = TerrainTopology.Enum.Lake;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "offshore":
				layer.Topologies = TerrainTopology.Enum.Offshore;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "rail":
				layer.Topologies = TerrainTopology.Enum.Rail;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "building":
				layer.Topologies = TerrainTopology.Enum.Building;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "cliffside":
				layer.Topologies = TerrainTopology.Enum.Cliffside;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "mountain":
				layer.Topologies = TerrainTopology.Enum.Mountain;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "clutter":
				layer.Topologies = TerrainTopology.Enum.Clutter;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "alt":
				layer.Topologies = TerrainTopology.Enum.Alt;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "tier0":
				layer.Topologies = TerrainTopology.Enum.Tier0;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "tier1":
				layer.Topologies = TerrainTopology.Enum.Tier1;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "tier2":
				layer.Topologies = TerrainTopology.Enum.Tier2;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "mainland":
				layer.Topologies = TerrainTopology.Enum.Mainland;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			case "hilltop":
				layer.Topologies = TerrainTopology.Enum.Hilltop;
				layer.Layer = TerrainManager.LayerType.Topology;
				break;
			default:
				return null;
		}

		return layer;
	}

private void ListMethods(string command = "")
{
    Post("");
    Post("----------------------------");
    Post("Rust Mapper Platinum Edition");
    Post("============================");

    if (string.IsNullOrEmpty(command))
    {
        Post("Commands:");
		Post("dir - lists user scripts");
		Post("run - execute script");
		Post("");
		Post("Additional help:");
        Post("/t - heightmap related");
        Post("/g - GenerativeManager");
        Post("/p - PrefabManager");
		Post("/m - MapManager");
        Post("/layers - List of layer keywords");
        Post("Use 'help /prefix' to see methods for a specific module.");
    }
    else
    {
        string prefix = command.TrimStart('/').ToLower();
        Type classType = prefix switch
        {
            "t" => typeof(TerrainManager),
            "g" => typeof(GenerativeManager),
            "p" => typeof(PrefabManager),
			"m" => typeof(MapManager),
            "layers" => null, // Handled separately for listing keywords
            _ => null
        };

        if (classType != null)
        {
            Post($"Available methods for {classType.Name}:");
            System.Reflection.MethodInfo[] methods = classType.GetMethods(
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Static);

            foreach (var method in methods)
            {
                if (!method.IsSpecialName && Attribute.IsDefined(method, typeof(ConsoleCommandAttribute)))
                {
                    var commandAttribute = (ConsoleCommandAttribute)Attribute.GetCustomAttribute(method, typeof(ConsoleCommandAttribute));
                    string methodSignature = $"{prefix.ToLower()}.{method.Name} ";
                    foreach (var param in method.GetParameters())
                    {
                        methodSignature += $"{param.ParameterType.Name}:{param.Name}, ";
                    }
                    if (method.GetParameters().Length > 0)
                    {
                        methodSignature = methodSignature.Remove(methodSignature.Length - 2); // Remove last ", "
                    }
                    Post($"{methodSignature} - {commandAttribute.Description}");
                }
            }

            // Special handling for PrefabManager (/p) to include keyword explanation
            if (prefix == "p")
            {
                Post("");
                Post("prefabs parameter keywords:");
                Post("'all': Operate on all prefabs managed by PrefabManager.");
                Post("'selection': Operate only on currently selected prefabs.");
            }

            // Special handling for GenerativeManager (/g) - console variables
            if (prefix == "g")
            {
                Post("");
                Post("Accessible console variables for GenerativeManager:");
                foreach (var variable in consoleVariables)
                {
                    var type = variable.Value.GetType();
                    var attribute = (ConsoleVariableAttribute)Attribute.GetCustomAttribute(type, typeof(ConsoleVariableAttribute));
                    if (attribute != null)
                    {
                        Post($"{variable.Key} - {attribute.Description}");
                    }
                }
            }
        }
        else if (prefix == "layers")
        {
            Post("Available layer keywords:");
            
            // List all TerrainSplat (Ground) keywords
            Post("Ground Layers:");
            Post("dirt, snow, sand, rock, grass, forest, stones, gravel");

            // List all TerrainBiome keywords
            Post("Biome Layers:");
            Post("arid, temperate, tundra, arctic");

            // List all TerrainTopology keywords
            Post("Topology Layers:");
            Post("field, cliff, summit, beach, foresttopo, ocean, oceanside");
			Post("riverside, lakeside, roadside, railside, swamp, river");
			Post("lake, offshore, rail, building, cliffside, mountain");
			Post("clutter, alt, tier0, tier1, tier2, mainland, hilltop");
        }
        else
        {
            Post($"Module '{prefix}' not recognized. Use 'help' to see available modules.");
        }
    }
}

	private void InitializeConsoleVariables()
	{
		var structs = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => a.GetTypes())
			.Where(t => t.IsValueType && t.IsDefined(typeof(ConsoleVariableAttribute), false));

		foreach (var type in structs)
		{
			var instance = Activator.CreateInstance(type);
			if (instance != null)
			{
				string structName = type.Name;
				consoleVariables[structName] = instance;
				Post($"Initialized {structName}...");
			}
		}
		Post("help for list of commands");
	}

	private object GetStructInstance(string structName)
	{
		if (consoleVariables.TryGetValue(structName, out var instance))
		{
			return instance;
		}
		return null;
	}
	
	private object[] ConvertParameters(System.Reflection.ParameterInfo[] methodParams, string[] stringParams)
	{
		List<object> convertedParams = new List<object>();

		for (int i = 0; i < methodParams.Length && i < stringParams.Length; i++)
		{
			Type paramType = methodParams[i].ParameterType;
			string paramValue = stringParams[i].Trim().ToLower(); // Case insensitive for keywords

			// Check if the parameter is the name of an existing variable
			if (consoleVariables.TryGetValue(paramValue, out object variableInstance))
			{
				if (paramType.IsAssignableFrom(variableInstance.GetType()))
				{
					convertedParams.Add(variableInstance);
				}
				else
				{
					Post($"Type mismatch: Cannot convert variable {paramValue} of type {variableInstance.GetType().Name} to {paramType.Name}.");
					throw new System.ArgumentException($"Type mismatch for parameter '{paramValue}'");
				}
			}
			else if (paramType == typeof(PrefabDataHolder[]))
			{
				// Handle special cases for PrefabDataHolder[]
				if (paramValue == "all")
				{
					// Assuming PrefabManager is a static class or singleton
					PrefabDataHolder[] allPrefabs = PrefabManager.CurrentMapPrefabs;
					if (allPrefabs == null)
					{
						Post("No prefabs found in CurrentMapPrefabs.");
						throw new System.ArgumentException("No prefabs available.");
					}
					convertedParams.Add(allPrefabs);
				}
				else if (paramValue == "selection")
				{
					// Assuming CameraManager is a static class or singleton with a method SelectedDataHolders
					PrefabDataHolder[] selectedPrefabs = CameraManager.Instance.SelectedDataHolders();
					if (selectedPrefabs == null || selectedPrefabs.Length == 0)
					{
						Post("No prefabs selected");
						throw new System.ArgumentException("No selected prefabs available.");
					}
					convertedParams.Add(selectedPrefabs);
				}
				else
				{
					Post($"Parameter '{paramValue}' is not recognized as 'all' or 'selection' for PrefabDataHolder[].");
					throw new System.ArgumentException($"Invalid value '{paramValue}' for PrefabDataHolder[] parameter.");
				}
			}
			else if (paramType == typeof(Layers))
			{
				// Handle Layers parameter by creating a new Layers object from the keyword
				Layers layer = CreateLayerFromKeyword(paramValue);
				if (layer == null)
				{
					Post($"Keyword '{paramValue}' not recognized for creating a Layers object.");
					throw new System.ArgumentException($"Invalid keyword '{paramValue}' for Layers parameter.");
				}
				convertedParams.Add(layer);
			}
			else
			{
				// If not a variable name, PrefabDataHolder[], or Layers, proceed with type conversion as before
				if (paramType == typeof(int))
				{
					convertedParams.Add(int.Parse(paramValue));
				}
				else if (paramType == typeof(float))
				{
					convertedParams.Add(float.Parse(paramValue));
				}
				else if (paramType == typeof(bool))
				{
					if (bool.TryParse(paramValue, out bool result))
						convertedParams.Add(result);
					else
						throw new System.FormatException($"Boolean parameter '{paramValue}' is invalid.");
				}
				else if (paramType == typeof(string))
				{
					convertedParams.Add(paramValue);
				}
				else
				{
					throw new System.NotSupportedException($"Parameter type {paramType.Name} not supported for automatic conversion.");
				}
			}
		}

		return convertedParams.ToArray();
	}
}