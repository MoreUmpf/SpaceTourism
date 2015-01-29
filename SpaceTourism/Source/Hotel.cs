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
	public class Hotel
	{
		public class Upgrade // Rating for upgrade parts
		{
			public string Name // Name of the part
			{
				get;
				private set;
			}
			
			public int Rating // Rating scaled from 1 to 10
			{
				get;
				private set;
			}
			
			public Upgrade(string name, int rating)
			{	
				Name = name;
				Rating = rating;
			}
		}
		
		public int UpgradeLevel // Level of upgrades on a vessel
		{
			get
			{
				if (upgradeLevel < 0)
					upgradeLevel = AnalyzeUpgrades();
				
				return upgradeLevel;
			}
		}
		
		public ProtoVessel ProtoVesselRef;
		
		public bool Tracked; // Determines if this hotel is being tracked
		
		public CelestialBody targetBody;
		public Vessel.Situations targetSit;

		int upgradeLevel = -1;
		
		public Hotel(ProtoVessel protoVessel)
		{
			this.ProtoVesselRef = protoVessel;
		}
		
		public Hotel(ConfigNode.Value value)
		{
			var hotelData = value.value.Split(", ".ToCharArray());
			var hotel = HighLogic.CurrentGame.flightState.protoVessels.Find(pvessel => pvessel.vesselID.ToString() == hotelData[0]);
					
			if (hotel == null)
				Debug.Log("[SpaceTourism] Hotel with id: " + hotelData[0] + " not found!");
			else
				ProtoVesselRef = hotel;
			
			Tracked = bool.Parse(hotelData[2]);
		}
		
		public static Hotel Load(ConfigNode.Value value)
		{
			var loadedHotel = new Hotel(value);
			
			if (loadedHotel.ProtoVesselRef == null)
				return null;
			return loadedHotel;
		}
		
		public void Save(ConfigNode node)
		{
			node.AddValue("hotel", ProtoVesselRef.vesselID + ", " + Tracked);
		}
		
		public void TrackHotel(CelestialBody body, Vessel.Situations situation)
		{
			if (!Tracked)
			{
				Tracked = true;
				targetBody = body;
				targetSit = situation;
				
				Register();
				SetVesselType();
			}
		}
		
		public void UntrackHotel()
		{
			if (Tracked)
			{
				Unregister();
				Tracked = false;
				upgradeLevel = -1;
			}
		}
		
		public void Register()
		{
			if (Tracked)
			{
				GameEvents.onVesselSituationChange.Add(OnVesselSituationChange);
				GameEvents.onVesselSOIChanged.Add(OnVesselSOIChanged);
				GameEvents.onVesselWasModified.Add(OnVesselWasModified);
			}
		}
		
		public void Unregister()
		{
			if (Tracked)
			{
				GameEvents.onVesselSituationChange.Remove(OnVesselSituationChange);
				GameEvents.onVesselSOIChanged.Remove(OnVesselSOIChanged);
				GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
			}	
		}
		
		public void SetVesselType()
		{
			// Change the vessel type to the detected type
			VesselType resultType;
			if (ProtoVesselRef.landed || ProtoVesselRef.splashed)
				resultType = VesselType.Base;
			else if (ProtoVesselRef.situation == Vessel.Situations.ORBITING)
				resultType = VesselType.Station;
			else
				return;

			ProtoVesselRef.vesselType = resultType;
			if (ProtoVesselRef.vesselRef != null)
				ProtoVesselRef.vesselRef.vesselType = resultType;
		}
		
		public int AnalyzeUpgrades()
		{
			int resultLevel;
			
			if (!BasicPartsPresent())
				return -1;
			
			foreach (ProtoPartSnapshot part in ProtoVesselRef.protoPartSnapshots)
			{
				var hotelUpgrade = Globals.HotelUpgrades.Find(upgrade => upgrade.Name == part.partName);
				
				if (hotelUpgrade != null)
					resultLevel += hotelUpgrade.Rating;
			}
			
			return resultLevel;
		}
		
		private bool BasicPartsPresent()
		{
			bool vesselHasAntenna = false;
			bool vesselHasPowerGen = false;
			bool vesselHasDockingPort = false;
			
			foreach (ProtoPartSnapshot part in ProtoVesselRef.protoPartSnapshots)
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
			
			return false;
		}
		
		private void OnVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> action)
		{
			if (Tracked && action.host.protoVessel == ProtoVesselRef && action.to != targetSit)
				TourismEvents.onHotelUnsuitable.Fire(this);
		}
		
		private void OnVesselSOIChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> action)
		{
			if (Tracked && action.host.protoVessel == ProtoVesselRef && action.to != targetBody)
				TourismEvents.onHotelUnsuitable.Fire(this);
		}
		
		private void OnVesselWasModified(Vessel vessel)
		{
			if (Tracked && vessel.protoVessel == ProtoVesselRef)
			{
				int newUpgradeLevel = AnalyzeUpgrades();
				
				if (newUpgradeLevel < 0)
				{
					upgradeLevel = -1;
					TourismEvents.onHotelUnsuitable.Fire(this);
				}
				else if (newUpgradeLevel != upgradeLevel)
				{
					upgradeLevel = newUpgradeLevel;
					TourismEvents.onHotelUpgradeChange.Fire(this);
				}
			}
		}
	}
}