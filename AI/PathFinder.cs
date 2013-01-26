using System;
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

        public PathFinder(){
            
        }
        public void computeAllPaths(Map map)
        {
            Point currentPoint = Point.Empty, previousPoint = Point.Empty, firstPoint = Point.Empty;
            MapSquare square;
            double pathScore, calculatedSpeed;
            for (int i = 0; i < map.Width; i++)
            {
                //Starting a new row, reset the score
                pathScore = 0;
                calculatedSpeed = 0;
                for (int j = 0; j < map.Height; j++)
                {
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
                        if(firstPoint != Point.Empty){
                            paths.Add(new Path(firstPoint, previousPoint, pathScore/(previousPoint.Y - firstPoint.Y)));
                        }
                        calculatedSpeed = 0;
                        firstPoint = Point.Empty;
                        previousPoint = Point.Empty;
                        continue;
                    }
                    //We have a road tile!
                    //Starting a new segment
                    if (previousPoint == Point.Empty){
                        firstPoint = currentPoint;
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


        List<Point> computeFastestPath(Map map, Point start, Point end)
        {
            //Should never happen but just to be sure
            if (start == end)
            {
                return new List<Point> { start };
            }



            return null;
        }
    }
}
