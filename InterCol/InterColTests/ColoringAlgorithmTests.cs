﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InterCol;
using System.IO;
using System.Text.RegularExpressions;

namespace InterColTests
{
    [TestClass]
    public class ColoringAlgorithmTests
    {
        string _graphPathCommon = "../../GraphExamples/";



        private bool StringEqualToWhitespace(string s1, string s2)
        {
            string normalized1 = Regex.Replace(s1, @"\s", "");
            string normalized2 = Regex.Replace(s2, @"\s", "");

            return String.Equals(
                normalized1,
                normalized2,
                StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void EdgeAlgorithmColoringTests()
        {
            List<List<int>> handCheckedResults =
            new List<List<int>>()
            {
                new List<int>(){5,4,4,5,3},
                new List<int>(){7,6,8,7,6,8,7},
                new List<int>(){6,5,4,4,5,6},
                new List<int>(){5,4,3,2,1},
                null,
                new List<int>(){8,6,7,5,8,6,8,7}
            };

            for (int ii = 1; ii < 4; ii++)
            {
                var graph = UndirectedGraph.Load(_graphPathCommon + "ColorTest" + ii.ToString() + ".txt");
                new EdgeAlgorithm().ColorGraph(graph);
                int colorIndex = 0;
                for (int i = 0; i < graph.AdjacencyMatrix.GetLength(0); i++)
                    for (int j = i + 1; j < graph.AdjacencyMatrix.GetLength(0); j++)
                        if (graph[i, j] == 1)
                        {
                            Assert.IsTrue(graph.ColorMatrix[i, j] == handCheckedResults[ii][colorIndex]);
                            colorIndex++;
                        }
            }
        }
        [TestMethod]
        public void MatchingAlgorithmColoringTests()
        {
            List<List<int>> handCheckedResults =
            new List<List<int>>()
            {
                new List<int>(){10,10,7,9,8,9,8,10,10,9},
                new List<int>(){11,10,11,9,11,10,11,9,11,10,11},
                new List<int>(){6,4,5,6,5,6},
                new List<int>(){9,8,7,9,8,9,7,8,9},
                null,
                new List<int>(){8,7,9,8,7,6,8,7,8}
            };

            for (int ii = 1; ii < 4; ii++)
            {
                var graph = UndirectedGraph.Load(_graphPathCommon + "MatchingColorTest" + ii.ToString() + ".txt");
                new MatchingAlgorithm().ColorGraph(graph);

                int colorIndex = 0;
                for (int i = 0; i < graph.AdjacencyMatrix.GetLength(0); i++)
                    for (int j = i + 1; j < graph.AdjacencyMatrix.GetLength(0); j++)
                        if (graph[i, j] == 1)
                        {
                            Assert.IsTrue(graph.ColorMatrix[i, j] == handCheckedResults[ii][colorIndex]);
                            colorIndex++;
                        }
            }
        }
    }
}