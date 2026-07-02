using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace Boot.Script
{
    public static class ECSHotFixHelper
{
    public static void InitializeDOTSWorld(Assembly hotAssembly)
    {
#if !UNITY_EDITOR
        // dotsAsseemblies为所有包含自定义Component、System等DOTS类型的AOT和热更新程序集
        if (hotAssembly == null)
        {
            Debug.LogError("热更程序集为空，DOTS初始化终止");
            return;
        }

        Assembly aotAssembly = Assembly.GetExecutingAssembly();
        Assembly[] dotsAssemblies = new Assembly[] { aotAssembly, hotAssembly };

        HashSet<Type> componentTypes = new HashSet<Type>();
        TypeManager.CollectComponentTypes(dotsAssemblies, componentTypes);
        TypeManager.AddComponentTypes(dotsAssemblies, componentTypes);
        TypeManager.RegisterSystemTypes(dotsAssemblies);
        TypeManager.InitializeSharedStatics();
        TypeManager.EarlyInitAssemblies(dotsAssemblies);
#endif

        DefaultWorldInitialization.Initialize("Default World", false);

        // 自动创建所有热更ISystem并挂载到对应系统组
        AutoSpawnAllHotISystem(World.DefaultGameObjectInjectionWorld, hotAssembly);
        
    }

    /// <summary>
    /// 自动遍历热更集所有ISystem，创建实例并按[UpdateInGroup]加入对应系统组
    /// 因为关闭了自动初始化World，系统不会自动实例化，必须手动创建
    /// </summary>
    private static void AutoSpawnAllHotISystem(World world, Assembly hotAssembly)
    {
        Type targetISystemType = typeof(ISystem);
        List<Type> systemTypeList = new List<Type>();

        // 筛选所有结构体实现的ISystem
        foreach (Type type in hotAssembly.GetTypes())
        {
            if (type.IsValueType && targetISystemType.IsAssignableFrom(type))
            {
                systemTypeList.Add(type);
            }
        }

        // 获取三大顶层系统组
        ComponentSystemGroup initGroup = world.GetOrCreateSystemManaged<InitializationSystemGroup>();
        ComponentSystemGroup simGroup = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
        ComponentSystemGroup presGroup = world.GetOrCreateSystemManaged<PresentationSystemGroup>();

        foreach (Type sysType in systemTypeList)
        {
            UpdateInGroupAttribute groupAttr = sysType.GetCustomAttribute<UpdateInGroupAttribute>();
            ComponentSystemGroup targetGroup = simGroup;

            if (groupAttr != null)
            {
                if (groupAttr.GroupType == typeof(InitializationSystemGroup))
                    targetGroup = initGroup;
                else if (groupAttr.GroupType == typeof(PresentationSystemGroup))
                    targetGroup = presGroup;
            }

            // 创建系统，接收返回值SystemHandle，不再出现类型转换报错
            SystemHandle sysHandle = world.CreateSystem(sysType);
            targetGroup.AddSystemToUpdateList(sysHandle);
            Debug.Log($"实例化热更ISystem: {sysType.FullName}");
        }
    }
}
}