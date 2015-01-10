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
using SpaceTourism.Contracts;

namespace SpaceTourism.TourismPhases
{
	public class Bases : TourismPhase
	{
		public Bases()
		{
			//...
			
			nextPhase = typeof(Multi);
		}
		
		//UNDONE: Not implemented
	}
}