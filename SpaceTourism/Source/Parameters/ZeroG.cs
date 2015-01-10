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
	public class ZeroG : ContractParameter
	{
		bool flightPathReached;
		
        protected override string GetHashString()
        {
        	return null;
        }
        
        protected override string GetTitle()
        {
        	return "Bring the tourists into a Zero-G Environment";
        }
        
        protected override string GetNotes()
		{
			if (TourismContractManager.Instance.DrawTouristList)
			{
				string notes = "Tourists:";
				foreach(var tourist in (Parent as SubOrbitalFlight).KerbalTourists)
	        	{
	        		if (tourist != null)
	        			notes += "\r\n- " + tourist.baseProtoCrewMember.name;
	        	}
	        	return notes;
			}
			return "\r\n \r\nTip: Reach the required Apoapsis before leaving the Atmosphere";
		}
        
		protected override void OnRegister()
		{
			GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
		}
		
		protected override void OnUnregister()
		{
			GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
		}
		
        private void OnParameterChange(Contract c, ContractParameter p)
        {
        	if (state == ParameterState.Incomplete)
        	{
	        	if (AllChildParametersComplete())
	        	{
	        		if (!flightPathReached)
	        		{
	        			foreach (var parameter in AllParameters.ToList())
	        			{
	        				RemoveParameter(parameter); // Remove all existing Subparameters
	        			}
	        			
	        			AddParameter(new ReachAltitudeEnvelope((float)(Parent as SubOrbitalFlight).MaxApA, (float)(Parent as SubOrbitalFlight).MinApA, "Wait until you reach Apoapsis "), null);
	        			flightPathReached = true;
	        		}
	        		else
	        		{
	        			SetComplete(); // Set paramter as complete
	        			flightPathReached = false;
	        			(Parent as SubOrbitalFlight).GetParameter(typeof(RecoverKerbal)).Enable();
	        		}
	        	}
	        	else
	        	{
	        		if (flightPathReached)
	        		{
	        			if (!flightMeetsRequirements())
	        			{
	        				RemoveParameter(typeof(ReachAltitudeEnvelope));
	        				SetFailed();
	        			}
	        		}
	        	}
        	}
        }
        
        private bool flightMeetsRequirements()
        {
        	var firstTouristsSeat = (Parent as SubOrbitalFlight).KerbalTourists.First().baseProtoCrewMember.seat; // Get the seat of the first tourist in the list
        	
        	if (firstTouristsSeat == null) // no seat = not in a vessel = requirements aren't met
        		return false;
        	
        	if (firstTouristsSeat.vessel.situation != Vessel.Situations.SUB_ORBITAL) // Vessel isn't orbiting = requirements aren't met
        		return false;
        	
        	if (firstTouristsSeat.vessel.GetOrbit().ApA > (Parent as SubOrbitalFlight).MaxApA || firstTouristsSeat.vessel.GetOrbit().ApA < (Parent as SubOrbitalFlight).MinApA) // Apoapsis has to be in range
        		return false;
        	
        	foreach(var kerbal in (Parent as SubOrbitalFlight).KerbalTourists) // Check if all tourists are in the same vessel
        	{
        		if (kerbal.baseProtoCrewMember.seat == null)
        			return false;
        		
        		if (kerbal.baseProtoCrewMember.seat.vessel != firstTouristsSeat.vessel)
        			return false;
        	}
        	
        	return true;
        }
	}
}