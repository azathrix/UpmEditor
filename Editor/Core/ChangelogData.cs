#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Azathrix.UpmEditor.Editor.Core
{
    /// <summary>
    /// Changelog category types (Keep a Changelog format)
    /// </summary>
    public enum ChangelogCategory
    {
        Added,
        Changed,
        Fixed,
        Removed,
        Deprecated,
        Security
    }

    /// <summary>
    /// Single changelog entry
    /// </summary>
    [Serializable]
    public class ChangelogEntry
    {
        public ChangelogCategory Category;
        public string Description = "";

        public ChangelogEntry() { }

        public ChangelogEntry(ChangelogCategory category, string description)
        {
            Category = category;
            Description = description;
        }
    }

    /// <summary>
    /// Changelog version section
    /// </summary>
    [Serializable]
    public class ChangelogVersion
    {
        public string Version = "";
        public string Date = "";
        public List<ChangelogEntry> Entries = new List<ChangelogEntry>();

        public ChangelogVersion() { }

        public ChangelogVersion(string version, string date)
        {
            Version = version;
            Date = date;
        }
    }

    /// <summary>
    /// Complete changelog data
    /// </summary>
    [Serializable]
    public class ChangelogData
    {
        public string Title = "Changelog";
        public string Description = "";
        public List<ChangelogVersion> Versions = new List<ChangelogVersion>();

        /// <summary>
        /// Add a new version at the top
        /// </summary>
        public ChangelogVersion AddVersion(string version, string date = null)
        {
            var v = new ChangelogVersion(version, date ?? DateTime.Now.ToString("yyyy-MM-dd"));
            Versions.Insert(0, v);
            return v;
        }

        /// <summary>
        /// Get version by version string
        /// </summary>
        public ChangelogVersion GetVersion(string version)
        {
            return Versions.Find(v => v.Version == version);
        }
    }
}
#endif
