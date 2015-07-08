using System;
using System.Windows;

namespace rChordata
{
    /// <summary>
    ///     DiamondToolbox is the main class in the DiamondToolbox DLL.  This
    ///     is the class that contains all the functionality to convert
    ///     Cartesian coordinates to Diamond coordinates.
    /// </summary>
    public class DiamondToolbox
    {
        /// <summary>
        ///     CartesianToDiamond is the main method in DiamondToolbox class.
        ///     This method converts a Cartesian coordinate into a Diamond coordinate.
        /// </summary>
        /// <param name="cartPoint">Cartesian coordiant in DoublePoint type</param>
        /// <param name="radius">Maximum Cartesian value positive or negative on either axis in double type</param>
        /// <returns>Diamond coordinate in DiamondPoint type</returns>
        public static DiamondPoint CartesianToDiamond(DoublePoint cartPoint, double radius)
        {
            DoublePoint[] diamondPolygon =
            {
                new DoublePoint(0, radius),
                new DoublePoint(radius, 0),
                new DoublePoint(0, -radius),
                new DoublePoint(-radius, 0)
            };

            // If the Cartesian point is outside of the diamond ...
            if (!IsInsidePolygon(diamondPolygon, cartPoint))
                // If the Cartesian point is on the Y axis ...
                if (cartPoint.X == 0)
                    // If the Cartesian point if North of the Diamond ...
                    if (cartPoint.Y > 0)
                        // Bring it back to the North most point
                        cartPoint.Y = radius;
                    else // If the Cartesian point is South of the Diamond ...
                        // Bring it back to the South most point
                        cartPoint.Y = -radius;
                // If the Cartesian point is on the X axis ...
                else if (cartPoint.Y == 0)
                    // If the Cartesian point if East of the Diamond ...
                    if (cartPoint.X > 0)
                        // Bring it back to the East most point
                        cartPoint.X = radius;
                    else // If the Cartesian point is West of the Diamond ...
                        // Bring it back to the West most point
                        cartPoint.X = -radius;
                else
                    // If the Cartesian point is in the Northeast ...
                    if (cartPoint.X > 0 && cartPoint.Y > 0)
                    // Move the Cartesian point to the diamond in
                    //   the direction of the center
                    //   This could throw an Exception, but it shouldn't
                    cartPoint = GetIntersection(
                        cartPoint, // line from test point
                        new DoublePoint(0, 0), //   to center
                        new DoublePoint(0, radius), // Line from North point
                        new DoublePoint(radius, 0)); //   to East point
                                                     // If the Cartesian point is in the Southeast ...
                else if (cartPoint.X > 0 && cartPoint.Y < 0)
                    // Move the Cartesian point to the diamond in
                    //   the direction of the center
                    //   This could throw an Exception, but it shouldn't
                    cartPoint = GetIntersection(
                        cartPoint, // line from test point
                        new DoublePoint(0, 0), //   to center
                        new DoublePoint(0, -radius), // Line from South point
                        new DoublePoint(radius, 0)); //   to East point
                                                     // If the Cartesian point is in the Southwest ...
                else if (cartPoint.X < 0 && cartPoint.Y < 0)
                    // Move the Cartesian point to the diamond in
                    //   the direction of the center
                    //   This could throw an Exception, but it shouldn't
                    cartPoint = GetIntersection(
                        cartPoint, // line from test point
                        new DoublePoint(0, 0), //   to center
                        new DoublePoint(0, -radius), // Line from South point
                        new DoublePoint(-radius, 0)); //   to West point
                                                      // If the Cartesian point is in the Northwest ...
                else
                    // Move the Cartesian point to the diamond in
                    //   the direction of the center
                    //   This could throw an Exception, but it shouldn't
                    cartPoint = GetIntersection(
                        cartPoint, // line from test point
                        new DoublePoint(0, 0), //   to center
                        new DoublePoint(0, radius), // Line from North point
                        new DoublePoint(-radius, 0)); //   to West point

            // Now that we have migrated the Cartesian point to the diamond ...

            // Calculate the left axis diamond value of the Cartesian point

            // Create a ray from the Cartesian point at a 45 degree angle through
            //   the upper left side of the diamond
            DoublePoint leftOuterEnd =
                new DoublePoint(cartPoint.X - (2 * radius), cartPoint.Y + (2 * radius));
            // Get the point on the ray that intesects the upper left diamond edge
            DoublePoint leftIntersect = GetIntersection(
                cartPoint, // Ray from Cartesian point
                leftOuterEnd, //   beyond the diamond at 45 degrees Northwest
                new DoublePoint(0, radius), // Line from North point
                new DoublePoint(-radius, 0)); //   to West point
            // Use the Cartesian Y values to find the diamond scale values
            double leftScale = (leftIntersect.Y - (radius / 2)) * 2;

            // Calculate the right axis diamond value of the Cartesian point

            // Create a ray from the Cartesian point at a 45 degree angle through
            //   the upper right side of the diamond
            DoublePoint rightOuterEnd =
                new DoublePoint(cartPoint.X + (2 * radius), cartPoint.Y + (2 * radius));
            // Get the point on the ray that intesects the upper right diamond edge
            DoublePoint rightIntersect = GetIntersection(
                cartPoint, // Ray from Cartesian point
                rightOuterEnd, //   beyond the diamond at 45 degrees Northeast
                new DoublePoint(0, radius), // Line from North point
                new DoublePoint(radius, 0)); //   to East point
            // Use the Cartesian Y values to find the diamond scale values
            double rightScale = (rightIntersect.Y - (radius / 2)) * 2;

            // Build the return value
            DiamondPoint ret = new DiamondPoint(leftScale, rightScale);

            return ret;
        }

