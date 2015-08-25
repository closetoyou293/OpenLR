﻿using GeoAPI.Geometries;
using OsmSharp;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using OpenLR.Referenced.Router;
using OsmSharp.Math.Geo.Simple;
using OsmSharp.Routing.Osm.Graphs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenLR.Referenced.Locations
{
    /// <summary>
    /// Represents a referenced line location with a graph as a reference.
    /// </summary>
    public class ReferencedLine : ReferencedLocation
    {
        /// <summary>
        /// Holds the graph.
        /// </summary>
        private BasicRouterDataSource<LiveEdge> _graph;

        /// <summary>
        /// Creates a new referenced line.
        /// </summary>
        /// <param name="graph"></param>
        public ReferencedLine(BasicRouterDataSource<LiveEdge> graph)
        {
            _graph = graph;
        }

        /// <summary>
        /// Gets or sets the vertices.
        /// </summary>
        public long[] Vertices { get; set; }

        /// <summary>
        /// Gets or sets the edges.
        /// </summary>
        public LiveEdge[] Edges { get; set; }

        /// <summary>
        /// Gets or sets the edge shapes.
        /// </summary>
        public GeoCoordinateSimple[][] EdgeShapes { get; set; }

        /// <summary>
        /// Gets or sets the offset at the beginning of the path representing this location.
        /// </summary>
        public float PositiveOffsetPercentage { get; set; }

        /// <summary>
        /// Gets or sets the offset at the end of the path representing this location.
        /// </summary>
        public float NegativeOffsetPercentage { get; set; }

        /// <summary>
        /// Adds another line location to this one.
        /// </summary>
        /// <param name="location"></param>
        public void Add(ReferencedLine location)
        {
            if(this.Vertices[this.Vertices.Length - 1] == location.Vertices[0])
            { // there is a match.
                // merge vertices.
                var vertices = new long[this.Vertices.Length + location.Vertices.Length - 1];
                this.Vertices.CopyTo(vertices, 0);
                for(int idx = 1; idx < location.Vertices.Length; idx++)
                {
                    vertices[this.Vertices.Length + idx - 1] = location.Vertices[idx];
                }
                this.Vertices = vertices;

                // merge edges.
                var edges = new LiveEdge[this.Edges.Length + location.Edges.Length];
                this.Edges.CopyTo(edges, 0);
                location.Edges.CopyTo(edges, this.Edges.Length);
                this.Edges = edges;
                return;
            }
            throw new Exception("Cannot add a location without them having one vertex incommon.");
        }

        /// <summary>
        /// Converts this referenced location to a geometry.
        /// </summary>
        /// <returns></returns>
        public IGeometry ToGeometry()
        {
            var geometryFactory = new GeometryFactory();

            // build coordinates list.
            var coordinates = new List<Coordinate>();
            for (int idx = 0; idx < this.Vertices.Length; idx++)
            {
                float latitude, longitude;
                _graph.GetVertex(this.Vertices[idx], out latitude, out longitude);
                coordinates.Add(new Coordinate(longitude, latitude));

                if (idx < this.Edges.Length)
                {
                    var edge = this.Edges[idx];
                    var edgeShape = this.EdgeShapes[idx];
                    if (edgeShape != null)
                    {
                        if (edge.Forward)
                        {
                            for (int coordIdx = 0; coordIdx < edgeShape.Length; coordIdx++)
                            {
                                coordinates.Add(new Coordinate()
                                {
                                    X = edgeShape[coordIdx].Longitude,
                                    Y = edgeShape[coordIdx].Latitude
                                });
                            }
                        }
                        else
                        {
                            for (int coordIdx = edgeShape.Length - 1; coordIdx >= 0; coordIdx--)
                            {
                                coordinates.Add(new Coordinate()
                                {
                                    X = edgeShape[coordIdx].Longitude,
                                    Y = edgeShape[coordIdx].Latitude
                                });
                            }
                        }
                    }
                }
            }
            return geometryFactory.CreateLineString(coordinates.ToArray());
        }

        /// <summary>
        /// Converts this referenced location to a geometry.
        /// </summary>
        /// <returns></returns>
        public FeatureCollection ToFeatures()
        {
            var featureCollection = new FeatureCollection();
            var geometryFactory = new GeometryFactory();

            // build coordinates list.
            var coordinates = new List<Coordinate>();
            for (int idx = 0; idx < this.Edges.Length; idx++)
            {
                float latitude, longitude;
                _graph.GetVertex(this.Vertices[idx], out latitude, out longitude);
                coordinates.Add(new Coordinate(longitude, latitude));

                var edge = this.Edges[idx];
                var edgeShape = this.EdgeShapes[idx];
                if (edgeShape != null)
                {
                    for (int coordIdx = 0; coordIdx < edgeShape.Length; coordIdx++)
                    {
                        coordinates.Add(new Coordinate()
                        {
                            X = edgeShape[coordIdx].Longitude,
                            Y = edgeShape[coordIdx].Latitude
                        });
                    }
                }

                var tags = _graph.TagsIndex.Get(edge.Tags);
                var table = tags.ToAttributes();

                _graph.GetVertex(this.Vertices[idx + 1], out latitude, out longitude);
                coordinates.Add(new Coordinate(longitude, latitude));

                featureCollection.Add(new Feature(geometryFactory.CreateLineString(coordinates.ToArray()), table));
                coordinates.Clear();
            }
            return featureCollection;
        }

        #region Validation

        /// <summary>
        /// Validates if the location is connected.
        /// </summary>
        /// <returns></returns>
        public void ValidateConnected(ReferencedEncoderBase mainEncoder)
        {
            var edges = this.Edges;
            var vertices = this.Vertices;

            // 1: Is the path connected?
            // 2: Is the path traversable?
            for (int edgeIdx = 0; edgeIdx < edges.Length; edgeIdx++)
            {
                var from = vertices[edgeIdx];
                var to = vertices[edgeIdx + 1];

                bool found = false;
                foreach (var edge in mainEncoder.Graph.GetEdges(from))
                {
                    if (edge.Key == to &&
                        edge.Value.Equals(edges[edgeIdx]))
                    { // edge was found, is valid.
                        found = true;
                        break;
                    }
                }
                if (!found)
                { // edge is not found, path not connected.
                    throw new ArgumentOutOfRangeException(string.Format("Edge {0} cannot be found between vertex {1} and {2}. The given path is not connected.",
                        edges[edgeIdx].ToInvariantString(), from, to));
                }
                // check whether the edge can traversed.
                var tags = mainEncoder.Graph.TagsIndex.Get(edges[edgeIdx].Tags);
                if (!mainEncoder.Vehicle.CanTraverse(tags))
                { // oeps, cannot be traversed.
                    throw new ArgumentOutOfRangeException(string.Format("Edge at index {0} cannot be traversed by vehicle {1}.", edgeIdx, mainEncoder.Vehicle.UniqueName));
                }
                // check whether the edge can be traversed in the correct direction.
                var oneway = mainEncoder.Vehicle.IsOneWay(tags);
                var canMoveForward = (oneway == null) || (oneway.Value == edges[edgeIdx].Forward);
                if (!canMoveForward)
                { // path cannot be traversed in this direction.
                    throw new ArgumentOutOfRangeException(string.Format("Edge at index {0} cannot be traversed by vehicle {1} in the direction given.", edgeIdx, mainEncoder.Vehicle.UniqueName));
                }
            }
        }

        /// <summary>
        /// Validates the offsets.
        /// </summary>
        public void ValidateOffsets(ReferencedEncoderBase mainEncoder)
        {

        }

        /// <summary>
        /// Validates the location for encoding in binary format.
        /// </summary>
        public void ValidateBinary(ReferencedEncoderBase mainEncoder)
        {

        }

        #endregion

        /// <summary>
        /// Adjusts this location to use valid LR-points.
        /// </summary>
        /// <param name="encoder">The encoder.</param>
        public void AdjustToValidPoints(ReferencedEncoderBase encoder)
        {
            var length = (float)this.Length(encoder).Value;
            var positiveOffsetLength = (this.PositiveOffsetPercentage / 100) * length;
            var negativeOffsetLength = (this.NegativeOffsetPercentage / 100) * length;
            var excludeSet = new HashSet<long>();
            if (!encoder.IsVertexValid(this.Vertices[0]))
            { // from is not valid, try to find a valid point.
                var pathToValid = encoder.FindValidVertexFor(this.Vertices[0], this.Edges[0], this.Vertices[1],
                    excludeSet, false);

                // build edges list.
                if (pathToValid != null)
                { // no path found, just leave things as is.
                    var shortestRoute = encoder.FindShortestPath(this.Vertices[1], pathToValid.Vertex, false);
                    while (shortestRoute != null && !shortestRoute.Contains(this.Vertices[0]))
                    { // the vertex that should be on this shortest route, isn't anymore.
                        // exclude the current target vertex, 
                        excludeSet.Add(pathToValid.Vertex);
                        // calulate a new path-to-valid.
                        pathToValid = encoder.FindValidVertexFor(this.Vertices[0], this.Edges[0], this.Vertices[1],
                            excludeSet, false);
                        if (pathToValid == null)
                        { // a new path was not found.
                            break;
                        }
                        shortestRoute = encoder.FindShortestPath(this.Vertices[1], pathToValid.Vertex, false);
                    }
                    if (pathToValid != null)
                    { // no path found, just leave things as is.
                        var newVertices = pathToValid.ToArray().Reverse().ToList();
                        var newEdges = new List<LiveEdge>();
                        for (int idx = 0; idx < newVertices.Count - 1; idx++)
                        { // loop over edges.
                            var edge = newVertices[idx].Edge;
                            // Next OsmSharp version: use closest.Value.Value.Reverse()?
                            var reverseEdge = new LiveEdge();
                            reverseEdge.Tags = edge.Tags;
                            reverseEdge.Forward = !edge.Forward;
                            reverseEdge.Distance = edge.Distance;

                            edge = reverseEdge;
                            newEdges.Add(edge);
                        }

                        // create new location.
                        var edgesArray = new LiveEdge[newEdges.Count + this.Edges.Length];
                        newEdges.CopyTo(0, edgesArray, 0, newEdges.Count);
                        this.Edges.CopyTo(0, edgesArray, newEdges.Count, this.Edges.Length);
                        var vertexArray = new long[newVertices.Count - 1 + this.Vertices.Length];
                        newVertices.ConvertAll(x => (long)x.Vertex).CopyTo(0, vertexArray, 0, newVertices.Count - 1);
                        this.Vertices.CopyTo(0, vertexArray, newVertices.Count - 1, this.Vertices.Length);

                        this.Edges = edgesArray;
                        this.Vertices = vertexArray;

                        // adjust offset length.
                        var newLength = (float)this.Length(encoder).Value;
                        positiveOffsetLength = positiveOffsetLength + (newLength - length);
                        length = newLength;
                    }
                }
            }
            excludeSet.Clear();
            if (!encoder.IsVertexValid(this.Vertices[this.Vertices.Length - 1]))
            { // from is not valid, try to find a valid point.
                var vertexCount = this.Vertices.Length;
                var pathToValid = encoder.FindValidVertexFor(this.Vertices[vertexCount - 1], this.Edges[
                    this.Edges.Length - 1].ToReverse(), this.Vertices[vertexCount - 2], excludeSet, true);

                // build edges list.
                if (pathToValid != null)
                { // no path found, just leave things as is.
                    var shortestRoute = encoder.FindShortestPath(this.Vertices[vertexCount - 2], pathToValid.Vertex, true);
                    while (shortestRoute != null && !shortestRoute.Contains(this.Vertices[vertexCount - 1]))
                    { // the vertex that should be on this shortest route, isn't anymore.
                        // exclude the current target vertex, 
                        excludeSet.Add(pathToValid.Vertex);
                        // calulate a new path-to-valid.
                        pathToValid = encoder.FindValidVertexFor(this.Vertices[vertexCount - 1], this.Edges[
                            this.Edges.Length - 1].ToReverse(), this.Vertices[vertexCount - 2], excludeSet, true);
                        if (pathToValid == null)
                        { // a new path was not found.
                            break;
                        }
                        shortestRoute = encoder.FindShortestPath(this.Vertices[vertexCount - 2], pathToValid.Vertex, true);
                    }
                    if (pathToValid != null)
                    { // no path found, just leave things as is.
                        var newVertices = pathToValid.ToArray().ToList();
                        var newEdges = new List<LiveEdge>();
                        for (int idx = 1; idx < newVertices.Count; idx++)
                        { // loop over edges.
                            var edge = newVertices[idx].Edge;
                            newEdges.Add(edge);
                        }

                        // create new location.
                        var edgesArray = new LiveEdge[newEdges.Count + this.Edges.Length];
                        this.Edges.CopyTo(0, edgesArray, 0, this.Edges.Length);
                        newEdges.CopyTo(0, edgesArray, this.Edges.Length, newEdges.Count);
                        var vertexArray = new long[newVertices.Count - 1 + this.Vertices.Length];
                        this.Vertices.CopyTo(0, vertexArray, 0, this.Vertices.Length);
                        newVertices.ConvertAll(x => (long)x.Vertex).CopyTo(1, vertexArray, this.Vertices.Length, newVertices.Count - 1);

                        this.Edges = edgesArray;
                        this.Vertices = vertexArray;

                        // adjust offset length.
                        var newLength = (float)this.Length(encoder).Value;
                        negativeOffsetLength = negativeOffsetLength + (newLength - length);
                        length = newLength;
                    }
                }
            }

            // update offset percentags.
            this.PositiveOffsetPercentage = (float)((positiveOffsetLength / length) * 100.0);
            this.NegativeOffsetPercentage = (float)((negativeOffsetLength / length) * 100.0);

            // fill shapes.
            this.EdgeShapes = new GeoCoordinateSimple[this.Edges.Length][];
            for (int i = 0; i < this.Edges.Length; i++)
            {
                this.EdgeShapes[i] = encoder.Graph.GetEdgeShape(
                    this.Vertices[i], this.Vertices[i + 1]);
            }
        }

        /// <summary>
        /// Adjusts this location by inserting intermediate LR-points if needed.
        /// </summary>
        /// 
        public void AdjustToValidDistances(ReferencedEncoderBase encoder, List<int> points)
        {
            this.AdjustToValidDistance(encoder, points, 0);
        }

        /// <summary>
        /// Adjusts the given location by inserting an intermediate LR-point at the point representing pointIdx.
        /// </summary>
        public void AdjustToValidDistance(ReferencedEncoderBase encoder, List<int> points, int start)
        {
            // get start/end vertex.
            var vertexIdx1 = points[start];
            var vertexIdx2 = points[start + 1];
            var count = vertexIdx2 - vertexIdx1 + 1;

            // calculate length to begin with.
            var coordinates = this.GetCoordinates(encoder, vertexIdx1, count);
            var length = coordinates.Length().Value;
            if (length > 15000)
            { // too long.
                // find the best intermediate point.
                var intermediatePoints = new SortedDictionary<double, int>();
                for (int idx = vertexIdx1 + 1; idx < vertexIdx1 + count - 2; idx++)
                {
                    var score = 0.0;
                    if (encoder.IsVertexValid(this.Vertices[idx]))
                    { // a valid vertex is obviously a better choice!
                        score = score + 4096;
                    }

                    // the length is good when close to 15000 but not over.
                    var lengthBefore = this.GetCoordinates(encoder, vertexIdx1, idx - vertexIdx1 + 1).Length();
                    if (lengthBefore.Value < 15000)
                    { // not over!
                        score = score + (1024 * (lengthBefore.Value / 15000));
                    }
                    var lengthAfter = this.GetCoordinates(encoder, idx, count - idx).Length();
                    if (lengthAfter.Value < 15000)
                    { // not over!
                        score = score + (1024 * (lengthAfter.Value / 15000));
                    }

                    // add to sorted dictionary.
                    intermediatePoints[8192 - score] = idx;
                }

                // select the best point and insert it in between.
                var bestPoint = intermediatePoints.First().Value;
                points.Insert(start + 1, bestPoint);

                // test the two distances.
                this.AdjustToValidDistance(encoder, points, start + 1);
                this.AdjustToValidDistance(encoder, points, start);
            }
        }
    }
}