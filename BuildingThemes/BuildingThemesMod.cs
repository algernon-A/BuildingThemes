﻿using ICities;
using System;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Plugins;
using System.Text;
using UnityEngine;
using System.Reflection;
using System.Resources;
using System.Threading;
using ColossalFramework.Math;

namespace BuildingThemes
{


    public class LevelUpExtension : LevelUpExtensionBase
    {

        public override ResidentialLevelUp OnCalculateResidentialLevelUp(ResidentialLevelUp levelUp,
            int averageEducation, int landValue, ushort buildingID, Service service, SubService subService,
            Level currentLevel)
        {
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            Building building = buildingManager.m_buildings.m_buffer[buildingID];
            if (BuildingThemesMod.isDebug)
            {
                UnityEngine.Debug.LogFormat("Building Themes: OnCalculateResidentialLevelUp. buildingID: {0}, target level: {1}, position: {2}. current thread: {3}", buildingID, levelUp.targetLevel, building.m_position, Thread.CurrentThread.ManagedThreadId);
            }
            DetoursHolder.position = building.m_position;
            return levelUp;
        }

        public override OfficeLevelUp OnCalculateOfficeLevelUp(OfficeLevelUp levelUp, int averageEducation,
            int serviceScore, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            Building building = buildingManager.m_buildings.m_buffer[buildingID];
            if (BuildingThemesMod.isDebug)
            {

                UnityEngine.Debug.LogFormat(
                    "Building Themes: OnCalculateOfficeLevelUp. buildingID: {0}, target level: {1}, position: {2}. current thread: {3}",
                    buildingID, levelUp.targetLevel, building.m_position, Thread.CurrentThread.ManagedThreadId);
            }
            DetoursHolder.position = building.m_position;
            return levelUp;
        }

        public override CommercialLevelUp OnCalculateCommercialLevelUp(CommercialLevelUp levelUp, int averageWealth,
            int landValue, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
           BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            Building building = buildingManager.m_buildings.m_buffer[buildingID];
            if (BuildingThemesMod.isDebug)
            {

                UnityEngine.Debug.LogFormat(
                    "Building Themes: OnCalculateCommercialLevelUp. buildingID: {0}, target level: {1}, position: {2}. current thread: {3}",
                    buildingID, levelUp.targetLevel, building.m_position, Thread.CurrentThread.ManagedThreadId);
            }
            DetoursHolder.position = building.m_position;
            return levelUp;
        }

        public override IndustrialLevelUp OnCalculateIndustrialLevelUp(IndustrialLevelUp levelUp, int averageEducation,
            int serviceScore, ushort buildingID, Service service, SubService subService, Level currentLevel)
        {
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            Building building = buildingManager.m_buildings.m_buffer[buildingID];
            if (BuildingThemesMod.isDebug)
            {

                UnityEngine.Debug.LogFormat(
                    "Building Themes: OnCalculateIndustrialLevelUp. buildingID: {0}, target level: {1}, position: {2}. current thread: {3}",
                    buildingID, levelUp.targetLevel, building.m_position, Thread.CurrentThread.ManagedThreadId);
            }
            DetoursHolder.position = building.m_position;
            return levelUp;
        }
    }



    public class BuildingThemesMod : LoadingExtensionBase, IUserMod
    {

        public static bool isDebug = false;

        public string Name
        {
            get
            {
                return "Building Themes";
            }
        }

        public string Description
        {
            get { return "Create building themes and apply them to map themes, cities and districts."; }
        }

        private UIButton tab;

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            if (isDebug)
            {
                Debug.Log("Building Themes: Initializing Building Themes Mod...");
            }
            Singleton<BuildingThemesManager>.instance.Reset();
            DetoursHolder.InitTable();
            DetoursHolder.FilteringStrategy = new DefaultFilteringStrategy();//new StubFilteringStrategy();
            //TODO(earalov): save redirected state

            DetoursHolder.getRandomBuildingInfo = typeof(BuildingManager).GetMethod("GetRandomBuildingInfo", BindingFlags.Instance | BindingFlags.Public);
            DetoursHolder.getRandomBuildingInfoState = RedirectionHelper.RedirectCalls(
                typeof(BuildingManager).GetMethod("GetRandomBuildingInfo", BindingFlags.Instance | BindingFlags.Public),
                typeof(DetoursHolder).GetMethod("GetRandomBuildingInfo", BindingFlags.Instance | BindingFlags.Public)
                );

