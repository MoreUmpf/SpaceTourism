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

namespace SpaceTourism
{
	public class TourismPhase
	{			
		public class ContractInfo // Infos about maximum counts of contracts at different prestige levels
		{
			public enum ContractRestriction
			{
				None,
				Landed,
				Orbital
			}
			
			public struct MaxCounts
			{
				public int Trivial, Significant, Exceptional;
				
				public int this[Contract.ContractPrestige prestige]
				{
					get
					{
						if (prestige == Contract.ContractPrestige.Trivial)
							return Trivial;
						
						if (prestige == Contract.ContractPrestige.Significant)
							return Significant;
						
						if (prestige == Contract.ContractPrestige.Exceptional)
							return Exceptional;
						
						return 0;
					}
					set
					{
						if (prestige == Contract.ContractPrestige.Trivial)
							Trivial = value;
						
						if (prestige == Contract.ContractPrestige.Significant)
							Significant = value;
						
						if (prestige == Contract.ContractPrestige.Exceptional)
							Exceptional = value;
					}
				}
			}
			
			public Type Type
			{
				get;
				private set;
			}
			
			public MaxCounts protoMaxCounts;
			
			public int OverallCount;
			
			public ContractRestriction Restriction;
			
			public ContractInfo(Type contract, int trivial, int significant, int exceptional, int overall, ContractRestriction restriction = ContractRestriction.None)
			{
				Type = contract;
				protoMaxCounts.Trivial = trivial;
				protoMaxCounts.Significant = significant;
				protoMaxCounts.Exceptional = exceptional;
				OverallCount = overall;
				Restriction = restriction;
			}
			
			public ContractInfo(ConfigNode.Value value)
			{
				Type = Globals.ContractTypes.Find(type => type.Name == value.name);
				
				var maxCounts = value.value.Split(", ".ToCharArray());
				protoMaxCounts.Trivial = int.Parse(maxCounts[0]);
				protoMaxCounts.Significant = int.Parse(maxCounts[2]);
				protoMaxCounts.Exceptional = int.Parse(maxCounts[4]);
				OverallCount = int.Parse(maxCounts[6]);
				Restriction = (ContractRestriction)Enum.Parse(typeof(ContractRestriction), maxCounts[8]);
			}
			
			public void WithdrawSurplusContracts()
			{
				var allContracts = ContractSystem.Instance.Contracts.FindAll(contract => contract.GetType() == Type);
				var offeredContracts = allContracts.FindAll(contract => contract.ContractState == Contract.State.Offered);
				
				// Withdraw according to the overall count
				var toRemove = offeredContracts.Take(Mathf.Clamp(allContracts.Count - OverallCount, 0, offeredContracts.Count));
				
				foreach (var contract in toRemove)
				{
					allContracts.Remove(contract);
					offeredContracts.Remove(contract);
					contract.Withdraw();
				}
						
				// Withdraw according to prestige counts
				foreach (var prestige in (Contract.ContractPrestige[])Enum.GetValues(typeof(Contract.ContractPrestige)))
				{
					int removeCount = allContracts.FindAll(contract => contract.Prestige == prestige).Count - protoMaxCounts[prestige];
					var targetContracts = offeredContracts.FindAll(contract => contract.Prestige == prestige);
					toRemove = targetContracts.Take(Mathf.Clamp(removeCount, 0, targetContracts.Count));
					
					foreach (var contract in toRemove)
						contract.Withdraw();
				}
			}
			
			public void SaveInfo(ConfigNode node)
			{
				node.AddValue(Type.Name, protoMaxCounts.Trivial + ", " +  protoMaxCounts.Significant + ", " +  protoMaxCounts.Exceptional + ", " +  OverallCount + ", " +  Restriction);
			}
		}
		
		public static List<ContractInfo> ContractInfos = new List<ContractInfo>();

		protected static bool skipTransition;
		protected static Type nextPhase;
		
		public static void Start()
		{
//			TourismContractManager.Instance.StartCoroutine("Update");
			TourismContractManager.Instance.CurrentPhase.OnStart();
			Debug.Log("[SpaceTourism] TourismPhase " + TourismContractManager.Instance.CurrentPhase.GetType().Name + " started!");
		}
		
		public static void Destroy()
		{
			if (TourismContractManager.Instance.CurrentPhase != null)
			{
//				TourismContractManager.Instance.StopCoroutine("Update");
				TourismContractManager.Instance.CurrentPhase.OnDestroy();
				Debug.Log("[SpaceTourism] TourismPhase " + TourismContractManager.Instance.CurrentPhase.GetType().Name + " destroyed!");
			}
		}
		
		private static void Update()
		{
			bool stop = false;
			while (!stop)
				stop = TourismContractManager.Instance.CurrentPhase.OnUpdate();
		}
		
		protected static void Advance()
		{
			if (skipTransition)
			{
				// Create the next phase and withdraw all remaining surplus contracts from the old phase
				TourismContractManager.Instance.CurrentPhase = ActivateNextPhase();
			}
			else
			{
				TourismContractManager.Instance.CurrentPhase = new TourismPhases.Transition();
			}
			
			TourismPhase.Start();
			
			Debug.LogWarning("[SpaceTourism] Advanced to tourism phase " + TourismContractManager.Instance.CurrentPhase.GetType().Name);
		}
		
		private static TourismPhase ActivateNextPhase()
		{
			Type backupNextPhase = Type.GetType(nextPhase.FullName); // Create a backup because the value will change during the process
			
			Activator.CreateInstance(backupNextPhase); // Add the new infos ontop of the old ones
			
			// Remove duplicates
			foreach (var contractInfo in ContractInfos.ConvertAll(info => info))
			{
				if (ContractInfos.FindAll(info => info.Type == contractInfo.Type).Count > 1)
					ContractInfos.Remove(contractInfo);
			}
			
			// Withdraw all remaining surplus contracts from the old phase
			foreach (var contractInfo in ContractInfos)
				contractInfo.WithdrawSurplusContracts();
			
			// Redo the creation process from scratch to get a new clean contract info list
			ContractInfos.Clear();
			return (TourismPhase)Activator.CreateInstance(backupNextPhase);
		}
		
		public static void Save(ConfigNode node)
		{
			var nodeMaxCounts = node.AddNode("MAXCOUNTS");
			
			foreach (var contractInfo in ContractInfos)
				contractInfo.SaveInfo(nodeMaxCounts);
			
			node.AddValue("skipTransition", skipTransition);
			node.AddValue("nextPhase", nextPhase.Name);
			
			TourismContractManager.Instance.CurrentPhase.OnSave(node);
		}
		
		public static void Load(ConfigNode node)
		{
			var valueList = node.GetNode("MAXCOUNTS").values;
			
			ContractInfos.Clear();
			foreach (ConfigNode.Value value in valueList)
				ContractInfos.Add(new ContractInfo(value));
				
			skipTransition = bool.Parse(node.GetValue("skipTransition"));
			
			var nextPhaseName = node.GetValue("nextPhase");
			nextPhase = Globals.PhaseTypes.Find(type => type.Name == nextPhaseName);
			
			TourismContractManager.Instance.CurrentPhase.OnLoad(node);
		}
		
		protected virtual void OnSave(ConfigNode node)
		{
		}
		
		protected virtual void OnLoad(ConfigNode node)
		{
		}
		
		protected virtual void OnStart()
		{
		}
		
		protected virtual void OnDestroy()
		{
		}
		
		protected virtual bool OnUpdate()
		{
			return true;
		}
	}
}