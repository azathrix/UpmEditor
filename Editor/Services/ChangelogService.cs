#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Azathrix.UpmEditor.Editor.Core;
using UnityEngine;

namespace Azathrix.UpmEditor.Editor.Services
{
    /// <summary>
    /// Service for parsing and writing CHANGELOG.md files
    /// </summary>
    public static class ChangelogService
    {
        private static readonly Regex VersionHeaderRegex = new Regex(@"^##\s*\[?(\d+\.\d+\.\d+(?:-[\w.]+)?)\]?\s*(?:-\s*)?(\d{4}-\d{2}-\d{2})?", RegexOptions.Compiled);
        private static readonly Regex CategoryHeaderRegex = new Regex(@"^###\s*(\w+)", RegexOptions.Compiled);
        private static readonly Regex EntryRegex = new Regex(@"^[-*]\s*(.+)", RegexOptions.Compiled);

        /// <summary>
        /// Parse CHANGELOG.md file
        /// </summary>
        public static ChangelogData ParseChangelog(string filePath)
        {
            var data = new ChangelogData();

            if (!File.Exists(filePath))
                return data;

            try
            {
                var lines = File.ReadAllLines(filePath);
                ChangelogVersion currentVersion = null;
                ChangelogCategory currentCategory = ChangelogCategory.Added;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;

                    // Check for title
                    if (trimmed.StartsWith("# ") && data.Versions.Count == 0)
                    {
                        data.Title = trimmed.Substring(2).Trim();
                        continue;
                    }

                    // Check for version header
                    var versionMatch = VersionHeaderRegex.Match(trimmed);
                    if (versionMatch.Success)
                    {
                        currentVersion = new ChangelogVersion
                        {
                            Version = versionMatch.Groups[1].Value,
                            Date = versionMatch.Groups[2].Success ? versionMatch.Groups[2].Value : ""
                        };
                        data.Versions.Add(currentVersion);
                        currentCategory = ChangelogCategory.Added;
                        continue;
                    }

                    // Check for category header
                    var categoryMatch = CategoryHeaderRegex.Match(trimmed);
                    if (categoryMatch.Success)
                    {
                        currentCategory = ParseCategory(categoryMatch.Groups[1].Value);
                        continue;
                    }

                    // Check for entry
                    var entryMatch = EntryRegex.Match(trimmed);
                    if (entryMatch.Success && currentVersion != null)
                    {
                        currentVersion.Entries.Add(new ChangelogEntry(currentCategory, entryMatch.Groups[1].Value));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse changelog: {e.Message}");
            }

            return data;
        }

        /// <summary>
        /// Write CHANGELOG.md file
        /// </summary>
        public static bool WriteChangelog(string filePath, ChangelogData data)
        {
            try
            {
                var sb = new StringBuilder();

                // Title
                sb.AppendLine($"# {data.Title}");
                sb.AppendLine();

                if (!string.IsNullOrEmpty(data.Description))
                {
                    sb.AppendLine(data.Description);
                    sb.AppendLine();
                }

                // Versions
                foreach (var version in data.Versions)
                {
                    // Version header
                    if (!string.IsNullOrEmpty(version.Date))
                        sb.AppendLine($"## [{version.Version}] - {version.Date}");
                    else
                        sb.AppendLine($"## [{version.Version}]");
                    sb.AppendLine();

                    // Group entries by category
                    var entriesByCategory = new Dictionary<ChangelogCategory, List<string>>();
                    foreach (var entry in version.Entries)
                    {
                        if (!entriesByCategory.ContainsKey(entry.Category))
                            entriesByCategory[entry.Category] = new List<string>();
                        entriesByCategory[entry.Category].Add(entry.Description);
                    }

                    // Write categories in standard order
                    var categoryOrder = new[] { ChangelogCategory.Added, ChangelogCategory.Changed, ChangelogCategory.Deprecated, ChangelogCategory.Removed, ChangelogCategory.Fixed, ChangelogCategory.Security };
                    foreach (var category in categoryOrder)
                    {
                        if (!entriesByCategory.ContainsKey(category))
                            continue;

                        sb.AppendLine($"### {category}");
                        foreach (var desc in entriesByCategory[category])
                        {
                            sb.AppendLine($"- {desc}");
                        }
                        sb.AppendLine();
                    }
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write changelog: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save changelog for a package
        /// </summary>
        public static bool SaveChangelog(PackageInfo pkg)
        {
            if (pkg?.Changelog == null) return false;

            var fullPath = Path.GetFullPath(pkg.Path);
            var changelogPath = Path.Combine(fullPath, "CHANGELOG.md");
            return WriteChangelog(changelogPath, pkg.Changelog);
        }

        private static ChangelogCategory ParseCategory(string text)
        {
            text = text.ToLowerInvariant();
            return text switch
            {
                "added" => ChangelogCategory.Added,
                "changed" => ChangelogCategory.Changed,
                "deprecated" => ChangelogCategory.Deprecated,
                "removed" => ChangelogCategory.Removed,
                "fixed" => ChangelogCategory.Fixed,
                "security" => ChangelogCategory.Security,
                _ => ChangelogCategory.Added
            };
        }

        /// <summary>
        /// Get category display name (Chinese)
        /// </summary>
        public static string GetCategoryDisplayName(ChangelogCategory category)
        {
            return category switch
            {
                ChangelogCategory.Added => "新增",
                ChangelogCategory.Changed => "变更",
                ChangelogCategory.Deprecated => "弃用",
                ChangelogCategory.Removed => "移除",
                ChangelogCategory.Fixed => "修复",
                ChangelogCategory.Security => "安全",
                _ => category.ToString()
            };
        }
    }
}
#endif
