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
		Type lastPhase;
		List<Type> contractsToComplete = new List<Type>();
		int contractsCompleted;
		
		public Transition()
		{
			skipTransition = true;
			
			GameEvents.Contract.onCompleted.Add(new EventData<Contract>.OnEvent(OnContractCompleted));
		}
		
		public Transition(TourismPhase last, Type next)
		{
			lastPhase = last.GetType();
			nextPhase = next;
			skipTransition = true;
			
			// Create a temporary instance of the next phase to get access to its unique contract max counts
			var tempPhase = (TourismPhase)Activator.CreateInstance(next);
			tempPhase.Destroy(); // Destroy the temporary phase immediately to prevent it from doing anything
			
			SetContractMaxCounts(MergeMaxCounts(last, tempPhase)); // Set Transitions set of contracts to half the counts of the last and the next phase
			contractsToComplete = tempPhase.GetContractMaxCounts().Keys.ToList();
			
			GameEvents.Contract.onCompleted.Add(new EventData<Contract>.OnEvent(OnContractCompleted));
		}
		
		protected override void OnDestroy()
		{
			GameEvents.Contract.onCompleted.Remove(new EventData<Contract>.OnEvent(OnContractCompleted));
		}
		
		protected override void OnLoad(ConfigNode node)
		{
			var lastPhaseName = node.GetValue("lastPhase");
			lastPhase = TourismContractManager.Instance.PhaseTypes.Find(type => type.Name == lastPhaseName);
			
			var contractNames = node.GetValue("contractsToComplete").Split(", ".ToCharArray());
			var contractTypes = AssemblyLoader.loadedTypes.FindAll(type => type.IsSubclassOf(typeof(ITourismContract)));
			
			foreach (var name in contractNames)
			{
				contractsToComplete.Add(contractTypes.Find(type => type.Name == name));
			}
			
			contractsCompleted = int.Parse(node.GetValue("contractsCompleted"));
		}
		
		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("lastPhase", lastPhase.Name);
			
			string complete = string.Empty;
			bool seperator;
			
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
		
		private Dictionary<Type, int> MergeMaxCounts(params TourismPhase[] phases)
		{
			var result = new Dictionary<Type, int>();
			foreach (var phase in phases)
			{
				foreach (var maxCount in phase.GetContractMaxCounts())
				{
					result.Add(maxCount.Key, (int)Math.Round(maxCount.Value / 2d)); //TODO: Make modifier configurable
				}
			}
			return result;
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