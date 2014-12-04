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
	public class TouristsTogether : ContractParameter
	{	
        protected override string GetHashString()
        {
        	return null;
        }
        protected override string GetTitle()
        {
        	return "All tourists together";
        }

        protected override void OnRegister()
        {
        	GameEvents.onFlightReady.Add(new EventVoid.OnEvent(OnFlightReady));
        	GameEvents.onPartCouple.Add(new EventData<GameEvents.FromToAction<Part, Part>>.OnEvent(OnPartCouple));
        	GameEvents.onPartJointBreak.Add(new EventData<PartJoint>.OnEvent(OnPartJointBreak));
        }
        
        protected override void OnUnregister()
        {
        	GameEvents.onFlightReady.Remove(new EventVoid.OnEvent(OnFlightReady));
        	GameEvents.onPartCouple.Remove(new EventData<GameEvents.FromToAction<Part, Part>>.OnEvent(OnPartCouple));
        	GameEvents.onPartJointBreak.Remove(new EventData<PartJoint>.OnEvent(OnPartJointBreak));
        }
        
        private void OnFlightReady()
        {
        	TrackVessel();
        }
        
        private void OnPartCouple(GameEvents.FromToAction<Part, Part> action)
        {
        	TrackVessel();
        }
        
        private void OnPartJointBreak(PartJoint joint)
        {
        	if (HighLogic.LoadedSceneIsFlight)
        		TrackVessel();
        }
        
        private void TrackVessel() // Check if all tourists are in the same vessel and change the ParameterState if needed
        {
        	var firstTouristsSeat = (Parent.Parent as OrbitVacation).kerbalTourists.First().baseProtoCrewMember.seat; // Get the seat of the first tourist in the list
        	
        	foreach(var kerbal in (Parent.Parent as OrbitVacation).kerbalTourists)
        	{
        		if (kerbal.baseProtoCrewMember.seat == null)
        		{
        			if (state == ParameterState.Complete)
        				SetIncomplete();
        			return;
        		}
        		
        		if (kerbal.baseProtoCrewMember.seat.vessel != firstTouristsSeat.vessel)
        		{
        			if (state == ParameterState.Complete)
        				SetIncomplete();
        			return;
        		}
        	}
        	SetComplete();
        }
	}
}