﻿using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Runtime.CompilerServices;
using BuildingThemes.Redirection;
using UnityEngine;

namespace BuildingThemes.Detour
{
    [TargetType(typeof(ZoneBlock))]
    public struct ZoneBlockDetour
    {
        private static int _zoneGridResolution = ZoneManager.ZONEGRID_RESOLUTION;
        private static int _zoneGridHalfResolution = ZoneManager.ZONEGRID_RESOLUTION / 2;
        private static readonly int EIGHTY_ONE_ZONEGRID_RESOLUTION = 270;
        private static readonly int EIGHTY_ONE_HALF_ZONEGRID_RESOLUTION = EIGHTY_ONE_ZONEGRID_RESOLUTION / 2;

        public static void SetUp()
        {
            if (!Util.IsModActive(BuildingThemesMod.EIGHTY_ONE_MOD) && !Util.IsEightyOne2Active())
            {
                return;
            }
            _zoneGridResolution = EIGHTY_ONE_ZONEGRID_RESOLUTION;
            _zoneGridHalfResolution = EIGHTY_ONE_HALF_ZONEGRID_RESOLUTION;
        }

        private static int debugCount = 0;

        [RedirectReverse]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CheckBlock(ref ZoneBlock zoneBlock, ushort blockID, ushort otherID, ref ZoneBlock other, int[] xBuffer, ItemClass.Zone zone, Vector2 startPos, Vector2 xDir,
            Vector2 zDir, Quad2 quad)
        {
            UnityEngine.Debug.LogError($"CheckBlock(): this={zoneBlock}"); //this is just to give the method some body
        }

        [RedirectReverse]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsGoodPlace(ref ZoneBlock zoneBlock, Vector2 position)
        {
            UnityEngine.Debug.LogError($"IsGoodPlace(): this={zoneBlock}"); //this is just to give the method some body
            return false;
        }

        // This method contains the modified plot calculation algorithm
        // Segments which were changed are marked with "begin mod" and "end mod"

