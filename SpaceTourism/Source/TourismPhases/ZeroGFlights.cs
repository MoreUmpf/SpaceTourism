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
			ContractInfos.Add(new ContractInfo(typeof(SubOrbitalFlight), 3, 2, 1, 3));
			
			nextPhase = typeof(Stations); // May change depending on what you build first
			skipTransition = false;
		}
		
		protected override void OnStart()
		{
			GameEvents.Contract.onCompleted.Add(OnContractCompleted);
			
		}
		
		protected override void OnDestroy()
		{
			GameEvents.Contract.onCompleted.Remove(OnContractCompleted);
		}
		
		private void OnContractCompleted(Contract contract)
		{
			Debug.Log("[ZeroGFlights] OnContractCompleted called!");
			if (contract.GetType() == typeof(FinePrint.Contracts.StationContract))
			{
				nextPhase = typeof(Stations);
				Advance();
			}
			else if (contract.GetType() == typeof(FinePrint.Contracts.BaseContract))
			{
				nextPhase = typeof(Bases);
				Advance();
			}
			Debug.Log("[ZeroGFlights] OnContractCompleted called! done");
		}
	}
}