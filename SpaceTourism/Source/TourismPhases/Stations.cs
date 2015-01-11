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
	public class Stations : TourismPhase
	{
		protected override void OnAwake()
		{
			contractMaxCounts.Add<UpgradeHotel>(2);
			
			nextPhase = typeof(Bases);
		}
		
		protected override void OnStart()
		{
			TourismEvents.onBaseCompleted.Add(new EventData<ProtoVessel>.OnEvent(OnBaseCompleted));
		}
		
		protected override void OnDestroy()
		{
			TourismEvents.onBaseCompleted.Remove(new EventData<ProtoVessel>.OnEvent(OnBaseCompleted));
		}
		
		private void OnBaseCompleted(ProtoVessel pvessel)
		{
			Advance();
		}
	}
}