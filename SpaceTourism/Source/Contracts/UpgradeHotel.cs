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
using SpaceTourism.Contracts.Parameters;
 
namespace SpaceTourism.Contracts
{
	public class UpgradeHotel : Contract, ITourismContract //TODO: Make seperate contracts inheriting from this for different upgrades
	{
		public ProtoVessel TargetHotel
		{
			get
			{
				return targetHotel;
			}
		}
		
		public object TargetUpgrade
		{
			get
			{
				return targetUpgrade;
			}
		}
		
		public List<KerbalTourist> KerbalTourists { // For compatibility with other tourism contracts
			get {
				return new List<KerbalTourist>();
			}
		}

		public int NumberOfKerbals { // For compatibility with other tourism contracts
			get {
				return 0;
			}
		}
		
		ProtoVessel targetHotel;
		object targetUpgrade;

		protected override bool Generate()
		{
			var contractInfo = TourismPhase.ContractInfos.Find(info => info.Type == typeof(UpgradeHotel));
			
			if (contractInfo == null)
				return false;
			
			var currentContracts = ContractSystem.Instance.GetCurrentContracts<UpgradeHotel>();
			
			if (currentContracts.Length >= contractInfo.OverallCount)
				return false;
			
			if (currentContracts.Count(contract => contract.prestige == prestige) >= contractInfo.protoMaxCounts[prestige])
				return false;
			
			var bodies = Contract.GetBodies_Reached(true, false);
        	if (bodies == null)
				return false;
        	
        	var targetBody = bodies[UnityEngine.Random.Range(0, bodies.Count())];
        	
        	var targetSituation = Vessel.Situations.ORBITING;
        	if (contractInfo.Restriction == TourismPhase.ContractInfo.ContractRestriction.None)
			{
        		if (UnityEngine.Random.Range(0, 2) == 1)
					targetSituation = Vessel.Situations.LANDED;
			}
        	else if (contractInfo.Restriction == TourismPhase.ContractInfo.ContractRestriction.Landed)
        		targetSituation = Vessel.Situations.LANDED;
        	
			targetHotel = TourismContractManager.Instance.GetAvailableHotel(targetBody, targetSituation);
			
			if (targetHotel == null)
				return false;
			
			switch (UnityEngine.Random.Range(0, 2))
			{
				case 0:	// Upgrade Kerbal capacity by 3-5 Kerbals
					targetUpgrade = UnityEngine.Random.Range(3, 6);
					Debug.Log("[UpgradeHotel Generate] targetUpgrade: capacity: " + targetUpgrade);
					break;
				case 1:	// Upgrade Hotel with special parts
					targetUpgrade = GetSpecialParts();
					Debug.Log("[UpgradeHotel Generate] targetUpgrade: SpecialParts");
					break;
			}
			
			// Change the vessel type to the detected type
			VesselType resultType = VesselType.Station;
			if (targetHotel.landed)
				resultType = VesselType.Base;
			
			var vesselRef = targetHotel.vesselRef;
			targetHotel.vesselType = resultType;
			if (vesselRef != null)
				vesselRef.vesselType = resultType;

			SetExpiry();
			SetScience(0f, targetBody);
			SetDeadlineYears(1f, targetBody);
			SetReputation(100f, 150f, targetBody);
			SetFunds(10000f, 15000f, targetBody);
            return true;
        }
		
        public override bool CanBeCancelled()
        {
            return true;
        }
        
        public override bool CanBeDeclined()
        {
            return true;
        }

        protected override string GetHashString()
        {
        	return MissionSeed + DateAccepted.ToString();
        }
        
        protected override string GetTitle()
        {
        	return "Upgrade your " + FlightGlobals.Bodies[targetHotel.orbitSnapShot.ReferenceBodyIndex].name + "-" + targetHotel.vesselType + " '" + targetHotel.vesselName + "'";
        }
        
        protected override string GetNotes()
        {
        	if (targetUpgrade.GetType() == typeof(List<ProtoPartSnapshot>))
        	{
        		string parts = string.Empty;
        		foreach (ProtoPartSnapshot part in (targetUpgrade as List<ProtoPartSnapshot>))
        		{
        			if (parts != string.Empty)
        				parts += "\r\n";
        			
        			parts += "- " + part.partName;
        		}
        		return parts;
        	}
			return string.Empty;
        }
        
