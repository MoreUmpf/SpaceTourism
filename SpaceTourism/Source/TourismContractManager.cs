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
		
		List<Hotel> existingHotels = new List<Hotel>();
		
		public override void OnAwake()
		{	
			Instance = this;

			GameEvents.Contract.onCompleted.Add(OnContractCompleted);
			GameEvents.onVesselRename.Add(OnVesselRename);
			GameEvents.onGUIMissionControlSpawn.Add(OnMCSpawn);
        	GameEvents.onGUIMissionControlDespawn.Add(OnMCDespawn);
        	
			Debug.Log("[SpaceTourism] Contract Manager initialized");
		}

		private void OnDestroy()
		{
			GameEvents.Contract.onCompleted.Remove(OnContractCompleted);
			GameEvents.onVesselRename.Remove(OnVesselRename);
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
					hotel.Save(nodeHotels);
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
				var values = node.GetNode("HOTELS").values;
				foreach (ConfigNode.Value value in values)
				{
					var result = Hotel.Load(value);
					
					if (result != null)
						existingHotels.Add(result);
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
			if (contract.GetType() == typeof(FinePrint.Contracts.StationContract))
			{
				existingHotels.Add(new Hotel(FlightGlobals.ActiveVessel.protoVessel));
			}
			else if (contract.GetType() == typeof(FinePrint.Contracts.BaseContract))
			{
				existingHotels.Add(new Hotel(FlightGlobals.ActiveVessel.protoVessel));
			}
		}
		
		private void OnVesselRename(GameEvents.HostedFromToAction<Vessel, string> action) // Also triggered when vessel type changed
		{
			var existingHotel = existingHotels.Find(hotel => hotel.ProtoVesselRef == action.host);
			
			if (existingHotel == null)
			{
				if (action.host.vesselType == VesselType.Base || action.host.vesselType == VesselType.Station)
					existingHotels.Add(new Hotel(action.host));
			}
			else
			{
				if (action.host.vesselType != VesselType.Base && action.host.vesselType != VesselType.Station)
					existingHotels.Remove(existingHotel);
			}
		}
		
		public ProtoVessel GetAvailableHotel(List<CelestialBody> bodies, TourismPhase.ContractInfo.ContractRestriction restriction) //TODO: Add support for floating bases (splashed)
		{
        	if (bodies == null)
				return null;
        	
        	var targetBody = bodies[UnityEngine.Random.Range(0, bodies.Count())];
        	
        	var targetSituation = Vessel.Situations.ORBITING;
        	if (restriction == TourismPhase.ContractInfo.ContractRestriction.None)
			{
        		if (UnityEngine.Random.Range(0, 2) == 1)
					targetSituation = Vessel.Situations.LANDED;
			}
        	else if (restriction == TourismPhase.ContractInfo.ContractRestriction.Landed)
        		targetSituation = Vessel.Situations.LANDED;
        	
        	return GetAvailableHotel(targetBody, targetSituation);
		}
		
		public ProtoVessel GetAvailableHotel(CelestialBody body, Vessel.Situations situation)
		{
			var basicHotel = existingHotels.FindAll(vessel => VesselMeetsRequirements(vessel, body, situation));
			var upgradedHotel = basicHotel.FindAll(vessel => vessel.protoPartSnapshots.Any(part => part.modules.Any(module => module.moduleName == "TourismModule")));
			
			if (upgradedStations.Count == 0)
				return basicStations.ElementAtOrDefault(UnityEngine.Random.Range(0, basicStations.Count));
			
			return upgradedStations.ElementAtOrDefault(UnityEngine.Random.Range(0, upgradedStations.Count));
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