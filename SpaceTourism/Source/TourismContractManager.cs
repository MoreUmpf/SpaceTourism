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
	[KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
	public class TourismContractManager : Singleton<TourismContractManager>
	{
		protected TourismContractManager();
		
		public enum TourismPhases
		{
			ZeroG,
			Transition1,
			Stations,
			Transition2,
			Bases,
			Transition3,
			Multi
		}
		
		public TourismPhases currentPhase;
		
		List<ProtoVessel> existingHotels = new List<ProtoVessel>();
		
		private void Awake()
		{
			if (AssemblyLoader.loadedAssemblies.Any(assembly => assembly.name == "FinePrint"))
			{
				Debug.Log("[SpaceTourism] 'FinePrint' not installed! This mod needs 'FinePrint' to be installed in order to work properly!");
				DestroyObject(this, 0f);
			}
			else
				DontDestroyOnLoad(this);
			
			Debug.Log("[SpaceTourism ContractManager] Awaked!");
		}
		
		private void Reset()
		{
			currentPhase = TourismPhases.ZeroG;
			existingHotels.Clear();
		}

		private void Start()
		{
			GameEvents.Contract.onCompleted.Add(new EventData<Contract>.OnEvent(OnContractCompleted));
			GameEvents.onGameStateSave.Add(new EventData<ConfigNode>.OnEvent(OnSave));
			GameEvents.onGameStateLoad.Add(new EventData<ConfigNode>.OnEvent(OnLoad));
			
			Debug.Log("[SpaceTourism ContractManager] Started!");
		}

		private void OnDestroy()
		{
			GameEvents.Contract.onCompleted.Remove(new EventData<Contract>.OnEvent(OnContractCompleted));
			GameEvents.onGameStateSave.Remove(new EventData<ConfigNode>.OnEvent(OnSave));
			GameEvents.onGameStateLoad.Remove(new EventData<ConfigNode>.OnEvent(OnLoad));
			
			Debug.Log("[SpaceTourism ContractManager] Destroyed!");
		}
		
		private void OnSave(ConfigNode node)
		{
			node = node.AddNode("SPACETOURISM");
			node.AddValue("currentPhase", currentPhase);
			var nodeHotels = node.AddNode("HOTELS");
			foreach (var hotel in existingHotels)
			{
				nodeHotels.AddValue("hotel", hotel.vesselID);
			}
		}
		
		private void OnLoad(ConfigNode node)
		{
			node = node.GetNode("SPACETOURISM");
			if (node == null)
				return;
			
			currentPhase = (TourismPhases)((int)Enum.Parse(typeof(TourismPhases), node.GetValue("currentPhase")));
			var nodeHotels = node.GetNode("HOTELS");
			existingHotels.Clear();
			foreach (var hotelID in nodeHotels.GetValues("hotel"))
			{
				existingHotels.Add(HighLogic.CurrentGame.flightState.protoVessels.Find(vessel => vessel.vesselID == hotelID));
			}
		}
		
		private void OnContractCompleted(Contract contract)
		{
			Debug.Log("[SpaceTourism ContractManager] Contract completed! Type: " + contract.GetType());
			if (contract.GetType() == typeof(FinePrint.Contracts.StationContract) || contract.GetType() == typeof(FinePrint.Contracts.BaseContract))
				existingHotels.Add(FlightGlobals.ActiveVessel.protoVessel);
		}
		
		public ProtoVessel GetAvailableHotel(CelestialBody body, Vessel.Situations situation)
		{
			var basicStations = existingHotels.FindAll(vessel => VesselMeetsRequirements(vessel, body, situation));
			var upgradedStations = basicStations.FindAll(vessel => vessel.protoPartSnapshots.Exists(part => part.modules.Exists(module => module.moduleName == "TourismModule")));
			
			if (upgradedStations.Count == 0)
				return basicStations.ElementAtOrDefault(UnityEngine.Random.Range(0, basicStations.Count - 1));
			
			return upgradedStations[UnityEngine.Random.Range(0, upgradedStations.Count - 1)];
		}
		
//		public ProtoVessel GetAvailableBase(CelestialBody body)
//		{
//			var basicStations = existingHotels.FindAll(vessel => VesselMeetsRequirements(vessel, body, Vessel.Situations.LANDED));
//			var upgradedStations = basicStations.FindAll(vessel => vessel.protoPartSnapshots.Exists(part => part.modules.Exists(module => module.moduleName == "TourismModule")));
//			
//			if (upgradedStations.Count == 0)
//				return basicStations.ElementAtOrDefault(UnityEngine.Random.Range(0, basicStations.Count - 1));
//			
//			return upgradedStations.ElementAt(UnityEngine.Random.Range(0, upgradedStations.Count - 1));
//		}
		
		private bool VesselMeetsRequirements(ProtoVessel protoVessel, CelestialBody body, Vessel.Situations situation)
		{
			bool vesselHasAntenna = false;
			bool vesselHasPowerGen = false;
			bool vesselHasDockingPort = false;
			
			if (FlightGlobals.Bodies[protoVessel.orbitSnapShot.ReferenceBodyIndex] == body && protoVessel.situation == situation)
			{
				foreach (ProtoPartSnapshot part in protoVessel.protoPartSnapshots)
				{
					vesselHasAntenna |= part.modules.Exists(p => p.moduleName == "ModuleDataTransmitter" || p.moduleName == "ModuleLimitedDataTransmitter" || 
					                                        	 p.moduleName == "ModuleRTDataTransmitter" || p.moduleName == "ModuleRTAntenna");
					vesselHasPowerGen |= part.modules.Exists(p => p.moduleName == "ModuleGenerator" || p.moduleName == "ModuleDeployableSolarPanel" || p.moduleName == "FNGenerator" || 
					                                         	  p.moduleName == "FNAntimatterReactor" || p.moduleName == "FNNuclearReactor" || p.moduleName == "FNFusionReactor" || 
					                                         	  p.moduleName == "KolonyConverter" || p.moduleName == "FissionGenerator" || p.moduleName == "ModuleCurvedSolarPanel");
					vesselHasDockingPort |= part.modules.Exists(p => p.moduleName == "ModuleDockingNode");
					
					if (vesselHasAntenna && vesselHasPowerGen && vesselHasDockingPort)
						return true;
				}
			}
			
			return false;
		}
    }
}