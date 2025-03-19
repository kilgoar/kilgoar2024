using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using RustMapEditor.Variables;

public static class ModManager
{
    public static List<WorldSerialization.MapData> moddingData = new List<WorldSerialization.MapData>();
    public static readonly string[] KnownDataNames = new string[] 
    { 
        "ioentitydata", "vehiclespawnpoints", "lootcontainerdata", "vendingdata", 
        "npcspawnpoints", "bradleypathpoints", "anchorpaths", "mappassword",
		"buildingblocks", "oceanpathpoints"
    };
	
	//compatibility for high-security data fields (prefab count + salt)
	public static string MapDataName(int PreFabCount, string DataName)
    {      
       try
       {
           using (var aes = Aes.Create())
           {
               var rfc2898DeriveBytes = new Rfc2898DeriveBytes(PreFabCount.ToString(), new byte[] { 73, 118, 97, 110, 32, 77, 101, 100, 118, 101, 100, 101, 118 });
               aes.Key = rfc2898DeriveBytes.GetBytes(32);
               aes.IV = rfc2898DeriveBytes.GetBytes(16);
               using (var memoryStream = new MemoryStream())
               {
                   using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                   {
                       var D = Encoding.Unicode.GetBytes(DataName);
                       cryptoStream.Write(D, 0, D.Length);
                       cryptoStream.Close();
                   }

                   return Convert.ToBase64String(memoryStream.ToArray());
               }
           }
       }
       catch { }
       return DataName;
    }

    public static void SetModdingData(List<WorldSerialization.MapData> data)
    {
        moddingData.Clear();
        if (data != null)
        {
            moddingData.AddRange(data);
        }
        else
        {
        }
    }

    public static List<WorldSerialization.MapData> GetModdingData()
    {
        return new List<WorldSerialization.MapData>(moddingData);
    }


    public static void AddOrUpdateModdingData(string name, byte[] data)
    {
        var existing = moddingData.Find(md => md.name == name);
        if (existing != null)
        {
            existing.data = data;
        }
        else
        {
            moddingData.Add(new WorldSerialization.MapData { name = name, data = data });
        }
    }


[ConsoleCommand("Sample code for programmatic window creation")]
public static TemplateWindow CreateSampleWindow()
{
    if (AppManager.Instance == null)
    {
        Debug.LogError("AppManager.Instance is not initialized. Cannot create sample window.");
        return null;
    }

    // Create the sample window
    // A Rect defines a rectangular area with (x, y, width, height).
    // - x: Horizontal position from the left edge of the screen (positive moves right).
    // - y: Vertical position from the top of the screen (negative moves down in Unity UI).
    // - width: The width of the rectangle in pixels.
    // - height: The height of the rectangle in pixels.
    TemplateWindow sampleWindow = AppManager.Instance.CreateWindow(
        titleText: "Sample Plugin Window",
        rect: new Rect(300, -150, 647, 400) //(1 + Math.Sqrt(5)) / 2; what ???
    );

    if (sampleWindow != null)
    {

        Toggle sampleToggle = AppManager.Instance.CreateToggle(
            sampleWindow.transform,
            new Rect(300, -70, 100, 20), 
            "Toggle Your FACE off!"
        );
        sampleToggle.onValueChanged.AddListener((value) => Debug.Log($"Toggle value changed to: {value}"));

        Button sampleButton = AppManager.Instance.CreateButton(
            sampleWindow.transform,
            new Rect(20, -50, 100, 22), 
            "Buttons?"
        );
        sampleButton.onClick.AddListener(() => Debug.Log("Default Button clicked!"));

        Button sampleBrightButton = AppManager.Instance.CreateBrightButton(
            sampleWindow.transform,
            new Rect(20, -100, 100, 22), 
            "RED Buttons?"
        );
        sampleBrightButton.onClick.AddListener(() => Debug.Log("Bright Button clicked!"));

        Text sampleLabel = AppManager.Instance.CreateLabelText(
            sampleWindow.transform,
            new Rect(300, -160, 300, 22),
            "Just TRY to label me"
        );

        Slider sampleSlider = AppManager.Instance.CreateSlider(
            sampleWindow.transform,
            new Rect(20, -150, 300, 25)
        );
        if (sampleSlider != null)
        {
            sampleSlider.minValue = 0f;
            sampleSlider.maxValue = 100f;
            sampleSlider.value = 50f;
            sampleSlider.onValueChanged.AddListener((value) => Debug.Log($"Slider value changed to: {value}"));
        }

        Dropdown sampleDropdown = AppManager.Instance.CreateDropdown(
            sampleWindow.transform,
            new Rect(20, -220, 300, 25)
        );
        sampleDropdown.options.Clear();
        sampleDropdown.options.Add(new Dropdown.OptionData("How DARE you"));
        sampleDropdown.options.Add(new Dropdown.OptionData("Monkey suits"));
        sampleDropdown.options.Add(new Dropdown.OptionData("Gingerbreads"));
        sampleDropdown.value = 0;
        sampleDropdown.onValueChanged.AddListener((value) => Debug.Log($"Dropdown value changed to: {sampleDropdown.options[value].text}"));

        InputField sampleInput = AppManager.Instance.CreateInputField(
            sampleWindow.transform,
            new Rect(20, -290, 300, 25),
            "This could be your next hell project"
        );
        sampleInput.onValueChanged.AddListener((value) => Debug.Log($"Input field value changed to: {value}"));
    }

    return sampleWindow;
}
	
    public static void ClearModdingData()
    {
        moddingData.Clear();
    }


    public static string[] GetKnownDataNames()
    {
        return KnownDataNames;
    }
}