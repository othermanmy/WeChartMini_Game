using Unity.Collections;
using Unity.Entities;

namespace Game.Script.ECS.Components.Enemy
{
    /// <summary>
    /// 敌人类型池初始化请求 
    /// </summary>
    public struct EnemyPoolInitRequest : IBufferElementData
    {
        //敌人类型名
        public FixedString64Bytes TypeName;
        //模板实例
        public Entity TemplateEntity;
        //预热数量
        public int PrewarmCount;
    }
}