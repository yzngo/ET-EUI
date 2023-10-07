using System.Collections.Generic;

namespace ET
{
   // 供登录中心服记录登录信息
   [ComponentOf(typeof(Scene))]
    public class LoginInfoRecordComponent: Entity, IAwake, IDestroy
    {
        public Dictionary<long, int> AccountLoginInfoDict = new Dictionary<long, int>();
    }
}
