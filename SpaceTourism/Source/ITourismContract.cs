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