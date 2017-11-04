using System;

namespace Halite2.hlt
{
    public class Collision
    {
        /**
         * Test whether a given line segment intersects a circular area.
         *
         * @param start  The start of the segment.
         * @param end    The end of the segment.
         * @param circle The circle to test against.
         * @param fudge  An additional safety zone to leave when looking for collisions. (Probably set it to ship radius 0.5)
         * @return true if the segment intersects, false otherwise
         */
        public static bool SegmentCircleIntersect(Position start, Position end, Entity circle, double fudge)
        {
            // Parameterize the segment as start + t * (end - start),
            // and substitute into the equation of a circle
            // Solve for t
            double circleRadius = circle.GetRadius();
            double startX = start.GetXPos();
            double startY = start.GetYPos();
            double endX = end.GetXPos();
            double endY = end.GetYPos();
            double centerX = circle.GetXPos();
            double centerY = circle.GetYPos();
            double dx = endX - startX;
            double dy = endY - startY;

            double a = Square(dx) + Square(dy);

            double b = -2 * (Square(startX) - (startX * endX)
                                - (startX * centerX) + (endX * centerX)
                                + Square(startY) - (startY * endY)
                                - (startY * centerY) + (endY * centerY));

            if (a == 0.0)
            {
                // Start and end are the same point
                return start.GetDistanceTo(circle) <= circleRadius + fudge;
            }

            // Time along segment when closest to the circle (vertex of the quadratic)
            double t = Math.Min(-b / (2 * a), 1.0);
            if (t < 0)
            {
                return false;
            }

            double closestX = startX + dx * t;
            double closestY = startY + dy * t;
            double closestDistance = new Position(closestX, closestY).GetDistanceTo(circle);

            return closestDistance <= circleRadius + fudge;
        }

        public static double Square(double num)
        {
            return num * num;
        }


        public static bool TwoLineSegmentIntersect(Position firstStart, Position firstEnd, 
            Position secondStart, Position secondEnd, int round)
        {
            return FindIntersection(new Line(firstStart, firstEnd), new Line(secondStart, secondEnd), round);
        }

        public struct Line
        {
            public Line(Position start, Position end)
            {
                X1 = start.X;
                Y1 = start.Y;

                X2 = end.X;
                Y2 = end.Y;
            }

            public double X1 { get; set; }
            public double Y1 { get; set; }

            public double X2 { get; set; }
            public double Y2 { get; set; }
        }

        public static bool FindIntersection(Line lineA, Line lineB, int round)
        {
            var tol = round < 10 ? Constants.SHIP_RADIUS * 2 + 0.02 : 0.00001f;

            double x1 = lineA.X1, y1 = lineA.Y1;
            double x2 = lineA.X2, y2 = lineA.Y2;

            double x3 = lineB.X1, y3 = lineB.Y1;
            double x4 = lineB.X2, y4 = lineB.Y2;

            //equations of the form x=c (two vertical lines)
            if (Math.Abs(x1 - x2) < tol && Math.Abs(x3 - x4) < tol && Math.Abs(x1 - x3) < tol)
            {
                //throw new Exception("Both lines overlap vertically, ambiguous intersection points.");
                //DebugLog.AddLog(round, "Both lines overlap vertically, ambiguous intersection points.");
                return true;
            }

            //equations of the form y=c (two horizontal lines)
            if (Math.Abs(y1 - y2) < tol && Math.Abs(y3 - y4) < tol && Math.Abs(y1 - y3) < tol)
            {
                //throw new Exception("Both lines overlap horizontally, ambiguous intersection points.");
                //DebugLog.AddLog(round, "Both lines overlap horizontally, ambiguous intersection points.");
                return true;
            }

            //equations of the form x=c (two vertical lines)
            if (Math.Abs(x1 - x2) < tol && Math.Abs(x3 - x4) < tol)
            {
                //DebugLog.AddLog(round, "!!! 1");
                return false;
            }

            //equations of the form y=c (two horizontal lines)
            if (Math.Abs(y1 - y2) < tol && Math.Abs(y3 - y4) < tol)
            {
                //DebugLog.AddLog(round, "!!! 2");
                return false;
            }

            //general equation of line is y = mx + c where m is the slope
            //assume equation of line 1 as Y1 = m1x1 + c1 
            //=> -m1x1 + Y1 = c1 ----(1)
            //assume equation of line 2 as Y2 = m2x2 + c2
            //=> -m2x2 + Y2 = c2 -----(2)
            //if line 1 and 2 intersect then X1=X2=x & Y1=Y2=y where (x,y) is the intersection point
            //so we will get below two equations 
            //-m1x + y = c1 --------(3)
            //-m2x + y = c2 --------(4)

            double x, y;

            //lineA is vertical X1 = X2
            //slope will be infinity
            //so lets derive another solution
            if (Math.Abs(x1 - x2) < tol)
            {
                //compute slope of line 2 (m2) and c2
                double m2 = (y4 - y3) / (x4 - x3);
                double c2 = -m2 * x3 + y3;

                //equation of vertical line is x = c
                //if line 1 and 2 intersect then X1=c1=x
                //subsitute x=X1 in (4) => -m2x1 + y = c2
                // => y = c2 + m2x1 
                x = x1;
                y = c2 + m2 * x1;
            }
            //lineB is vertical x3 = x4
            //slope will be infinity
            //so lets derive another solution
            else if (Math.Abs(x3 - x4) < tol)
            {
                //compute slope of line 1 (m1) and c2
                double m1 = (y2 - y1) / (x2 - x1);
                double c1 = -m1 * x1 + y1;

                //equation of vertical line is x = c
                //if line 1 and 2 intersect then x3=c3=x
                //subsitute x=x3 in (3) => -m1x3 + y = c1
                // => y = c1 + m1x3 
                x = x3;
                y = c1 + m1 * x3;
            }
            //lineA & lineB are not vertical 
            //(could be horizontal we can handle it with slope = 0)
            else
            {
                //compute slope of line 1 (m1) and c2
                double m1 = (y2 - y1) / (x2 - x1);
                double c1 = -m1 * x1 + y1;

                //compute slope of line 2 (m2) and c2
                double m2 = (y4 - y3) / (x4 - x3);
                double c2 = -m2 * x3 + y3;

                //solving equations (3) & (4) => x = (c1-c2)/(m2-m1)
                //plugging x value in equation (4) => y = c2 + m2 * x
                x = (c1 - c2) / (m2 - m1);
                y = c2 + m2 * x;

                //verify by plugging intersection point (x, y)
                //in orginal equations (1) & (2) to see if they intersect
                //otherwise x,y values will not be finite and will fail this check
                if (!(Math.Abs(-m1 * x + y - c1) < tol
                    && Math.Abs(-m2 * x + y - c2) < tol))
                {
                    return true;
                }
            }

            //x,y can intersect outside the line segment since line is infinitely long
            //so finally check if x, y is within both the line segments
            if (IsInsideLine(lineA, x, y) &&
                IsInsideLine(lineB, x, y))
            {
                //return new Position() { x = x, y = y };
                return true;
            }

            //return default null (no intersection)
            return false;

        }

        /// <summary>
        /// Returns true if given point(x,y) is inside the given line segment
        /// </summary>
        /// <param name="line"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private static bool IsInsideLine(Line line, double x, double y)
        {
            return ((x >= line.X1 && x <= line.X2)
                        || (x >= line.X2 && x <= line.X1))
                   && ((y >= line.Y1 && y <= line.Y2)
                        || (y >= line.Y2 && y <= line.Y1));
        }

    }
}
