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
		
		public static List<Hotel.Upgrade> HotelUpgrades
		{
			get
			{
				if (hotelUpgrades == null)
					GetUpgrades();
				
				return hotelUpgrades;
			}
		}
		
		static List<Type> contractTypes;
		static List<Type> phaseTypes;
		static List<Hotel.Upgrade> hotelUpgrades;
		
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
		
		private static void GetUpgrades() //TODO: Make configureable
		{
			hotelUpgrades = new List<Hotel.Upgrade>();
			
			hotelUpgrades.Add(new Hotel.Upgrade("cupola", 5));
			hotelUpgrades.Add(new Hotel.Upgrade("science_module", 2));
			hotelUpgrades.Add(new Hotel.Upgrade("GooExperiment", 1));
		}
	}
	
	public static class TourismEvents
	{
		public static EventData<Hotel> onHotelUpgradeChange = new EventData<Hotel>("HotelUpgradeChange");
		public static EventData<Hotel> onHotelUnsuitable = new EventData<Hotel>("HotelUnsuitable");
	}
	
	//TODO: Add debug log controller
	//TODO: Add config
}

namespace SpaceTourism.Contracts
{
	public interface ITourismContract
	{
		ProtoVessel TargetHotel
		{
			get;
		}
	}
	
	public interface ITourismVacation : ITourismContract //TODO: Create interfaces for upgrade contracts, vacation contracts, more
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
	
	public interface ITourismUpgrade : ITourismContract
	{
		
	}
}