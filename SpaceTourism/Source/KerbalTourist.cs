using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP;
using KSPAchievements;
using Contracts;
using Contracts.Parameters;
using UnityEngine;

namespace SpaceTourism
{
	public class KerbalTourist
	{
		public enum KerbalState
		{
			NotReadyForVacation,
			ReadyForVacation,
			OnVacation,
			OnWayHome
		}
		
		public readonly ProtoCrewMember baseProtoCrewMember;
		KerbalState kerbalState;

		public KerbalTourist(ProtoCrewMember kerbal, KerbalState state)
		{
			this.baseProtoCrewMember = kerbal;
			this.kerbalState = state;
		}
		
		public KerbalTourist(string name, KerbalState state)
		{
			foreach(var kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
			{
				if (kerbal.name == name)
				{
					this.baseProtoCrewMember = kerbal;
					break;
				}
			}
			this.kerbalState = state;
		}
		
		public KerbalTourist(ConfigNode node)
		{
			var name = node.GetValue("name");
			foreach(var kerbal in HighLogic.CurrentGame.CrewRoster.Crew)
			{
				if (kerbal.name == name)
				{
					this.baseProtoCrewMember = kerbal;
					break;
				}
			}
			kerbalState = (KerbalState)((int)Enum.Parse(typeof(KerbalState), node.GetValue("touristState")));
		}
		
		public KerbalState state {
			get 
			{
				return kerbalState;
			}
			set 
			{
				TourismEvents.onTouristStateChange.Fire(this, kerbalState, value);
				kerbalState = value;
			}
		}
		
		public void Save(ConfigNode node)
		{
			node.AddValue("name", baseProtoCrewMember.name);
			node.AddValue("touristState", kerbalState);
		}
		
		public static bool DeadKerbal(KerbalTourist kerbal)
        {
        	return kerbal.baseProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Dead;
        }
        
        public static bool AssignedKerbal(KerbalTourist kerbal)
        {
        	return kerbal.baseProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Assigned;
        }
        
        public static bool AvailableKerbal(KerbalTourist kerbal)
        {
        	return kerbal.baseProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Available;
        }
	}
}