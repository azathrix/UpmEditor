#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Azathrix.UpmEditor.Editor.Core
{
    /// <summary>
    /// Package information with editing state
    /// </summary>
    public class PackageInfo
    {
        public string Path;
        public UPMPackageData Data;
        public ChangelogData Changelog;
        public bool IsSelected;
        public bool IsDirty;
        public string OriginalVersion;

        public string Name => Data?.name ?? "";
        public string DisplayName => Data?.displayName ?? Name;
        public string Version => Data?.version ?? "";

        public PackageInfo(string path, UPMPackageData data)
        {
            Path = path;
            Data = data;
            OriginalVersion = data?.version;
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }

        public void ClearDirty()
        {
            IsDirty = false;
            OriginalVersion = Data?.version;
        }
    }

    /// <summary>
    /// Cache for loaded packages and dependency graph
    /// </summary>
    public class PackageCache
    {
        public List<PackageInfo> Packages = new List<PackageInfo>();
        public Dictionary<string, List<string>> DependencyGraph = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> ReverseDependencies = new Dictionary<string, List<string>>();
        public Dictionary<string, List<PackageInfo>> ScopeGroups = new Dictionary<string, List<PackageInfo>>();

        private Dictionary<string, PackageInfo> _packageByName = new Dictionary<string, PackageInfo>();

        /// <summary>
        /// Get package by name
        /// </summary>
        public PackageInfo GetPackage(string name)
        {
            _packageByName.TryGetValue(name, out var pkg);
            return pkg;
        }

        /// <summary>
        /// Get all selected packages
        /// </summary>
        public List<PackageInfo> GetSelectedPackages()
        {
            return Packages.FindAll(p => p.IsSelected);
        }

        /// <summary>
        /// Get all dirty packages
        /// </summary>
        public List<PackageInfo> GetDirtyPackages()
        {
            return Packages.FindAll(p => p.IsDirty);
        }

        /// <summary>
        /// Get packages that depend on the given package
        /// </summary>
        public List<PackageInfo> GetDependents(string packageName)
        {
            var result = new List<PackageInfo>();
            if (ReverseDependencies.TryGetValue(packageName, out var dependents))
            {
                foreach (var depName in dependents)
                {
                    var pkg = GetPackage(depName);
                    if (pkg != null)
                        result.Add(pkg);
                }
            }
            return result;
        }

        /// <summary>
        /// Get packages that this package depends on
        /// </summary>
        public List<PackageInfo> GetDependencies(string packageName)
        {
            var result = new List<PackageInfo>();
            if (DependencyGraph.TryGetValue(packageName, out var deps))
            {
                foreach (var depName in deps)
                {
                    var pkg = GetPackage(depName);
                    if (pkg != null)
                        result.Add(pkg);
                }
            }
            return result;
        }

        /// <summary>
        /// Clear all data
        /// </summary>
        public void Clear()
        {
            Packages.Clear();
            DependencyGraph.Clear();
            ReverseDependencies.Clear();
            ScopeGroups.Clear();
            _packageByName.Clear();
        }

        /// <summary>
        /// Add a package to the cache
        /// </summary>
        public void AddPackage(PackageInfo pkg)
        {
            Packages.Add(pkg);
            if (!string.IsNullOrEmpty(pkg.Name))
                _packageByName[pkg.Name] = pkg;
        }

        /// <summary>
        /// Build dependency graphs after all packages are added
        /// </summary>
        public void BuildGraphs()
        {
            DependencyGraph.Clear();
            ReverseDependencies.Clear();
            ScopeGroups.Clear();

            foreach (var pkg in Packages)
            {
                var name = pkg.Name;
                if (string.IsNullOrEmpty(name)) continue;

                // Build forward dependency graph
                var deps = new List<string>();
                if (pkg.Data?.dependencies != null)
                {
                    foreach (var dep in pkg.Data.dependencies.Keys)
                        deps.Add(dep);
                }
                DependencyGraph[name] = deps;

                // Build reverse dependency graph
                foreach (var dep in deps)
                {
                    if (!ReverseDependencies.ContainsKey(dep))
                        ReverseDependencies[dep] = new List<string>();
                    ReverseDependencies[dep].Add(name);
                }

                // Group by scope
                var scope = ExtractScope(name);
                if (!ScopeGroups.ContainsKey(scope))
                    ScopeGroups[scope] = new List<PackageInfo>();
                ScopeGroups[scope].Add(pkg);
            }
        }

        /// <summary>
        /// Extract scope from package name (e.g., com.azathrix.framework -> com.azathrix)
        /// </summary>
        public static string ExtractScope(string packageName)
        {
            if (string.IsNullOrEmpty(packageName)) return "";
            var parts = packageName.Split('.');
            return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : packageName;
        }
    }
}
#endif