        /// <summary>
        ///     IsInsidePolygon takes a pollygon as an array of points as type Point and determines
        ///     if a testpoint as type Point is within the polygon
        /// </summary>
        /// <param name="polygon">array of type Point that defines a polygon</param>
        /// <param name="testPoint">a point of type Point to test</param>
        /// <returns>True if the point is within the polygon, else false</returns>
        public static bool IsInsidePolygon(Point[] polygon, Point testPoint)
        {
            bool ret = true;
            Point p1;
            Point p2;
            Int32 indx;
            double xcross;
            Int32 counter = 0;

            if (polygon.GetLength(0) < 3)
                ret = false; // polygon isn't even a triangle
            else
                foreach (Point corner in polygon)
                    if (corner.X == testPoint.X && corner.Y == testPoint.Y)
                        ret = false; // testPoint is one of the corners

            if (!ret) return ret;

            p1 = polygon[polygon.GetLength(0) - 1]; // start with last point hence the closing line segment
            for (indx = 0; indx < polygon.GetLength(0); indx++)
            {
                // This algorithm creates a ray from the test point to the right.  It counts the
                // number of line segments of the polygon that the ray crosses.  An even number
                // means the test point is outside of the polygon, an odd number means the test
                // point is inside the polygon

                p2 = polygon[indx];
                if (testPoint.Y > System.Math.Min(p1.Y, p2.Y))
                    if (testPoint.Y <= System.Math.Max(p1.Y, p2.Y))
                        if (testPoint.X <= System.Math.Max(p1.X, p2.X))
                            if (p1.Y != p2.Y) // horizontal segments are invalid
                            {
                                xcross = (testPoint.Y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y) + p1.X;
                                if (p1.X == p2.X || testPoint.X <= xcross)
                                    counter++;
                            }
                p1 = p2;
            }
            ret = (counter % 2 != 0);
            return ret;
        }

        /// <summary>
        ///     IsInsidePolygon takes a pollygon as an array of points as type DoublePoint and determines
        ///     if a testpoint as type DoublePoint is within the polygon
        /// </summary>
        /// <param name="polygon">array of type DoublePoint that defines a polygon</param>
        /// <param name="testPoint">a point of type DoublePoint to test</param>
        /// <returns>True if the point is within the polygon, else false</returns>
        public static bool IsInsidePolygon(DoublePoint[] polygon, DoublePoint testPoint)
        {
            bool ret = true;
            DoublePoint p1;
            DoublePoint p2;
            Int32 indx;
            double xcross;
            Int32 counter = 0;

            if (polygon.GetLength(0) < 3)
                ret = false; // polygon isn't even a triangle
            else
                foreach (DoublePoint corner in polygon)
                    if (corner.X == testPoint.X && corner.Y == testPoint.Y)
                        ret = false; // testPoint is one of the corners

            if (!ret) return ret;

            // This algorithm creates a ray from the test point to
            // the right.  It counts the number of line segments of
            // the polygon that the ray crosses.  An even number
            // means the test point is outside of the polygon, an
            // odd number means the test point is inside the polygon.

            // start with last point hence the closing line segment
            p1 = polygon[polygon.GetLength(0) - 1];
            for (indx = 0; indx < polygon.GetLength(0); indx++)
            {
                p2 = polygon[indx];
                if (testPoint.Y > System.Math.Min(p1.Y, p2.Y))
                    if (testPoint.Y <= System.Math.Max(p1.Y, p2.Y))
                        if (testPoint.X <= System.Math.Max(p1.X, p2.X))
                            // horizontal segments are invalid
                            if (p1.Y != p2.Y)
                            {
                                xcross = (testPoint.Y - p1.Y) * (p2.X - p1.X)
                                         / (p2.Y - p1.Y) + p1.X;
                                if (p1.X == p2.X || testPoint.X <= xcross)
                                    counter++;
                            }
                p1 = p2;
            }
            ret = (counter % 2 != 0);
            return ret;
        }

