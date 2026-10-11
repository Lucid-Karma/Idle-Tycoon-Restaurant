using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CrazyGames
{
    public class Utils
    {
        public static string ToJson<T>(T[] array)
        {
            var wrapper = new JsonWrapper<T>();
            wrapper.Items = array;
            return FixJson(JsonUtility.ToJson(wrapper));
        }

        private static string FixJson(string value)
        {
            return value.Substring(9, value.Length - 10);
        }

        [Serializable]
        private class JsonWrapper<T>
        {
            public T[] Items;
        }

        public static string AppendQueryParameters(string baseUrl, Dictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return baseUrl;
            }

            // previously used HttpUtility here, but it's not supported in some NET versions in some Unity versions
            var queryString = string.Join(
                "&",
                parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}")
            );

            return baseUrl.Contains("?") ? $"{baseUrl}&{queryString}" : $"{baseUrl}?{queryString}";
        }

        public static string ConvertDictionaryToJson(Dictionary<string, string> dictionary)
        {
            var jsonBuilder = new StringBuilder();
            jsonBuilder.Append("{");

            var count = 0;
            foreach (var kvp in dictionary)
            {
                count++;
                jsonBuilder.AppendFormat("\"{0}\":\"{1}\"", EscapeString(kvp.Key), EscapeString(kvp.Value));

                if (count < dictionary.Count)
                {
                    jsonBuilder.Append(",");
                }
            }

            jsonBuilder.Append("}");
            return jsonBuilder.ToString();
        }

        public static string EscapeString(string input)
        {
            return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>
        /// Since Unity's JsonUtility does not support deserializing directly to a Dictionary, we use a wrapper class to first deserialize the JSON into a list of key-value pairs, and then convert that list into a Dictionary.
        /// </summary>
        /// <param name="json">Can be "null" or a JSON that matches DictionaryWrapper class</param>
        /// <returns>null if the input string is "null", otherwise a dictionary</returns>
        public static Dictionary<string, string> DeserializeCustomJsonDictionary(string json)
        {
            if (json == "null")
            {
                return null;
            }

            DictionaryWrapper wrapper = JsonUtility.FromJson<DictionaryWrapper>(json);
            return wrapper.entries.ToDictionary(e => e.key, e => e.value);
        }
    }

    public class MessageStrings
    {
        public const string SDK_NOT_INITIALIZED =
            "CrazySDK initialization required. Call CrazySDK.Init() and await its completion callback before using other SDK methods.";
    }

    [Serializable]
    class DictionaryEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    class DictionaryWrapper
    {
        public List<DictionaryEntry> entries;
    }
}
