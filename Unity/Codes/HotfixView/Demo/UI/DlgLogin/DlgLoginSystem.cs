using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ET
{
	public static  class DlgLoginSystem
	{

		public static void RegisterUIEvent(this DlgLogin self)
		{
			// 使用 AddListenrAsync 代替 AddListener, 防止连续点击
			self.View.E_LoginButton.AddListenerAsync(() => { return self.OnLoginClickHandler();});
		}

		public static void ShowWindow(this DlgLogin self, Entity contextData = null)
		{
			
		}
		
		public static async ETTask OnLoginClickHandler(this DlgLogin self)
		{
			try
			{
				int errorCode = await LoginHelper.Login(self.DomainScene(),
					ConstValue.LoginAddress,
					self.View.E_AccountInputField.GetComponent<InputField>().text,
					self.View.E_PasswordInputField.GetComponent<InputField>().text);
				if (errorCode != ErrorCode.ERR_Success)
				{
					Log.Error(errorCode.ToString());
					return;
				}
				 // 显示登录之后的页面逻辑
				 self.DomainScene().GetComponent<UIComponent>().HideWindow(WindowID.WindowID_Login);
				 self.DomainScene().GetComponent<UIComponent>().ShowWindow(WindowID.WindowID_Lobby);

			}
			catch (Exception e)
			{
				Log.Error(e.ToString());
				throw;
			}
		}
		
		public static void HideWindow(this DlgLogin self)
		{

		}
		
	}
}
