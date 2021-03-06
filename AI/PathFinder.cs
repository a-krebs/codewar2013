﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlayerCSharpAI.api;
using System.Drawing;

namespace PlayerCSharpAI.AI
{
    class PathFinder
    {
        private static int MAX_SPEED = 6;
        private static int MIN_SPEED = 1;
        protected List<Path> paths = new List<Path>();
        protected double[,] matrix = null;
        Dictionary<int, Point> labels = new Dictionary<int, Point>();
        Dictionary<Point, int> indices = new Dictionary<Point, int>();

        public PathFinder(){
        }
        public void computeAllPaths(Map map)
        {
            Point currentPoint = Point.Empty, previousPoint = Point.Empty, firstPoint = Point.Empty;
            MapSquare square, firstSquare, previousSquare, currentSquare;
            MapSquare square, firstSquare, previousSquare, currentSquare, perpSquare1, perpSquare2;
            double pathScore, calculatedSpeed;
            for (int i = 0; i < map.Width; i++)
            {
                //Starting a new row, reset the score
                pathScore = 1;
                calculatedSpeed = 1;
                for (int j = 0; j < map.Height; j++)
                {
                    firstPoint = new Point(i, j);
                    firstSquare = map.SquareOrDefault(firstPoint);
                    previousPoint = new Point(i, j - 1);
                    previousSquare = map.SquareOrDefault(previousPoint);
                    //Stop on lights, stop signs and after grass breaks
                    if (firstSquare == null || !firstSquare.IsDriveable || (previousSquare.IsDriveable && (!firstSquare.Signal || firstSquare.StopSigns == MapSquare.STOP_SIGNS.NONE)))
                    {
                        continue;
                    }
                    for (int k = j + 1; k < map.Height; k++)
                    {
                        perpSquare1 = map.SquareOrDefault(new Point(i + 1, k));
                        perpSquare2 = map.SquareOrDefault(new Point(Math.Max(0, i - 1), k));
                        previousPoint = new Point(i, k - 1);
                        previousSquare = map.SquareOrDefault(previousPoint);
                        currentPoint = new Point(i, k);
                        currentSquare = map.SquareOrDefault(currentPoint);
                        if (!currentSquare.IsDriveable)
                        {
                            //Reached the end of the path
                            paths.Add(new Path(firstPoint, previousPoint, pathScore / (previousPoint.Y - firstPoint.Y + previousPoint.X - firstPoint.X + 1)));
                            break;
                        }
                        calculatedSpeed = Math.Max(MIN_SPEED, Math.Min(calculatedSpeed + 0.1, MAX_SPEED));
                        pathScore += calculatedSpeed;
                        if (currentSquare.Signal || currentSquare.StopSigns != MapSquare.STOP_SIGNS.NONE || (perpSquare1 != null && perpSquare1.IsDriveable) || (perpSquare2 != null && perpSquare2.IsDriveable))
                        {
                            paths.Add(new Path(firstPoint, currentPoint, pathScore / (currentPoint.Y - firstPoint.Y + currentPoint.X - firstPoint.X)));
                            if (currentSquare.StopSigns == MapSquare.STOP_SIGNS.STOP_NORTH || currentSquare.StopSigns == MapSquare.STOP_SIGNS.STOP_SOUTH)
                            {
                                //Stop sign is the end of a continuous drivable segment
                                break;
                            }
                        }


                    }
                }
            }

            for (int i = 0; i < map.Height; i++)
            {
                //Starting a new row, reset the score
                pathScore = 1;
                calculatedSpeed = 1;
                for (int j = 0; j < map.Width; j++)
                {
                    firstPoint = new Point(j, i);
                    firstSquare = map.SquareOrDefault(firstPoint);
                    previousPoint = new Point(j - 1, i);
                    previousSquare = map.SquareOrDefault(previousPoint);
                    //Stop on lights, stop signs and after grass breaks
                    if (firstSquare == null || !firstSquare.IsDriveable || (previousSquare.IsDriveable && (!firstSquare.Signal || firstSquare.StopSigns == MapSquare.STOP_SIGNS.NONE)))
                    {
                        continue;
                    }
                    for (int k = j + 1; k < map.Width; k++)
                    {
                        perpSquare1 = map.SquareOrDefault(new Point(k, i + 1));
                        perpSquare2 = map.SquareOrDefault(new Point(k, Math.Max(0, i - 1)));
                        previousPoint = new Point(k - 1, i);
                        previousSquare = map.SquareOrDefault(previousPoint);
                        currentPoint = new Point(k, i);
                        currentSquare = map.SquareOrDefault(currentPoint);
                        if (!currentSquare.IsDriveable)
                        {
                            //Reached the end of the path
                            paths.Add(new Path(firstPoint, previousPoint, pathScore / (previousPoint.Y - firstPoint.Y + previousPoint.X - firstPoint.X + 1)));
                            break;
                        }
                        calculatedSpeed = Math.Max(MIN_SPEED, Math.Min(calculatedSpeed + 0.1, MAX_SPEED));
                        pathScore += calculatedSpeed;
                        if (currentSquare.Signal || currentSquare.StopSigns != MapSquare.STOP_SIGNS.NONE || (perpSquare1 != null && perpSquare1.IsDriveable) || (perpSquare2 != null && perpSquare2.IsDriveable))
                        {
                            paths.Add(new Path(firstPoint, currentPoint, pathScore / (currentPoint.Y - firstPoint.Y + currentPoint.X - firstPoint.X)));
                            if (currentSquare.StopSigns == MapSquare.STOP_SIGNS.STOP_EAST || currentSquare.StopSigns == MapSquare.STOP_SIGNS.STOP_WEST)
                            {
                                //Stop sign is the end of a continuous drivable segment
                                break;
                            }
                        }


                    }
                }
            }









            /* ORIGINAL IMPLEMENTATION

                    currentPoint = new Point(i, j);
                    square = map.SquareOrDefault(currentPoint);
                    //Not a road tile or not driveable or IS a stop sign or IS a light
                    //TODO: Fix light logic
                    if (square == null || !square.IsDriveable || (firstPoint != Point.Empty && (square.StopSigns != MapSquare.STOP_SIGNS.NONE || square.Signal)))
                    {
                        if (square.StopSigns != MapSquare.STOP_SIGNS.NONE || square.Signal)
                        {
                            //Hacky fix to intersection overlap
                            previousPoint = currentPoint;
                            MapSquare nextSquare = map.SquareOrDefault(new Point(i, j + 1));
                            if (nextSquare != null && nextSquare.IsDriveable)
                            {
                                j--;
                            }
                        }
                        //We have a path of at least length 1
                        //TODO: Fix when we have a point at 0,0
                        if (firstPoint != Point.Empty)
                        {
                            paths.Add(new Path(firstPoint, previousPoint, pathScore / (previousPoint.Y - firstPoint.Y)));
                        }
                        calculatedSpeed = 0;
                        pathScore = 0;
                        firstPoint = Point.Empty;
                        previousPoint = Point.Empty;
                        continue;
                    }
                    //We have a road tile!
                    //Starting a new segment
                    if (calculatedSpeed == 0)
                    {
                        if (map.SquareOrDefault(previousPoint).IsDriveable)
                        {
                            firstPoint = previousPoint;
                            calculatedSpeed = 1;
                            pathScore = 1;
                        }
                        else
                        {
                            firstPoint = currentPoint;
                        }
                    }
                    calculatedSpeed = Math.Max(MIN_SPEED, Math.Min(calculatedSpeed + 0.1, MAX_SPEED));
                    pathScore += calculatedSpeed;
                    previousPoint = currentPoint;
                }
            }


            for (int i = 0; i < map.Height; i++)
            {
                //Starting a new row, reset the score
                pathScore = 0;
                calculatedSpeed = 0;
                for (int j = 0; j < map.Width; j++)
                {
                    currentPoint = new Point(j, i);
                    square = map.SquareOrDefault(currentPoint);
                    //Not a road tile or not driveable or IS a stop sign or IS a light
                    //TODO: Fix light logic
                    if (square == null || !square.IsDriveable || (firstPoint != Point.Empty && (square.StopSigns != MapSquare.STOP_SIGNS.NONE || square.Signal)))
                    {
                        if (square.StopSigns != MapSquare.STOP_SIGNS.NONE || square.Signal)
                        {
                            //Hacky fix to intersection overlap
                            previousPoint = currentPoint;
                            MapSquare nextSquare = map.SquareOrDefault(new Point(j+1, i));
                            if (nextSquare != null && nextSquare.IsDriveable)
                            {
                                j--;
                            }
                        }
                        //We have a path of at least length 1
                        //TODO: Fix when we have a point at 0,0
                        if (firstPoint != Point.Empty)
                        {
                            paths.Add(new Path(firstPoint, previousPoint, pathScore / (previousPoint.X - firstPoint.X)));
                        }
                        calculatedSpeed = 0;
                        firstPoint = Point.Empty;
                        previousPoint = currentPoint;
                        continue;
                    }
                    //We have a road tile!
                    //Starting a new segment
                    if (calculatedSpeed == 0)
                    {
                        if (map.SquareOrDefault(previousPoint).IsDriveable)
                        {
                            firstPoint = previousPoint;
                            calculatedSpeed = 1;
                            pathScore = 1;
                        }
                        else
                        {
                            firstPoint = currentPoint;
                        }
                    }
                    calculatedSpeed = Math.Max(MIN_SPEED, Math.Min(calculatedSpeed + 0.1, MAX_SPEED));
                    pathScore += calculatedSpeed;
                    previousPoint = currentPoint;

                }
            }
             */
            //Clean up the paths to remove length 1 paths that are in other paths
            paths = (from a in paths orderby a.start.X, a.start.Y select a).ToList();
            IQueryable<Path> path1 = (from a in paths where (a.end.X == a.start.X && a.end.Y == a.start.Y) orderby a.start.X, a.end.X, a.start.Y, a.end.Y select a).AsQueryable();
            IQueryable<Path> pathOver1 = (from a in paths where (a.end.X - a.start.X) + (a.end.Y - a.start.Y) > 0 orderby a.start.X, a.end.X, a.start.Y, a.end.Y select a).AsQueryable();
            foreach (Path p in path1){
          
                foreach (Path p1 in pathOver1)
                {
                    if (p.start.X >= p1.start.X && p.start.X <= p1.end.X && p.start.Y >= p1.start.Y && p.start.Y <= p1.end.Y)
                    {
                        paths.Remove(p);
                    }
                }
            }

        }

