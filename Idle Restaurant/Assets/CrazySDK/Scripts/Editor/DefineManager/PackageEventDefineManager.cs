using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEngine;

namespace CrazyGames
{
#if UNITY_6000_0_OR_NEWER
    [InitializeOnLoad]
    public static class PackageEventDefineManager
    {
        private struct PackageInfo
        {
            public string Symbol;
            public string PackageName;
            public string NamespaceName;
        }

        private static readonly Dictionary<string, PackageInfo> PackageDefinitions = new Dictionary<string, PackageInfo>
        {
            {
                "PROJECT_AUDITOR_AVAILABLE",
                new PackageInfo
                {
                    Symbol = "PROJECT_AUDITOR_AVAILABLE",
                    PackageName = "com.unity.project-auditor",
                    NamespaceName = "Unity.ProjectAuditor.Editor.ProjectAuditor, Unity.ProjectAuditor.Editor",
                }
            },
            {
                "ADDRESSABLE_AVAILABLE",
                new PackageInfo
                {
                    Symbol = "ADDRESSABLE_AVAILABLE",
                    PackageName = "com.unity.addressables",
                    NamespaceName = "UnityEngine.AddressableAssets.Addressables, Unity.Addressables",
                }
            },
        };

        static PackageEventDefineManager()
        {
            // rely on Package Manager API to be able to remove symbols before the package is actually removed (and avoid compilation errors)
            Events.registeringPackages += OnRegisteringPackages;
            Events.registeredPackages += OnRegisteredPackages;

            // this is kept for the initial start, when we don't know what is/isn't installed
            EditorApplication.delayCall += CheckAndCleanSymbols;
        }

        // detect a package being removed, and remove related symbol before the package is actually removed
        private static void OnRegisteringPackages(PackageRegistrationEventArgs args)
        {
            foreach (var package in args.removed)
            {
                if (PackageDefinitions.Values.Any(p => p.PackageName == package.name))
                {
                    var packageInfo = PackageDefinitions.Values.First(p => p.PackageName == package.name);
                    RemoveSymbolFromAllTargets(packageInfo.Symbol);
                }
            }
        }

        // detect a package being added, and add related symbol after the package is actually added
        private static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            foreach (var package in args.added)
            {
                if (PackageDefinitions.Values.Any(p => p.PackageName == package.name))
                {
                    var packageInfo = PackageDefinitions.Values.First(p => p.PackageName == package.name);
                    AddSymbolToAllTargets(packageInfo.Symbol);
                }
            }
        }

        private static void CheckAndCleanSymbols()
        {
            foreach (var package in PackageDefinitions.Values)
            {
                bool isInstalled = IsPackageInstalled(package.NamespaceName);
                bool hasSymbol = HasSymbol(package.Symbol);

                if (!isInstalled && hasSymbol)
                {
                    RemoveSymbolFromAllTargets(package.Symbol);
                }
                else if (isInstalled && !hasSymbol)
                {
                    AddSymbolToAllTargets(package.Symbol);
                }
            }
        }

        private static bool IsPackageInstalled(string namespaceName)
        {
            try
            {
                return System.Type.GetType(namespaceName) != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasSymbol(string symbol)
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL);
            return symbols.Contains(symbol);
        }

        private static void AddSymbolToAllTargets(string symbol)
        {
            try
            {
                var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL);

                if (!symbols.Contains(symbol))
                {
                    symbols = string.IsNullOrEmpty(symbols) ? symbol : symbols + ";" + symbol;
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, symbols);

                    AssetDatabase.Refresh();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not add symbol: {e.Message}");
            }
        }

        private static void RemoveSymbolFromAllTargets(string symbol)
        {
            try
            {
                var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL);

                if (symbols.Contains(symbol))
                {
                    var symbolsList = symbols.Split(';').Where(d => d != symbol);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, string.Join(";", symbolsList));

                    AssetDatabase.Refresh();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not remove symbol: {e.Message}");
            }
        }
    }
#endif
}
