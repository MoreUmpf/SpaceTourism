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
	public class VacTimer : ContractParameter
	{
		const int KERBIN_SECONDS_PER_DAY = 21600;
		
		double timeFinished;
		int timeDispay;
		
		public VacTimer()
		{
		}
		
		public VacTimer(int numberOfKerbinDays)
		{
			this.timeFinished = Planetarium.GetUniversalTime() + (double)(numberOfKerbinDays * KERBIN_SECONDS_PER_DAY);
		}
		
        protected override string GetHashString()
        {
        	return null;
        }
        protected override string GetTitle()
        {
        	return "Vacation is running!       Remaining time: " + timeDispay + " days";
        }
        
        protected override void OnSave (ConfigNode node)
        {
        	node.AddValue("timeFinished", timeFinished);
        }
        
        protected override void OnLoad (ConfigNode node)
        {
        	timeFinished = double.Parse(node.GetValue("timeFinished"));
        }
        
		protected override void OnUpdate()
		{
			if (Planetarium.GetUniversalTime() >= timeFinished) // When time has elapsed
			{
				SetComplete(); // Mark the Timer-Parameter as complete
			}
			
			int remainingTime = (int)Math.Ceiling((timeFinished - Planetarium.GetUniversalTime()) / KERBIN_SECONDS_PER_DAY); // Calculate remaining time
			if (timeDispay != remainingTime) // If the displayed remaining time differs from the actual remaining time in days
			{
				timeDispay = remainingTime; // Update the displayed remaining time
				GameEvents.Contract.onParameterChange.Fire(Parent.Parent as Contract, this); // Trigger a redraw of the ContractApp to show the correct remaining time
				// We have to use the GameEvent to trigger a redraw because the Redraw-Methods of the ContractApp are set as private.
			}
		}
	}
}