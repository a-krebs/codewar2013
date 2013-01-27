using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlayerCSharpAI.api;
using System.Drawing;

namespace PlayerCSharpAI.AI
{
    class Path
    {
        public Point start { get; protected set; }
        public Point end { get; protected set; }
        public double pathScore { get; protected set; }
        public int pathLength { get; protected set; }
        public double pathTime { get { return pathLength / pathScore; }}

        public Path(Point start, Point end, double pathScore)
        {
            this.start = start;
            this.end = end;
            this.pathScore = pathScore;
            this.pathLength = (end.X - start.X) + (end.Y - start.Y);
        }
        
    }
}
