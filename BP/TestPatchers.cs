using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DSPCalculator.BP
{
    public class TestPatchers
    {
        // 制造台密铺的 long distance = 0.02356195第一纬  0.02513266赤道 一个格子是四分之一上面这个数
        // 制造台密铺的 lat distance = 0.02513281赤道

        // 各种制造台、工厂的slot位置换算成蓝图中的xyz，等同于这些工厂的prefabDesc的slotPoses[i]的位置的xyz乘0.8！！！！！！！！！！！

        public static bool enabled = false;

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(BlueprintUtils), "GenerateBlueprintData")]
        public static bool TestLog(BlueprintData _blueprintData, PlanetData _planet, PlanetAuxData _auxData, int[] _objIds, int _objCount, float _divideLongitude)
        {
            return true;
            _blueprintData.ResetContentAsEmpty();
            if (_planet == null)
            {
                return false;
            }
            PlanetFactory factory = _planet.factory;
            if (factory == null || !_planet.factoryLoaded)
            {
                return false;
            }
            if (_objCount <= 0 || _objIds == null)
            {
                return false;
            }
            EntityData[] entityPool = factory.entityPool;
            PrebuildData[] prebuildPool = factory.prebuildPool;
            FactorySystem factorySystem = _planet.factory.factorySystem;
            int segmentCnt = 200;
            if (_planet.aux != null && _planet.aux.activeGrid != null)
            {
                segmentCnt = _planet.aux.activeGrid.segment;
            }
            BPGratBox boundingRange = BlueprintUtils.GetBoundingRange(_planet, _auxData, _objIds, _objCount, _divideLongitude);
            int areaCount = BlueprintUtils.GetAreaCount(boundingRange.y, boundingRange.w, segmentCnt);
            Debug.Log($"area count = {areaCount}");
            BPGratBox[] array = new BPGratBox[areaCount];
            BPGratBox[] array2 = new BPGratBox[areaCount];
            BlueprintUtils.SplitGratBoxInTropicAreas(boundingRange, array, array2, segmentCnt);
            if (BlueprintUtils._tmp_building_dict == null)
            {
                BlueprintUtils._tmp_building_dict = new Dictionary<int, BlueprintBuilding>();
            }
            BlueprintUtils._tmp_building_dict.Clear();
            _blueprintData.areas = new BlueprintArea[areaCount];
            _blueprintData.buildings = new BlueprintBuilding[_objCount];
            for (int i = 0; i < _objCount; i++)
            {
                _blueprintData.buildings[i] = new BlueprintBuilding();
                _blueprintData.buildings[i].index = i;
                _blueprintData.buildings[i].areaIndex = -1;
                BlueprintUtils._tmp_building_dict[_objIds[i]] = _blueprintData.buildings[i];
            }
            int num = 0;
            int num2 = 0;
            float latitudeRadPerGrid = BlueprintUtils.GetLatitudeRadPerGrid(segmentCnt);
            BuildingParameters buildingParameters = default(BuildingParameters);
            for (int j = 0; j < areaCount; j++)
            {
                BPGratBox bpgratBox = array2[j];
                int longitudeSegmentCount = BlueprintUtils.GetLongitudeSegmentCount(BlueprintUtils.GetDir(array[j].x, array[j].y), segmentCnt);
                float longitudeRadPerGrid = BlueprintUtils.GetLongitudeRadPerGrid(longitudeSegmentCount, segmentCnt);
                if (longitudeSegmentCount > num2)
                {
                    num2 = longitudeSegmentCount;
                    num = j;
                }
                Debug.Log($"longSegCount = {longitudeSegmentCount}");
                float lastLat = 0;
                float lastLon = 0;
                for (int k = 0; k < _objCount; k++)
                {
                    if (_blueprintData.buildings[k].areaIndex < 0)
                    {
                        int num3 = _objIds[k];
                        bool flag = num3 < 0;
                        Vector3 vector = flag ? prebuildPool[-num3].pos : entityPool[num3].pos;
                        Vector3 vector2 = vector.normalized;
                        float latitudeRad = BlueprintUtils.GetLatitudeRad(vector2);
                        if (bpgratBox.y - 1E-05f <= latitudeRad && latitudeRad <= bpgratBox.w + 1E-05f)
                        {
                            float longitudeRad = BlueprintUtils.GetLongitudeRad(vector2);
                            Vector3 vector3 = Vector3.zero;
                            Quaternion rhs = Quaternion.identity;
                            Quaternion rhs2 = Quaternion.identity;
                            int num4 = 0;
                            short protoId;
                            short modelIndex;
                            float tilt;
                            if (flag)
                            {
                                vector3 = prebuildPool[-num3].pos2;
                                rhs = prebuildPool[-num3].rot;
                                rhs2 = prebuildPool[-num3].rot2;
                                protoId = prebuildPool[-num3].protoId;
                                modelIndex = prebuildPool[-num3].modelIndex;
                                num4 = prebuildPool[-num3].colliderId;
                                tilt = prebuildPool[-num3].tilt;
                            }
                            else
                            {
                                vector3 = entityPool[num3].pos;
                                rhs = entityPool[num3].rot;
                                rhs2 = entityPool[num3].rot;
                                protoId = entityPool[num3].protoId;
                                modelIndex = entityPool[num3].modelIndex;
                                int inserterId = entityPool[num3].inserterId;
                                if (inserterId != 0)
                                {
                                    vector3 = factorySystem.inserterPool[inserterId].pos2;
                                    rhs2 = factorySystem.inserterPool[inserterId].rot2;
                                }
                                tilt = entityPool[num3].tilt;
                            }
                            Vector3 vector4 = vector3.normalized;
                            float latitudeRad2 = BlueprintUtils.GetLatitudeRad(vector4);
                            float longitudeRad2 = BlueprintUtils.GetLongitudeRad(vector4);
                            Debug.Log($"lat distance = {latitudeRad2 - lastLat}, lon distance = {longitudeRad2 - lastLon}");
                            Debug.Log($"pos1={vector}, pos2 = {vector3}, tilt = {tilt}");

                            lastLat = latitudeRad2;
                            lastLon = longitudeRad2;
                            float localOffset_z = (vector.magnitude - _planet.realRadius - 0.2f) / 1.3333333f;
                            float localOffset_z2 = (vector3.magnitude - _planet.realRadius - 0.2f) / 1.3333333f;
                            bool flag2 = BlueprintUtils.IsPolePoint(vector2, 5E-07f);
                            bool flag3 = BlueprintUtils.IsPolePoint(vector4, 5E-07f);
                            vector2 = (flag2 ? ((vector2.y > 0f) ? Vector3.up : Vector3.down) : vector2);
                            vector4 = (flag3 ? ((vector2.y > 0f) ? Vector3.up : Vector3.down) : vector4);
                            _blueprintData.buildings[k].areaIndex = j;
                            _blueprintData.buildings[k].inputObj = null;
                            _blueprintData.buildings[k].outputObj = null;
                            _blueprintData.buildings[k].localOffset_x = (longitudeRad - array[j].x) / longitudeRadPerGrid;
                            _blueprintData.buildings[k].localOffset_y = (latitudeRad - array[j].y) / latitudeRadPerGrid;
                            _blueprintData.buildings[k].localOffset_z = localOffset_z;
                            _blueprintData.buildings[k].localOffset_x2 = (longitudeRad2 - array[j].x) / longitudeRadPerGrid;
                            _blueprintData.buildings[k].localOffset_y2 = (latitudeRad2 - array[j].y) / latitudeRadPerGrid;
                            _blueprintData.buildings[k].localOffset_z2 = localOffset_z2;
                            if (_blueprintData.buildings[k].localOffset_x < -0.5001f)
                            {
                                _blueprintData.buildings[k].localOffset_x += (float)(longitudeSegmentCount * 5);
                            }
                            if (_blueprintData.buildings[k].localOffset_x2 < -0.5001f)
                            {
                                _blueprintData.buildings[k].localOffset_x2 += (float)(longitudeSegmentCount * 5);
                            }
                            if (_blueprintData.buildings[k].localOffset_x > -0.5001f + (float)(longitudeSegmentCount * 5))
                            {
                                _blueprintData.buildings[k].localOffset_x -= (float)(longitudeSegmentCount * 5);
                            }
                            if (_blueprintData.buildings[k].localOffset_x2 > -0.5001f + (float)(longitudeSegmentCount * 5))
                            {
                                _blueprintData.buildings[k].localOffset_x2 -= (float)(longitudeSegmentCount * 5);
                            }
                            if (flag2)
                            {
                                _blueprintData.buildings[k].localOffset_x = 0f;
                            }
                            if (flag3)
                            {
                                _blueprintData.buildings[k].localOffset_x2 = 0f;
                            }
                            Quaternion quaternion = Maths.SphericalRotation(vector2, 0f);
                            Quaternion quaternion2 = Maths.SphericalRotation(vector4, 0f);
                            quaternion.w = -quaternion.w;
                            quaternion2.w = -quaternion2.w;
                            _blueprintData.buildings[k].yaw = (quaternion * rhs).eulerAngles.y;
                            _blueprintData.buildings[k].yaw2 = (quaternion2 * rhs2).eulerAngles.y;
                            _blueprintData.buildings[k].tilt = tilt;
                            _blueprintData.buildings[k].itemId = protoId;
                            _blueprintData.buildings[k].modelIndex = modelIndex;
                            ModelProto modelProto = LDB.models.Select((int)modelIndex);
                            if (modelProto.prefabDesc.isInserter)
                            {
                                _blueprintData.buildings[k].inputToSlot = 1;
                                _blueprintData.buildings[k].outputFromSlot = 0;
                                _blueprintData.buildings[k].pitch = (quaternion * rhs).eulerAngles.x;
                                _blueprintData.buildings[k].pitch2 = (quaternion2 * rhs2).eulerAngles.x;
                                _blueprintData.buildings[k].tilt = (quaternion * rhs).eulerAngles.z;
                                _blueprintData.buildings[k].tilt2 = (quaternion2 * rhs2).eulerAngles.z;
                                bool flag4;
                                int num5;
                                int num6;
                                factory.ReadObjectConn(num3, 1, out flag4, out num5, out num6);
                                if (num5 != 0 && BlueprintUtils._tmp_building_dict.ContainsKey(num5))
                                {
                                    _blueprintData.buildings[k].inputObj = BlueprintUtils._tmp_building_dict[num5];
                                    ModelProto modelProto2 = (num5 > 0) ? LDB.models.Select((int)factory.GetEntityData(num5).modelIndex) : LDB.models.Select((int)factory.GetPrebuildData(-num5).modelIndex);
                                    _blueprintData.buildings[k].inputFromSlot = (modelProto2.prefabDesc.isBelt ? -1 : num6);
                                    _blueprintData.buildings[k].inputOffset = (int)(flag ? prebuildPool[-num3].pickOffset : factory.factorySystem.inserterPool[entityPool[num3].inserterId].pickOffset);
                                }
                                factory.ReadObjectConn(num3, 0, out flag4, out num5, out num6);
                                if (num5 != 0 && BlueprintUtils._tmp_building_dict.ContainsKey(num5))
                                {
                                    _blueprintData.buildings[k].outputObj = BlueprintUtils._tmp_building_dict[num5];
                                    ModelProto modelProto3 = (num5 > 0) ? LDB.models.Select((int)factory.GetEntityData(num5).modelIndex) : LDB.models.Select((int)factory.GetPrebuildData(-num5).modelIndex);
                                    _blueprintData.buildings[k].outputToSlot = (modelProto3.prefabDesc.isBelt ? -1 : num6);
                                    _blueprintData.buildings[k].outputOffset = (int)(flag ? prebuildPool[-num3].insertOffset : factory.factorySystem.inserterPool[entityPool[num3].inserterId].insertOffset);
                                }
                            }
                            else if (modelProto.prefabDesc.isBelt)
                            {
                                if (flag)
                                {
                                    factory.PostRefreshPrebuildDisplay(-num3, false);
                                    int num7 = num4 >> 20;
                                    num4 &= 1048575;
                                    _blueprintData.buildings[k].yaw = (quaternion * _planet.physics.colChunks[num7].colliderPool[num4].q).eulerAngles.y;
                                    _blueprintData.buildings[k].yaw2 = _blueprintData.buildings[k].yaw;
                                    _blueprintData.buildings[k].tilt = tilt;
                                }
                                _blueprintData.buildings[k].inputToSlot = 1;
                                _blueprintData.buildings[k].outputFromSlot = 0;
                                bool flag5;
                                int num8;
                                int num9;
                                factory.ReadObjectConn(num3, 1, out flag5, out num8, out num9);
                                if (num8 != 0 && BlueprintUtils._tmp_building_dict.ContainsKey(num8) && !((num8 > 0) ? LDB.models.Select((int)factory.GetEntityData(num8).modelIndex) : LDB.models.Select((int)factory.GetPrebuildData(-num8).modelIndex)).prefabDesc.isBelt)
                                {
                                    _blueprintData.buildings[k].inputObj = BlueprintUtils._tmp_building_dict[num8];
                                    _blueprintData.buildings[k].inputFromSlot = num9;
                                }
                                factory.ReadObjectConn(num3, 0, out flag5, out num8, out num9);
                                if (num8 != 0 && BlueprintUtils._tmp_building_dict.ContainsKey(num8))
                                {
                                    _blueprintData.buildings[k].outputObj = BlueprintUtils._tmp_building_dict[num8];
                                    _blueprintData.buildings[k].outputToSlot = num9;
                                }
                            }
                            else if (modelProto.prefabDesc.multiLevel)
                            {
                                _blueprintData.buildings[k].inputToSlot = 14;
                                _blueprintData.buildings[k].outputFromSlot = 15;
                                _blueprintData.buildings[k].inputFromSlot = 15;
                                _blueprintData.buildings[k].outputToSlot = 14;
                                bool flag6;
                                int num10;
                                int inputFromSlot;
                                factory.ReadObjectConn(num3, 14, out flag6, out num10, out inputFromSlot);
                                if (num10 != 0 && BlueprintUtils._tmp_building_dict.ContainsKey(num10))
                                {
                                    _blueprintData.buildings[k].inputObj = BlueprintUtils._tmp_building_dict[num10];
                                    _blueprintData.buildings[k].inputFromSlot = inputFromSlot;
                                }
                            }
                            else if (modelProto.prefabDesc.addonType == EAddonType.Storage)
                            {
                                _blueprintData.buildings[k].inputToSlot = 0;
                                bool flag7;
                                int num11;
                                int inputFromSlot2;
                                factory.ReadObjectConn(num3, 0, out flag7, out num11, out inputFromSlot2);
                                if (num11 != 0 && BlueprintUtils._tmp_building_dict.ContainsKey(num11))
                                {
                                    _blueprintData.buildings[k].inputObj = BlueprintUtils._tmp_building_dict[num11];
                                    _blueprintData.buildings[k].inputFromSlot = inputFromSlot2;
                                }
                            }
                            int num12 = 0;
                            _blueprintData.buildings[k].parameters = null;
                            buildingParameters.CopyFromFactoryObject(num3, factory, true, false);
                            buildingParameters.ToParamsArray(ref _blueprintData.buildings[k].parameters, ref num12);
                            _blueprintData.buildings[k].recipeId = buildingParameters.recipeId;
                            _blueprintData.buildings[k].filterId = buildingParameters.filterId;
                            if (_blueprintData.buildings[k].parameters == null)
                            {
                                _blueprintData.buildings[k].parameters = new int[0];
                            }
                            _blueprintData.buildings[k].content = buildingParameters.content;
                        }
                    }
                }
            }
            Vector3 vector5 = BlueprintUtils.GetDir(array[num].x, array[num].y);
            int parentIndex = num;
            for (int l = num - 1; l >= 0; l--)
            {
                Vector3 dir = BlueprintUtils.GetDir(array[l].x, array[l].y);
                int longitudeSegmentCount2 = BlueprintUtils.GetLongitudeSegmentCount(dir, segmentCnt);
                float longitudeRadPerGrid2 = BlueprintUtils.GetLongitudeRadPerGrid(longitudeSegmentCount2, segmentCnt);
                _blueprintData.areas[l] = new BlueprintArea();
                _blueprintData.areas[l].index = l;
                _blueprintData.areas[l].parentIndex = parentIndex;
                _blueprintData.areas[l].areaSegments = longitudeSegmentCount2;
                IntVector2 tropicAnchorOffset = BlueprintUtils.GetTropicAnchorOffset(dir, vector5, array[l].x, true, segmentCnt);
                _blueprintData.areas[l].tropicAnchor = tropicAnchorOffset.y;
                _blueprintData.areas[l].anchorLocalOffsetX = tropicAnchorOffset.x;
                float num13 = array[l].y - BlueprintUtils.GetLatitudeRad(vector5);
                _blueprintData.areas[l].anchorLocalOffsetY = BlueprintUtils._round2int(num13 / latitudeRadPerGrid);
                float num14 = array[l].width / longitudeRadPerGrid2;
                float num15 = array[l].height / latitudeRadPerGrid;
                _blueprintData.areas[l].width = (int)(num14 + 1.5f);
                _blueprintData.areas[l].height = (int)(num15 + 1.5f);
                parentIndex = l;
                vector5 = dir;
            }
            vector5 = BlueprintUtils.GetDir(array[num].x, array[num].y);
            parentIndex = num;
            for (int m = num + 1; m < areaCount; m++)
            {
                Vector3 dir2 = BlueprintUtils.GetDir(array[m].x, array[m].y);
                int longitudeSegmentCount3 = BlueprintUtils.GetLongitudeSegmentCount(dir2, segmentCnt);
                float longitudeRadPerGrid3 = BlueprintUtils.GetLongitudeRadPerGrid(longitudeSegmentCount3, segmentCnt);
                _blueprintData.areas[m] = new BlueprintArea();
                _blueprintData.areas[m].index = m;
                _blueprintData.areas[m].parentIndex = parentIndex;
                _blueprintData.areas[m].areaSegments = longitudeSegmentCount3;
                IntVector2 tropicAnchorOffset2 = BlueprintUtils.GetTropicAnchorOffset(dir2, vector5, array[m].x, true, segmentCnt);
                _blueprintData.areas[m].tropicAnchor = tropicAnchorOffset2.y;
                _blueprintData.areas[m].anchorLocalOffsetX = tropicAnchorOffset2.x;
                float num16 = array[m].y - BlueprintUtils.GetLatitudeRad(vector5);
                _blueprintData.areas[m].anchorLocalOffsetY = BlueprintUtils._round2int(num16 / latitudeRadPerGrid);
                float num17 = array[m].width / longitudeRadPerGrid3;
                float num18 = array[m].height / latitudeRadPerGrid;
                _blueprintData.areas[m].width = (int)(num17 + 1.5f);
                _blueprintData.areas[m].height = (int)(num18 + 1.5f);
                parentIndex = m;
                vector5 = dir2;
            }
            int longitudeSegmentCount4 = BlueprintUtils.GetLongitudeSegmentCount(BlueprintUtils.GetDir(array[num].x, array[num].y), segmentCnt);
            float longitudeRadPerGrid4 = BlueprintUtils.GetLongitudeRadPerGrid(longitudeSegmentCount4, segmentCnt);
            _blueprintData.areas[num] = new BlueprintArea();
            _blueprintData.areas[num].index = num;
            _blueprintData.areas[num].parentIndex = -1;
            _blueprintData.areas[num].areaSegments = longitudeSegmentCount4;
            _blueprintData.areas[num].tropicAnchor = 0;
            _blueprintData.areas[num].anchorLocalOffsetX = 0;
            _blueprintData.areas[num].anchorLocalOffsetY = 0;
            float num19 = array[num].width / longitudeRadPerGrid4;
            float num20 = array[num].height / latitudeRadPerGrid;
            _blueprintData.areas[num].width = (int)(num19 + 1.5f);
            _blueprintData.areas[num].height = (int)(num20 + 1.5f);
            int num21 = 0;
            for (int n = 0; n < _blueprintData.areas.Length; n++)
            {
                num21 += _blueprintData.areas[n].height;
            }
            _blueprintData.dragBoxSize_x = _blueprintData.areas[num].width;
            _blueprintData.dragBoxSize_y = num21;
            if (areaCount == 1)
            {
                _blueprintData.cursorOffset_x = BlueprintUtils._round2int((float)_blueprintData.areas[num].width * 0.5f - 0.01f);
                _blueprintData.cursorOffset_y = BlueprintUtils._round2int((float)_blueprintData.areas[num].height * 0.5f - 0.01f);
            }
            else
            {
                float num22 = (boundingRange.w - boundingRange.y) * 0.5f + boundingRange.y;
                _blueprintData.cursorOffset_x = BlueprintUtils._round2int((float)_blueprintData.areas[num].width * 0.5f - 0.01f);
                _blueprintData.cursorOffset_y = BlueprintUtils._round2int((num22 - array[num].y) / latitudeRadPerGrid);
            }
            _blueprintData.cursorTargetArea = num;
            _blueprintData.primaryAreaIdx = num;
            BlueprintUtils._tmp_building_dict.Clear();
            return false;
        }



        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(BlueprintData), "Export")]
        public static bool BPExportTest(ref BlueprintData __instance)
        {
            Debug.Log($"\n\n\n------------------BP DATA------------------\n");
            //foreach (FieldInfo fieldInfo in __instance.GetType().GetFields())
            //{
            //    bool flag = fieldInfo.IsLiteral || fieldInfo.IsStatic;
            //    if(!flag)
            //        Debug.Log($"{fieldInfo.Name} = {Traverse.Create(__instance).Field(fieldInfo.Name).GetValue()};");
            //}
            //foreach (PropertyInfo propertyInfo in __instance.GetType().GetProperties())
            //{
            //    bool flag = propertyInfo.CanRead;
            //    if (flag)
            //        Debug.Log($"{propertyInfo.Name} = {Traverse.Create(__instance).Property(propertyInfo.Name).GetValue()};");
            //}
            //Debug.Log($"area len = {__instance.areas.Length}");


            return true;
        }
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(BlueprintArea), "Export")]
        //public static bool BPAreaExportTest(ref BlueprintArea __instance)
        //{
        //    Debug.Log($"\n------------------Area DATA------------------\n");
        //    foreach (FieldInfo fieldInfo in __instance.GetType().GetFields())
        //    {
        //        bool flag = fieldInfo.IsLiteral || fieldInfo.IsStatic;
        //        if (!flag)
        //            Debug.Log($"{fieldInfo.Name} = {Traverse.Create(__instance).Field(fieldInfo.Name).GetValue()};");
        //    }
        //    return true;
        //}
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BlueprintBuilding), "Export")]
        public static bool BPBuildingExportTest(ref BlueprintBuilding __instance)
        {
            Debug.Log($"\n---Building {__instance.index}---");
            if (!(__instance.itemId >= 2001 && __instance.itemId <= 2004) || true)
            {
                foreach (FieldInfo fieldInfo in __instance.GetType().GetFields())
                {
                    bool flag = fieldInfo.IsLiteral || fieldInfo.IsStatic;
                    if (!flag)
                        Debug.Log($"{fieldInfo.Name} = {Traverse.Create(__instance).Field(fieldInfo.Name).GetValue()};");
                }
                if (__instance.parameters != null)
                {
                    Debug.Log($"param len = {__instance.parameters.Length}");
                    for (int i = 0; i < __instance.parameters.Length; i++)
                    {
                        if (__instance.parameters[i] != 0)
                            Debug.Log($"param[{i}] = {__instance.parameters[i]}");
                    }
                }
            }
            return true;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(BlueprintUtils), "InitBuildPreviewByBPData")]
        //public static bool InitBuildPreviewByBPDataPrePatch(BlueprintData _blueprintData, ref BuildPreview[] _bpArray, int _dotsCursor, ref int __result)
        //{
        //    int num = _blueprintData.buildings.Length;
        //    int num2 = (_blueprintData.areas.Length > 1) ? num : (num * _dotsCursor);
        //    if (_bpArray == null || _bpArray.Length < num2)
        //    {
        //        BuildPreview[] array = _bpArray;
        //        _bpArray = new BuildPreview[num2];
        //    }
        //    for (int i = 0; i < num2; i++)
        //    {
        //        if (_bpArray[i] == null)
        //        {
        //            _bpArray[i] = new BuildPreview();
        //        }
        //        BuildPreview buildPreview = _bpArray[i];
        //        buildPreview.ResetAll();
        //        buildPreview.previewIndex = -1;
        //        buildPreview.bpgpuiModelId = i + 1;
        //        buildPreview.bpgpuiModelInstIndex = -1;
        //        int num3 = i / num * num;
        //        int num4 = i % num;
        //        BlueprintBuilding blueprintBuilding = _blueprintData.buildings[num4];
        //        Utils.logger.LogInfo($"building param len {blueprintBuilding.parameters.Length}, and id/model{blueprintBuilding.itemId},{blueprintBuilding.modelIndex}");
        //        buildPreview.item = LDB.items.Select((int)blueprintBuilding.itemId);
        //        buildPreview.desc = LDB.models.Select((int)blueprintBuilding.modelIndex).prefabDesc;
        //        buildPreview.isConnNode = buildPreview.desc.isBelt;
        //        if (blueprintBuilding.outputObj != null && _bpArray[blueprintBuilding.outputObj.index + num3] == null)
        //        {
        //            _bpArray[blueprintBuilding.outputObj.index + num3] = new BuildPreview();
        //        }
        //        if (blueprintBuilding.inputObj != null && _bpArray[blueprintBuilding.inputObj.index + num3] == null)
        //        {
        //            _bpArray[blueprintBuilding.inputObj.index + num3] = new BuildPreview();
        //        }
        //        buildPreview.output = ((blueprintBuilding.outputObj == null) ? null : _bpArray[blueprintBuilding.outputObj.index + num3]);
        //        buildPreview.input = ((blueprintBuilding.inputObj == null) ? null : _bpArray[blueprintBuilding.inputObj.index + num3]);
        //        buildPreview.outputFromSlot = blueprintBuilding.outputFromSlot;
        //        buildPreview.inputToSlot = blueprintBuilding.inputToSlot;
        //        buildPreview.outputToSlot = blueprintBuilding.outputToSlot;
        //        buildPreview.inputFromSlot = blueprintBuilding.inputFromSlot;
        //        buildPreview.inputOffset = blueprintBuilding.inputOffset;
        //        buildPreview.outputOffset = blueprintBuilding.outputOffset;
        //        buildPreview.recipeId = blueprintBuilding.recipeId;
        //        buildPreview.filterId = blueprintBuilding.filterId;
        //        if (buildPreview.desc.lodCount > 0 && buildPreview.desc.lodMeshes != null && buildPreview.desc.lodMeshes[0] != null)
        //        {
        //            buildPreview.needModel = true;
        //        }
        //        else
        //        {
        //            buildPreview.needModel = false;
        //        }
        //        buildPreview.SetParameters(blueprintBuilding.parameters.Length, blueprintBuilding.parameters);
        //        buildPreview.content = blueprintBuilding.content;
        //        buildPreview.coverbp = null;
        //        buildPreview.condition = EBuildCondition.Ok;
        //    }
        //    __result = num2;
        //    return false;
        //}


        public static void TestCreateBuilding()
        {

        }

        public static void TestCreateExample()
        {
            //Dictionary<int, Dictionary<int, int>> gridMap = new Dictionary<int, Dictionary<int, int>>();

            //BlueprintData bp = BpBuilder.CreateEmpty();
            //List<BlueprintBuilding> buildings = new List<BlueprintBuilding>();
            ////buildings.AddBelts(ref gridMap, 2002, 0, 0, 0, 2, 0, 0);
            ////buildings.AddAssembler(ref gridMap, 2303, 51, 1, 2, 0);
            ////buildings.AddSorter(2011, 1, 0, 3, -1, 6, 0, 0);

            //// bp.PostProcess(buildings);
            
            //BpProcessor p = new BpProcessor();
            //p.AddBelts(2001, 0, 0, 0, 10, 0, 0, -1, -1, 1101);
            //p.AddBelts(2001, 0, 4, 0, 10, 4, 0,-1,-1,0, 1301);
            //p.AddBelts(2001, -6, 5, 0, 10, 5, 0,-1, -1,1102);
            //p.AddBelts(2002, -7, 6, 0, 10, 8, 3, -1, -1, 1102);
            //p.AddBelts(2002, 20, -4, 0, -8, -5, 0);
            //p.AddPLS(-7, 0);
            //p.SetPLSStorage(p.PLSs[0], 0, 1301, true);
            //p.ConnectPLSToBelt(p.PLSs[0], 1, 0, p.gridMap.GetBuilding(-7, 6));
            //p.ConnectPLSToBelt(p.PLSs[0], 6, -1, p.gridMap.GetBuilding(-8, -5));
            //int beginX = 1;
            //int dist = BpDB.assemblerInfos[2304].DragDistanceX;
            //int beginY = 0 + BpDB.assemblerInfos[2304].centerDistanceBottom;
            //for (int i = 0; i < 3; i++)
            //{
            //    int x = beginX + dist * i;
            //    int y = beginY;
            //    int index = p.AddAssembler(2304, 50, x, y, 0,0,true);
            //    p.AssemblerConnectToBelt(index, 0, 2012, 1, false, 1301);
            //    p.AssemblerConnectToBelt(index, 1, 2012, 2, true, 0);
            //    p.AssemblerConnectToBelt(index, 8, 2012, -1, true, 0);
            //}
            //p.PostProcess();

            //GameMain.mainPlayer.controller.OpenBlueprintPasteMode(p.blueprintData, GameConfig.blueprintFolder + "DSPCalcBPTemp.txt");
        }

        public static void TestCreateSorter()
        {

        }

        public static BlueprintData TestCreateBP(int width = 10, int height = 5)
        {
            BlueprintData bp = new BlueprintData();
            bp.ResetAsEmpty();
            bp.layout = EIconLayout.OneIcon;
            bp.icon0 = 41508; // 戴森球计划白色标志
            bp.patch = 1;
            bp.cursorOffset_x = 0;
            bp.cursorOffset_y = 0;
            bp.shortDesc = "DSPCalc_Quick";
            bp.desc = "";
            bp.cursorTargetArea = 0;
            bp.dragBoxSize_x = width;
            bp.dragBoxSize_y = height;
            bp.primaryAreaIdx = 0;
            BlueprintArea bpa = new BlueprintArea();
            bp.areas[0] = bpa;
            bpa.index = 0;
            bpa.parentIndex = -1;
            bpa.tropicAnchor = 0;
            bpa.areaSegments = 200;
            bpa.anchorLocalOffsetX = 0;
            bpa.anchorLocalOffsetY = 0;
            bpa.width = width;
            bpa.height = height;
            bp.buildings = new BlueprintBuilding[0];
            return bp;
        }

        public static void TestCreateBPAssembler(short itemId = 2303)
        {
            BlueprintData bp = TestCreateBP();
            
            bp.areas = new BlueprintArea[1];

            bp.buildings = new BlueprintBuilding[1];
            BlueprintBuilding bpb = new BlueprintBuilding();
            bp.buildings[0] = bpb;
            bpb.index = 0;
            bpb.areaIndex = 0;
            bpb.localOffset_x = 0;
            bpb.localOffset_y = 0;
            bpb.localOffset_z = 0;
            bpb.localOffset_x2 = 0;
            bpb.localOffset_y2 = 0;
            bpb.localOffset_z2 = 0;
            bpb.pitch = 0;
            bpb.pitch2 = 0;
            bpb.yaw = 0;
            bpb.yaw2 = 0;
            bpb.tilt = 0;
            bpb.tilt2 = 0;
            bpb.itemId = itemId;
            bpb.modelIndex = (short)LDB.items.Select(itemId).prefabDesc.modelIndex;
            bpb.recipeId = 97;
            bpb.parameters = new int[1];
            bpb.parameters[0] = 0;
            GameMain.mainPlayer.controller.OpenBlueprintPasteMode(bp, GameConfig.blueprintFolder + "DSPCalcBPTemp.txt");
        }

        /// <summary>
        /// xDir代表传送带方向，为正时从自西向东
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="len"></param>
        /// <param name="xDir"></param>
        public static void TestCreateBPBelt(int itemId, int len, int xDir)
        {
            if (len <= 0)
                return;
            BlueprintData bp = TestCreateBP();
            if(len > 1) // 喷涂机位置
                bp.buildings = new BlueprintBuilding[len + 4];
            else
                bp.buildings = new BlueprintBuilding[len];
            bp.dragBoxSize_x = len;
            bp.dragBoxSize_y = 3;
            bp.areas[0].width = len;
            bp.areas[0].height = 3;
            int centerYOffset = 1;
            // 要从传送带末尾开始建立bpbuilding对象，因为每个中间格子的传送带对象都需要一个output对象（除了最末尾的），而input对象所有传送带都不需要
            int beginOffset = xDir > 0 ? len - 1 : 0;
            int bump = xDir > 0 ? -1 : 1;
            int xOffset = beginOffset;
            float yaw = xDir > 0 ? 90 : 270; // 向北为0度。向东为90度。
            for (int i = 0; i < len; i++)
            {
                BlueprintBuilding b = new BlueprintBuilding();
                bp.buildings[i] = b;

                b.index = i;
                b.areaIndex = 0;
                b.localOffset_x = xOffset;
                b.localOffset_y = centerYOffset;
                b.localOffset_z = 0;
                b.localOffset_x2 = xOffset;
                b.localOffset_y2 = centerYOffset;
                b.localOffset_z2 = 0;
                b.inputToSlot = 1; // 无论是不是最头端（没有输入），都有inputToSlot=1
                b.yaw = yaw; 
                b.yaw2 = yaw;
                b.itemId = (short)itemId;
                b.modelIndex = (short)LDB.items.Select(itemId).prefabDesc.modelIndex;
                if(i > 0) // 不是最末端（没有输出到的下一个传送带）时，有outputSlot=1，最末端则为0（默认）
                {
                    b.outputToSlot = 1;
                    b.outputObj = bp.buildings[i - 1];
                }
                b.parameters = new int[0]; // 传送带的param0号位置为显示标签的图标，1号位置为数量（0则不显示）。
                xOffset += bump;
            }

            // 喷涂机
            if(len > 1)
            {
                if (true)
                {
                    BlueprintBuilding b = new BlueprintBuilding();
                    bp.buildings[len] = b;
                    b.index = len;
                    b.areaIndex = 0;
                    b.localOffset_x = xDir > 0 ? 1 : len - 2;
                    b.localOffset_y = centerYOffset;
                    b.localOffset_z = 0;
                    b.localOffset_x2 = b.localOffset_x;
                    b.localOffset_y2 = centerYOffset;
                    b.localOffset_z2 = 0;
                    b.yaw = yaw;
                    b.yaw2 = yaw;
                    b.itemId = 2313;
                    b.modelIndex = (short)LDB.items.Select(2313).prefabDesc.modelIndex;
                    b.outputFromSlot = 15;
                    b.outputToSlot = 14;
                    b.inputFromSlot = 15;
                    b.inputToSlot = 14;
                    b.parameters = new int[0];
                }

                for (int i = 0; i < 3; i++)
                {
                    BlueprintBuilding b = new BlueprintBuilding();
                    bp.buildings[len + i + 1] = b;
                    b.index = len + i + 1;
                    b.areaIndex = 0;
                    b.localOffset_x = xDir > 0 ? 0 : len - 1;
                    b.localOffset_y = centerYOffset - 1 + i;
                    b.localOffset_z = 1;
                    b.localOffset_x2 = b.localOffset_x;
                    b.localOffset_y2 = b.localOffset_y;
                    b.localOffset_z2 = b.localOffset_z;
                    b.yaw = 90;
                    b.yaw2 = 90;
                    b.itemId = (short)itemId;
                    b.modelIndex = (short)LDB.items.Select(itemId).prefabDesc.modelIndex;
                    if (i > 0) // 不是最末端（没有输出到的下一个传送带）时，有outputSlot=1，最末端则为0（默认）
                    {
                        b.outputToSlot = 1;
                        b.outputObj = bp.buildings[len + i];
                    }
                    b.inputToSlot = 1; // 无论是不是最头端（没有输入），都有inputToSlot=1
                    b.parameters = new int[0];
                }

            }

            GameMain.mainPlayer.controller.OpenBlueprintPasteMode(bp, GameConfig.blueprintFolder + "DSPCalcBPTemp.txt");
        }
    }
}
