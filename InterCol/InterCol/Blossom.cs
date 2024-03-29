﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCol
{
    public class Blossom
    {
        UndirectedGraph _graph;
        List<Edge> _graphEdges;
        public Blossom(UndirectedGraph graph)
        {
            _graph = new UndirectedGraph(graph);
            _graphEdges = _graph.Edges;
        }
        public List<Edge> MaximumMatching()
        {
            if (_graph.Edges.Count == 0)
                return new List<Edge>();
            return MaximumMatching(_graph, new List<Edge>() { _graph.Edges[0]});
        }

        private List<Edge> MaximumMatching(UndirectedGraph graph, List<Edge> currentMatching)
        {
            List<Edge> augmentingPath = FindAugmentingPath(graph, currentMatching);
            if (augmentingPath.Count != 0)
                return MaximumMatching(graph, AugmentAlong(currentMatching, augmentingPath));
            else return currentMatching;
        }

        private List<Edge> AugmentAlong(List<Edge> currentMatching, List<Edge> augmentingPath)
        {
            List<Edge> augmentedMatching = augmentingPath.Intersect(currentMatching,new EdgeComparer()).ToList();
            List<Edge> remainingMatching = currentMatching.Except(augmentingPath, new EdgeComparer()).ToList();

            if (augmentingPath.Count % 2 != 1 || augmentingPath.Except(augmentedMatching, new EdgeComparer()).Count() != augmentedMatching.Count + 1)
                throw new Exception("Invalid augmenting path");
            for (int i = 0; i < augmentingPath.Count; i += 2)
                remainingMatching.Add(augmentingPath[i]);
            return remainingMatching;
        }

        private List<Edge> FindAugmentingPath(UndirectedGraph graph, List<Edge> currentMatching)
        {
            Dictionary<int, UndirectedGraph> forest = new Dictionary<int, UndirectedGraph>();
            List<int> unmarkedV = new List<int>(Enumerable.Range(0, graph.VerticeCount()));
            List<Edge> unmarkedE = graph.Edges.Except(currentMatching, new EdgeComparer()).ToList();
            List<int> exposedV = GetExposedV(graph, currentMatching);
            foreach (int v in exposedV)
                forest[v] = new UndirectedGraph(graph.VerticeCount());
            while (GetEvenDistanceV(forest, unmarkedV) != -1)
            {
                int targetV = GetEvenDistanceV(forest, unmarkedV);
                while (GetUnmarkedE(targetV, unmarkedE) != null)
                {
                    Edge targetE = GetUnmarkedE(targetV, unmarkedE);
                    int secondV = targetE.V1 == targetV ? targetE.V2 : targetE.V1;
                    if (!InForest(forest, secondV))
                    {
                        //secondV is matched, so add its matched edge and targetE to forest[targetV]
                        int matchingV = GetMatchingV(currentMatching, secondV);
                        forest[GetVerticeRoot(forest, targetV)].AddEdge(targetE.V1, targetE.V2);
                        forest[GetVerticeRoot(forest, targetV)].AddEdge(secondV, matchingV);
                    }
                    else
                    {
                        if (!OddForestDistance(forest, secondV))
                        {
                            if (GetVerticeRoot(forest, targetV) != GetVerticeRoot(forest, secondV))
                            {

                                return GetPathFromRoot(forest[GetVerticeRoot(forest, targetV)], GetVerticeRoot(forest, targetV), targetV).
                                    Concat(new List<Edge>() { new Edge(targetV,secondV)}).
                                    Concat(GetPathToRoot(forest[GetVerticeRoot(forest, secondV)], GetVerticeRoot(forest, secondV), secondV)).ToList();
                            }
                            else
                            {
                                //Contract a blossom in G
                                List<Edge> blossom = GetBlossom(targetV, secondV, forest);
                                UndirectedGraph contractedGraph = Contract(graph, blossom, targetV);
                                List<Edge> newMatching = Contract(currentMatching, blossom, targetV);
                                List<Edge> path = FindAugmentingPath(contractedGraph, newMatching);
                                return Lift(path, blossom, targetV, graph, currentMatching);
                            }
                        }
                    }
                    unmarkedE.Remove(targetE);
                }
                unmarkedV.Remove(targetV);
            }
            return new List<Edge>();
        }

        private List<Edge> Lift(List<Edge> path, List<Edge> blossom, int blossomRoot, UndirectedGraph graph, List<Edge> currentMatching)
        {
            if (path.Count == 0) return path;
            List<Edge> blossomPart = null;
            List<Edge> rootNeighbours = path.Where(e => e.V1 == blossomRoot || e.V2 == blossomRoot).ToList();
            int blossomEdgeIndex = path.IndexOf(path.First(e => e.V1 == blossomRoot || e.V2 == blossomRoot));

            if (rootNeighbours.Count == 1)
            {
                List<Edge> unrolledBlossom = UnrollFromRoot(blossom, blossomRoot);
                if (blossomEdgeIndex == 0)
                {
                    int neighbourIndex = rootNeighbours[0].V1 == blossomRoot ? rootNeighbours[0].V2 : rootNeighbours[0].V1;
                    int realBlossomRoot = unrolledBlossom.SelectMany(e => new List<int> { e.V1, e.V2 }).Distinct().First(v => graph[v, neighbourIndex] == 1);
                    List<Edge> realUnrolledBlossom = UnrollFromRoot(blossom, realBlossomRoot);
                    if (currentMatching.Contains(realUnrolledBlossom[realUnrolledBlossom.Count - 1], new EdgeComparer()))
                    {
                        realUnrolledBlossom.Reverse();
                    }
                    return realUnrolledBlossom.Take(blossom.Count - 1).Reverse().
                        Concat(new List<Edge>() { new Edge(realBlossomRoot, neighbourIndex)}).Concat(path.Skip(1)).ToList();
                }
                else
                {
                    return path.Concat(unrolledBlossom.Take(blossom.Count - 1)).ToList();
                }
            }
            else
            {
                if (rootNeighbours.Count != 2) throw new Exception("Invalid neighbours number");

                int liftedRootNeighbour = rootNeighbours.SelectMany(e => new List<int> { e.V1, e.V2 }).Distinct().First(v => v != blossomRoot && graph[v, blossomRoot] == 1);
                int liftedNonRootNeighbour = rootNeighbours.SelectMany(e => new List<int> { e.V1, e.V2 }).Distinct().First(v => v != blossomRoot && v != liftedRootNeighbour);

                List<Edge> unrolledBlossom = UnrollFromRoot(blossom, blossomRoot);

                int blossomNeighbour = unrolledBlossom.SelectMany(e => new List<int> { e.V1, e.V2 }).Distinct().First(v => v != blossomRoot && graph[v, liftedNonRootNeighbour] == 1);
                Edge neighbourBlossomEdge = unrolledBlossom.First(e => e.V1 == blossomNeighbour || e.V2 == blossomNeighbour);

                int distanceV = RootDistance(unrolledBlossom, neighbourBlossomEdge);
                if (distanceV % 2 == 0)
                    blossomPart = unrolledBlossom.Take(distanceV).ToList();
                else
                    blossomPart = unrolledBlossom.Skip(distanceV).Reverse().ToList();

                return path.Take(blossomEdgeIndex).
                    Concat(new List<Edge> { new Edge(liftedRootNeighbour, blossomRoot) }).
                    Concat(blossomPart).
                    Concat(new List<Edge> { new Edge(blossomNeighbour, liftedNonRootNeighbour) }).
                    Concat(path.Skip(blossomEdgeIndex + 2)).ToList();
            }

        }

        private List<Edge> UnrollFromRoot(List<Edge> blossom, int blossomRoot)
        {
            if (blossom[0].V1 == blossomRoot || blossom[0].V2 == blossomRoot)
            {
                if (blossom[1].V1 == blossomRoot || blossom[1].V2 == blossomRoot)
                    return blossom.Skip(1).Concat(blossom.Take(1)).ToList();
                else
                    return blossom;
            }
            for (int i = 1; i<blossom.Count;i++)
            {
                if (blossom[i].V1 == blossomRoot || blossom[i].V2 == blossomRoot)
                    return blossom.Take(i+1).Reverse().Concat(blossom.Skip(i + 1).Reverse()).ToList();
            }
            throw new Exception("Root not in blossom");
        }

        private int RootDistance(List<Edge> blossom, Edge neighbourBlossomEdge)
        {
            return blossom.IndexOf(neighbourBlossomEdge) + 1;
        }

        private List<Edge> Contract(List<Edge> currentMatching, List<Edge> blossom, int contractV)
        {
            List<Edge> matchingCopy = new List<Edge>();

            List<int> toContract = blossom.Select(e => e.V1).Concat(blossom.Select(e => e.V2)).Distinct().Except(new List<int>() { contractV }).ToList();
            foreach (Edge e in currentMatching)
            {
                if (toContract.Contains(e.V1) && toContract.Contains(e.V2))
                    continue;
                if (toContract.Contains(e.V1))
                    matchingCopy.Add(new Edge(contractV, e.V2));
                else if (toContract.Contains(e.V2))
                    matchingCopy.Add(new Edge(contractV, e.V1));
                else
                    matchingCopy.Add(new Edge(e.V1, e.V2));
            }
            return matchingCopy;
        }

        private UndirectedGraph Contract(UndirectedGraph graph, List<Edge> blossom, int contractV)
        {
            UndirectedGraph graphCopy = new UndirectedGraph(graph);
            List<int> toContract = blossom.Select(e => e.V1).Concat(blossom.Select(e => e.V2)).Distinct().Except(new List<int>() { contractV }).ToList();
            for (int i = 0; i < graphCopy.AdjacencyMatrix.GetLength(0); i++)
                foreach (int j in toContract)
                {
                    if (toContract.Contains(i) && graphCopy.AdjacencyMatrix[i,j]==1)
                    {
                        graphCopy.RemoveEdge(i, j);
                    }
                    else if (i != contractV && graphCopy.AdjacencyMatrix[i, j] == 1)
                    {
                        graphCopy.RemoveEdge(i, j);
                        graphCopy.AddEdge(i, contractV);
                    }
                    if (i == contractV && graphCopy.AdjacencyMatrix[i, j] == 1)
                    {
                        graphCopy.RemoveEdge(i, j);
                    }
                }
            return graphCopy;
        }

        private List<Edge> GetBlossom(int targetV, int secondV, Dictionary<int, UndirectedGraph> forest)
        {
            UndirectedGraph targetGraph = forest[GetVerticeRoot(forest, targetV)];
            return GetPathToRoot(targetGraph, targetV, secondV).
                Concat(new List<Edge>() { new Edge(targetV, secondV) }).ToList();
        }
        private List<Edge> GetPathFromRoot(UndirectedGraph undirectedGraph, int root, int targetV)
        {
            return GetPathToRoot(undirectedGraph, targetV, root);
        }

        private List<Edge> GetPathToRoot(UndirectedGraph undirectedGraph, int root, int startV)
        {
            if (startV == root) return new List<Edge>();
            //pair (newVertice,oldVertice)
            bool[,] discovered = new bool[undirectedGraph.AdjacencyMatrix.GetLength(0), undirectedGraph.AdjacencyMatrix.GetLength(0)];
            Queue<KeyValuePair<int, int>> verticeQueue = new Queue<KeyValuePair<int, int>>();
            List<KeyValuePair<int, int>> searchedVertices = new List<KeyValuePair<int, int>>();
            verticeQueue.Enqueue(new KeyValuePair<int, int>(startV, -1));
            while (verticeQueue.Count != 0)
            {
                KeyValuePair<int, int> currentVertice = verticeQueue.Dequeue();
                searchedVertices.Add(currentVertice);
                if (currentVertice.Key == root)
                    return GetPath(startV, root, searchedVertices);
                int newV = currentVertice.Key;
                for (int i = 0; i < undirectedGraph.AdjacencyMatrix.GetLength(0); i++)
                {
                    if (i != newV && undirectedGraph.AdjacencyMatrix[i, newV] == 1)
                        if (!discovered[i, newV])
                        {
                            discovered[i, newV] = discovered[newV, i] = true;
                            verticeQueue.Enqueue(new KeyValuePair<int, int>(i, newV));
                        }
                }
            }
            throw new Exception("Path not found");
        }

        private List<Edge> GetPath(int start, int finish, List<KeyValuePair<int, int>> searchedVertices)
        {
            List<Edge> path = new List<Edge>();
            path.Add(new Edge(finish, searchedVertices.Where(v => v.Key == finish).First().Value));
            while (true)
            {
                KeyValuePair<int, int> edge = searchedVertices.First(v => v.Key == path.First().V2);
                if (edge.Value == -1) break;
                int newV = searchedVertices.First(v => v.Key == path.First().V2).Value;
                path.Insert(0, new Edge(path.First().V2, newV));
                if (newV == start)
                    break;
            }
            return path;
        }

        private int GetVerticeRoot(Dictionary<int, UndirectedGraph> forest, int targetV)
        {
            foreach (var pair in forest)
            {
                int key = pair.Key;
                if (key == targetV) return key;
                UndirectedGraph tree = pair.Value;
                if (tree.VerticeDegree(targetV) > 0)
                    return key;
            }
            return -1;
        }

        private bool OddForestDistance(Dictionary<int, UndirectedGraph> forest, int vertice)
        {
            foreach (var pair in forest)
            {
                int root = pair.Key;
                UndirectedGraph tree = pair.Value;
                try
                {
                    if (vertice == root) return false;
                    int distance = GetPathFromRoot(tree, root, vertice).Count;
                    if (distance % 2 == 1)
                        return true;
                    else
                        return false;
                }
                catch
                {
                }
            }
            throw new Exception("Vertice not in forest");
        }

        private int GetMatchingV(List<Edge> currentMatching, int vertice)
        {
            Edge match = currentMatching.Single(e => e.V1 == vertice || e.V2 == vertice);
            return match.V1 == vertice ? match.V2 : match.V1;
        }

        private bool InForest(Dictionary<int, UndirectedGraph> forest, int secondV)
        {
            foreach (var treePair in forest)
            {
                if (treePair.Key == secondV) return true;
                if (treePair.Value.VerticeDegree(secondV) > 0)
                    return true;
            }
            return false;
        }

        private Edge GetUnmarkedE(int targetV, List<Edge> unmarkedE)
        {
            return unmarkedE.FirstOrDefault(e => e.V1 == targetV || e.V2 == targetV);
        }

        private int GetEvenDistanceV(Dictionary<int, UndirectedGraph> forest, List<int> unmarkedV)
        {
            foreach (int v in unmarkedV)
                foreach (var pair in forest)
                {
                    int root = pair.Key;
                    UndirectedGraph tree = pair.Value;
                    try
                    {
                        if (v == root) return v;
                        int distance = GetPathFromRoot(tree, root, v).Count;
                        if (distance % 2 == 0)
                            return v;
                        else
                            break;
                    }
                    catch
                    {
                    }
                }
            return -1;
        }

        private List<int> GetExposedV(UndirectedGraph graph, List<Edge> currentMatching)
        {
            List<int> exposedV = new List<int>(Enumerable.Range(0, graph.VerticeCount()));
            foreach (Edge e in currentMatching)
            {
                exposedV.Remove(e.V1);
                exposedV.Remove(e.V2);
            }
            return exposedV;
        }
    }
}
