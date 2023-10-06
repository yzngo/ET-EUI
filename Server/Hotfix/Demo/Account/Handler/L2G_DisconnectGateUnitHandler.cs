using System;
using System.Threading.Tasks;

namespace ET
{
    // Gate服务器处理 来自 LoginCenter服务器 的断开连接请求
    public class L2G_DisconnectGateUnitHandler : AMActorRpcHandler<Scene, L2G_DisconnectGateUnit, G2L_DisconnectGateUnit>
    {
        protected override async ETTask Run(Scene scene, L2G_DisconnectGateUnit request, G2L_DisconnectGateUnit response, Action reply)
        {
            long accountId = request.AccountId;
            using (await CoroutineLockComponent.Instance.Wait(CoroutineLockType.GateLoginLock, accountId.GetHashCode()))
            {
                PlayerComponent playerComponent = scene.GetComponent<PlayerComponent>();
                Player gateUnit = playerComponent.Get(accountId);
                // 如果玩家不存在，直接返回
                if (gateUnit == null)
                {
                    reply();
                    return;
                }
                // 如果玩家存在，移除玩家
                playerComponent.Remove(accountId);
                gateUnit.Dispose();
            }

            reply();
            await ETTask.CompletedTask;
        }
    }
}