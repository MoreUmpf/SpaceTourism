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
		protected Dictionary<Type, int> contractMaxCounts = new Dictionary<Type, int>();
		
		protected bool skipTransition;
		protected Type nextPhase;
		
		public void Destroy()
		{
			TourismContractManager.Instance.StopCoroutine("Update");
			OnDestroy();
		}
		
		private void Update()
		{
			bool stop;
			while (!stop)
				stop = OnUpdate();
		}
		
		protected void InitUpdateLoop()
		{
			TourismContractManager.Instance.StartCoroutine("Update");
		}
		
		protected void Advance()
		{
			if (skipTransition)
			{
				TourismContractManager.Instance.CurrentPhase = (TourismPhase)Activator.CreateInstance(nextPhase);
			}
			else
			{
				TourismContractManager.Instance.CurrentPhase = new TourismPhases.Transition(this, nextPhase); //TODO: Make configurable
			}
			
			SetContractMaxCounts(TourismContractManager.Instance.CurrentPhase.contractMaxCounts); // Withdraw remaining contracts which aren't present in the next phase
			
			Debug.LogWarning("[SpaceTourism] Advanced from phase " + GetType().Name + " to " + TourismContractManager.Instance.CurrentPhase.GetType().Name);
			Destroy();
		}
		
		public bool ContractIsActive<T>() where T : Contract, Contracts.ITourismContract
		{
			return contractMaxCounts.ContainsKey(typeof(T));
		}
		
		public ReadOnlyDictionary<Type, int> GetContractMaxCounts()
		{
			return new ReadOnlyDictionary<Type, int>(contractMaxCounts);
		}
			
		public int GetContractMaxCount<T>() where T : Contract, Contracts.ITourismContract
		{
			int value;
			if (contractMaxCounts.TryGetValue(typeof(T), out value))
				return value;
			
			return 0;
		}
		
		protected void SetContractMaxCounts(Dictionary<Type, int> newMaxCounts)
		{
			foreach (var maxCount in contractMaxCounts)
			{
				int count = 0;
				var firstPair = newMaxCounts.FirstOrDefault(keyValuePair => keyValuePair.Key == maxCount.Key);
				
				if (!firstPair.Equals(null))
					count = firstPair.Value;
				
				// Call SetContractMaxCount<T>(count) with T being the type of the current dictionary-entry(maxCount.Key)
				typeof(TourismPhase).GetMethod("SetContractMaxCount").MakeGenericMethod(maxCount.Key).Invoke(this, new [] {(object)count});
				newMaxCounts.Remove(maxCount.Key);
			}
			
			contractMaxCounts = contractMaxCounts.Union(newMaxCounts).ToDictionary(pair => pair.Key, pair => pair.Value);;
		}
		
		protected void SetContractMaxCount<T>(int count) where T : Contract, Contracts.ITourismContract
		{	
			// Set a new maximum count for the current type of contract
			if (contractMaxCounts.Remove(typeof(T)))
			{
				// If a maximum count for this contract-type existed(count > 0) the existing offered(not active) contracts will be withdrawn until the desired count is reached
				int activeCount = ContractSystem.Instance.GetActiveContractCount();
				var offeredContracts = ContractSystem.Instance.GetCurrentContracts<T>().Where(contract => contract.ContractState == Contract.State.Offered).ToList();
				while (activeCount + offeredContracts.Count > count)
				{
					// Remove the lowest prestige contracts first
					var toRemove = offeredContracts.Find(contract => (contract as Contract).Prestige == Contract.ContractPrestige.Trivial);
					if (toRemove == null)
					{
						toRemove = offeredContracts.Find(contract => (contract as Contract).Prestige == Contract.ContractPrestige.Significant);
						if (toRemove == null)
						{
							toRemove = offeredContracts.Find(contract => (contract as Contract).Prestige == Contract.ContractPrestige.Exceptional);
							if (toRemove == null)
								break; // When no offered contracts remain this loop will end (we don't remove active contracts)
						}
					}
					
					offeredContracts.Remove(toRemove);
					toRemove.Withdraw();
				}
			}
			
			if (count > 0)
				contractMaxCounts.Add(typeof(T), count); // add new count if needed
		}
		
		public void Save(ConfigNode node)
		{
			Debug.Log("[SpaceTourism] Saving TourismPhase: " + GetType() + ", to node: " + node.name);
			var nodeMaxCounts = node.AddNode("MAXCOUNTS");
			
			foreach (var maxCount in contractMaxCounts)
			{
				if (!maxCount.Equals(null))
				{
					nodeMaxCounts.AddValue(maxCount.Key.Name, maxCount.Value);
					Debug.Log("[SpaceTourism] Saved MaxCount: " + maxCount.Key.Name + " = " + maxCount.Value);
				}
			}
			
			node.AddValue("skipTransition", skipTransition.ToString());
			node.AddValue("nextPhase", nextPhase.Name);
			
			OnSave(node);
			Destroy();
		}
		
		public void Load(ConfigNode node)
		{
			var maxCounts = node.GetNode("MAXCOUNTS").values;
			var names = maxCounts.DistinctNames();
			var values = maxCounts.GetValues();
			
			int result;
			for (int i = 0; i < maxCounts.Count; i++)
			{
				if (!string.IsNullOrEmpty(names[i]) && int.TryParse(values[i], out result))
					contractMaxCounts.Add(TourismContractManager.Instance.PhaseTypes.Find(type => type.Name == names[i]), result);
			}
			
			skipTransition = bool.Parse(node.GetValue("skipTransition"));
			var nextPhaseName = node.GetValue("nextPhase");
			nextPhase = TourismContractManager.Instance.PhaseTypes.Find(type => type.Name == nextPhaseName);
			
			OnLoad(node);
		}
		
		protected virtual void OnSave(ConfigNode node)
		{
		}
		
		protected virtual void OnLoad(ConfigNode node)
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