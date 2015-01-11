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
	public class SubOrbitalFlight : Contract, ITourismContract
	{
		public List<KerbalTourist> KerbalTourists
		{
			get
			{
				return kerbalTourists;
			}
		}
		
		public int NumberOfKerbals
		{
			get
			{
				return numberOfKerbals;
			}
		}
		
		public double MinApA
		{
			get
			{
				return minApA;
			}
		}
		
		public double MaxApA
		{
			get
			{
				return maxApA;
			}
		}
		
		List<KerbalTourist> kerbalTourists = new List<KerbalTourist>();
		int numberOfKerbals;
		double minApA;
		double maxApA;
		
		ZeroG zeroG;
		RecoverKerbal recoverKerbal;
		TouristDeaths touristDeaths;
		
		ReachSituation reachSituation;
		ReachApoapsis reachApoapsis;
		TouristsTogether touristsTogether;
		
		string messageFailure = "[no message found]";

		protected override bool Generate()
		{
			if (ContractSystem.Instance.GetCurrentContracts<SubOrbitalFlight>().Count() >= 
			    TourismContractManager.Instance.CurrentPhase.ContractMaxCounts.GetMaxCount<SubOrbitalFlight>(false))
				return false;
			
        	UnityEngine.Random.seed = MissionSeed;
        	numberOfKerbals = (int)Math.Round(UnityEngine.Random.Range(1f, 2.6f));
        	minApA = Math.Round(UnityEngine.Random.Range(100000f, 150000f));
        	maxApA = minApA + 5000;
        	
            zeroG = (ZeroG)AddParameter(new ZeroG(), null);
            reachSituation = (ReachSituation)zeroG.AddParameter(new ReachSituation(Vessel.Situations.SUB_ORBITAL, string.Empty), null); 
            reachSituation.DisableOnStateChange = false;
            reachApoapsis = (ReachApoapsis)zeroG.AddParameter(new ReachApoapsis(), null);
            reachApoapsis.DisableOnStateChange = false;
            touristsTogether = (TouristsTogether)zeroG.AddParameter(new TouristsTogether(), null);
            touristsTogether.DisableOnStateChange = false;
            
			recoverKerbal = (RecoverKerbal)AddParameter(new RecoverKerbal("Recover the Tourists", RecoverKerbal.CompleteCondition.All, RecoverKerbal.CompleteCondition.Any), null); //Bring the Tourists safely back to Kerbin
//			recoverKerbal.SetFunds(0f, 4000f);
//			recoverKerbal.SetReputation(0f, 10f);
			recoverKerbal.Disable();
			
			touristDeaths = (TouristDeaths)AddParameter(new TouristDeaths(), null);
			touristDeaths.DisableOnStateChange = false;
			touristDeaths.SetComplete();
			touristDeaths.SetFunds(0f, 6000f);
			touristDeaths.SetReputation(0f, 20f);
			
			SetExpiry();
			SetScience(0f, Planetarium.fetch.Home);
			SetDeadlineYears(1f, Planetarium.fetch.Home);
			SetReputation(35f * numberOfKerbals, 32f * numberOfKerbals, Planetarium.fetch.Home);
			SetFunds(6000f * numberOfKerbals, 14000f * numberOfKerbals, Planetarium.fetch.Home);
            return true;
        }
		
		protected override void OnAccepted()
		{
			for (int i = 0; i < numberOfKerbals; i++)
			{
				var kerbal = HighLogic.CurrentGame.CrewRoster.GetNewKerbal();
				kerbalTourists.Add(new KerbalTourist(kerbal, KerbalTourist.KerbalState.NotReadyForVacation));
				recoverKerbal.AddKerbal(kerbal.name);
			}
			
			
		}
		
		protected override void OnFailed()
		{	
			if (touristDeaths.State == ParameterState.Failed)
			{
				System.Threading.Thread.Sleep(10000);
				if (kerbalTourists.Any(kerbal => kerbal.baseProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Dead))
				{
					if (kerbalTourists.Any(kerbal => kerbal.baseProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Assigned))
					{
						messageFailure = "A Kerbal got killed during his vacation in space!\r\n" +
										 "You need to recover the other Kerbals immediately to prevent additional penalties!";
						//kerbalTourists.FindAll(predicateAssigned))	//<-- recover all Kerbals in this List
						//TODO: Add Recovery Contract for penalty
					    // delay before RecoveryContract: 10sec
					}
					else
					{
						if (kerbalTourists.Any(kerbal => kerbal.baseProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Available))
							messageFailure = "A Kerbal got killed during his vacation in space!\r\n" +
											 "Kerbals all around Kerbin are now scared of space travel.\r\n" +
											 "You won't be able to bring Kerbals to their space vacation for a while.";
						else
							messageFailure = "You killed a whole group of Kerbals during their vacation in space!\r\n" +
											 "Kerbals all around Kerbin are now scared of space travel.\r\n" +
											 "You won't be able to bring Kerbals to their space vacation for a while.";
					}
				}
			}
			else
			{
				if (kerbalTourists.Any(kerbal => kerbal.baseProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Assigned))
				{
					messageFailure = "You failed at flying your Kerbals to their vacation!\r\n" +
									 "You need to recover the remaining Kerbals immediately to prevent additional penalties!";
					//kerbalTourists.FindAll(predicateAssigned))	//<-- recover all Kerbals in this List
					//TODO: Add Recovery Contract for penalty
					//no delay!
				}
				else
				{
					messageFailure = "You failed at flying your Kerbals to their vacation!";
				}
			}
			
			foreach(var kerbal in kerbalTourists)
			{
				HighLogic.CurrentGame.CrewRoster.Remove(kerbal.baseProtoCrewMember.name);
			}
			kerbalTourists.Clear();
		}

		protected override void OnCancelled()
		{
			foreach(var kerbal in kerbalTourists)
			{
				kerbal.baseProtoCrewMember.type = ProtoCrewMember.KerbalType.Unowned; //FIXME: Spams save with Unowned Kerbals after a while
				// Dont remove them from the CrewRoster because if Tourist completes ProgressTracking-Node the ProgressTracking-Loader will look for the removed Kerbal and gives error
				// HighLogic.CurrentGame.CrewRoster.Remove(kerbal.baseProtoCrewMember.name);
			}
			kerbalTourists.Clear();
		}
		
		protected override void OnCompleted()
		{
			foreach(var kerbal in kerbalTourists)
			{
				kerbal.baseProtoCrewMember.type = ProtoCrewMember.KerbalType.Unowned; //FIXME: Spams save with Unowned Kerbals after a while
				// HighLogic.CurrentGame.CrewRoster.Remove(kerbal.baseProtoCrewMember.name);
			}
			kerbalTourists.Clear();
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
        	if (numberOfKerbals == 1)
        		return "Fly a Kerbal on a suborbital trajectory over Kerbin";

        	return "Fly " + numberOfKerbals + " Kerbals on a suborbital trajectory over Kerbin";
        }
        
        protected override string GetNotes()
        {
			return "If the flight is interrupted, a penalty contract for rescuing the remaining tourists will be imposed upon you!";
        }
        
        protected override string GetDescription()
        {
            return TextGen.GenerateBackStories (Agent.Name, Agent.GetMindsetString (), "docking", "dock", "kill all humans", MissionSeed);
        }
        
        protected override string GetSynopsys()
        {
            if (numberOfKerbals == 1)
        		return "Fly a Kerbal on a suborbital trajectory over Kerbin";

        	return "Fly " + numberOfKerbals + " Kerbals on a suborbital trajectory over Kerbin";
        }
        
        protected override string MessageFailed()
        {
        	return messageFailure;
        }
        
        protected override string MessageCompleted()
        {
        	if (numberOfKerbals == 1)
        		return "You have succesfully made the dream of one Kerbal come true";

        	return "You have succesfully made the dream of " + numberOfKerbals + " Kerbals come true";
        }
               
        protected override void OnSave(ConfigNode node)
        {
            foreach(var kerbal in kerbalTourists)
            {
            	if (kerbal != null)
            		kerbal.Save(node.AddNode("TOURIST"));
            }
            
            node.AddValue("numberOfKerbals", numberOfKerbals);
            node.AddValue("minApA", minApA);
            node.AddValue("maxApA", maxApA);
            node.AddValue("messageFailure", messageFailure);
        }

        protected override void OnLoad(ConfigNode node)
        {
            foreach(var subNode in node.GetNodes("TOURIST"))
            {
            	kerbalTourists.Add(new KerbalTourist(subNode));
            }
            numberOfKerbals = int.Parse(node.GetValue("numberOfKerbals"));
            minApA = double.Parse(node.GetValue("minApA"));
            maxApA = double.Parse(node.GetValue("maxApA"));
			zeroG = (ZeroG)GetParameter(typeof(ZeroG));		
            recoverKerbal = (RecoverKerbal)GetParameter(typeof(RecoverKerbal));
            touristDeaths = (TouristDeaths)GetParameter(typeof(TouristDeaths));
            messageFailure = node.GetValue("messageFailure");
        }

        public override bool MeetRequirements()
        {
        	if (TourismContractManager.Instance.CurrentPhase.ContractMaxCounts.GetMaxCount<SubOrbitalFlight>(false) > 0 && 
        	    ProgressTracking.Instance.NodeComplete("Kerbin", "ReturnFromOrbit"))
        		return true;
        	return false;
        }

        
    }
}