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
		public ZeroGFlights()
		{
			SetContractMaxCount<SubOrbitalFlight>(1);
			
			nextPhase = typeof(Stations);
			
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