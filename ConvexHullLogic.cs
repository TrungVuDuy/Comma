using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comma
{
    internal class ConvexHullLogic
    {
        internal static Polyline CalculateCH(List<Point3d> inputPoints)
        {
            // Find the lowest and the rightmost point
            FindLowest(inputPoints);
            // Sort the points based on the angle they are maxing with the positive X axis
            AngleSort(inputPoints);
            // Call Graham`s Sort
            List<Point3d> CHpts = GrahamSort(inputPoints);
            return new Polyline(CHpts);

        }

        private static List<Point3d> GrahamSort(List<Point3d> inputPoints)
        {
            List<Point3d> CH = new List<Point3d>();
            CH.Add(inputPoints[0]);
            CH.Add(inputPoints[1]);
            int i = 2;
            while (i < inputPoints.Count)
            {
                double det = Determinant(CH[CH.Count - 2],
                                         CH[CH.Count - 1],
                                         inputPoints[i]);
                if (det > -0.000001)
                {
                    CH.Add(inputPoints[i]);
                    i++;
                }
                else if (det < 0 && CH.Count > 2)
                {
                    CH.RemoveAt(CH.Count - 1);
                }
            }
            CH.Add(CH[0]);
            //// Add dots to Rhino for checking the order of points
            //for (int j = 0; j < CH.Count; j++)
            //{
            //    Rhino.RhinoDoc.ActiveDoc.Objects.AddPoint(CH[j]);
            //    // Optionally add text dots with indicates
            //    Rhino.RhinoDoc.ActiveDoc.Objects.AddTextDot(j.ToString(), CH[j]);

            //}
            return CH;

        }

        private static double Determinant(Point3d Pt1, Point3d Pt2, Point3d Pt3)
        {
            double[] row1 = new double[3];
            double[] row2 = new double[3];
            double[] row3 = new double[3];
            double det;

            row1[0] = row2[0] = row3[0] = 1;

            row1[1] = Pt1.X;
            row1[2] = Pt1.Y;

            row2[1] = Pt2.X;
            row2[2] = Pt2.Y;

            row3[1] = Pt3.X;
            row3[2] = Pt3.Y;

            /* 
                    1   X1  Y1
                Det 1   X2  Y2
                    1   X3  Y3
             */
            det = (row1[0] * row2[1] * row3[2]) +
                  (row1[1] * row2[2] * row3[0]) +
                  (row1[2] * row2[0] * row3[1]) -
                  (row1[0] * row2[2] * row3[1]) -
                  (row1[1] * row2[0] * row3[2]) -
                  (row1[2] * row2[1] * row3[0]);
            return det;
        }

        private static void AngleSort(List<Point3d> inputPoints)
        {
            int i, j;
            for (i = 1; i < inputPoints.Count - 1; i++)
            {
                for (j = i + 1; j < inputPoints.Count; j++)
                {
                    double angleI = Vector3d.VectorAngle(Vector3d.XAxis, new Vector3d(inputPoints[i] - inputPoints[0]));
                    double angleJ = Vector3d.VectorAngle(Vector3d.XAxis, new Vector3d(inputPoints[j] - inputPoints[0]));

                    if (angleJ < angleI)
                    {
                        // Manual Swap
                        Point3d swapTemp = inputPoints[i];
                        inputPoints[i] = inputPoints[j];
                        inputPoints[j] = swapTemp;
                    }
                    else if (Math.Abs(angleI - angleJ) < 0.00001)
                    {
                        double lengthI = (inputPoints[i] - inputPoints[0]).Length;
                        double lengthJ = (inputPoints[j] - inputPoints[0]).Length;
                        if (lengthJ < lengthI)
                        {
                            // Manual Swap
                            Point3d swapTemp = inputPoints[i];
                            inputPoints[i] = inputPoints[j];
                            inputPoints[j] = swapTemp;
                        }
                    }
                }
            }
        }

        private static void FindLowest(List<Point3d> inputPoints)
        {
            int i;
            int m = 0; // index in the List of the lowest point

            for (i = 1; i < inputPoints.Count; i++)
            {
                if (inputPoints[i].Y < inputPoints[m].Y ||
                    inputPoints[i].Y == inputPoints[m].Y && inputPoints[i].X > inputPoints[m].X)
                {
                    m = i;
                }
            }
            //Manual Swap
            Point3d swapTemp = inputPoints[0];
            inputPoints[0] = inputPoints[m];
            inputPoints[m] = swapTemp;
            //Rhino.RhinoDoc.ActiveDoc.Objects.AddPoint(inputPoints[0]);
            //// Optionally add text dots with indicates
            //Rhino.RhinoDoc.ActiveDoc.Objects.AddTextDot(0.ToString(), inputPoints[0]);
        }
    }
}
