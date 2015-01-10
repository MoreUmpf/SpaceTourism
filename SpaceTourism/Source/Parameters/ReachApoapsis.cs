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

namespace SpaceTourism.Contracts.Parameters
{
	public class ReachApoapsis : ContractParameter
	{	
        protected override string GetHashString()
        {
        	return null;
        }
        
        protected override string GetTitle()
        {
        	return "Apoapsis from " + (Parent.Parent as SubOrbitalFlight).MinApA.ToString("n0") + "m to " + (Parent.Parent as SubOrbitalFlight).MaxApA.ToString("n0") + "m";
        }
        
        protected override void OnUpdate()
        {
        	var touristSeat = (Parent.Parent as SubOrbitalFlight).KerbalTourists.First().baseProtoCrewMember.seat;
        	if (touristSeat == null)
        		return;
        	
        	if (touristSeat.vessel.GetOrbit().ApA >= (Parent.Parent as SubOrbitalFlight).MinApA && touristSeat.vessel.GetOrbit().ApA <= (Parent.Parent as SubOrbitalFlight).MaxApA)
        		SetComplete();
        	else
        		SetIncomplete();
        }
	}
}