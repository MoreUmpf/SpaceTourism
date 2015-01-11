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
using SpaceTourism.Contracts;

namespace SpaceTourism.TourismPhases
{
	public class ZeroGFlights : TourismPhase
	{	
		protected override void OnAwake()
		{
			contractMaxCounts.Add<SubOrbitalFlight>(1);
			
			nextPhase = typeof(Stations);
		}
		
		protected override void OnStart()
		{
			TourismEvents.onStationCompleted.Add(new EventData<ProtoVessel>.OnEvent(OnStationCompleted));
		}
		
		protected override void OnDestroy()
		{
			TourismEvents.onStationCompleted.Remove(new EventData<ProtoVessel>.OnEvent(OnStationCompleted));
		}
		
		private void OnStationCompleted(ProtoVessel pvessel)
		{
			Advance();
		}
	}
}