        protected override string GetDescription()
        {
            return TextGen.GenerateBackStories (Agent.Name, Agent.GetMindsetString (), "docking", "dock", "kill all humans", MissionSeed);
        }
        
        protected override string GetSynopsys()
        {
        	string upgrade = "with a space potato! (something went wrong here!)";
        	if (targetUpgrade.GetType() == typeof(int))
        		upgrade = "to hold " + targetUpgrade + " more Kerbals";
        	else if (targetUpgrade.GetType() == typeof(List<ProtoPartSnapshot>))
        		upgrade = "with following parts:";
        	
        	return "Upgrade your " + FlightGlobals.Bodies[targetHotel.orbitSnapShot.ReferenceBodyIndex].name + "-" + targetHotel.vesselType + " '" + targetHotel.vesselName + "' " + upgrade;
        }
        
//        protected override string MessageFailed()
//        {
//        }
        
        protected override string MessageCompleted()
        {
        	return "You have succesfully upgraded your " + FlightGlobals.Bodies[targetHotel.orbitSnapShot.ReferenceBodyIndex].name + "-" + targetHotel.vesselType + " '" + targetHotel.vesselName + "'!";
        }
        
        protected override void OnSave(ConfigNode node)
        {
			if (targetUpgrade.GetType() == typeof(int))
			{
        		node.AddValue("upgradeCapacity", targetUpgrade);
        		Debug.Log("[UpgradeHotel] Saved upgradeCapacity: additional capacity: " + targetUpgrade);
			}
			else if (targetUpgrade.GetType() == typeof(List<ProtoPartSnapshot>))
			{
        		foreach (ProtoPartSnapshot part in (targetUpgrade as List<ProtoPartSnapshot>))
	        	{
	        		node.AddValue("upgradeParts", part.flightID);
	        		Debug.Log("[UpgradeHotel] Saved upgradePart: name: " + part.partName + " ID: " + part.flightID);
	        	}
			}
			else 
        		Debug.Log("[UpgradeHotel] Unable to save target upgrade!");
        	
            node.AddValue("targetHotel", targetHotel.vesselID);
            Debug.Log("[UpgradeHotel] Saved targetHotel: ID: " + targetHotel.vesselID);
        }

        protected override void OnLoad(ConfigNode node)
        {
        	if (node.HasValue("upgradeCapacity"))
        	{
        		targetUpgrade = int.Parse(node.GetValue("upgradeCapacity"));
        		Debug.Log("[UpgradeHotel] Loaded upgradeCapacity: additional capacity: " + targetUpgrade);
        	}
        	else if (node.HasValue("upgradeParts"))
        	{
        		var upgradePartIDs = node.GetValues("upgradeParts");
        		List<ProtoPartSnapshot> targetParts = new List<ProtoPartSnapshot>();
        		foreach (var ID in upgradePartIDs)
	        	{
        			targetParts.Add(FlightGlobals.FindProtoPartByID(uint.Parse(ID)));
        			Debug.Log("[UpgradeHotel] Loaded upgradePart: name: " + targetParts.Last().partName + " ID: " + ID);
	        	}
        		targetUpgrade = targetParts;
        	}
        	
        	var hotelID = node.GetValue("targetHotel");
        	targetHotel = HighLogic.CurrentGame.flightState.protoVessels.Find(pvessel => pvessel.vesselID.ToString() == hotelID);
        	Debug.Log("[UpgradeHotel] Loaded targetHotel: ID: " + targetHotel.vesselID);
        }

        public override bool MeetRequirements()
        {
			if (ProgressTracking.Instance.NodeComplete("Kerbin", "ReturnFromOrbit"))
        		return true;
        	return false;
        }
        
        private List<ProtoPartSnapshot> GetSpecialParts()
        {
        	return new List<ProtoPartSnapshot>(new [] {targetHotel.protoPartSnapshots.First(), targetHotel.protoPartSnapshots.Last()});
        }
    }
}