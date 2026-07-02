using System;
using AOT;
using HybridCLR;
using Unity.Entities;
namespace Game.Script.ECS.Systems
{
    

    public static class PreserveDOTSCallback
    {
        [ReversePInvokeWrapperGeneration(200)]
        [MonoPInvokeCallback(typeof(SystemBaseRegistry.ForwardingFunc))]
        public static void ForwardMethod(IntPtr system, IntPtr state)
        {
        }
    }
}