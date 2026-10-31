using System;
using System.Collections.Generic;
using Hung.Base.Composition;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Hung.Base.Editor
{
    /// <summary>Rejects duplicate Resources identities and invalid startup profiles before building.</summary>
    public sealed class PluginStartupProfileBuildValidator : IPreprocessBuildWithReport
    {
        /// <inheritdoc/>
        public int callbackOrder => 0;
        /// <inheritdoc/>
        public void OnPreprocessBuild(BuildReport report) => ValidateProject();
        /// <summary>Validates applicable Resources assets without mutating them.</summary>
        public static void ValidateProject()
        {
            var profiles=new List<string>();
            var errors=new List<string>();
            foreach(var guid in AssetDatabase.FindAssets("t:PluginStartupProfile"))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid).Replace('\\','/');
                if(!path.EndsWith("/Resources/"+PluginStartupPolicy.ResourcePath+".asset",StringComparison.OrdinalIgnoreCase)) continue;
                profiles.Add(path);
                var profile=AssetDatabase.LoadAssetAtPath<PluginStartupProfile>(path);
                if(profile==null) { errors.Add("Unreadable plugin profile: "+path); continue; }
                foreach(var error in profile.Validate()) errors.Add(path+": "+error);
            }
            if(profiles.Count>1) errors.Add("Duplicate Resources/HungPluginStartup assets: "+string.Join(", ",profiles));
            if(errors.Count!=0) throw new BuildFailedException(string.Join("\n",errors));
        }
    }
}
