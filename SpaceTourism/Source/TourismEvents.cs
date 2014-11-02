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
	public static class TourismEvents
	{
		public static EventData<KerbalTourist, KerbalTourist.KerbalState, KerbalTourist.KerbalState> onTouristStateChange = new EventData<KerbalTourist, KerbalTourist.KerbalState, KerbalTourist.KerbalState>("TouristStateChange");
	}
}
