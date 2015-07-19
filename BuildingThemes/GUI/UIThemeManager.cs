﻿using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BuildingThemes.GUI
{
    public class UIThemeManager : UIPanel
    {
        private UITitleBar m_title;
        private UIBuildingFilter m_filter;
        private UIFastList m_themeSelection;
        private UIButton m_themeAdd;
        private UIButton m_themeRemove;
        private UIFastList m_buildingSelection;
        private UIButton m_includeAll;
        private UIButton m_includeNone;
        private UITextureSprite m_preview;
        private UISprite m_noPreview;
        private PreviewRenderer m_previewRenderer;
        private BuildingInfo m_renderPrefab;

        private FastList<List<BuildingItem>> m_themes = new FastList<List<BuildingItem>>();
        Configuration.Theme[] m_allThemes;

        #region Constant values
        private const float LEFT_WIDTH = 250;
        private const float MIDDLE_WIDTH = 395;
        private const float RIGHT_WIDTH = 250;
        private const float HEIGHT = 550;
        private const float SPACING = 5;
        private const float TITLE_HEIGHT = 40;
        #endregion

        private static GameObject _gameObject;

        public static void Initialize()
        {
            try
            {
                // Creating our own gameObect, helps finding the UI in ModTools
                _gameObject = new GameObject("BuildingThemes");
                _gameObject.transform.parent = UIView.GetAView().transform;
                _gameObject.AddComponent<GUI.UIThemeManager>();
            }
            catch (Exception e)
            {
                // Catching any exception to not block the loading process of other mods
                Debugger.Log("Building Themes: An error has happened during the UI creation.");
                Debugger.LogException(e);
            }
        }

        public static void Destroy()
        {
            try
            {
                if (_gameObject != null)
                    GameObject.Destroy(_gameObject);
            }
            catch (Exception e)
            {
                // Catching any exception to not block the unloading process of other mods
                Debugger.Log("Building Themes: An error has happened during the UI destruction.");
                Debugger.LogException(e);
            }
        }

        public override void Start()
        {
            base.Start();

            backgroundSprite = "UnlockingPanel2";
            isVisible = true;
            canFocus = true;
            isInteractive = true;
            width = SPACING + LEFT_WIDTH + SPACING + MIDDLE_WIDTH + SPACING + RIGHT_WIDTH + SPACING;
            height = TITLE_HEIGHT + HEIGHT + SPACING;
            relativePosition = new Vector3(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

            InitBuildingLists();
            SetupControls();
        }

        private void SetupControls()
        {
            // Title Bar
            m_title = AddUIComponent<UITitleBar>();
            m_title.title = "Building Themes Manager";
            m_title.iconSprite = "ToolbarIconZoomOutCity";

            // Filter
            m_filter = AddUIComponent<UIBuildingFilter>();
            m_filter.width = width - LEFT_WIDTH - SPACING * 3;
            m_filter.height = 75;
            m_filter.relativePosition = new Vector3(LEFT_WIDTH + SPACING * 2, TITLE_HEIGHT);

            m_filter.eventFilteringChanged += (c, i) =>
            {
                if (m_themeSelection != null && m_themeSelection.selectedIndex != -1)
                {
                    m_buildingSelection.rowsData = Filter(m_themes[m_themeSelection.selectedIndex]);
                }
            };

            // Panels
            UIPanel left = AddUIComponent<UIPanel>();
            left.width = LEFT_WIDTH;
            left.height = HEIGHT;
            left.relativePosition = new Vector3(SPACING, TITLE_HEIGHT);

            UIPanel middle = AddUIComponent<UIPanel>();
            middle.width = MIDDLE_WIDTH;
            middle.height = HEIGHT - m_filter.height + SPACING;
            middle.relativePosition = new Vector3(LEFT_WIDTH + SPACING * 2, TITLE_HEIGHT + m_filter.height);

            UIPanel right = AddUIComponent<UIPanel>();
            right.width = RIGHT_WIDTH;
            right.height = HEIGHT - SPACING - m_filter.height;
            right.relativePosition = new Vector3(LEFT_WIDTH + MIDDLE_WIDTH + SPACING * 3, TITLE_HEIGHT + m_filter.height);

            // Theme selection
            m_themeSelection = UIFastList.Create<UIThemeItem>(left);

            m_themeSelection.backgroundSprite = "UnlockingPanel";
            m_themeSelection.width = left.width;
            m_themeSelection.height = left.height - SPACING - 30;
            m_themeSelection.canSelect = true;
            m_themeSelection.rowHeight = 40;
            m_themeSelection.relativePosition = Vector3.zero;

            m_themeSelection.rowsData.m_buffer = m_allThemes;
            m_themeSelection.rowsData.m_size = m_allThemes.Length;
            m_themeSelection.DisplayAt(0);

            m_themeSelection.eventSelectedIndexChanged += (c, i) =>
            {
                m_buildingSelection.selectedIndex = -1;
                m_buildingSelection.rowsData = Filter(m_themes[i]);
                m_buildingSelection.DisplayAt(0);
            };

            m_themeAdd = UIUtils.CreateButton(left);
            m_themeAdd.width = (LEFT_WIDTH - SPACING) / 2;
            m_themeAdd.text = "New Theme";
            m_themeAdd.relativePosition = new Vector3(0, HEIGHT- m_themeAdd.height);

            m_themeRemove = UIUtils.CreateButton(left);
            m_themeRemove.width = (LEFT_WIDTH - SPACING) / 2;
            m_themeRemove.text = "Delete Theme";
            m_themeRemove.isEnabled = false;
            m_themeRemove.relativePosition = new Vector3(LEFT_WIDTH - m_themeRemove.width, HEIGHT - m_themeRemove.height);

            // Building selection
            m_buildingSelection = UIFastList.Create<UIBuildingItem>(middle);

            m_buildingSelection.backgroundSprite = "UnlockingPanel";
            m_buildingSelection.width = middle.width;
            m_buildingSelection.height = middle.height - 40;
            m_buildingSelection.canSelect = true;
            m_buildingSelection.rowHeight = 40;
            m_buildingSelection.relativePosition = Vector3.zero;

            m_buildingSelection.rowsData = new FastList<object>();

            BuildingItem selectedItem = null;
            m_buildingSelection.eventSelectedIndexChanged += (c, i) =>
            {
                selectedItem = m_buildingSelection.selectedItem as BuildingItem;
            };

            m_buildingSelection.eventMouseLeave += (c, p) =>
            {
                if (selectedItem != null)
                    UpdatePreview(selectedItem.prefab);
                else
                    UpdatePreview(null);
            };

            m_includeNone = UIUtils.CreateButton(middle);
            m_includeNone.width = 55;
            m_includeNone.text = "None";
            m_includeNone.relativePosition = new Vector3(MIDDLE_WIDTH - m_includeNone.width, m_buildingSelection.height + SPACING);

            m_includeAll = UIUtils.CreateButton(middle);
            m_includeAll.width = 55;
            m_includeAll.text = "All";
            m_includeAll.relativePosition = new Vector3(m_includeNone.relativePosition.x - m_includeAll.width - SPACING, m_buildingSelection.height + SPACING);

            UILabel include = middle.AddUIComponent<UILabel>();
            include.width = 100;
            include.padding = new RectOffset(0, 0, 8, 0);
            include.text = "Include:";
            include.relativePosition = new Vector3(m_includeAll.relativePosition.x - include.width - SPACING, m_buildingSelection.height + SPACING);

            // Preview
            UIPanel previewPanel = right.AddUIComponent<UIPanel>();
            previewPanel.backgroundSprite = "GenericPanel";
            previewPanel.width = right.width;
            previewPanel.height = previewPanel.width;
            previewPanel.relativePosition = Vector3.zero;

            m_preview = previewPanel.AddUIComponent<UITextureSprite>();
            m_preview.size = previewPanel.size;
            m_preview.relativePosition = Vector3.zero;

            m_noPreview = previewPanel.AddUIComponent<UISprite>();
            m_noPreview.spriteName = "Niet";
            m_noPreview.relativePosition = new Vector3((previewPanel.width - m_noPreview.spriteInfo.width) / 2, (previewPanel.height - m_noPreview.spriteInfo.height) / 2);

            m_previewRenderer = gameObject.AddComponent<PreviewRenderer>();
            m_previewRenderer.size = m_preview.size * 2; // Twice the size for anti-aliasing

            previewPanel.eventMouseDown += (c, p) =>
            {
                eventMouseMove += RotateCamera;
            };

            previewPanel.eventMouseUp += (c, p) =>
            {
                eventMouseMove -= RotateCamera;
            };

            previewPanel.eventMouseWheel += (c, p) =>
            {
                m_previewRenderer.zoom -= Mathf.Sign(p.wheelDelta) * 0.25f;
                RenderPreview();
            };
        }

        public void UpdatePreview(BuildingInfo prefab)
        {
            m_renderPrefab = prefab;

            if (m_renderPrefab != null && m_renderPrefab.m_mesh != null)
            {
                m_previewRenderer.cameraRotation = 210f;
                m_previewRenderer.zoom = 4f;
                m_previewRenderer.mesh = m_renderPrefab.m_mesh;
                m_previewRenderer.material = m_renderPrefab.m_material;

                RenderPreview();

                m_preview.texture = m_previewRenderer.texture;

                m_noPreview.isVisible = false;
            }
            else
            {
                m_preview.texture = null;
                m_noPreview.isVisible = true;
            }
        }

        private void RenderPreview()
        {
            if (m_renderPrefab == null) return;

            if (m_renderPrefab.m_useColorVariations)
            {
                Color materialColor = m_renderPrefab.m_material.color;
                m_renderPrefab.m_material.color = m_renderPrefab.m_color0;
                m_previewRenderer.Render();
                m_renderPrefab.m_material.color = materialColor;
            }
            else
            {
                m_previewRenderer.Render();
            }
        }

        private void InitBuildingLists()
        {
            m_allThemes = Singleton<BuildingThemesManager>.instance.GetAllThemes().ToArray();
            Array.Sort(m_allThemes, ThemeCompare);

            for(int i = 0; i< m_allThemes.Length; i++)
            {
                if (m_allThemes[i] != null)
                    m_themes.Add(GetBuildingItemList(m_allThemes[i]));
            }
        }

        private List<BuildingItem> GetBuildingItemList(Configuration.Theme theme)
        {
            // List of all growables prefabs
            Dictionary<string, BuildingItem> buildingDictionary = new Dictionary<string, BuildingItem>();
            for (uint i = 0; i < PrefabCollection<BuildingInfo>.PrefabCount(); i++)
            {
                BuildingInfo prefab = PrefabCollection<BuildingInfo>.GetPrefab(i);
                if (prefab != null && prefab.m_placementStyle == ItemClass.Placement.Automatic)
                {
                    BuildingItem item = new BuildingItem();
                    item.prefab = PrefabCollection<BuildingInfo>.GetPrefab(i);
                    buildingDictionary.Add(item.name, item);
                }
            }

            // Combine growables with buildings in configuration
            List<BuildingItem> list = buildingDictionary.Values.ToList<BuildingItem>();

            Configuration.Building[] buildings = theme.buildings.ToArray();
            for (int i = 0; i < buildings.Length; i++)
            {
                if (buildingDictionary.ContainsKey(buildings[i].name))
                {
                    // Associate building with prefab
                    BuildingItem item = buildingDictionary[buildings[i].name];
                    item.building = buildings[i];
                }
                else
                {
                    // Prefab not found, adding building without prefab
                    BuildingItem item = new BuildingItem();
                    item.building = buildings[i];
                    list.Add(item);
                }
            }

            list.Sort(BuildingCompare);
            return list;
        }

        private void RotateCamera(UIComponent c, UIMouseEventParameter p)
        {
            m_previewRenderer.cameraRotation -= p.moveDelta.x / m_preview.width * 360f;
            RenderPreview();
        }

        #region Filtering/Sorting
        private FastList<object> Filter(List<BuildingItem> list)
        {
            bool shouldFilter = m_filter.buildingLevel != ItemClass.Level.None || m_filter.buildingSize != Vector2.zero || !m_filter.buildingName.IsNullOrWhiteSpace() || !m_filter.IsAllZoneSelected();
            
            if (shouldFilter)
            {
                List<BuildingItem> filtered = new List<BuildingItem>();
                for (int i = 0; i < list.Count; i++)
                {
                    BuildingItem item = (BuildingItem)list[i];
                    bool prefabExists = item.prefab != null;

                    // Level
                    if (m_filter.buildingLevel != ItemClass.Level.None && !(prefabExists && item.prefab.m_class.m_level == m_filter.buildingLevel)) continue;

                    // size
                    Vector2 buildingSize = m_filter.buildingSize;
                    if (m_filter.buildingSize != Vector2.zero && !(prefabExists && item.prefab.m_cellWidth == buildingSize.x && item.prefab.m_cellLength == buildingSize.y)) continue;

                    // zone
                    bool inZone = false;
                    if (prefabExists)
                    {
                        ItemClass itemClass = item.prefab.m_class;
                        if (m_filter.IsZoneSelected(UIBuildingFilter.Zone.ResidentialLow) && itemClass.m_subService == ItemClass.SubService.ResidentialLow) inZone = true;
                        if (m_filter.IsZoneSelected(UIBuildingFilter.Zone.ResidentialHigh) && itemClass.m_subService == ItemClass.SubService.ResidentialHigh) inZone = true;
                        if (m_filter.IsZoneSelected(UIBuildingFilter.Zone.CommercialLow) && itemClass.m_subService == ItemClass.SubService.CommercialLow) inZone = true;
                        if (m_filter.IsZoneSelected(UIBuildingFilter.Zone.CommercialHigh) && itemClass.m_subService == ItemClass.SubService.CommercialHigh) inZone = true;
                        if (m_filter.IsZoneSelected(UIBuildingFilter.Zone.Industrial) && itemClass.m_subService == ItemClass.SubService.IndustrialGeneric) inZone = true;
                        if (m_filter.IsZoneSelected(UIBuildingFilter.Zone.Farming) && itemClass.m_subService == ItemClass.SubService.IndustrialFarming) inZone = true;
                        if (m_filter.IsZoneSelected(UIBuildingFilter.Zone.Forestry) && itemClass.m_subService == ItemClass.SubService.IndustrialForestry) inZone = true;
                        if (m_filter.IsZoneSelected(UIBuildingFilter.Zone.Oil) && itemClass.m_subService == ItemClass.SubService.IndustrialOil) inZone = true;
                        if (m_filter.IsZoneSelected(UIBuildingFilter.Zone.Ore) && itemClass.m_subService == ItemClass.SubService.IndustrialOre) inZone = true;
                        if (m_filter.IsZoneSelected(UIBuildingFilter.Zone.Office) && itemClass.m_service == ItemClass.Service.Office) inZone = true;
                    }

                    if (!inZone && !m_filter.IsAllZoneSelected()) continue;

                    // Name
                    if (!m_filter.buildingName.IsNullOrWhiteSpace() && !item.name.ToLower().Contains(m_filter.buildingName.ToLower())) continue;

                    filtered.Add(item);
                }

                list = filtered;
            }

            FastList<object> fastList = new FastList<object>();
            fastList.m_buffer = list.ToArray();
            fastList.m_size = list.Count;

            return fastList;
        }

        private static int ThemeCompare(Configuration.Theme a, Configuration.Theme b)
        {
            // Sort by name
            return a.name.CompareTo(b.name);
        }

        private static int BuildingCompare(BuildingItem a, BuildingItem b)
        {
            // Sort by displayName > level > size > name
            int compare = a.displayName.CompareTo(b.displayName);
            if (compare == 0) compare = a.level.CompareTo(b.level);
            if (compare == 0) compare = a.size.CompareTo(b.size);
            if (compare == 0) compare = a.name.CompareTo(b.name);

            return compare;
        }
        #endregion
    }
}
