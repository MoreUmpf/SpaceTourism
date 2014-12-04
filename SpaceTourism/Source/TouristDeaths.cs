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
 
namespace SpaceTourism.Contracts.Parameters
{
	public class TouristDeaths : ContractParameter
	{
//		List<string> kerbalTourists = new List<string>();
		
        protected override string GetHashString()
        {
        	return null;
        }
        protected override string GetTitle()
        {
        	return "Don't kill any Tourists!";
        }
		
		protected override string GetMessageFailed()
		{
			return string.Empty;
		}

        protected override void OnRegister()
        {
        	GameEvents.onCrewKilled.Add(new EventData<EventReport>.OnEvent(OnCrewKilled));
        }
        
        protected override void OnUnregister()
        {
        	GameEvents.onCrewKilled.Remove(new EventData<EventReport>.OnEvent(OnCrewKilled));
        }
        
        public void MarkAsComplete()
        {
        	SetComplete();
        }

        private void OnCrewKilled(EventReport report)
        {
            if (report.eventType == FlightEvents.CREW_KILLED)
            {
            	if (state != ParameterState.Failed)
            	{
	            	foreach(var kerbal in (Parent as OrbitVacation).kerbalTourists)
	            	{
	            		if (kerbal.baseProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Dead)
	            		{
							SetFailed();
		            		return;
	            		}
	            	}
            	}
            }
        }
	}
}
