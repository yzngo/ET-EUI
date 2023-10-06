using System.Collections.Generic;


namespace ET
{
	public static class RealmGateAddressHelper
	{
		public static StartSceneConfig GetGate(int zone, long accountId)
		{
			List<StartSceneConfig> zoneGates = StartSceneConfigCategory.Instance.Gates[zone];
			
			// 完全随机
			// int n = RandomHelper.RandomNumber(0, zoneGates.Count);
			
			// 使用账号id的hash值来决定连接哪个网关服务器，
			// 可以保证同一账号的连接始终是同一个网关服务器
			int n = accountId.GetHashCode() % zoneGates.Count;

			return zoneGates[n];
		}
	}
}
