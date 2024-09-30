using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace Comma
{
    public class UConvexHull : Command
    {
        public UConvexHull()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static UConvexHull Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "UConvexHull";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Select the points
            if (!SelectPoints(out List<Point3d> inputPoints))
                return Result.Failure;
            // Check for errors
            if (AreThereErrors(inputPoints))
                return Result.Failure;

            // Calculate the Convex Hull
            Polyline resCH = UConvexHullLogic.CalculateCH(inputPoints);

            // Add the polyline to the Rhino document
            if (resCH != null && resCH.IsValid)
            {
                doc.Objects.AddPolyline(resCH);
                doc.Views.Redraw(); // Redraw the view to ensure the polyline is visible
            }
            else
            {
                RhinoApp.WriteLine("Failed to create a valid convex hull polyline.");
                return Result.Failure;
            }
            return Result.Success;
        }

        private bool AreThereErrors(List<Point3d> inputPoints)
        {
            //if(inputPoints.Count < 3)
            //{
            //    RhinoApp.WriteLine("We need at least 3 points");
            //    return true;
            //}
            if (AreThereDuplicates(inputPoints))
            {
                RhinoApp.WriteLine("ERROR - There are some duplicates");
            }
            return false;
        }
        private bool AreThereDuplicates(List<Point3d> inputPoints)
        {
            int i, j;

            for (i = 0; i < inputPoints.Count; i++)
            {
                for (j = i + 1; j < inputPoints.Count; j++)
                {
                    if ((inputPoints[j] - inputPoints[i]).Length < 0.0001)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private bool SelectPoints(out List<Point3d> inputPoints)
        {
            var go = new GetObject();
            go.GeometryFilter = Rhino.DocObjects.ObjectType.Point;
            go.GetMultiple(1, 0);

            if (go.CommandResult() != Result.Success || go.ObjectCount < 3)
            {
                inputPoints = null;
                return false;
            }
            inputPoints = new List<Point3d>(go.ObjectCount);

            for (int i = 0; i < go.ObjectCount; i++)
            {
                Point3d point = go.Object(i).Point().Location;
                inputPoints.Add(point);
            }
            return true;
        }
    }
}
