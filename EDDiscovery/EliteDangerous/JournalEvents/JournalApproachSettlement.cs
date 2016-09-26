﻿using Newtonsoft.Json.Linq;
using System.Linq;

namespace EDDiscovery.EliteDangerous.JournalEvents
{
    //When written: when approaching a planetary settlement
    //Parameters:
    // •	Name

    public class JournalApproachSettlement : JournalEntry
    {
        public JournalApproachSettlement(JObject evt) : base(evt, JournalTypeEnum.ApproachSettlement)
        {
            Name = Tools.GetStringDef(evt["Name"]);

        }
        public string Name { get; set; }

    }
}
