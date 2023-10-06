using System;
using System.Threading.Tasks;
using MongoDB.Driver.Linq;

namespace ET
{
    [ActorMessageHandler]
    public class A2L_LoginAccountRequestHandler : AMActorRpcHandler<Scene, A2L_LoginAccountRequest, L2A_LoginAccountResponse>
    {
        protected override async ETTask Run(Scene scene, A2L_LoginAccountRequest request, L2A_LoginAccountResponse response, Action reply)
        {
            long accountId = request.AccountId;
            using (await CoroutineLockComponent.Instance.Wait(CoroutineLockType.LoginCenterLock, accountId.GetHashCode()))
            {
                // 如果账号中心服务器中没有此账号的信息
                if (!scene.GetComponent<LoginInfoRecordComponent>().IsExist(accountId))
                {
                    // Yzn 保存登录信息？
                    reply();
                    return;
                }
                // 否则
                int zone = scene.GetComponent<LoginInfoRecordComponent>().Get(accountId);
                // 同一区服可能对应多个 网关服务器
                StartSceneConfig gateConfig = RealmGateAddressHelper.GetGate(zone, accountId);
                
                // 发送消息给Gate网关服务器，踢在线玩家下线
                var g2LDisconnectGateUnit = (G2L_DisconnectGateUnit) await MessageHelper.CallActor(gateConfig.InstanceId, 
                    new L2G_DisconnectGateUnit() {AccountId = accountId}
                );
                response.Error = g2LDisconnectGateUnit.Error;
                reply();


            }
            await Task.CompletedTask;
        }
    }
}