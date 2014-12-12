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
 
namespace SpaceTourism.Contracts
{
	public class SurfaceVacation : Contract
	{
		CelestialBody targetBody = null;
		int numberOfKerbals;
		int numberOfDays = 3;
		
		protected override bool Generate()
		{
			targetBody = GetReachedTarget(false, true);
            if (targetBody == null)
                return false;
            	
            numberOfKerbals = GetNumberOfKerbals();
            if (numberOfKerbals == 0)
                return false;

            this.AddParameter (new KerbalDeaths(), null);

            base.SetExpiry();
            base.SetScience(0f, targetBody);
            base.SetDeadlineYears(1f, targetBody);
            base.SetReputation(150f, 120f, targetBody);
            base.SetFunds(3000f * numberOfKerbals, 10000f * numberOfKerbals, 15000f * numberOfKerbals, targetBody);
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
            return targetBody.bodyName;
        }
        
        protected override string GetTitle()
        {
        	if (numberOfKerbals == 1)
        		return "Fly one Kerbal to vacation on " + targetBody.theName;

        	return "Fly " + numberOfKerbals + " Kerbals to vacation on " + targetBody.theName;
        }
        
        protected override string GetDescription()
        {
            return TextGen.GenerateBackStories (Agent.Name, Agent.GetMindsetString (), "docking", "dock", "kill all humans", new System.Random ().Next());
        }

		protected override string GetNotes()
		{
			return "some random note";
		}
        
        protected override string GetSynopsys()
        {
            if (numberOfKerbals == 1)
        		return "Fly one Kerbal to a " + numberOfDays + " day vacation on " + targetBody.theName;

        	return "Fly " + numberOfKerbals + " Kerbals to a " + numberOfDays + " day vacation on " + targetBody.theName;
        }
        
        protected override string MessageCompleted()
        {
        	if (numberOfKerbals == 1)
        		return "You have succesfully made the dream of one Kerbal come true";

        	return "You have succesfully made the dream of " + numberOfKerbals + " Kerbals come true";
        }

        protected override void OnLoad(ConfigNode node)
        {
            int bodyID = int.Parse(node.GetValue ("targetBody"));
            foreach(var body in FlightGlobals.Bodies)
            {
                if (body.flightGlobalsIndex == bodyID)
                    targetBody = body;
            }
            numberOfKerbals = int.Parse(node.GetValue ("numberOfKerbals"));
            numberOfDays = int.Parse(node.GetValue ("numberOfDays"));
            
        }
        protected override void OnSave(ConfigNode node)
        {
            int bodyID = targetBody.flightGlobalsIndex;
            node.AddValue("targetBody", bodyID);
            node.AddValue("numberOfKerbals", numberOfKerbals);
            node.AddValue("numberOfDays", numberOfDays);
        }

        //for testing purposes
        public override bool MeetRequirements()
        {
            return false;
        }
        
        protected static CelestialBody GetReachedTarget(bool includeKerbin, bool includeSun)
        {
        	var bodies = Contract.GetBodies_Reached(includeKerbin, includeSun);
            if (bodies != null)
            {
            	if (bodies.Count > 0)
            	{
            		return bodies[UnityEngine.Random.Range(0, bodies.Count - 1)];
            	}
            }
            return null;
        }
        
        protected static int GetNumberOfKerbals()
        {
        	var bodies = Contract.GetBodies_Reached(true, true);
        	if (bodies != null)
        		return bodies.Count * (int)Math.Round(UnityEngine.Random.Range(1.0f, 2.6f));
        	
        	return 0;
        }
    }
}