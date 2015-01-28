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
	public static class Globals
	{
		public static List<Type> ContractTypes
		{
			get
			{
				if (contractTypes == null)
					GetTypes();
				
				return contractTypes;
			}
		}
		
		public static List<Type> PhaseTypes
		{
			get
			{
				if (phaseTypes == null)
					GetTypes();
				
				return phaseTypes;
			}
		}
		
		static List<Type> contractTypes;
		static List<Type> phaseTypes;
		
		private static void GetTypes()
		{
			contractTypes = new List<Type>();
			phaseTypes = new List<Type>();
			
			foreach (var loadedAssembly in AssemblyLoader.loadedAssemblies)
			{
				var types = loadedAssembly.assembly.GetTypes();
				phaseTypes.AddRange(types.Where(type => type.IsSubclassOf(typeof(TourismPhase))));
				contractTypes.AddRange(types.Where(type => type.GetInterface("ITourismContract") != null));
			}
		}
	}
	
	//TODO: Add debug log controller
}

namespace SpaceTourism.Contracts
{
	public interface ITourismContract //TODO: Create interfaces for upgrade contracts, vacation contracts, more
	{
		List<KerbalTourist> KerbalTourists
		{
			get;
		}
		
		int NumberOfKerbals
		{
			get;
		}
	}
}