        /// <summary>
        ///     GetIntersection finds the intersection as type Point of two lines each of which
        ///     is defined by two end points of type Point
        /// </summary>
        /// <param name="l1p1">First endpoint of type Point of first line</param>
        /// <param name="l1p2">Second endpoint of type Point of first line</param>
        /// <param name="l2p1">First endpoint of type Point of second line</param>
        /// <param name="l2p2">Second endpoint of type Point of second line</param>
        /// <returns>The intersection of the two lines as type Point</returns>
        public static Point GetIntersection(Point l1p1, Point l1p2, Point l2p1, Point l2p2)
        {
            Point intersection = new Point { X = 0, Y = 0 };
            Point tempPoint;
            double intercept1;
            double intercept2;
            double slope1;
            double slope2;

            if (l1p1.X == l1p2.X) throw new Exception("Line one is vertical");
            if (l2p1.X == l2p2.X) throw new Exception("Line two is vertical");

            if (l1p2.X < l1p1.X)
            {
                tempPoint = l1p1;
                l1p1 = l1p2;
                l1p2 = tempPoint;
            }
            if (l2p2.X < l2p1.X)
            {
                tempPoint = l2p1;
                l2p1 = l2p2;
                l2p2 = tempPoint;
            }

            slope1 = (l1p2.Y - l1p1.Y) / (l1p2.X - l1p1.X);
            slope2 = (l2p2.Y - l2p1.Y) / (l2p2.X - l2p1.X);

            if (slope1 == slope2) throw new Exception("Lines are parallel");

            intercept1 = -((slope1 * l1p1.X) - l1p1.Y);
            intercept2 = -((slope2 * l2p1.X) - l2p1.Y);

            intersection.X = (int)((intercept2 - intercept1) / (slope1 - slope2));
            intersection.Y = (int)((slope1 * intersection.X) + intercept1);

            return intersection;
        }

        /// <summary>
        ///     GetIntersection finds the intersection as type DoublePoint of two lines each of which
        ///     is defined by two end points of type DoublePoint
        /// </summary>
        /// <param name="l1p1">First endpoint of type DoublePoint of first line</param>
        /// <param name="l1p2">Second endpoint of type DoublePoint of first line</param>
        /// <param name="l2p1">First endpoint of type DoublePoint of second line</param>
        /// <param name="l2p2">Second endpoint of type DoublePoint of second line</param>
        /// <returns>The intersection of the two lines as type DoublePoint</returns>
        public static DoublePoint GetIntersection(DoublePoint l1p1, DoublePoint l1p2, DoublePoint l2p1, DoublePoint l2p2)
        {
            DoublePoint intersection = new DoublePoint(0, 0);
            DoublePoint tempPoint;
            double intercept1;
            double intercept2;
            double slope1;
            double slope2;

            if (l1p1.X == l1p2.X) throw new Exception("Line one is vertical");
            if (l2p1.X == l2p2.X) throw new Exception("Line two is vertical");

            if (l1p2.X < l1p1.X)
            {
                tempPoint = l1p1;
                l1p1 = l1p2;
                l1p2 = tempPoint;
            }
            if (l2p2.X < l2p1.X)
            {
                tempPoint = l2p1;
                l2p1 = l2p2;
                l2p2 = tempPoint;
            }

            slope1 = (l1p2.Y - l1p1.Y) / (l1p2.X - l1p1.X);
            slope2 = (l2p2.Y - l2p1.Y) / (l2p2.X - l2p1.X);

            if (slope1 == slope2) throw new Exception("Lines are parallel");

            intercept1 = -((slope1 * l1p1.X) - l1p1.Y);
            intercept2 = -((slope2 * l2p1.X) - l2p1.Y);

            intersection.X = (intercept2 - intercept1) / (slope1 - slope2);
            intersection.Y = (slope1 * intersection.X) + intercept1;

            return intersection;
        }
    }

    /// <summary>
    ///     DoublePoint is a data class that has two Cartesian axes values as type System.Double
    /// </summary>
    public class DoublePoint
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="x">Value of X axis as type System.Double</param>
        /// <param name="y">Value of Y axis as type System.Double</param>
        public DoublePoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        ///     Parameter for value of X axis as type System.Double
        /// </summary>
        public double X { get; set; }

        /// <summary>
        ///     Parameter for value of Y axis as type System.Double
        /// </summary>
        public double Y { get; set; }
    }

    /// <summary>
    ///     DiamondPoint is a data class that has two Diamond axes values as type System.Double
    /// </summary>
    public class DiamondPoint
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="left">Value of Left axis as type System.Double</param>
        /// <param name="right">Value of Right axis as type System.Double</param>
        public DiamondPoint(double left, double right)
        {
            Left = left;
            Right = right;
        }

        /// <summary>
        ///     Parameter for value of Left axis as type System.Double
        /// </summary>
        public double Left { get; set; }

        /// <summary>
        ///     Parameter for value of Right axis as type System.Double
        /// </summary>
        public double Right { get; set; }
    }
}

