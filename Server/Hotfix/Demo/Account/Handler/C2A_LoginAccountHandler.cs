using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ET
{
    [FriendClass(typeof (Account))]
    public class C2A_LoginAccountHandler: AMRpcHandler<C2A_LoginAccount, A2C_LoginAccount>
    {
        protected override async ETTask Run(
            Session session,
            C2A_LoginAccount request,
            A2C_LoginAccount response,
            Action reply
        )
        {
            // 每次连入的 session 都是新创建的
            if (session.DomainScene().SceneType != SceneType.Account)
            {
                Log.Error($"请求的Scene错误，当前Scene为：{session.DomainScene().SceneType}");
                session.Dispose();
                return;
            }

            // 刚accept的session只持续5秒，必须通过验证，否则断开
            // 所以这里需要移除SessionAcceptTimeoutComponent, 相当于验证通过
            session.RemoveComponent<SessionAcceptTimeoutComponent>();

            // 如果玩家连续点击，可能进来多个请求，只处理第一个
            if (session.GetComponent<SessionLockingComponent>() != null)
            {
                response.Error = ErrorCode.ERR_RequestRepeatedly;
                reply();
                session.Disconnect().Coroutine();
                return;
            }

            // 检查账号，密码不能为空
            if (string.IsNullOrEmpty(request.AccountName) || string.IsNullOrEmpty(request.Password))
            {
                response.Error = ErrorCode.ERR_LoginInfoIsNull;
                reply();
                session.Disconnect().Coroutine();
                return;
            }

            // 验证账号
            // 长度在 4 到 20 个字符之间。
            // 只包含字母（大小写不敏感）、数字或下划线。
            if (!Regex.IsMatch(request.AccountName.Trim(), @"^[a-zA-Z0-9_]{4,20}$"))
            {
                response.Error = ErrorCode.ERR_AccountNameFormError;
                reply();
                session.Disconnect().Coroutine();
                return;
            }

            // 验证密码
            // if (!Regex.IsMatch(request.Password.Trim(), @"^(?=.*[0-9].*)(?=.*[A-Z].*)(?=.*[a-z].*).{6,15}$"))
            // yzn 暂时兼容在客户端把密码加密后，验证无法通过
            if (!Regex.IsMatch(request.Password.Trim(), @"^[a-zA-Z0-9_]+$"))
            {
                response.Error = ErrorCode.ERR_PasswordFormError;
                reply();
                session.Disconnect().Coroutine();
                return;
            }

            using (session.AddComponent<SessionLockingComponent>())
            {
                // 使用协程锁，解决同一玩家相同的账号密码同时登录的问题
                using (await CoroutineLockComponent.Instance.Wait(
                           CoroutineLockType.LoginAccount, request.AccountName.Trim().GetHashCode())
                )
                {
                    // 查询数据库，验证账号密码是否匹配
                    List<Account> accountInfoList = await DBManagerComponent.Instance
                            .GetZoneDB(session.DomainZone())
                            .Query<Account>(d => d.AccountName.Equals(request.AccountName.Trim()));
                    Account account = null;
                    if (accountInfoList != null && accountInfoList.Count > 0)
                    {
                        account = accountInfoList[0];
                        session.AddChild(account);
                        if (account.AccountType == (int)AccountType.BlackList)
                        {
                            response.Error = ErrorCode.ERR_AccountInBlackListError;
                            reply();
                            session.Disconnect().Coroutine();
                            account.Dispose();
                            return;
                        }

                        if (!account.Password.Equals(request.Password))
                        {
                            response.Error = ErrorCode.ERR_LoginPasswordError;
                            reply();
                            session.Disconnect().Coroutine();
                            account.Dispose();
                            return;
                        }
                    }
                    else
                    {
                        account = session.AddChild<Account>();
                        account.AccountName = request.AccountName.Trim();
                        account.Password = request.Password.Trim();
                        account.CreateTime = TimeHelper.ServerNow();
                        account.AccountType = (int)AccountType.General;
                        // domain zone 代表区服，1服，2服，3服等
                        await DBManagerComponent.Instance.GetZoneDB(session.DomainZone()).Save<Account>(account);
                    }
                    
                    // 查询账号中心服务器
                    StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.GetBySceneName(session.DomainZone(), "LoginCenter");
                    long loginCenterInstanceId = startSceneConfig.InstanceId;
                    var loginAccountResponse = (L2A_LoginAccountResponse) await ActorMessageSenderComponent.Instance.Call(
                        loginCenterInstanceId, new A2L_LoginAccountRequest()
                        {
                            AccountId = account.Id
                        });

                    if (loginAccountResponse.Error != ErrorCode.ERR_Success)
                    {
                        response.Error = loginAccountResponse.Error;
                        reply();
                        session.Disconnect().Coroutine();
                        account.Dispose();
                        return;
                    }

                    // domain scene 代表账号服务器
                    long accountSessionInstanceId = session.DomainScene().GetComponent<AccountSessionsComponent>().Get(account.Id);
                    // 把 session instance id 转化成 Session 实例
                    Session otherSession = Game.EventSystem.Get(accountSessionInstanceId) as Session;
                    // 把原来的用户踢下线
                    otherSession?.Send(new A2C_Disconnect() {Error = 0});
                    otherSession?.Disconnect().Coroutine();
                    // 新用户上线
                    session.DomainScene().GetComponent<AccountSessionsComponent>().Add(account.Id, session.InstanceId);
                    // 添加断线检测
                    session.AddComponent<AccountCheckOutTimeComponent, long>(account.Id);
                    // 生成 Token
                    string token = TimeHelper.ServerNow() + RandomHelper.RandomNumber(int.MinValue, int.MaxValue).ToString();
                    session.DomainScene().GetComponent<TokenComponent>().Remove(account.Id);
                    session.DomainScene().GetComponent<TokenComponent>().Add(account.Id, token);
                    response.AccountId = account.Id;
                    response.Token = token;
                    reply();
                    account.Dispose();
                }
            }
        }
    }
}
