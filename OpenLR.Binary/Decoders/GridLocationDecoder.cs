﻿using OpenLR.Binary.Data;
using OpenLR.Locations;

namespace OpenLR.Binary.Decoders
{
    /// <summary>
    /// A decoder that decodes binary data into a grid location.
    /// </summary>
    public class GridLocationDecoder : BinaryDecoder
    {
        /// <summary>
        /// Decodes the given data into a location reference.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override ILocation Decode(byte[] data)
        {
            // decode box.
            var lowerLeft = CoordinateConverter.Decode(data, 1);
            var upperRight = CoordinateConverter.DecodeRelative(lowerLeft, data, 7);
            
            // decode column/row info.
            var columns = data[11] * 256 + data[12];
            var rows = data[13] * 256 + data[14];

            // create grid location.
            var grid = new GridLocation();
            grid.LowerLeft = lowerLeft;
            grid.UpperRight = upperRight;
            grid.Columns = columns;
            grid.Rows = rows;
            return grid;
        }
    }
}