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
		
		public Transition(Type last, Type next)
		{
			lastPhase = last;
			nextPhase = next;
			
			MergeMaxCounts(last, next); // Set Transitions set of contracts to half the counts of the last and the next phase
			contractsToComplete = contractMaxCounts.GetContractTypes();
			
			Awake();
		}
		
		protected override void OnAwake()
		{
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
			var lastPhaseName = node.GetValue("lastPhase");
			lastPhase = Globals.PhaseTypes.Find(type => type.Name == lastPhaseName);
			
			var contractNames = node.GetValue("contractsToComplete").Split(", ".ToCharArray());
			
			foreach (var name in contractNames)
			{
				contractsToComplete.Add(Globals.ContractTypes.Find(type => type.Name == name));
			}
			
			contractsCompleted = int.Parse(node.GetValue("contractsCompleted"));
		}
		
		protected override void OnSave(ConfigNode node)
		{
			node.AddValue("lastPhase", lastPhase.Name);
			
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
		
		private void MergeMaxCounts(params Type[] phases)
		{
			foreach (var phase in phases)
			{
				contractMaxCounts.Add(ProtoList(phase), 0.5); //TODO: Make modifier configurable
			}
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