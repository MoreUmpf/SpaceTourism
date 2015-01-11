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
	public class UpgradeHotel : Contract, ITourismContract
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
			if (ContractSystem.Instance.GetCurrentContracts<UpgradeHotel>().Count() >= 
			    TourismContractManager.Instance.CurrentPhase.ContractMaxCounts.GetMaxCount<UpgradeHotel>(false))
				return false;
			
			var bodies = Contract.GetBodies_Reached(true, false);
        	if (bodies == null)
				return false;
        	
        	UnityEngine.Random.seed = MissionSeed;
        	
        	var targetBody = bodies[UnityEngine.Random.Range(0, bodies.Count() - 1)];
        	
        	var targetSituation = Vessel.Situations.ORBITING;
			if (UnityEngine.Random.Range(0, 1) == 1)
			{
				targetSituation = Vessel.Situations.LANDED;
			}
        	
			Debug.Log("[UpgradeHotel Generate] body: " + targetBody.name + ", situation: " + targetSituation.ToString().ToLower());
			targetHotel = TourismContractManager.Instance.GetAvailableHotel(targetBody, targetSituation);
			Debug.Log("[UpgradeHotel Generate] targetHotel retrieved!");
			if (targetHotel == null)
				return false;
			Debug.Log("[UpgradeHotel Generate] targetHotel: " + targetHotel.vesselName);
			
			switch (UnityEngine.Random.Range(0, 1))
			{
				case 0:	// Upgrade Kerbal capacity by 3-5 Kerbals
					targetUpgrade = UnityEngine.Random.Range(3, 5);
					Debug.Log("[UpgradeHotel Generate] targetUpgrade: capacity: " + targetUpgrade);
					break;
				case 1:	// Upgrade Hotel with special parts
					targetUpgrade = GetSpecialParts();
					Debug.Log("[UpgradeHotel Generate] targetUpgrade: SpecialParts");
					break;
			}
			
			if (targetHotel.landed)
				targetHotel.vesselType = VesselType.Base;
			else
				targetHotel.vesselType = VesselType.Station;

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
        	string upgrade;
        	if (targetUpgrade.GetType() == typeof(int))
        		upgrade = "to hold " + targetUpgrade + " more Kerbals";
        	else if (targetUpgrade.GetType() == typeof(List<ProtoPartSnapshot>))
        		upgrade = "with following parts:";
        	else
        		upgrade = "with a space potato! (something went wrong here!)";
        	
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
        		targetUpgrade = node.GetValue("upgradeCapacity");
        		Debug.Log("[UpgradeHotel] Loaded upgradeCapacity: additional capacity: " + targetUpgrade);
        	}
        	else if (node.HasValue("upgradeParts"))
        	{
        		var upgradePartIDs = node.GetValues("upgradeParts");
        		foreach (var ID in upgradePartIDs)
	        	{
        			(targetUpgrade as List<ProtoPartSnapshot>).Add(FlightGlobals.FindProtoPartByID(uint.Parse(ID)));
        			Debug.Log("[UpgradeHotel] Loaded upgradePart: name: " + (targetUpgrade as List<ProtoPartSnapshot>).Last().partName + " ID: " + ID);
	        	}
        	}
        	
        	targetHotel = HighLogic.CurrentGame.flightState.protoVessels.Find(pvessel => pvessel.vesselID.ToString() == node.GetValue("targetHotel"));
        	Debug.Log("[UpgradeHotel] Loaded targetHotel: ID: " + targetHotel.vesselID);
        }

        public override bool MeetRequirements()
        {
			if (TourismContractManager.Instance.CurrentPhase.ContractMaxCounts.GetMaxCount<UpgradeHotel>(false) > 0 && 
        	    ProgressTracking.Instance.NodeComplete("Kerbin", "ReturnFromOrbit"))
        		return true;
        	return false;
        }
        
        private List<ProtoPartSnapshot> GetSpecialParts()
        {
        	return new List<ProtoPartSnapshot>(new [] {targetHotel.protoPartSnapshots.First(), targetHotel.protoPartSnapshots.Last()});
        }
    }
}