﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ColossalFramework;
using System;

namespace BuildingThemes
{
    public class BuildingThemesManager : Singleton<BuildingThemesManager>
    {

        private readonly Dictionary<uint, HashSet<Configuration.Theme>> _districtsThemes =
            new Dictionary<uint, HashSet<Configuration.Theme>>(128);
        private readonly Dictionary<uint, HashSet<string>> _mergedThemes =
            new Dictionary<uint, HashSet<string>>(128);

        private readonly Configuration configuration;
        private static readonly string filename = "BuildingThemes.xml";

        public BuildingThemesManager()
        {
            if (BuildingThemesMod.isDebug)
            {
                UnityEngine.Debug.LogFormat("Building Themes: Constructing BuildingThemesManager", Thread.CurrentThread.ManagedThreadId);
            }
            
            configuration = Configuration.Deserialize(filename);
            if (configuration == null)
            {
                if (BuildingThemesMod.isDebug)
                {
                    UnityEngine.Debug.LogFormat("Building Themes: No theme config file discovered. Generating default config");
                }
                configuration = new Configuration();
                Configuration.Serialize(filename, configuration);
            }

            Configuration.addBuiltInEuropeanTheme(configuration);
            Configuration.addBuiltInInternationalTheme(configuration);
        }

        public void Reset()
        {
            _districtsThemes.Clear();
            _mergedThemes.Clear();
        }

        public void AddTheme(Configuration.Theme theme, string modName) 
        {
            if (configuration == null || theme == null)
            {
                return;
            }

            var existingTheme = configuration.getTheme(theme.name);

            if (existingTheme != null)
            {
                existingTheme.buildings.AddRange(theme.buildings);
            }
            else
            {
                configuration.themes.Add(theme);
            }

            Configuration.Serialize(filename, configuration);

            UnityEngine.Debug.LogFormat("Building Themes: Theme {0} added by mod {1}", theme.name, modName);
        }

        public void EnableTheme(uint districtIdx, Configuration.Theme theme, bool autoMerge)
        {
            if (BuildingThemesMod.isDebug)
            {
                UnityEngine.Debug.LogFormat("Building Themes: BuildingThemesManager. Enabling theme {0} for district {1}. auto merge: {2}",
                    theme.name, districtIdx, autoMerge);
            }
            HashSet<Configuration.Theme> themes;
            themes = GetDistrictThemes(districtIdx, true);

            if (!themes.Add(theme))
            {
                if (BuildingThemesMod.isDebug)
                {
                    UnityEngine.Debug.LogFormat("Building Themes: BuildingThemesManager. Theme {0} was already enabled for district {1}.",
                        theme.name, districtIdx);
                }
                return;
            }
            SetThemes(districtIdx, themes, autoMerge);
        }

        public void SetThemes(uint districtIdx, HashSet<Configuration.Theme> themes, bool autoMerge)
        {
            _districtsThemes[districtIdx] = themes;
            if (autoMerge)
            {
                MergeDistrictThemes(districtIdx);
            }
        }

        public void DisableTheme(uint districtIdx, string themeName, bool autoMerge)
        {
            if (BuildingThemesMod.isDebug)
            {
                UnityEngine.Debug.LogFormat("Building Themes: BuildingThemesManager. Disabling theme {0} for district {1}. auto merge: {2}",
                themeName, districtIdx, autoMerge);
            }
            var themes = GetDistrictThemes(districtIdx, true);
            if (themes.RemoveWhere(theme => theme.name.Equals(themeName)) <= 0)
            {
                if (BuildingThemesMod.isDebug)
                {
                    UnityEngine.Debug.LogFormat("Building Themes: BuildingThemesManager. Theme {0} was already disabled for district {1}.",
                        themeName, districtIdx);
                }
                return;
            }
            SetThemes(districtIdx, themes, autoMerge);
        }



        public HashSet<string> MergeDistrictThemes(uint districtIdx)
        {
            if (BuildingThemesMod.isDebug)
            {
                UnityEngine.Debug.LogFormat("Building Themes: BuildingThemesManager. Merging themes for district {0}.", districtIdx);
            }
            var themes = GetDistrictThemes(districtIdx, true);
            var mergedTheme = MergeThemes(themes);
            _mergedThemes[districtIdx] = mergedTheme;
            return mergedTheme;

        }

        private static HashSet<string> MergeThemes(HashSet<Configuration.Theme> themes)
        {
            var mergedTheme = new HashSet<string>();
            foreach (var building in themes.SelectMany(theme => theme.buildings))
            {
                if (!building.include)
                {
                    continue;
                }
                if (mergedTheme.Add(building.name))
                {
                    if (BuildingThemesMod.isDebug)
                    {
                        UnityEngine.Debug.LogFormat("Building Themes: BuildingThemesManager. Adding building {0} to merged theme.", building.name);
                    }
                }

            }
            return mergedTheme;
        }


        public bool DoesBuildingBelongToDistrict(string buildingName, uint districtIdx)
        {
            return GetDistrictThemes(districtIdx, true).Count == 0 || GetMergedThemes(districtIdx).Contains(buildingName);
        }

        private HashSet<string> GetMergedThemes(uint districtIdx)
        {
            HashSet<string> theme;
            _mergedThemes.TryGetValue(districtIdx, out theme);
            return theme ?? (_mergedThemes[districtIdx] = MergeDistrictThemes(districtIdx));
        }

        private HashSet<Configuration.Theme> getDefaultDistrictThemes(uint districtIdx) 
        {
            var theme = new HashSet<Configuration.Theme>();
            
            if (districtIdx == 0)
            {
                // city-wide default derived from environment (european, sunny, boreal, tropical)

                var env = Singleton<SimulationManager>.instance.m_metaData.m_environment;

                if (env == "Europe")
                {
                    theme.Add(GetThemeNyName("European"));
                }
                else
                {
                    theme.Add(GetThemeNyName("International"));
                }
            }
            else
            { 
                // district theme derived from city-wide theme

                theme.UnionWith(GetDistrictThemes(0, true));
            }

            return theme;
        }


        public HashSet<Configuration.Theme> GetDistrictThemes(uint districtIdx, bool initializeIfNull)
        {
            HashSet<Configuration.Theme> themes;
            _districtsThemes.TryGetValue(districtIdx, out themes);
            return themes ?? (initializeIfNull ? _districtsThemes[districtIdx] = getDefaultDistrictThemes(districtIdx) : null);
        }

        public List<Configuration.Theme> GetAllThemes()
        {
            return configuration.themes;
        }

        public Configuration.Theme GetThemeNyName(string themeName)
        {
            var themes = configuration.themes.Where(theme => theme.name == themeName).ToList();
            return themes.Count == 0 ? null : themes[0];
        }
    }
}