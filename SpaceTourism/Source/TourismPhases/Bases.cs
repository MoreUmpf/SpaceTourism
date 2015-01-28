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
	public class Bases : TourismPhase
	{
		public Bases()
		{
			ContractInfos.Add(new ContractInfo(typeof(UpgradeHotel), 2, 3, 2, 3, ContractInfo.ContractRestriction.Landed));
			
			nextPhase = typeof(BasesStations);
			skipTransition = true;
		}
		
		protected override void OnStart()
		{
			GameEvents.Contract.onCompleted.Add(new EventData<Contract>.OnEvent(OnContractCompleted));
		}
		
		protected override void OnDestroy()
		{
			GameEvents.Contract.onCompleted.Remove(new EventData<Contract>.OnEvent(OnContractCompleted));
		}
		
		private void OnContractCompleted(Contract contract)
		{
			if (contract.GetType() == typeof(FinePrint.Contracts.StationContract))
				Advance();
		}
	}
}