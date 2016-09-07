﻿using OpenLR.Model;
using OsmSharp.Collections.Tags;

namespace OpenLR.Referenced.NWB
{
    /// <summary>
    /// Contains the NWB mapping, mapping NWB attributes to OpenLR FOW and FRC.
    /// </summary>
    public static class NWBMapping
    {
        /// <summary>
        /// Holds the baan sub soort field.
        /// </summary>
        public static string BAANSUBSRT = "BAANSUBSRT";

        /// <summary>
        /// Holds the wegbeheerder soort field.
        /// </summary>
        public static string WEGBEHSRT = "WEGBEHSRT";

        /// <summary>
        /// Holds the wegnummer field.
        /// </summary>
        public static string WEGNUMMER = "WEGNUMMER";

        /// <summary>
        /// Holds the name of the hecto letter field.
        /// </summary>
        public static string HECTOLTTR = "HECTO_LTTR";

        /// <summary>
        /// Holds the name of the rijrichting field.
        /// </summary>
        public static string RIJRICHTNG = "RIJRICHTNG";

        /// <summary>
        /// Maps NWB attributes and translates them to OpenLR FOW and FRC.
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="fow"></param>
        /// <param name="frc"></param>
        /// <returns>False if no valid mapping was found.</returns>
        public static bool ToOpenLR(this TagsCollectionBase tags, out FormOfWay fow, out FunctionalRoadClass frc)
        {
            fow = FormOfWay.Undefined;
            frc = FunctionalRoadClass.Frc7;
            string baansubsrt = string.Empty, wegbeerder = string.Empty, wegnummer = string.Empty, rijrichting = string.Empty, dvkletter_ = string.Empty;
            if (!tags.TryGetValue(NWBMapping.BAANSUBSRT, out baansubsrt) &
                !tags.TryGetValue(NWBMapping.WEGBEHSRT, out wegbeerder) &
                !tags.TryGetValue(NWBMapping.WEGNUMMER, out wegnummer) &
                !tags.TryGetValue(NWBMapping.HECTOLTTR, out dvkletter_) &
                !tags.TryGetValue(NWBMapping.RIJRICHTNG, out rijrichting))
            { // not even a BAANSUBSRT tag!
                // defaults: FRC5, OTHER.
                fow = FormOfWay.Other;
                frc = FunctionalRoadClass.Frc5;
                return true;
            }

            // make sure everything is lowercase.
            char? dvkletter = null; // assume dkv letter is the suffix used for exits etc. see: http://www.wegenwiki.nl/Hectometerpaal#Suffix
            if (!string.IsNullOrWhiteSpace(wegbeerder)) { wegbeerder = wegbeerder.ToLowerInvariant(); }
            if (!string.IsNullOrWhiteSpace(baansubsrt)) { baansubsrt = baansubsrt.ToLowerInvariant(); }
            if (!string.IsNullOrWhiteSpace(wegnummer)) { wegnummer = wegnummer.ToLowerInvariant(); if (!string.IsNullOrEmpty(dvkletter_)) dvkletter = dvkletter_[0]; }
            if (!string.IsNullOrWhiteSpace(rijrichting)) { rijrichting = rijrichting.ToLowerInvariant(); }

            fow = FormOfWay.Other;
            frc = FunctionalRoadClass.Frc5;
            if(wegbeerder == "r")
            {
                if(baansubsrt == "hr")
                {
                    fow = FormOfWay.Motorway;
                    frc = FunctionalRoadClass.Frc0;
                }
                else if(baansubsrt == "nrb" || 
                    baansubsrt == "mrb")
                {
                    fow = FormOfWay.Roundabout;
                    frc = FunctionalRoadClass.Frc0;
                }
                else if(baansubsrt == "pst")
                {
                    if (dvkletter.HasValue)
                    {
                        fow = FormOfWay.SlipRoad;
                        if (dvkletter == 'a' ||
                            dvkletter == 'b' ||
                            dvkletter == 'c' ||
                            dvkletter == 'd')
                        { // r  pst (a|b|c|d)
                            frc = FunctionalRoadClass.Frc3;
                        }
                        else
                        { // r  pst !(a|b|c|d)
                            frc = FunctionalRoadClass.Frc0;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(rijrichting))
                    { // r  pst !(a|b|c|d)
                        fow = FormOfWay.SlipRoad;
                        frc = FunctionalRoadClass.Frc0;
                    }
                }
                else if(baansubsrt == "opr" ||
                    baansubsrt == "afr")
                {
                    frc = FunctionalRoadClass.Frc3;
                    fow = FormOfWay.SlipRoad;
                }
                else if(baansubsrt.StartsWith("vb"))
                {
                    if (!string.IsNullOrWhiteSpace(rijrichting))
                    {
                        fow = FormOfWay.SlipRoad;
                        frc = FunctionalRoadClass.Frc0;
                    }
                }
            }
            else if(wegbeerder == "p")
            {
                if (baansubsrt == "hr")
                {
                    frc = FunctionalRoadClass.Frc3;
                    fow = FormOfWay.MultipleCarriageWay;
                    if(string.IsNullOrWhiteSpace(rijrichting))
                    {
                        frc = FunctionalRoadClass.Frc2;
                        fow = FormOfWay.SingleCarriageWay;
                    }
                }
                else if (baansubsrt == "nrb" ||
                    baansubsrt == "mrb")
                {
                    frc = FunctionalRoadClass.Frc3;
                    fow = FormOfWay.Roundabout;
                }
                else if (baansubsrt == "opr" ||
                    baansubsrt == "afr")
                {
                    frc = FunctionalRoadClass.Frc3;
                    fow = FormOfWay.SlipRoad;
                }
                else if (baansubsrt == "pst" ||
                    baansubsrt.StartsWith("vb"))
                {
                    frc = FunctionalRoadClass.Frc3;
                    fow = FormOfWay.SlipRoad;
                }
            }
            return true;
        }
    }
}