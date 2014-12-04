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
	public class OrbitVacation : Contract
	{
		public const int MAX_CONTRACTS = 3;
		
		public List<KerbalTourist> kerbalTourists = new List<KerbalTourist>();
		
		public static bool drawTouristList = true; //TODO: Export to Base SpaceTourism Assembly as CurrentScene-variable
		
		CelestialBody targetBody = Planetarium.fetch.Home;
		int numberOfKerbals = 1;
		int numberOfDays = 1;
		
		VacationTime vacationTime;
		RecoverKerbal recoverKerbal;
		TouristDeaths touristDeaths;
		
		ReachDestination reachDestination;
		ReachSituation reachSituation;
		TouristsTogether touristsTogether;
		
		string messageFailure = "[no message found]";
		
		Predicate<KerbalTourist> predicateDead = DeadKerbal;
		Predicate<KerbalTourist> predicateAssigned = AssignedKerbal;
		Predicate<KerbalTourist> predicateAvailable = AvailableKerbal;

		protected override bool Generate()
		{
			var bodies = Contract.GetBodies_Reached(true, true);
        	if (bodies == null)
				return false;	
        	
        	UnityEngine.Random.seed = MissionSeed;
        	numberOfKerbals = bodies.Count * (int)Math.Round(UnityEngine.Random.Range(1f, 2.6f));
        	numberOfDays = (int)Math.Round(7f + UnityEngine.Random.Range(0f, 7f) * GameVariables.Instance.GetContractPrestigeFactor(Prestige));
        	targetBody = bodies[UnityEngine.Random.Range(0, bodies.Count() - 1)];
        	
            vacationTime = (VacationTime)AddParameter(new VacationTime(targetBody, numberOfDays, null), null);
            reachDestination = (ReachDestination)vacationTime.AddParameter(new ReachDestination(targetBody, string.Empty), null);
            reachDestination.DisableOnStateChange = false;
            reachSituation = (ReachSituation)vacationTime.AddParameter(new ReachSituation(Vessel.Situations.ORBITING, string.Empty), null); 
            reachSituation.DisableOnStateChange = false;
            touristsTogether = (TouristsTogether)vacationTime.AddParameter(new TouristsTogether(), null);
            touristsTogether.DisableOnStateChange = false;
            
			recoverKerbal = (RecoverKerbal)AddParameter(new RecoverKerbal("Recover the Tourists"), null); //Bring the Tourists safely back to Kerbin
			recoverKerbal.SetFunds(0f, 4000f);
			recoverKerbal.SetReputation(0f, 10f);
			recoverKerbal.Disable();
			
			touristDeaths = (TouristDeaths)AddParameter(new TouristDeaths(), null);
			touristDeaths.DisableOnStateChange = false;
			touristDeaths.MarkAsComplete();
			touristDeaths.SetFunds(0f, 6000f);
			touristDeaths.SetReputation(0f, 20f);
			
			SetExpiry();
			SetScience(0f, targetBody);
			SetDeadlineYears(1f, targetBody);
			SetReputation(35f * numberOfKerbals * numberOfDays / 7, 32f * numberOfKerbals * numberOfDays / 7, targetBody);
			SetFunds(6000f * numberOfKerbals * numberOfDays / 7, 14000f * numberOfKerbals * numberOfDays / 7, targetBody);  //TODO: Add numberOfDays to calculations
            return true;
        }
		
		protected override void OnAccepted()
		{
			for (int i = 0; i < numberOfKerbals; i++)
			{
				var kerbal = HighLogic.CurrentGame.CrewRoster.GetNewKerbal();
				kerbalTourists.Add(new KerbalTourist(kerbal, KerbalTourist.KerbalState.NotReadyForVacation));
				recoverKerbal.AddKerbal(kerbal.name);
				Debug.Log("[OrbitVacation] Added Kerbaltourist: " + kerbal.name);
			}
			
			
		}
		
		protected override void OnFailed()
		{	
			if (touristDeaths.State == ParameterState.Failed)
			{
				System.Threading.Thread.Sleep(10000);
				if (kerbalTourists.Exists(predicateDead))
				{
					if (kerbalTourists.Exists(predicateAssigned))
					{
						messageFailure = "A Kerbal got killed during his vacation in space!\r\n" +
										 "You need to recover the other Kerbals immediately to prevent additional penalties!";
						//kerbalTourists.FindAll(predicateAssigned))	//<-- recover all Kerbals in this List
						//TODO: Add Recovery Contract for penalty
					    // delay before RecoveryContract: 10sec
					}
					else
					{
						if (kerbalTourists.Exists(predicateAvailable))
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
				if (kerbalTourists.Exists(predicateAssigned))
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
        	if (ContractSystem.Instance.GetCurrentContracts<OrbitVacation>().Count() < MAX_CONTRACTS)
        	{
        		return "OrbitVacation." + ContractSystem.Instance.GetCompletedContracts<OrbitVacation>().Count() 
        						  + "." + ContractSystem.Instance.GetCurrentContracts<OrbitVacation>().Count();
        	}
        	return "OrbitVacation.0.0";
        }
        
        protected override string GetTitle()
        {
        	if (numberOfKerbals == 1)
        		return "Fly one Kerbal to vacation around " + targetBody.theName;

        	return "Fly " + numberOfKerbals + " Kerbals to vacation around " + targetBody.theName;
        }
        
        protected override string GetNotes()
        {
			return "some random notes";
        }
        
        protected override string GetDescription()
        {
            return TextGen.GenerateBackStories (Agent.Name, Agent.GetMindsetString (), "docking", "dock", "kill all humans", MissionSeed);
        }
        
        protected override string GetSynopsys()
        {
            if (numberOfKerbals == 1)
        		return "Fly one Kerbal to a " + numberOfDays + " day vacation in Orbit around " + targetBody.theName;

        	return "Fly " + numberOfKerbals + " Kerbals to a " + numberOfDays + " day vacation in Orbit around " + targetBody.theName;
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

        protected override void OnLoad(ConfigNode node)
        {
        	targetBody = FlightGlobals.Bodies.ElementAt(int.Parse(node.GetValue("targetBody")));
            foreach(var subNode in node.GetNodes("TOURIST"))
            {
            	kerbalTourists.Add(new KerbalTourist(subNode));
            }
            numberOfDays = int.Parse(node.GetValue("numberOfDays"));
            numberOfKerbals = int.Parse(node.GetValue("numberOfKerbals")); 
			vacationTime = (VacationTime)GetParameter(typeof(VacationTime));			
            recoverKerbal = (RecoverKerbal)GetParameter(typeof(RecoverKerbal));
            touristDeaths = (TouristDeaths)GetParameter(typeof(TouristDeaths));
            messageFailure = node.GetValue("messageFailure");
        }
        
        protected override void OnRegister() //TODO: Export to Base SpaceTourism Assembly
        {
        	GameEvents.onGUIMissionControlSpawn.Add(new EventVoid.OnEvent(OnMCSpawn));
        	GameEvents.onGUIMissionControlDespawn.Add(new EventVoid.OnEvent(OnMCDespawn));
        }
        
        protected override void OnUnregister()
        {
        	GameEvents.onGUIMissionControlSpawn.Remove(new EventVoid.OnEvent(OnMCSpawn));
        	GameEvents.onGUIMissionControlDespawn.Remove(new EventVoid.OnEvent(OnMCDespawn));
        }
        
        protected override void OnSave(ConfigNode node)
        {
        	node.AddValue("targetBody", targetBody.flightGlobalsIndex);
            foreach(var kerbal in kerbalTourists)
            {
            	if (kerbal != null)
            	{
            		kerbal.Save(node.AddNode("TOURIST"));
            	}
            }
            node.AddValue("numberOfDays", numberOfDays);
            node.AddValue("numberOfKerbals", numberOfKerbals);
            node.AddValue("messageFailure", messageFailure);
        }

        public override bool MeetRequirements()
        {
			return ProgressTracking.Instance.NodeComplete("Kerbin", "ReturnFromOrbit");
        }
        
        private static bool DeadKerbal(KerbalTourist kerbal)
        {
        	return kerbal.baseProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Dead;
        }
        
        private static bool AssignedKerbal(KerbalTourist kerbal)
        {
        	return kerbal.baseProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Assigned;
        }
        
        private static bool AvailableKerbal(KerbalTourist kerbal)
        {
        	return kerbal.baseProtoCrewMember.rosterStatus == ProtoCrewMember.RosterStatus.Available;
        }
        
        private void OnMCSpawn()
        {
        	OrbitVacation.drawTouristList = false;
        }
        
        private void OnMCDespawn()
        {
        	OrbitVacation.drawTouristList = true;
        }
    }
}