// Copyright Epic Games, Inc. All Rights Reserved.
// This file is automatically generated. Changes to this file may be overwritten.

namespace Epic.OnlineServices.Lobby
{
	/// <summary>
	/// Input parameters for the <see cref="LobbyModification.RemoveMemberAttribute" /> function.
	/// </summary>
	public class LobbyModificationRemoveMemberAttributeOptions
	{
		/// <summary>
		/// Name of the key
		/// </summary>
		public string Key { get; set; }
	}

	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 8)]
	internal struct LobbyModificationRemoveMemberAttributeOptionsInternal : ISettable, System.IDisposable
	{
		private int m_ApiVersion;
		private System.IntPtr m_Key;

		public string Key
		{
			set
			{
				Helper.TryMarshalSet(ref m_Key, value);
			}
		}

		public void Set(LobbyModificationRemoveMemberAttributeOptions other)
		{
			if (other != null)
			{
				m_ApiVersion = LobbyModification.LobbymodificationRemovememberattributeApiLatest;
				Key = other.Key;
			}
		}

		public void Set(object other)
		{
			Set(other as LobbyModificationRemoveMemberAttributeOptions);
		}

		public void Dispose()
		{
			Helper.TryMarshalDispose(ref m_Key);
		}
	}
}