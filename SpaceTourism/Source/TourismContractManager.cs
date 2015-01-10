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
	[KSPScenario((ScenarioCreationOptions)96, new [] {
		GameScenes.FLIGHT,
		GameScenes.TRACKSTATION,
		GameScenes.SPACECENTER,
		GameScenes.EDITOR
	})]
	public class TourismContractManager : ScenarioModule
	{	
		public static TourismContractManager Instance
		{
			get;
			private set;
		}
		
		public List<Type> PhaseTypes
		{
			get
			{
				return phaseTypes;
			}
		}
		
		public TourismPhase CurrentPhase
		{
			get
			{
				return currentPhase;
			}
			set
			{
				currentPhase = value;
			}
		}
		
		public bool DrawTouristList
		{
			get
			{
				return drawTouristList;
			}
		}
		
//		public enum TourismPhases
//		{
//			ZeroG,
//			Transition1,
//			Stations,
//			Transition2,
//			Bases,
//			Transition3,
//			Multi
//		}
		
		List<Type> phaseTypes;
		TourismPhase currentPhase;
		
		bool drawTouristList = true;
		
		List<ProtoVessel> existingHotels = new List<ProtoVessel>();

		public override void OnAwake()
		{
			Instance = this;
			phaseTypes = AssemblyLoader.loadedTypes.FindAll(type => type.IsSubclassOf(typeof(TourismPhase)));
			
			foreach (var type in phaseTypes)
			{
				Debug.Log("[SpaceTourism] Loaded Type: " + type.Name);
			}
			
			GameEvents.Contract.onCompleted.Add(new EventData<Contract>.OnEvent(OnContractCompleted));
			GameEvents.onGUIMissionControlSpawn.Add(new EventVoid.OnEvent(OnMCSpawn));
        	GameEvents.onGUIMissionControlDespawn.Add(new EventVoid.OnEvent(OnMCDespawn));
			
			Debug.Log("[SpaceTourism] Contract Manager initialized");
		}

		private void OnDestroy()
		{
			GameEvents.Contract.onCompleted.Remove(new EventData<Contract>.OnEvent(OnContractCompleted));
			GameEvents.onGUIMissionControlSpawn.Remove(new EventVoid.OnEvent(OnMCSpawn));
        	GameEvents.onGUIMissionControlDespawn.Remove(new EventVoid.OnEvent(OnMCDespawn));
			
			Debug.Log("[SpaceTourism] Contract Manager destroyed");
		}
		
		public override void OnSave(ConfigNode node)
		{
			Debug.Log("[SpaceTourism] Saving TourismContractManager to node: " + node.name);
			var nodePhase = node.AddNode("PHASE");
			nodePhase.AddValue("name", currentPhase.GetType().Name);
			currentPhase.Save(nodePhase);
			
			var nodeHotels = node.AddNode("HOTELS");
			foreach (var hotel in existingHotels)
			{
				if (hotel != null)
					nodeHotels.AddValue("hotel", hotel.vesselID);
			}
		}
		
		public override void OnLoad(ConfigNode node)
		{
			if (node.HasNode("PHASE"))
			{
				var nodePhase = node.GetNode("PHASE");
				var phaseName = nodePhase.GetValue("name");
				
				if (string.IsNullOrEmpty(phaseName))
				{
					currentPhase = new TourismPhases.ZeroGFlights(); //TODO: Make configurable
				}
				else
				{
					currentPhase = (TourismPhase)Activator.CreateInstance(phaseTypes.Find(type => type.Name == phaseName));
					currentPhase.Load(nodePhase);
				}
			}
			else
				currentPhase = new TourismPhases.ZeroGFlights(); //TODO: Make configurable
				
			if (node.HasNode("HOTELS"))
			{
				foreach (var hotelID in node.GetNode("HOTELS").GetValues("hotel"))
				{
					existingHotels.Add(HighLogic.CurrentGame.flightState.protoVessels.Find(pvessel => pvessel.vesselID.ToString() == hotelID));
				}
			}
		}
		
		private void OnMCSpawn()
        {
        	drawTouristList = false;
        }
        
        private void OnMCDespawn()
        {
        	drawTouristList = true;
        }
		
		private void OnContractCompleted(Contract contract)
		{
			Debug.Log("[SpaceTourism][ContractManager] Contract of type " + contract.GetType() + " has been completed!");
			if (contract.GetType() == typeof(FinePrint.Contracts.StationContract))
			{
				existingHotels.Add(FlightGlobals.ActiveVessel.protoVessel);
				TourismEvents.onStationCompleted.Fire(FlightGlobals.ActiveVessel.protoVessel);
			}
			else if (contract.GetType() == typeof(FinePrint.Contracts.BaseContract))
			{
				existingHotels.Add(FlightGlobals.ActiveVessel.protoVessel);
				TourismEvents.onBaseCompleted.Fire(FlightGlobals.ActiveVessel.protoVessel);
			}
		}
		
		public ProtoVessel GetAvailableHotel(CelestialBody body, Vessel.Situations situation)
		{
			var basicStations = existingHotels.FindAll(vessel => VesselMeetsRequirements(vessel, body, situation));
			var upgradedStations = basicStations.FindAll(vessel => vessel.protoPartSnapshots.Any(part => part.modules.Any(module => module.moduleName == "TourismModule")));
			
			if (upgradedStations.Count == 0)
				return basicStations.ElementAtOrDefault(UnityEngine.Random.Range(0, basicStations.Count - 1));
			
			return upgradedStations.ElementAtOrDefault(UnityEngine.Random.Range(0, upgradedStations.Count - 1));
		}
		
		private bool VesselMeetsRequirements(ProtoVessel protoVessel, CelestialBody body, Vessel.Situations situation)
		{
			bool vesselHasAntenna = false;
			bool vesselHasPowerGen = false;
			bool vesselHasDockingPort = false;
			
			if (FlightGlobals.Bodies[protoVessel.orbitSnapShot.ReferenceBodyIndex] == body && protoVessel.situation == situation)
			{
				foreach (ProtoPartSnapshot part in protoVessel.protoPartSnapshots)
				{
					vesselHasAntenna |= part.modules.Any(p => p.moduleName == "ModuleDataTransmitter" || p.moduleName == "ModuleLimitedDataTransmitter" || 
					                                        	 p.moduleName == "ModuleRTDataTransmitter" || p.moduleName == "ModuleRTAntenna");
					vesselHasPowerGen |= part.modules.Any(p => p.moduleName == "ModuleGenerator" || p.moduleName == "ModuleDeployableSolarPanel" || p.moduleName == "FNGenerator" || 
					                                         	  p.moduleName == "FNAntimatterReactor" || p.moduleName == "FNNuclearReactor" || p.moduleName == "FNFusionReactor" || 
					                                         	  p.moduleName == "KolonyConverter" || p.moduleName == "FissionGenerator" || p.moduleName == "ModuleCurvedSolarPanel");
					vesselHasDockingPort |= part.modules.Any(p => p.moduleName == "ModuleDockingNode");
					
					if (vesselHasAntenna && vesselHasPowerGen && vesselHasDockingPort)
						return true;
				}
			}
			
			return false;
		}
    }
}