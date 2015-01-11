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
		public class MaxCountList
		{		
			List<Type> contractTypes = new List<Type>();
			List<int> contractMaxCounts = new List<int>();
			
			List<Type> protoContractTypes = new List<Type>();
			List<int> protoContractMaxCounts = new List<int>();
			
			public void Add(MaxCountList list)
			{
				Add(list, 1);
			}
			
			public void Add(MaxCountList list, double modifier)
			{
				for (int i = 0; i < list.protoContractTypes.Count; i++) 
				{
					Add(list.protoContractTypes[i], (int)Math.Round(list.protoContractMaxCounts[i] * modifier), true);
				}
			}
			
			public void Add<T>(int maxCount) where T : Contract, Contracts.ITourismContract
			{
				Add(typeof(T), maxCount, true);
			}
			
			public void Add(Type contract, int maxCount)
			{
				Add(contract, maxCount, true);
			}
			
			private void Add(Type contract, int maxCount, bool proto)
			{
				if (IsValidTourismContract(contract) && maxCount > 0 && !protoContractTypes.Contains(contract))
				{
					if (proto)
					{
						protoContractTypes.Add(contract);
						protoContractMaxCounts.Add(maxCount);
					}
					else
					{
						contractTypes.Add(contract);
						contractMaxCounts.Add(maxCount);
					}
				}
			}
			
			public void AddRange(Type[] contracts, int[] maxCounts)
			{
				AddRange(contracts, maxCounts, true);
			}
			
			private void AddRange(Type[] contracts, int[] maxCounts, bool proto)
			{
				if (contracts.Length == maxCounts.Length)
				{
					for (int i = 0; i < contracts.Length; i++)
					{
						Add(contracts[i], maxCounts[i], proto);
					}
				}
			}
			
			public void RemoveAt(int index)
			{
				RemoveAt(index, true);
			}
			
			private void RemoveAt(int index, bool proto)
			{
				if (index != -1)
				{
					if (proto)
					{
						protoContractTypes.RemoveAt(index);
						protoContractMaxCounts.RemoveAt(index);
					}
					else
					{
						contractTypes.RemoveAt(index);
						contractMaxCounts.RemoveAt(index);
					}
				}
			}
			
			public void Remove(Type contract)
			{
				Remove(contract, true);
			}
			
			private void Remove(Type contract, bool proto)
			{
				RemoveAt(IndexOf(contract, proto), proto);
			}
			
			public int IndexOf(Type contract, bool proto)
			{
				if (proto)
					return protoContractTypes.IndexOf(contract);
				
				return contractTypes.IndexOf(contract);
			}
			
			public int GetMaxCountAt(int index, bool proto)
			{
				if (index != -1)
				{
					if (proto)
						return protoContractMaxCounts.ElementAt(index);
					
					return contractMaxCounts.ElementAt(index);
				}
				
				return 0;
			}
			
			public int GetMaxCount(Type contract, bool proto)
			{
				return GetMaxCountAt(IndexOf(contract, proto), proto);
			}
			
			public int GetMaxCount<T>(bool proto) where T : Contract, Contracts.ITourismContract
			{
				return GetMaxCount(typeof(T), proto);
			}
			
			public List<Type> GetContractTypes()
			{
				return protoContractTypes;
			}
			
			public bool IsEmpty(bool proto)
			{
				if (proto)
					return protoContractTypes.Count == 0;
				
				return contractTypes.Count == 0;
			}
			
			public void SetMaxCount(Type contract, int maxCount)
			{
				SetMaxCount(contract, maxCount, true);
			}
			
			private void SetMaxCount(Type contract, int maxCount, bool proto)
			{
				if (IsValidTourismContract(contract))
					typeof(MaxCountList).GetMethod("SetMaxCount").MakeGenericMethod(contract).Invoke(this, new [] {(object)maxCount, (object)proto});
			}
				
			public void SetMaxCount<T>(int maxCount) where T : Contract, Contracts.ITourismContract
			{
				SetMaxCount<T>(maxCount, true);
			}
			
			private void SetMaxCount<T>(int maxCount, bool proto) where T : Contract, Contracts.ITourismContract
			{
				Remove(typeof(T), proto);
				Add(typeof(T), maxCount, proto);
				
				if (!proto)
					CorrectContractCount<T>(maxCount);
			}
			
			public void ApplyChanges()
			{
				foreach (var contract in Globals.ContractTypes)
				{
					CorrectContractCount(contract, GetMaxCount(contract, true));
				}
				
				contractTypes = protoContractTypes;
				contractMaxCounts = protoContractMaxCounts;
			}
			
			private void CorrectContractCount(Type contract, int maxCount)
			{
				if (IsValidTourismContract(contract))
					typeof(MaxCountList).GetMethod("CorrectContractCount").MakeGenericMethod(contract).Invoke(this, new [] {(object)maxCount});
			}
			
			private void CorrectContractCount<T>(int maxCount) where T : Contract, Contracts.ITourismContract
			{
				var activeContracts = ContractSystem.Instance.GetCurrentActiveContracts<T>();
				var offeredContracts = ContractSystem.Instance.GetCurrentContracts<T>().Where(contract => contract.ContractState == Contract.State.Offered).ToList();
				
				while (offeredContracts.Count + activeContracts.Length > maxCount)
				{
					// Remove the lowest prestige contracts first
					var toRemove = offeredContracts.FirstOrDefault(contract => (contract as Contract).Prestige == Contract.ContractPrestige.Trivial);
					if (toRemove == null)
					{
						toRemove = offeredContracts.FirstOrDefault(contract => (contract as Contract).Prestige == Contract.ContractPrestige.Significant);
						if (toRemove == null)
						{
							toRemove = offeredContracts.FirstOrDefault(contract => (contract as Contract).Prestige == Contract.ContractPrestige.Exceptional);
							if (toRemove == null)
								break; // When no offered contracts remain this loop will end (we don't remove active contracts)
						}
					}
						
					offeredContracts.Remove(toRemove);
					toRemove.Withdraw();
				}
			}
			
			private bool IsValidTourismContract(Type contract)
			{
				if (contract != null && Globals.ContractTypes.Contains(contract))
					return true;
				
				Debug.LogError("[SpaceTourism] Contract " + contract.Name + " is not a valid tourism contract!");
				return false;
			}
			
			public void Save(ConfigNode node)
			{
				for (int i = 0; i < protoContractTypes.Count; i++) 
				{
					node.AddValue(protoContractTypes[i].Name, protoContractMaxCounts[i]);
				}
			}
			
			public void Load(ConfigNode node)
			{
				for (int i = 0; i < node.values.Count; i++)
				{
					SetMaxCount(Globals.ContractTypes.Find(type => type.Name == node.values[i].name), int.Parse(node.values[i].value), true);
				}
			}
		}
		
		public MaxCountList ContractMaxCounts
		{
			get
			{
				return contractMaxCounts;
			}
		}
		
		protected MaxCountList contractMaxCounts = new MaxCountList();

		protected bool skipTransition;
		protected Type nextPhase;
		
		public TourismPhase()
		{
			Awake();
		}
		
		~TourismPhase()
		{
			Destroy();
		}
		
		public static MaxCountList ProtoList(Type phase)
		{
			if (phase != null && Globals.PhaseTypes.Contains(phase))
				return (MaxCountList)typeof(TourismPhase).GetMethod("ProtoList").MakeGenericMethod(phase).Invoke(null, new object[0]);
			
			Debug.LogError("[SpaceTourism] TourismPhase " + phase.Name + " is not a valid tourism phase!");
			return null;
		}
		
		public static MaxCountList ProtoList<T>() where T : TourismPhase, new()
		{
			return new T().contractMaxCounts;
		}
		
		public void Awake()
		{
			OnAwake();
			Debug.Log("[SpaceTourism] TourismPhase " + GetType().Name + " is now awake!");
		}
		
		public void Start()
		{
			contractMaxCounts.ApplyChanges();
			TourismContractManager.Instance.StartCoroutine("Update");
			OnStart();
		}
		
		public void Destroy()
		{
			TourismContractManager.Instance.StopCoroutine("Update");
			OnDestroy();
		}
		
		private void Update()
		{
			bool stop = false;
			while (!stop)
				stop = OnUpdate();
		}
		
		protected void Advance()
		{
			if (skipTransition)
			{
				TourismContractManager.Instance.CurrentPhase = (TourismPhase)Activator.CreateInstance(nextPhase);
			}
			else
			{
				TourismContractManager.Instance.CurrentPhase = new TourismPhases.Transition(GetType(), nextPhase); //TODO: Make configurable
			}
			
			TourismContractManager.Instance.CurrentPhase.Start();
			
			Debug.LogWarning("[SpaceTourism] Advanced from phase " + GetType().Name + " to " + TourismContractManager.Instance.CurrentPhase.GetType().Name);
		}
		
		public void Save(ConfigNode node)
		{
			contractMaxCounts.Save(node.AddNode("MAXCOUNTS"));
			node.AddValue("skipTransition", skipTransition.ToString());
			node.AddValue("nextPhase", nextPhase.Name);
			
			OnSave(node);
			Destroy();
		}
		
		public void Load(ConfigNode node)
		{
			contractMaxCounts.Load(node.GetNode("MAXCOUNTS"));
			skipTransition = bool.Parse(node.GetValue("skipTransition"));
			
			var nextPhaseName = node.GetValue("nextPhase");
			nextPhase = Globals.PhaseTypes.Find(type => type.Name == nextPhaseName);
			
			OnLoad(node);
		}
		
		protected virtual void OnSave(ConfigNode node)
		{
		}
		
		protected virtual void OnLoad(ConfigNode node)
		{
		}
		
		protected virtual void OnAwake()
		{
			Debug.LogError("[SpaceTourism] OnAwake()-Method not implemented in: " + GetType().Name);
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