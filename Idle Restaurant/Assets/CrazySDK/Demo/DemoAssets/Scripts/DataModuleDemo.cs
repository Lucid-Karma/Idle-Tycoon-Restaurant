using System;
using System.Globalization;
using CrazyGames;
using UnityEngine;
using UnityEngine.UI;

public class DataModuleDemo : MonoBehaviour
{
    [Header("Int")]
    public InputField intKeyName;
    public InputField intKeyValue;

    [Header("Float")]
    public InputField floatKeyName;
    public InputField floatKeyValue;

    [Header("String")]
    public InputField strKeyName;
    public InputField strKeyValue;

    [Header("Other")]
    public InputField hasKeyName;
    public InputField deleteKeyName;

    private void Start()
    {
        CrazySDK.Init(() => { }); // ensure if starting this scene from editor it is initialized
    }

    public void SetInt()
    {
        CrazySDK.Data.SetInt(intKeyName.text, int.Parse(intKeyValue.text));
    }

    public void GetInt()
    {
        try
        {
            Debug.Log(CrazySDK.Data.GetInt(intKeyName.text));
        }
        catch (Exception)
        {
            Debug.LogError("Failed to parse int " + intKeyName.text);
        }
    }

    public void SetFloat()
    {
        CrazySDK.Data.SetFloat(floatKeyName.text, float.Parse(floatKeyValue.text, CultureInfo.InvariantCulture));
    }

    public void GetFloat()
    {
        try
        {
            Debug.Log(CrazySDK.Data.GetFloat(floatKeyName.text));
        }
        catch (Exception)
        {
            Debug.LogError("Failed to parse float " + floatKeyName.text);
        }
    }

    public void SetStr()
    {
        CrazySDK.Data.SetString(strKeyName.text, strKeyValue.text);
    }

    public void GetStr()
    {
        Debug.Log(CrazySDK.Data.GetString(strKeyName.text));
    }

    public void HasKey()
    {
        Debug.Log(CrazySDK.Data.HasKey(hasKeyName.text));
    }

    public void DeleteKey()
    {
        CrazySDK.Data.DeleteKey(deleteKeyName.text);
        Debug.Log("Key " + deleteKeyName.text + " deleted");
    }

    public void DeleteAll()
    {
        CrazySDK.Data.DeleteAll();
        Debug.Log("All keys deleted");
    }
}
