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
	public class VacationTime : ContractParameter
	{
		bool vacationIsRunning;
		CelestialBody targetBody;
		int numberOfDays;
		string[] vacationLuxuries;
		
		public VacationTime()
		{			
		}
		
		public VacationTime(CelestialBody body, int days, string[] luxuries)
        {
            this.numberOfDays = days;
            this.targetBody = body;
            this.vacationLuxuries = luxuries;
        }

        protected override string GetHashString()
        {
        	return null;
        }
        
        protected override string GetTitle()
        {
        	return "Leave the tourists for " + numberOfDays + " Days in Orbit around " + targetBody.theName;
        	//TODO: Add Titles for different vacations(with/without luxuries ; in Orbit/on Surface ; at SpaceStation X/SurfaceBase Y)
        }
        
		protected override void OnRegister()
		{
			GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
		}
		
		protected override void OnUnregister()
		{
			GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
		}

        protected override void OnSave (ConfigNode node)
        {
        	node.AddValue("numberOfDays", numberOfDays);
        	node.AddValue("targetBody", targetBody.flightGlobalsIndex);
        	node.AddValue("vacationIsRunning", vacationIsRunning);
        }
        
        protected override void OnLoad (ConfigNode node)
        {
			numberOfDays = int.Parse(node.GetValue("numberOfDays"));
			targetBody = FlightGlobals.Bodies.ElementAt(int.Parse(node.GetValue("targetBody")));
			vacationIsRunning = bool.Parse(node.GetValue("vacationIsRunning"));
        }

        protected void OnParameterChange(Contract c, ContractParameter p)
        {
        	if (state == ParameterState.Incomplete)
        	{
	        	if (AllChildParametersComplete())
	        	{
	        		if (!vacationIsRunning) // If all tourists are ready for vacation and vacation hasn't begun
	        		{
	        			foreach (var parameter in AllParameters.ToList())
	        			{
	        				RemoveParameter(parameter); // Remove all existing Subparameters
	        			}
	        			
	        			AddParameter(new VacTimer(numberOfDays), null); // Add new vacation timer
	        			vacationIsRunning = true;
	        		}
	        		else // If vacation is complete
	        		{
	        			SetComplete(); // Set paramter as complete
	        		}
	        	}
	        	else
	        	{
	        		if (vacationIsRunning)
	        		{
	        			if (!vacMeetsRequirements())
	        			{
	        				RemoveParameter(typeof(VacTimer));
	        				SetFailed();
	        			}
	        		}
	        	}
        	}
        }
        
        private bool vacMeetsRequirements()
        {
        	Debug.Log("[VacationTime] vacMeetRequirements called!");
        	var firstTouristsSeat = (Parent as OrbitVacation).kerbalTourists.First().baseProtoCrewMember.seat; // Get the seat of the first tourist in the list
        	
        	if (firstTouristsSeat == null) // no seat = not in a vessel = requirements aren't met
        		return false;
        	
        	if (firstTouristsSeat.vessel.mainBody != targetBody) // tourists mainBody differs from targetBody = requirements aren't met
        		return false;
        	
        	if (firstTouristsSeat.vessel.situation != Vessel.Situations.ORBITING) // Vessel isn't orbiting = requirements aren't met
        		return false;
        	
        	foreach(var kerbal in (Parent as OrbitVacation).kerbalTourists) // Check if all tourists are in the same vessel
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