            DetoursHolder.zoneBlockSimulationStep = typeof(ZoneBlock).GetMethod("SimulationStep", BindingFlags.Public | BindingFlags.Instance);
            DetoursHolder.zoneBlockSimulationStepPtr = DetoursHolder.zoneBlockSimulationStep.MethodHandle.GetFunctionPointer();
            DetoursHolder.zoneBlockSimulationStepDetourPtr = typeof(DetoursHolder).GetMethod("ZoneBlockSimulationStep", BindingFlags.Public | BindingFlags.Instance).MethodHandle.GetFunctionPointer();
            DetoursHolder.zoneBlockSimulationStepState = RedirectionHelper.PatchJumpTo(
                DetoursHolder.zoneBlockSimulationStepPtr,
                DetoursHolder.zoneBlockSimulationStepDetourPtr
                );
            DetoursHolder.resourceManagerAddResource = typeof(ImmaterialResourceManager).GetMethod("AddResource", new[] { typeof(ImmaterialResourceManager.Resource), typeof(int), typeof(Vector3), typeof(float) });
            DetoursHolder.resourceManagerAddResourcePtr = DetoursHolder.resourceManagerAddResource.MethodHandle.GetFunctionPointer();
            DetoursHolder.resourceManagerAddResourceDetourPtr = typeof(DetoursHolder).GetMethod("ImmaterialResourceManagerAddResource").MethodHandle.GetFunctionPointer();
            DetoursHolder.resourceManagerAddResourceState = RedirectionHelper.PatchJumpTo(
                DetoursHolder.resourceManagerAddResourcePtr,
                DetoursHolder.resourceManagerAddResourceDetourPtr
                );
            if (isDebug)
            {
                Debug.Log("Building Themes: Building Themes Mod successfully intialized.");
            }
        }

        private Boolean initializedGUI = false;

        public override void OnLevelLoaded(LoadMode mode) 
        {
            // Is it an actual game ?
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame) return;

            initializedGUI = true;


            // TODO load data (serialized for policies, xml for themes)

