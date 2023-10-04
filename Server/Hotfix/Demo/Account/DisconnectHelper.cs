namespace ET
{
    public static class DisconnectHelper
    {
        public static async ETTask Disconnect(this Session session)
        {
            if (session == null || session.IsDisposed)
            {
                return;
            }
            long instanceId = session.InstanceId;
            await TimerComponent.Instance.WaitAsync(1000);
            // 因为等待期间，session 可能提前释放，所以先比较一下
            if (session.InstanceId != instanceId)
            {
                return;
            }
            // 
            session.Dispose();
        }
    }
}