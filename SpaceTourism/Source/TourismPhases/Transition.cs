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
	public class Transition : TourismPhase
	{
		List<Type> contractsToComplete = new List<Type>();
		int contractsCompleted;
		
		public Transition()
		{
			if (nextPhase == null)
				return;
			
			int oldInfoCount = ContractInfos.Count; // Remember the old count of contract infos
			Type backupNextPhase = Type.GetType(nextPhase.FullName); // Create a backup because the value will change during the process
			Debug.Log("[SpaceTourism] Copied Type: " + nextPhase.Name + " to " + backupNextPhase.Name);
			
			Activator.CreateInstance(backupNextPhase); // Let the next phase add all its infos to the list
			
			// Take all the new contract types from the new list by excluding the old ones
			contractsToComplete = ContractInfos.ConvertAll(contractInfo => contractInfo.Type);
			contractsToComplete.RemoveRange(0, oldInfoCount);
			
			// Remove duplicates
			foreach (var contractInfo in ContractInfos.ConvertAll(info => info))
			{
				if (ContractInfos.FindAll(info => info.Type == contractInfo.Type).Count > 1)
					ContractInfos.Remove(contractInfo);
			}
			
			// Apply a modifier to all contract counts in the list so the counts will be: half last phase, half next phase contracts (smooth transition between tourism phases)
			foreach (var contractInfo in ContractInfos)
			{
				foreach (var prestige in (Contract.ContractPrestige[])Enum.GetValues(typeof(Contract.ContractPrestige)))
				{
					contractInfo.protoMaxCounts[prestige] = (int)Math.Round((double)contractInfo.protoMaxCounts[prestige] * 0.5d); //TODO: Make modifier configurable
				}
				contractInfo.OverallCount =  (int)Math.Round((double)contractInfo.OverallCount * 0.5d);
				
				contractInfo.WithdrawSurplusContracts();
			}
			
			nextPhase = backupNextPhase;
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
		
		protected override void OnLoad(ConfigNode node)
		{
			var contractNames = node.GetValue("contractsToComplete").Split(", ".ToCharArray());
			
			foreach (var name in contractNames)
			{
				if (!string.IsNullOrEmpty(name))
					contractsToComplete.Add(Globals.ContractTypes.Find(type => type.Name == name));
			}
			
			contractsCompleted = int.Parse(node.GetValue("contractsCompleted"));
		}
		
		protected override void OnSave(ConfigNode node)
		{
			string complete = string.Empty;
			bool seperator = false;
			
			foreach (var contractType in contractsToComplete)
			{
				if (seperator)
					complete += ", ";
				
				complete += contractType.Name;
				seperator = true;
			}
			
			node.AddValue("contractsToComplete", complete);
			node.AddValue("contractsCompleted", contractsCompleted);
		}
		
		private void OnContractCompleted(Contract contract)
		{
			if (contractsToComplete.Contains(contract.GetType()))
				contractsCompleted++;
			
			if (contractsCompleted >= 2) //TODO: Make configurable
				Advance();
		}
	}
}