        [RedirectMethod]
        public static void SimulationStep(ref ZoneBlock zoneBlock, ushort blockID)
        {
            if (Debugger.Enabled && debugCount < 10)
            {
                debugCount++;
                Debugger.LogFormat("Building Themes: Detoured ZoneBlock.SimulationStep was called. blockID: {0}, position: {1}.", blockID, zoneBlock.m_position);
            }

            ZoneManager zoneManager = Singleton<ZoneManager>.instance;
            int rowCount = zoneBlock.RowCount;
            float m_angle = zoneBlock.m_angle;
            Vector2 xDirection = new Vector2(Mathf.Cos(m_angle), Mathf.Sin(m_angle)) * 8f;
            Vector2 zDirection = new Vector2(xDirection.y, -xDirection.x);
            ulong num = zoneBlock.m_valid & ~(zoneBlock.m_occupied1 | zoneBlock.m_occupied2);
            int spawnpointRow = 0;
            ItemClass.Zone zone = ItemClass.Zone.Unzoned;
            for (int i = 0; i < 4; i++)
            {
                if (zone != 0)
                {
                    break;
                }
                spawnpointRow = Singleton<SimulationManager>.instance.m_randomizer.Int32((uint)rowCount);
                if ((num & (ulong)(1L << (spawnpointRow << 3))) != 0)
                {
                    zone = zoneBlock.GetZone(0, spawnpointRow);
                }
            }
            DistrictManager instance2 = Singleton<DistrictManager>.instance;

            Vector3 m_position = (Vector3)zoneBlock.m_position;

            byte district = instance2.GetDistrict(m_position);
            int num4;
            switch (zone)
            {
                case ItemClass.Zone.ResidentialLow:
                    num4 = zoneManager.m_actualResidentialDemand;
                    num4 += instance2.m_districts.m_buffer[(int)district].CalculateResidentialLowDemandOffset();
                    break;
                case ItemClass.Zone.ResidentialHigh:
                    num4 = zoneManager.m_actualResidentialDemand;
                    num4 += instance2.m_districts.m_buffer[(int)district].CalculateResidentialHighDemandOffset();
                    break;
                case ItemClass.Zone.CommercialLow:
                    num4 = zoneManager.m_actualCommercialDemand;
                    num4 += instance2.m_districts.m_buffer[(int)district].CalculateCommercialLowDemandOffset();
                    break;
                case ItemClass.Zone.CommercialHigh:
                    num4 = zoneManager.m_actualCommercialDemand;
                    num4 += instance2.m_districts.m_buffer[(int)district].CalculateCommercialHighDemandOffset();
                    break;
                case ItemClass.Zone.Industrial:
                    num4 = zoneManager.m_actualWorkplaceDemand;
                    num4 += instance2.m_districts.m_buffer[(int)district].CalculateIndustrialDemandOffset();
                    break;
                case ItemClass.Zone.Office:
                    num4 = zoneManager.m_actualWorkplaceDemand;
                    num4 += instance2.m_districts.m_buffer[(int)district].CalculateOfficeDemandOffset();
                    break;

                //begin mod
                default:
                    return;
                    // end mod
            }
            Vector2 a = VectorUtils.XZ(m_position);
            Vector2 vector3 = a - 3.5f * xDirection + ((float)spawnpointRow - 3.5f) * zDirection;
            int[] tmpXBuffer = zoneManager.m_tmpXBuffer;
            for (int i = 0; i < 13; i++)
            {
                tmpXBuffer[i] = 0;
            }
            Quad2 quad = default(Quad2);
            quad.a = a - 4f * xDirection + ((float)spawnpointRow - 10f) * zDirection;
            quad.b = a + 3f * xDirection + ((float)spawnpointRow - 10f) * zDirection;
            quad.c = a + 3f * xDirection + ((float)spawnpointRow + 2f) * zDirection;
            quad.d = a - 4f * xDirection + ((float)spawnpointRow + 2f) * zDirection;
            Vector2 vector4 = quad.Min();
            Vector2 vector5 = quad.Max();

            //begin mod
            int num5 = Mathf.Max((int)((vector4.x - 46f) / 64f + _zoneGridHalfResolution), 0);
            int num6 = Mathf.Max((int)((vector4.y - 46f) / 64f + _zoneGridHalfResolution), 0);
            int num7 = Mathf.Min((int)((vector5.x + 46f) / 64f + _zoneGridHalfResolution), _zoneGridResolution - 1);
            int num8 = Mathf.Min((int)((vector5.y + 46f) / 64f + _zoneGridHalfResolution), _zoneGridResolution - 1);
            //end mod
            for (int j = num6; j <= num8; j++)
            {
                for (int k = num5; k <= num7; k++)
                {
                    //begin mod
                    ushort num9 = zoneManager.m_zoneGrid[j * _zoneGridResolution + k];
                    //end mod
                    int num10 = 0;
                    while (num9 != 0)
                    {
                        Vector3 positionVar = zoneManager.m_blocks.m_buffer[(int)num9].m_position;
                        float num11 = Mathf.Max(Mathf.Max(vector4.x - 46f - positionVar.x, vector4.y - 46f - positionVar.z),
                            Mathf.Max(positionVar.x - vector5.x - 46f, positionVar.z - vector5.y - 46f));

                        if (num11 < 0f)
                        {
                            CheckBlock(ref zoneBlock, blockID, num9, ref zoneManager.m_blocks.m_buffer[num9], tmpXBuffer, zone, vector3, xDirection, zDirection, quad);
                        }
                        num9 = zoneManager.m_blocks.m_buffer[(int)num9].m_nextGridBlock;
                        if (++num10 >= 49152)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }

            for (int l = 0; l < 13; l++)
            {
                uint num12 = (uint)tmpXBuffer[l];
                int num13 = 0;
                bool newFlag = (num12 & 0x10000) == 65536;
                bool flag = (num12 & 0x20000) == 131072;
                bool flag2 = false;
                while ((num12 & 1u) != 0u)
                {
                    num13++;
                    flag2 = ((num12 & 65536u) != 0u);
                    num12 >>= 1;
                }
                switch (num13)
                {
                    case 5:
                    case 6:
                        num13 = ((!flag2) ? 4 : (num13 - (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) + 2)));
                        num13 |= 0x40000;
                        break;
                    case 7:
                        num13 = 4;
                        num13 |= 0x40000;
                        break;
                }
                if (num13 != 0)
                {
                    if (newFlag)
                    {
                        num13 |= 0x10000;
                    }
                    if (flag)
                    {
                        num13 |= 0x20000;
                    }
                }
                tmpXBuffer[l] = num13;
            }
            int num14 = tmpXBuffer[6] & 65535;
            if (num14 == 0)
            {
                return;
            }

            bool flag3 = IsGoodPlace(ref zoneBlock, vector3);
            if (Singleton<SimulationManager>.instance.m_randomizer.Int32(100u) >= num4)
            {
                if (flag3)
                {
                    zoneManager.m_goodAreaFound[(int)zone] = 1024;
                }
                return;
            }
            if (!flag3 && zoneManager.m_goodAreaFound[(int)zone] > -1024)
            {
                if (zoneManager.m_goodAreaFound[(int)zone] == 0)
                {
                    zoneManager.m_goodAreaFound[(int)zone] = -1;
                }
                return;
            }
            int num15 = 6;
            int num16 = 6;
            bool flag4 = true;
            while (true)
            {
                if (flag4)
                {
                    while (num15 != 0)
                    {
                        if ((tmpXBuffer[num15 - 1] & 65535) != num14)
                        {
                            break;
                        }
                        num15--;
                    }
                    while (num16 != 12)
                    {
                        if ((tmpXBuffer[num16 + 1] & 65535) != num14)
                        {
                            break;
                        }
                        num16++;
                    }
                }
                else
                {
                    while (num15 != 0)
                    {
                        if ((tmpXBuffer[num15 - 1] & 65535) < num14)
                        {
                            break;
                        }
                        num15--;
                    }
                    while (num16 != 12)
                    {
                        if ((tmpXBuffer[num16 + 1] & 65535) < num14)
                        {
                            break;
                        }
                        num16++;
                    }
                }
                int num17 = num15;
                int num18 = num16;
                while (num17 != 0)
                {
                    if ((tmpXBuffer[num17 - 1] & 65535) < 2)
                    {
                        break;
                    }
                    num17--;
                }
                while (num18 != 12)
                {
                    if ((tmpXBuffer[num18 + 1] & 65535) < 2)
                    {
                        break;
                    }
                    num18++;
                }
                bool flag5 = num17 != 0 && num17 == num15 - 1;
                bool flag6 = num18 != 12 && num18 == num16 + 1;
                if (flag5 && flag6)
                {
                    if (num16 - num15 > 2)
                    {
                        num15++;
                        num16--;
                        break;
                    }
                    if (num14 <= 2)
                    {
                        if (!flag4)
                        {
                            break;
                        }
                    }
                    else
                    {
                        num14--;
                    }
                }
                else if (flag5)
                {
                    if (num16 - num15 > 1)
                    {
                        num15++;
                        break;
                    }
                    if (num14 <= 2)
                    {
                        if (!flag4)
                        {
                            break;
                        }
                    }
                    else
                    {
                        num14--;
                    }
                }
                else if (flag6)
                {
                    if (num16 - num15 > 1)
                    {
                        num16--;
                        break;
                    }
                    if (num14 <= 2)
                    {
                        if (!flag4)
                        {
                            break;
                        }
                    }
                    else
                    {
                        num14--;
                    }
                }
                else
                {
                    if (num15 != num16)
                    {
                        break;
                    }
                    if (num14 <= 2)
                    {
                        if (!flag4)
                        {
                            break;
                        }
                    }
                    else
                    {
                        num14--;
                    }
                }
                flag4 = false;
            }
            int num19;
            int num20;
            if (num14 == 1 && num16 - num15 >= 1)
            {
                num15 += Singleton<SimulationManager>.instance.m_randomizer.Int32((uint)(num16 - num15));
                num16 = num15 + 1;
                num19 = num15 + Singleton<SimulationManager>.instance.m_randomizer.Int32(2u);
                num20 = num19;
            }
            else
            {
                do
                {
                    num19 = num15;
                    num20 = num16;
                    if (num16 - num15 == 2)
                    {
                        if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0)
                        {
                            num20--;
                        }
                        else
                        {
                            num19++;
                        }
                    }
                    else if (num16 - num15 == 3)
                    {
                        if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0)
                        {
                            num20 -= 2;
                        }
                        else
                        {
                            num19 += 2;
                        }
                    }
                    else if (num16 - num15 == 4)
                    {
                        if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0)
                        {
                            num16 -= 2;
                            num20 -= 3;
                        }
                        else
                        {
                            num15 += 2;
                            num19 += 3;
                        }
                    }
                    else if (num16 - num15 == 5)
                    {
                        if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0)
                        {
                            num16 -= 3;
                            num20 -= 2;
                        }
                        else
                        {
                            num15 += 3;
                            num19 += 2;
                        }
                    }
                    else if (num16 - num15 >= 6)
                    {
                        if (num15 == 0 || num16 == 12)
                        {
                            if (num15 == 0)
                            {
                                num15 = 3;
                                num19 = 2;
                            }
                            if (num16 == 12)
                            {
                                num16 = 9;
                                num20 = 10;
                            }
                        }
                        else if (Singleton<SimulationManager>.instance.m_randomizer.Int32(2u) == 0)
                        {
                            num16 = num15 + 3;
                            num20 = num19 + 2;
                        }
                        else
                        {
                            num15 = num16 - 3;
                            num19 = num20 - 2;
                        }
                    }
                }
                while (num16 - num15 > 3 || num20 - num19 > 3);
            }
            int depth_A = 4;
            int width_A = num16 - num15 + 1;
            BuildingInfo.ZoningMode zoningMode = BuildingInfo.ZoningMode.Straight;
            bool flag7 = true;
            for (int m = num15; m <= num16; m++)
            {
                depth_A = Mathf.Min(depth_A, tmpXBuffer[m] & 65535);
                if ((tmpXBuffer[m] & 0x40000) == 0)
                {
                    flag7 = false;
                }
            }
            if (num16 >= num15)
            {
                if ((tmpXBuffer[num15] & 65536) != 0)
                {
                    zoningMode = BuildingInfo.ZoningMode.CornerLeft;
                    num20 = num15 + num20 - num19;
                    num19 = num15;
                }
                else if (((uint)tmpXBuffer[num16] & 0x20000u) != 0)
                {
                    zoningMode = BuildingInfo.ZoningMode.CornerRight;
                    num19 = num16 + num19 - num20;
                    num20 = num16;
                }
            }
            int depth_B = 4;
            int width_B = num20 - num19 + 1;
            BuildingInfo.ZoningMode zoningMode2 = BuildingInfo.ZoningMode.Straight;
            bool flag8 = true;
            for (int n = num19; n <= num20; n++)
            {
                depth_B = Mathf.Min(depth_B, tmpXBuffer[n] & 65535);
                if ((tmpXBuffer[n] & 0x40000) == 0)
                {
                    flag8 = false;
                }
            }
            if (num20 >= num19)
            {
                if (((uint)tmpXBuffer[num19] & 0x10000u) != 0)
                {
                    zoningMode2 = BuildingInfo.ZoningMode.CornerLeft;
                }
                if (((uint)tmpXBuffer[num20] & 0x20000u) != 0)
                {
                    zoningMode2 = BuildingInfo.ZoningMode.CornerRight;
                }
            }
            ItemClass.Service service = ItemClass.Service.None;
            ItemClass.SubService subService = ItemClass.SubService.None;
            ItemClass.Level level = ItemClass.Level.Level1;
            switch (zone)
            {
                case ItemClass.Zone.ResidentialLow:
                    service = ItemClass.Service.Residential;
                    subService = ItemClass.SubService.ResidentialLow;
                    break;
                case ItemClass.Zone.ResidentialHigh:
                    service = ItemClass.Service.Residential;
                    subService = ItemClass.SubService.ResidentialHigh;
                    break;
                case ItemClass.Zone.CommercialLow:
                    service = ItemClass.Service.Commercial;
                    subService = ItemClass.SubService.CommercialLow;
                    break;
                case ItemClass.Zone.CommercialHigh:
                    service = ItemClass.Service.Commercial;
                    subService = ItemClass.SubService.CommercialHigh;
                    break;
                case ItemClass.Zone.Industrial:
                    service = ItemClass.Service.Industrial;
                    break;
                case ItemClass.Zone.Office:
                    service = ItemClass.Service.Office;
                    break;
                default:
                    return;
            }
            BuildingInfo buildingInfo = null;
            Vector3 vector6 = Vector3.zero;
            int num25_row = 0;
            int length = 0;
            int width = 0;
            BuildingInfo.ZoningMode zoningMode3 = BuildingInfo.ZoningMode.Straight;
            int num28 = 0;

            // begin mod
            int depth_alt = Mathf.Min(depth_A, 4);
            int width_alt = width_A;
            // end mod

            while (num28 < 8) // while (num28 < 6)
            {
                switch (num28)
                {
                    // Corner cases

                    case 0:
                        if (zoningMode != BuildingInfo.ZoningMode.Straight)
                        {
                            num25_row = num15 + num16 + 1;
                            length = depth_A;
                            width = width_A;
                            zoningMode3 = zoningMode;
                            goto IL_D6A;
                        }
                        break;
                    case 1:
                        if (zoningMode2 != BuildingInfo.ZoningMode.Straight)
                        {
                            num25_row = num19 + num20 + 1;
                            length = depth_B;
                            width = width_B;
                            zoningMode3 = zoningMode2;
                            goto IL_D6A;
                        }
                        break;
                    case 2:
                        if (zoningMode != BuildingInfo.ZoningMode.Straight)
                        {
                            if (depth_A >= 4)
                            {
                                num25_row = num15 + num16 + 1;
                                length = ((!flag7) ? 2 : 3);
                                width = width_A;
                                zoningMode3 = zoningMode;
                                goto IL_D6A;
                            }
                        }
                        break;
                    case 3:
                        if (zoningMode2 != BuildingInfo.ZoningMode.Straight)
                        {
                            if (depth_B >= 4)
                            {
                                num25_row = num19 + num20 + 1;
                                length = ((!flag8) ? 2 : 3);
                                width = width_B;
                                zoningMode3 = zoningMode2;
                                goto IL_D6A;
                            }
                        }
                        break;
                    // begin mod
                    case 4:
                        if (zoningMode != BuildingInfo.ZoningMode.Straight)
                        {
                            if (width_alt > 1)
                            {
                                width_alt--;
                            }
                            else if (depth_alt > 1)
                            {
                                depth_alt--;
                                width_alt = width_A;
                            }
                            else
                            {
                                break;
                            }

                            if (width_alt == width_A)
                            {
                                num25_row = num15 + num16 + 1;

                            }
                            else
                            {
                                if (zoningMode == BuildingInfo.ZoningMode.CornerLeft)
                                {
                                    num25_row = num15 + num16 + 1 - (width_A - width_alt);
                                }
                                else
                                {
                                    num25_row = num15 + num16 + 1 + (width_A - width_alt);
                                }
                            }

                            length = depth_alt;
                            width = width_alt;

                            zoningMode3 = zoningMode;

                            num28--;
                            goto IL_D6A;
                        }
                        break;
                    // end mod
                    // Straight cases
                    case 5:
                        num25_row = num15 + num16 + 1;
                        length = depth_A;
                        width = width_A;
                        zoningMode3 = BuildingInfo.ZoningMode.Straight;
                        goto IL_D6A;
                    case 6:
                        // begin mod

                        // reset variables
                        depth_alt = Mathf.Min(depth_A, 4);
                        width_alt = width_A;

                        // end mod

                        //int width_B = num20 - num19 + 1;
                        num25_row = num19 + num20 + 1;
                        length = depth_B;
                        width = width_B;
                        zoningMode3 = BuildingInfo.ZoningMode.Straight;
                        goto IL_D6A;
                    // begin mod
                    case 7:

                        if (width_alt > 1)
                        {
                            width_alt--;
                        }
                        else
                        {
                            break;
                        }

                        if (width_alt == width_A)
                        {
                            num25_row = num15 + num16 + 1;
                        }
                        else if (width_A % 2 != width_alt % 2)
                        {
                            num25_row = num15 + num16;
                        }
                        else
                        {
                            num25_row = num15 + num16 + 1;
                        }

                        length = depth_alt;
                        width = width_alt;

                        zoningMode3 = BuildingInfo.ZoningMode.Straight;

                        num28--;
                        goto IL_D6A;
                    // end mod
                    default:
                        goto IL_D6A;
                }
            IL_DF0:
                num28++;
                continue;
            IL_D6A:
                vector6 = m_position + VectorUtils.X_Y(((float)length * 0.5f - 4f) * xDirection + ((float)num25_row * 0.5f + (float)spawnpointRow - 10f) * zDirection);
                if (zone == ItemClass.Zone.Industrial)
                {
                    ZoneBlock.GetIndustryType(vector6, out subService, out level);
                }
                else if (zone == ItemClass.Zone.CommercialLow || zone == ItemClass.Zone.CommercialHigh)
                {
                    ZoneBlock.GetCommercialType(vector6, zone, width, length, out subService, out level);
                }
                else if (zone == ItemClass.Zone.ResidentialLow || zone == ItemClass.Zone.ResidentialHigh)
                {
                    ZoneBlock.GetResidentialType(vector6, zone, width, length, out subService, out level);
                }
                else if (zone == ItemClass.Zone.Office)
                {
                    ZoneBlock.GetOfficeType(vector6, zone, width, length, out subService, out level);
                }

                byte district2 = instance2.GetDistrict(vector6);
                ushort style = instance2.m_districts.m_buffer[(int)district2].m_Style;
                if (Singleton<BuildingManager>.instance.m_BuildingWrapper != null)
                {
                    Singleton<BuildingManager>.instance.m_BuildingWrapper.OnCalculateSpawn(vector6, ref service, ref subService, ref level, ref style);
                }

                // begin mod

                // Here we are calling a custom getRandomBuildingInfo method

                buildingInfo = RandomBuildings.GetRandomBuildingInfo_Spawn(vector6, ref Singleton<SimulationManager>.instance.m_randomizer, service, subService, level, width, length, zoningMode3, style);

                // end mod

                if (buildingInfo != null)
                {
                    // begin mod

                    // If the depth of the found prefab is smaller than the one we were looking for, recalculate the size
                    // This is done by checking the position of every prop
                    // Plots only get shrinked when no assets are placed on the extra space

                    // This is needed for themes which only contain small buildings (e.g. 1x2) 
                    // because those buildings would occupy more space than needed!

                    if (buildingInfo.GetWidth() == width && buildingInfo.GetLength() != length)
                    {
                        // Calculate the z position of the furthest away prop
                        float biggestPropPosZ = 0;

                        if (buildingInfo.m_props != null)
                        {
                            foreach (var prop in buildingInfo.m_props)
                            {
                                if (prop == null) continue;

                                biggestPropPosZ = Mathf.Max(biggestPropPosZ, buildingInfo.m_expandFrontYard ? prop.m_position.z : -prop.m_position.z);
                            }
                        }

                        // Check if the furthest away prop is outside of the bounds of the prefab
                        float occupiedExtraSpace = biggestPropPosZ - buildingInfo.GetLength() * 4;
                        if (occupiedExtraSpace <= 0)
                        {
                            // No? Then shrink the plot to the prefab length so no space is wasted!
                            length = buildingInfo.GetLength();
                        }
                        else
                        {
                            // Yes? Shrink the plot so all props are in the bounds
                            int newLength = buildingInfo.GetLength() + Mathf.CeilToInt(occupiedExtraSpace / 8);
                            length = Mathf.Min(length, newLength);
                        }

                        vector6 = m_position + VectorUtils.X_Y(((float)length * 0.5f - 4f) * xDirection + ((float)num25_row * 0.5f + (float)spawnpointRow - 10f) * zDirection);
                    }

                    // This block handles Corner buildings. We always shrink them
                    else if (buildingInfo.GetLength() == width && buildingInfo.GetWidth() != length)
                    {
                        length = buildingInfo.GetWidth();
                        vector6 = m_position + VectorUtils.X_Y(((float)length * 0.5f - 4f) * xDirection + ((float)num25_row * 0.5f + (float)spawnpointRow - 10f) * zDirection);
                    }

                    // end mod

                    if (Debugger.Enabled)
                    {
                        Debugger.LogFormat("Found prefab: {5} - {0}, {1}, {2}, {3} x {4}", service, subService, level, width, length, buildingInfo.name);
                    }
                    break;
                }
                if (Debugger.Enabled)
                {

                }
                goto IL_DF0;
            }
            if (buildingInfo == null)
            {
                if (Debugger.Enabled)
                {
                    Debugger.LogFormat("No prefab found: {0}, {1}, {2}, {3} x {4}", service, subService, level, width, length);
                }
                return;
            }
            float num29 = Singleton<TerrainManager>.instance.WaterLevel(VectorUtils.XZ(vector6));
            if (num29 > vector6.y || Singleton<DisasterManager>.instance.IsEvacuating(vector6))
            {
                return;
            }
            float num30 = m_angle + (float)Math.PI / 2f;
            if (zoningMode3 == BuildingInfo.ZoningMode.CornerLeft && buildingInfo.m_zoningMode == BuildingInfo.ZoningMode.CornerRight)
            {
                num30 -= (float)Math.PI / 2f;
                length = width;
            }
            else if (zoningMode3 == BuildingInfo.ZoningMode.CornerRight && buildingInfo.m_zoningMode == BuildingInfo.ZoningMode.CornerLeft)
            {
                num30 += (float)Math.PI / 2f;
                length = width;
            }
            if (Singleton<BuildingManager>.instance.CreateBuilding(out var building, ref Singleton<SimulationManager>.instance.m_randomizer, buildingInfo, vector6, num30, length, Singleton<SimulationManager>.instance.m_currentBuildIndex))
            {
                Singleton<SimulationManager>.instance.m_currentBuildIndex++;
                switch (service)
                {
                    case ItemClass.Service.Residential:
                        zoneManager.m_actualResidentialDemand = Mathf.Max(0, zoneManager.m_actualResidentialDemand - 5);
                        break;
                    case ItemClass.Service.Commercial:
                        zoneManager.m_actualCommercialDemand = Mathf.Max(0, zoneManager.m_actualCommercialDemand - 5);
                        break;
                    case ItemClass.Service.Industrial:
                        zoneManager.m_actualWorkplaceDemand = Mathf.Max(0, zoneManager.m_actualWorkplaceDemand - 5);
                        break;
                    case ItemClass.Service.Office:
                        zoneManager.m_actualWorkplaceDemand = Mathf.Max(0, zoneManager.m_actualWorkplaceDemand - 5);
                        break;
                }
                if (zone == ItemClass.Zone.ResidentialHigh || zone == ItemClass.Zone.CommercialHigh)
                {
                    Singleton<BuildingManager>.instance.m_buildings.m_buffer[building].m_flags |= Building.Flags.HighDensity;
                }
            }
            zoneManager.m_goodAreaFound[(int)zone] = 1024;
        }
    }
}
