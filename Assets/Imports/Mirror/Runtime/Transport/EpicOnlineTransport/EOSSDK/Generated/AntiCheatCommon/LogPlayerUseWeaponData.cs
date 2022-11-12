// Copyright Epic Games, Inc. All Rights Reserved.
// This file is automatically generated. Changes to this file may be overwritten.

namespace Epic.OnlineServices.AntiCheatCommon
{
	public class LogPlayerUseWeaponData : ISettable
	{
		/// <summary>
		/// Locally unique value used in RegisterClient/RegisterPeer
		/// </summary>
		public System.IntPtr PlayerHandle { get; set; }

		/// <summary>
		/// Player's current world position as a 3D vector
		/// </summary>
		public Vec3f PlayerPosition { get; set; }

		/// <summary>
		/// Player's view rotation as a quaternion
		/// </summary>
		public Quat PlayerViewRotation { get; set; }

		/// <summary>
		/// True if the player's view is zoomed (e.g. using a sniper rifle), otherwise false
		/// </summary>
		public bool IsPlayerViewZoomed { get; set; }

		/// <summary>
		/// Set to true if the player is using a melee attack, otherwise false
		/// </summary>
		public bool IsMeleeAttack { get; set; }

		/// <summary>
		/// Name of the weapon used. Will be truncated to <see cref="AntiCheatCommonInterface.LogplayeruseweaponWeaponnameMaxLength" /> bytes if longer.
		/// </summary>
		public string WeaponName { get; set; }

		internal void Set(LogPlayerUseWeaponDataInternal? other)
		{
			if (other != null)
			{
				PlayerHandle = other.Value.PlayerHandle;
				PlayerPosition = other.Value.PlayerPosition;
				PlayerViewRotation = other.Value.PlayerViewRotation;
				IsPlayerViewZoomed = other.Value.IsPlayerViewZoomed;
				IsMeleeAttack = other.Value.IsMeleeAttack;
				WeaponName = other.Value.WeaponName;
			}
		}

		public void Set(object other)
		{
			Set(other as LogPlayerUseWeaponDataInternal?);
		}
	}

	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 8)]
	internal struct LogPlayerUseWeaponDataInternal : ISettable, System.IDisposable
	{
		private System.IntPtr m_PlayerHandle;
		private System.IntPtr m_PlayerPosition;
		private System.IntPtr m_PlayerViewRotation;
		private int m_IsPlayerViewZoomed;
		private int m_IsMeleeAttack;
		private System.IntPtr m_WeaponName;

		public System.IntPtr PlayerHandle
		{
			get
			{
				return m_PlayerHandle;
			}

			set
			{
				m_PlayerHandle = value;
			}
		}

		public Vec3f PlayerPosition
		{
			get
			{
				Vec3f value;
				Helper.TryMarshalGet<Vec3fInternal, Vec3f>(m_PlayerPosition, out value);
				return value;
			}

			set
			{
				Helper.TryMarshalSet<Vec3fInternal, Vec3f>(ref m_PlayerPosition, value);
			}
		}

		public Quat PlayerViewRotation
		{
			get
			{
				Quat value;
				Helper.TryMarshalGet<QuatInternal, Quat>(m_PlayerViewRotation, out value);
				return value;
			}

			set
			{
				Helper.TryMarshalSet<QuatInternal, Quat>(ref m_PlayerViewRotation, value);
			}
		}

		public bool IsPlayerViewZoomed
		{
			get
			{
				bool value;
				Helper.TryMarshalGet(m_IsPlayerViewZoomed, out value);
				return value;
			}

			set
			{
				Helper.TryMarshalSet(ref m_IsPlayerViewZoomed, value);
			}
		}

		public bool IsMeleeAttack
		{
			get
			{
				bool value;
				Helper.TryMarshalGet(m_IsMeleeAttack, out value);
				return value;
			}

			set
			{
				Helper.TryMarshalSet(ref m_IsMeleeAttack, value);
			}
		}

		public string WeaponName
		{
			get
			{
				string value;
				Helper.TryMarshalGet(m_WeaponName, out value);
				return value;
			}

			set
			{
				Helper.TryMarshalSet(ref m_WeaponName, value);
			}
		}

		public void Set(LogPlayerUseWeaponData other)
		{
			if (other != null)
			{
				PlayerHandle = other.PlayerHandle;
				PlayerPosition = other.PlayerPosition;
				PlayerViewRotation = other.PlayerViewRotation;
				IsPlayerViewZoomed = other.IsPlayerViewZoomed;
				IsMeleeAttack = other.IsMeleeAttack;
				WeaponName = other.WeaponName;
			}
		}

		public void Set(object other)
		{
			Set(other as LogPlayerUseWeaponData);
		}

		public void Dispose()
		{
			Helper.TryMarshalDispose(ref m_PlayerHandle);
			Helper.TryMarshalDispose(ref m_PlayerPosition);
			Helper.TryMarshalDispose(ref m_PlayerViewRotation);
			Helper.TryMarshalDispose(ref m_WeaponName);
		}
	}
}