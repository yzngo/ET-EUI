using System.Collections.Generic;

namespace ET
{
    // 区服信息管理组件
    [ComponentOf(typeof(Scene))]
    [ChildType(typeof(ServerInfo))]
    public class ServerInfosComponent : Entity, IAwake, IDestroy
    {
        public List<ServerInfo> ServerInfoList = new List<ServerInfo>();
    }
}