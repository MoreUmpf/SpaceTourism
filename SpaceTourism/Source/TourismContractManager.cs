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
		
		public TourismPhase CurrentPhase
		{
			get
			{
				return currentPhase;
			}
			set
			{
				TourismPhase.Destroy();
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
		
		TourismPhase currentPhase;
		
		bool drawTouristList = true;
		
		List<ProtoVessel> existingHotels = new List<ProtoVessel>();
		
		public override void OnAwake()
		{	
			Instance = this;

			GameEvents.Contract.onCompleted.Add(OnContractCompleted);
			GameEvents.onGUIMissionControlSpawn.Add(OnMCSpawn);
        	GameEvents.onGUIMissionControlDespawn.Add(OnMCDespawn);
        	
			Debug.Log("[SpaceTourism] Contract Manager initialized");
		}

		private void OnDestroy()
		{
			GameEvents.Contract.onCompleted.Remove(OnContractCompleted);
			GameEvents.onGUIMissionControlSpawn.Remove(OnMCSpawn);
        	GameEvents.onGUIMissionControlDespawn.Remove(OnMCDespawn);
        	
        	TourismPhase.Destroy();
			
			Debug.Log("[SpaceTourism] Contract Manager destroyed");
		}
		
		public override void OnSave(ConfigNode node)
		{
			var nodePhase = node.AddNode("PHASE");
			nodePhase.AddValue("name", currentPhase.GetType().Name);
			TourismPhase.Save(nodePhase);
			
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
					CurrentPhase = new TourismPhases.ZeroGFlights(); //TODO: Make configurable
				}
				else
				{
					CurrentPhase = (TourismPhase)Activator.CreateInstance(Globals.PhaseTypes.Find(type => type.Name == phaseName));
					TourismPhase.Load(nodePhase);
				}
			}
			else
				CurrentPhase = new TourismPhases.ZeroGFlights(); //TODO: Make configurable
			
			TourismPhase.Start();
				
			if (node.HasNode("HOTELS"))
			{
				foreach (var hotelID in node.GetNode("HOTELS").GetValues("hotel"))
				{
					var hotel = HighLogic.CurrentGame.flightState.protoVessels.Find(pvessel => pvessel.vesselID.ToString() == hotelID);
					
					if (hotel == null)
						Debug.Log("[SpaceTourism] Hotel with id: " + hotelID + " not found!");
					else
						existingHotels.Add(hotel);
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
			Debug.Log("[SpaceTourism][ContractManager] Contract of type " + contract.GetType().Name + " has been completed!");
			if (contract.GetType() == typeof(FinePrint.Contracts.StationContract))
			{
				existingHotels.Add(FlightGlobals.ActiveVessel.protoVessel);
			}
			else if (contract.GetType() == typeof(FinePrint.Contracts.BaseContract))
			{
				existingHotels.Add(FlightGlobals.ActiveVessel.protoVessel);
			}
		}
		
		public ProtoVessel GetAvailableHotel(CelestialBody body, Vessel.Situations situation)
		{
			var basicStations = existingHotels.FindAll(vessel => VesselMeetsRequirements(vessel, body, situation));
			var upgradedStations = basicStations.FindAll(vessel => vessel.protoPartSnapshots.Any(part => part.modules.Any(module => module.moduleName == "TourismModule")));
			
			if (upgradedStations.Count == 0)
				return basicStations.ElementAtOrDefault(UnityEngine.Random.Range(0, basicStations.Count));
			
			return upgradedStations.ElementAtOrDefault(UnityEngine.Random.Range(0, upgradedStations.Count));
		}
		
		private bool VesselMeetsRequirements(ProtoVessel protoVessel, CelestialBody body, Vessel.Situations situation)
		{
			bool vesselHasAntenna = false;
			bool vesselHasPowerGen = false;
			bool vesselHasDockingPort = false;
			
			Debug.Log("[SpaceTourism] Checking hotel: body: " + FlightGlobals.Bodies[protoVessel.orbitSnapShot.ReferenceBodyIndex] + " sit: " + protoVessel.situation);
			
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
			
			Debug.Log("[SpaceTourism] Check failed!");
			return false;
		}
    }
}