#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace  Azathrix.UpmEditor.Editor.Core
{
    /// <summary>
    /// Default visibility for package in Package Manager
    /// </summary>
    public enum DefaultVisibility
    {
        Visible,
        Hidden
    }

    /// <summary>
    /// Data model for package.json
    /// </summary>
    [Serializable]
    public class UPMPackageData
    {
        public string name = "";
        public string displayName = "";
        public string version = UPMConstants.DefaultVersion;
        public string unity = UPMConstants.DefaultUnityVersion;
        public string description = "";
        public string license = "";
        public string documentationUrl = "";
        public string changelogUrl = "";
        public string licensesUrl = "";
        public List<string> keywords = new List<string>();
        public AuthorInfo author = new AuthorInfo();
        public Dictionary<string, string> dependencies = new Dictionary<string, string>();
        public bool hideInEditor = false; // true = Hidden, false = Visible
        public List<SampleInfo> samples = new List<SampleInfo>();

        [Serializable]
        public class AuthorInfo
        {
            public string name = "";
            public string email = "";
            public string url = "";
        }

        [Serializable]
        public class SampleInfo
        {
            public string displayName = "";
            public string description = "";
            public string path = "";
        }

        /// <summary>
        /// Create a copy of this package data
        /// </summary>
        public UPMPackageData Clone()
        {
            var clone = new UPMPackageData
            {
                name = name,
                displayName = displayName,
                version = version,
                unity = unity,
                description = description,
                license = license,
                documentationUrl = documentationUrl,
                changelogUrl = changelogUrl,
                licensesUrl = licensesUrl,
                keywords = new List<string>(keywords),
                author = new AuthorInfo
                {
                    name = author.name,
                    email = author.email,
                    url = author.url
                },
                dependencies = new Dictionary<string, string>(dependencies),
                hideInEditor = hideInEditor,
                samples = samples.ConvertAll(s => new SampleInfo
                {
                    displayName = s.displayName,
                    description = s.description,
                    path = s.path
                })
            };
            return clone;
        }
    }

    /// <summary>
    /// Template options for package creation
    /// </summary>
    [Serializable]
    public class PackageTemplateOptions
    {
        public bool createRuntime = true;
        public bool createEditor = true;
        public bool createReadme = true;
        public bool createChangelog = false;
        public bool createLicense = false;
        public bool createTests = false;
        public bool createDocumentation = false;
    }
}
#endif