        public List<Point> computeFastestPath(Point start, Point end)
        {
            // Should never happen but just to be sure
            if (start == end)
            {
                return new List<Point> { start };
            }

            // get the start point's index
            int startIndex = indices[start];

            // run dijkstra's on the graph
            Dijkstra calculator = new Dijkstra(matrix, startIndex);

            // build list of resulting path
            List<Point> fastestPath = new List<Point>();
            foreach (int nodeIndex in calculator.path)
            {
                fastestPath.Add(labels[nodeIndex]);
            }
            return fastestPath;
        }

        public void generateAdjacencyMatrix()
        {
            // get all nodes
            List<Point> nodes = (from a in paths select a.start).Union(from b in paths select b.end).Distinct().ToList();
            // label them by integer
           
            int index = 0;
            foreach (Point n in nodes)
            {
                labels.Add(index, n);
                indices.Add(n, index);
                index++;
            }
            matrix = new double[labels.Count, labels.Count];

            for (int i = 0; i < labels.Count; i++)
            {
                for (int j = 0; j < labels.Count; j++)
                {
                    matrix[i, j] = WeightOfPath(labels[i], labels[j], paths);
                }
            }
        }

        private double WeightOfPath(Point start, Point end, List<Path> paths)
        {
            foreach (Path path in paths)
            {
                if ((path.start == start && path.end == end) || (path.start == end && path.end == start))
                {
                    return path.pathScore;
                }
            }
            return 0;
        }

        public double DistanceSrcDest(Point start, Point end)
        {
            List<Point> points_list = computeFastestPath(start, end);
            double sum = new double();
            sum = 0;
            for (int i = 0; i < points_list.Count - 1; i++)
            {
                Point path_start = points_list[i];
                Point path_end = points_list[i + 1];

                sum += getPath(path_start, path_end).pathLength;
            }
            return sum;
        }

         /*
         * Gets the time it will take to go along path. Refactored from
         * other methods.
         */
        public double getTimeForPath(List<Point> points_list)
        {
            double sum = 0;
            for (int i=0; i<points_list.Count-1; i++)
            {
                Point start = points_list[i];
                Point end = points_list[i + 1];

                sum += getPath(start, end).pathTime;

            }

            return sum;
        }

        public Path getPath(Point start, Point end) {
             foreach (Path path in paths)
            {
                if ((path.start == start && path.end == end) ||
                    (path.start == end && path.end == start))
                {
                    return path;
                }
            }
            return new Path(start, end, 1);
        }
    }
}