            // Hook into policies GUI
            ToolsModifierControl.policiesPanel.component.eventVisibilityChanged += OnPoliciesPanelVisibilityChanged;
        }

        public override void OnLevelUnloading()
        {
            if (!initializedGUI) return;

            // Remove the custom policy tab
            RemoveThemesTab();
            Singleton<BuildingThemesManager>.instance.Reset();

            ToolsModifierControl.policiesPanel.component.eventVisibilityChanged -= OnPoliciesPanelVisibilityChanged;
        }

        public override void OnReleased() 
        {
            RedirectionHelper.RevertRedirect(DetoursHolder.getRandomBuildingInfo, DetoursHolder.getRandomBuildingInfoState);
            RedirectionHelper.RevertJumpTo(DetoursHolder.zoneBlockSimulationStepPtr, DetoursHolder.zoneBlockSimulationStepState);
            RedirectionHelper.RevertJumpTo(DetoursHolder.resourceManagerAddResourcePtr, DetoursHolder.resourceManagerAddResourceState);
        }



        private string GetCurrentEnvironment()
        {
            return Singleton<SimulationManager>.instance.m_metaData.m_environment;
        }

        // GUI stuff

        private void OnPoliciesPanelVisibilityChanged(UIComponent component, bool visible)
        {
            // It is necessary to remove the custom tab when the panel is closed 
            // because the game logic is coupled to the GUI
            if (visible)
            {
                AddThemesTab();
            }
            else
            {
                RemoveThemesTab();
            }
        }

        private void AddThemesTab()
        {
            UITabstrip tabstrip = ToolsModifierControl.policiesPanel.Find("Tabstrip") as UITabstrip;
            tab = tabstrip.AddTab("Themes");
            tab.stringUserData = "CityPlanning";

            // recalculate the width of the tabs
            for (int i = 0; i < tabstrip.tabCount; i++)
            {
                tabstrip.tabs[i].width = tabstrip.width / ((float)tabstrip.tabCount - 1);
            }

            // TODO this is hacky. better store it in a field
            GameObject go = GameObject.Find("Tab 5 - Themes");
            if (go == null)
            {
                return;
            }

            // remove the default stuff if something is in there
            foreach (Transform child in go.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            UIPanel container = go.GetComponent<UIPanel>();

            container.autoLayout = true;
            container.autoLayoutDirection = LayoutDirection.Vertical;
            container.autoLayoutPadding.top = 5;
            
            container.isVisible = tabstrip.selectedIndex == 4;

            foreach (Configuration.Theme theme in Singleton<BuildingThemesManager>.instance.GetAllThemes())
            {
                AddThemePolicyButton(container, theme);
            }
        }

        private void RemoveThemesTab()
        {
            // TODO this is hacky. better store it in a field
            GameObject go = GameObject.Find("Tab 5 - Themes");
            if (go == null)
            {
                return;
            }
            GameObject.Destroy(go);

            UITabstrip tabstrip = ToolsModifierControl.policiesPanel.Find("Tabstrip") as UITabstrip;
            tabstrip.RemoveUIComponent(tab);
            GameObject.Destroy(tab.gameObject);
        }

        private void AddThemePolicyButton(UIPanel container, Configuration.Theme theme)
        {

            UIPanel policyPanel = container.AddUIComponent<UIPanel>();
            policyPanel.name = theme.name;
            policyPanel.backgroundSprite = "GenericPanel";
            policyPanel.size = new Vector2(364f, 44f);
            policyPanel.objectUserData = ToolsModifierControl.policiesPanel;
            policyPanel.stringUserData = "None";

            UIButton policyButton = policyPanel.AddUIComponent<UIButton>();
            policyButton.name = "PolicyButton";
            policyButton.text = theme.name;
            policyButton.size = new Vector2(324f, 40f);
            policyButton.focusedBgSprite = "PolicyBarBackActive";
            policyButton.normalBgSprite = "PolicyBarBack";
            policyButton.relativePosition = new Vector3(2f, 2f, 0f);
            policyButton.textPadding.left = 50;
            policyButton.textColor = new Color32(0, 0, 0, 255);
            policyButton.disabledTextColor = new Color32(0, 0, 0, 255);
            policyButton.hoveredTextColor = new Color32(0, 0, 0, 255);
            policyButton.pressedTextColor = new Color32(0, 0, 0, 255);
            policyButton.focusedTextColor = new Color32(0, 0, 0, 255);
            policyButton.disabledColor = new Color32(124, 124, 124, 255);
            policyButton.dropShadowColor = new Color32(103, 103, 103, 255);
            policyButton.dropShadowOffset = new Vector2(1f, 1f);
            policyButton.textHorizontalAlignment = UIHorizontalAlignment.Left;
            policyButton.useDropShadow = false;
            policyButton.textScale = 0.875f;
            policyButton.gameObject.AddComponent<ThemePolicyContainer>();

            UICheckBox policyCheckBox = policyButton.AddUIComponent<UICheckBox>();
            policyCheckBox.name = "Checkbox";
            policyCheckBox.size = new Vector2(363f, 44f);
            policyCheckBox.relativePosition = new Vector3(0f, -2f, 0f);
            policyCheckBox.clipChildren = true;
            policyCheckBox.objectUserData = theme;

            ushort districtId1 = (ushort)ToolsModifierControl.policiesPanel.targetDistrict;

            var districtThemes = Singleton<BuildingThemesManager>.instance.GetDistrictThemes(districtId1, true);
            policyCheckBox.isChecked = districtThemes.Contains(theme);


            policyCheckBox.eventCheckChanged += delegate(UIComponent component, bool enabled)
            {
                lock (component)
                {
                    uint districtId = (uint)ToolsModifierControl.policiesPanel.targetDistrict;
                    if (enabled)
                    {
                        Singleton<BuildingThemesManager>.instance.EnableTheme(districtId, theme, true);
                        if (BuildingThemesMod.isDebug)
                        {
                            Debug.Log("enabled theme " + theme.name + " in district " + districtId);
                        }
                    }
                    else
                    {
                        Singleton<BuildingThemesManager>.instance.DisableTheme(districtId, theme.name, true);
                        if (BuildingThemesMod.isDebug)
                        {
                            Debug.Log("disabled theme " + theme.name + " in district " + districtId);
                        }
                    } 
                }

            };


            UISprite sprite = policyCheckBox.AddUIComponent<UISprite>();
            sprite.name = "Unchecked";
            sprite.spriteName = "ToggleBase";
            sprite.size = new Vector2(16f, 16f);
            sprite.relativePosition = new Vector3(336.6984f, 14, 0f);

            policyCheckBox.checkedBoxObject = sprite.AddUIComponent<UISprite>();
            policyCheckBox.checkedBoxObject.name = "Checked";
            ((UISprite)policyCheckBox.checkedBoxObject).spriteName = "ToggleBaseFocused";
            policyCheckBox.checkedBoxObject.size = new Vector2(16f, 16f);
            policyCheckBox.checkedBoxObject.relativePosition = Vector3.zero;

            // TODO link the checkbox and the focus of the button (like PolicyContainer component does)
        }

    }
}
