#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Azathrix.UpmEditor.Editor.Core;
using UnityEngine;

namespace Azathrix.UpmEditor.Editor.Services
{
    /// <summary>
    /// Service for scanning packages and building dependency graphs
    /// </summary>
    public static class PackageScanService
    {
        /// <summary>
        /// Scan all local packages in Packages folder
        /// </summary>
        public static PackageCache ScanPackages()
        {
            var cache = new PackageCache();
            var packagesPath = Path.GetFullPath(UPMConstants.PackagesPath);

            if (!Directory.Exists(packagesPath))
                return cache;

            foreach (var dir in Directory.GetDirectories(packagesPath))
            {
                var packageJsonPath = Path.Combine(dir, UPMConstants.PackageJsonFileName);
                if (!File.Exists(packageJsonPath))
                    continue;

                // Skip non-local packages (those with @ in path, like com.unity.xxx@version)
                var dirName = Path.GetFileName(dir);
                if (dirName.Contains("@"))
                    continue;

                var data = PackageJsonService.ReadPackageJson(dir);
                if (data == null)
                    continue;

                var relativePath = "Packages/" + dirName;
                var pkg = new PackageInfo(relativePath, data);

                // Load changelog if exists
                var changelogPath = Path.Combine(dir, "CHANGELOG.md");
                if (File.Exists(changelogPath))
                    pkg.Changelog = ChangelogService.ParseChangelog(changelogPath);

                cache.AddPackage(pkg);
            }

            cache.BuildGraphs();
            return cache;
        }

        /// <summary>
        /// Refresh a single package in the cache
        /// </summary>
        public static void RefreshPackage(PackageCache cache, string packageName)
        {
            var pkg = cache.GetPackage(packageName);
            if (pkg == null) return;

            var fullPath = Path.GetFullPath(pkg.Path);
            var data = PackageJsonService.ReadPackageJson(fullPath);
            if (data != null)
            {
                pkg.Data = data;
                pkg.ClearDirty();
            }

            var changelogPath = Path.Combine(fullPath, "CHANGELOG.md");
            if (File.Exists(changelogPath))
                pkg.Changelog = ChangelogService.ParseChangelog(changelogPath);

            cache.BuildGraphs();
        }

        /// <summary>
        /// Save a package's data
        /// </summary>
        public static bool SavePackage(PackageInfo pkg)
        {
            if (pkg?.Data == null) return false;

            var fullPath = Path.GetFullPath(pkg.Path);
            var result = PackageJsonService.WritePackageJson(fullPath, pkg.Data);
            if (result)
                pkg.ClearDirty();
            return result;
        }

        /// <summary>
        /// Save all dirty packages
        /// </summary>
        public static int SaveAllDirty(PackageCache cache)
        {
            var saved = 0;
            foreach (var pkg in cache.GetDirtyPackages())
            {
                if (SavePackage(pkg))
                    saved++;
            }
            return saved;
        }

        /// <summary>
        /// Update dependency version in dependent packages
        /// </summary>
        public static void UpdateDependencyVersion(PackageCache cache, string dependencyName, string newVersion, List<PackageInfo> packagesToUpdate)
        {
            foreach (var pkg in packagesToUpdate)
            {
                if (pkg.Data?.dependencies != null && pkg.Data.dependencies.ContainsKey(dependencyName))
                {
                    pkg.Data.dependencies[dependencyName] = newVersion;
                    pkg.MarkDirty();
                }
            }
        }

        /// <summary>
        /// Get sorted scope list
        /// </summary>
        public static List<string> GetSortedScopes(PackageCache cache)
        {
            var scopes = new List<string>(cache.ScopeGroups.Keys);
            scopes.Sort();
            return scopes;
        }
    }
}
#endif
