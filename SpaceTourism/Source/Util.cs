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
	
	public static class TourismEvents
	{
		public static EventData<KerbalTourist, KerbalTourist.KerbalState, KerbalTourist.KerbalState> onTouristStateChange = new EventData<KerbalTourist, KerbalTourist.KerbalState, KerbalTourist.KerbalState>("TouristStateChange");
		public static EventData<ProtoVessel> onStationCompleted = new EventData<ProtoVessel>("StationCompleted");
		public static EventData<ProtoVessel> onBaseCompleted = new EventData<ProtoVessel>("BaseCompleted");
	}
}

namespace SpaceTourism.Contracts
{
	public interface ITourismContract
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