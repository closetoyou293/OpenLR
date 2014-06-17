﻿using OpenLR.Decoding;
using OpenLR.OsmSharp.Decoding;
using OpenLR.Referenced;
using OsmSharp.Routing.Graph;
using OsmSharp.Routing.Graph.Router;
using OsmSharp.Routing.Graph.Router.Dykstra;
using OsmSharp.Routing.Osm.Graphs;
using System;

namespace OpenLR.OsmSharp
{
    /// <summary>
    /// A referenced decoder implementation.
    /// </summary>
    public abstract class ReferencedDecoderBase<TEdge> : OpenLR.Referenced.Decoding.ReferencedDecoder
        where TEdge : IDynamicGraphEdgeData
    {
        /// <summary>
        /// Holds the basic router datasource.
        /// </summary>
        private readonly IBasicRouterDataSource<TEdge> _graph;

        /// <summary>
        /// Holds the referenced circle decoder.
        /// </summary>
        private readonly ReferencedCircleDecoder<TEdge> _referencedCircleDecoder;

        /// <summary>
        /// Holds the referenced geo coordinate decoder.
        /// </summary>
        private readonly ReferencedGeoCoordinateDecoder<TEdge> _referencedGeoCoordinateDecoder;

        /// <summary>
        /// Holds the referenced grid decoder.
        /// </summary>
        private readonly ReferencedGridDecoder<TEdge> _referencedGridDecoder;

        /// <summary>
        /// Holds the referenced line decoder.
        /// </summary>
        private readonly ReferencedLineDecoder<TEdge> _referencedLineDecoder;

        /// <summary>
        /// Holds the referenced point along line decoder.
        /// </summary>
        private readonly ReferencedPointAlongLineDecoder<TEdge> _referencedPointAlongLineDecoder;

        /// <summary>
        /// Holds the referenced polygon decoder.
        /// </summary>
        private readonly ReferencedPolygonDecoder<TEdge> _referencedPolygonDecoder;

        /// <summary>
        /// Holds the referenced rectangle decoder.
        /// </summary>
        private readonly ReferencedRectangleDecoder<TEdge> _referencedRectangleDecoder;

        /// <summary>
        /// Creates a new referenced decoder.
        /// </summary>
        /// <param name="locationDecoder"></param>
        public ReferencedDecoderBase(IBasicRouterDataSource<TEdge> graph, Decoder locationDecoder)
            :base(locationDecoder)
        {
            _graph = graph;

            _referencedCircleDecoder = this.GetReferencedCircleDecoder(_graph);
            _referencedGeoCoordinateDecoder = this.GetReferencedGeoCoordinateDecoder(_graph);
            _referencedGridDecoder = this.GetReferencedGridDecoder(_graph);
            _referencedLineDecoder = this.GetReferencedLineDecoder(_graph);
            _referencedPointAlongLineDecoder = this.GetReferencedPointAlongLineDecoder(_graph);
            _referencedPolygonDecoder = this.GetReferencedPolygonDecoder(_graph);
            _referencedRectangleDecoder = this.GetReferencedRectangleDecoder(_graph);
        }

        /// <summary>
        /// Returns the router.
        /// </summary>
        /// <returns></returns>
        protected abstract IBasicRouter<TEdge> GetRouter();

        /// <summary>
        /// Holds the referenced circle decoder.
        /// </summary>
        protected virtual ReferencedCircleDecoder<TEdge> GetReferencedCircleDecoder(IBasicRouterDataSource<TEdge> graph)
        {
            return new ReferencedCircleDecoder<TEdge>(this.LocationDecoder.CreateCircleLocationDecoder(), graph, this.GetRouter());
        }

        /// <summary>
        /// Holds the referenced geo coordinate decoder.
        /// </summary>
        protected virtual ReferencedGeoCoordinateDecoder<TEdge> GetReferencedGeoCoordinateDecoder(IBasicRouterDataSource<TEdge> graph)
        {
            return new ReferencedGeoCoordinateDecoder<TEdge>(this.LocationDecoder.CreateGeoCoordinateLocationDecoder(), graph, this.GetRouter());
        }

        /// <summary>
        /// Holds the referenced grid decoder.
        /// </summary>
        protected virtual ReferencedGridDecoder<TEdge> GetReferencedGridDecoder(IBasicRouterDataSource<TEdge> graph)
        {
            return new ReferencedGridDecoder<TEdge>(this.LocationDecoder.CreateGridLocationDecoder(), graph, this.GetRouter());
        }

        /// <summary>
        /// Holds the referenced line decoder.
        /// </summary>
        protected virtual ReferencedLineDecoder<TEdge> GetReferencedLineDecoder(IBasicRouterDataSource<TEdge> graph)
        {
            return new ReferencedLineDecoder<TEdge>(this.LocationDecoder.CreateLineLocationDecoder(), graph, this.GetRouter());
        }

        /// <summary>
        /// Holds the referenced point along line decoder.
        /// </summary>
        protected virtual ReferencedPointAlongLineDecoder<TEdge> GetReferencedPointAlongLineDecoder(IBasicRouterDataSource<TEdge> graph)
        {
            return new ReferencedPointAlongLineDecoder<TEdge>(this.LocationDecoder.CreatePointAlongLineLocationDecoder(), graph, this.GetRouter());
        }


        /// <summary>
        /// Holds the referenced polygon decoder.
        /// </summary>
        protected virtual ReferencedPolygonDecoder<TEdge> GetReferencedPolygonDecoder(IBasicRouterDataSource<TEdge> graph)
        {
            return new ReferencedPolygonDecoder<TEdge>(this.LocationDecoder.CreatePolygonLocationDecoder(), graph, this.GetRouter());
        }


        /// <summary>
        /// Holds the referenced rectangle decoder.
        /// </summary>
        protected virtual ReferencedRectangleDecoder<TEdge> GetReferencedRectangleDecoder(IBasicRouterDataSource<TEdge> graph)
        {
            return new ReferencedRectangleDecoder<TEdge>(this.LocationDecoder.CreateRectangleLocationDecoder(), graph, this.GetRouter());
        }

        /// <summary>
        /// Decodes the given data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public override ReferencedLocation Decode(string data)
        {
            if(_referencedCircleDecoder.CanDecode(data))
            {
                return _referencedCircleDecoder.Decode(data);
            }
            if(_referencedGeoCoordinateDecoder.CanDecode(data))
            {
                return _referencedGeoCoordinateDecoder.Decode(data);
            }
            if(_referencedGridDecoder.CanDecode(data))
            {
                return _referencedGridDecoder.Decode(data);
            }
            if(_referencedLineDecoder.CanDecode(data))
            {
                return _referencedLineDecoder.Decode(data);
            }
            if(_referencedPointAlongLineDecoder.CanDecode(data))
            {
                return _referencedPointAlongLineDecoder.Decode(data);
            }
            if(_referencedPolygonDecoder.CanDecode(data))
            {
                return _referencedPolygonDecoder.Decode(data);
            }
            if(_referencedRectangleDecoder.CanDecode(data))
            {
                return _referencedRectangleDecoder.Decode(data);
            }
            throw new ArgumentOutOfRangeException("data",
                string.Format("Data cannot be decode by any of the registered decoders: {0}", data));
        }
    }
}