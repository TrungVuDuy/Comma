using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.Input;

namespace Comma
{
    public class USpotCoordinates : Command
    {
        public USpotCoordinates()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static USpotCoordinates Instance { get; private set; }

        // Persistent settings keys
        private static string prevDecimalsKey = "Decimals";
        private static string prevUnitKey = "Unit";
        private static string prevDynamicModeKey = "DynamicMode";

        public override string EnglishName => "USpotCoordinates";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Get persistent settings
            int prevDecimals = Settings.GetInteger(prevDecimalsKey, 3);
            bool prevUnit = Settings.GetBool(prevUnitKey, true); // True for meters, False for millimeters
            bool prevDynamicMode = Settings.GetBool(prevDynamicModeKey, true); // True for Dynamic, False for Static

            // List to store the selected points
            var ptList = new System.Collections.Generic.List<Point3d>();

            // Initialize GetPoint object for user input
            var gp = new GetPoint();
            gp.SetCommandPrompt("First curve point");
            gp.DynamicDraw += (sender, e) =>
            {
                if (ptList.Count > 0)
                {
                    // Draw a line from the last point to the current point
                    e.Display.DrawPolyline(ptList, System.Drawing.Color.Red, 2);
                    e.Display.DrawLine(ptList[ptList.Count - 1], e.CurrentPoint, System.Drawing.Color.Red, 2);
                }
            };

            // Add options using OptionList
            var unit_list = new[] { "Meters", "Millimeters" };
            var mode_list = new[] { "Dynamic", "Static" };
            var decimals_list = new[] { "0", "1", "2", "3", "4", "5" };

            var unit_value = prevUnit ? 0 : 1;
            var mode_value = prevDynamicMode ? 0 : 1;
            var decimals_value = prevDecimals;

            gp.AddOptionList("Unit", unit_list, unit_value);
            gp.AddOptionList("Mode", mode_list, mode_value);
            gp.AddOptionList("Decimals", decimals_list, decimals_value);

            // Start the point selection loop
            while (true)
            {
                var getResult = gp.Get();
                if (gp.CommandResult() != Result.Success)
                {
                    // If the user cancels or presses enter, break the loop
                    break;
                }

                if (getResult == GetResult.Point)
                {
                    // Add the selected point to the list
                    ptList.Add(gp.Point());
                    // Update the command prompt for the next point
                    gp.SetCommandPrompt("Next curve point. Press Enter when done");
                }
                else if (getResult == GetResult.Option)
                {
                    var option = gp.Option();
                    if (option != null)
                    {
                        if (option.Index == 0)
                            unit_value = option.CurrentListOptionIndex;
                        else if (option.Index == 1)
                            mode_value = option.CurrentListOptionIndex;
                        else if (option.Index == 2)
                            decimals_value = option.CurrentListOptionIndex;
                    }
                }
            }

            if (ptList.Count < 2)
            {
                RhinoApp.WriteLine("Not enough points selected to create a leader.");
                return Result.Cancel;
            }

            // Add the polyline through the selected points
            var polyline = new Polyline(ptList);
            var polylineId = doc.Objects.AddPolyline(polyline);
            if (polylineId == Guid.Empty)
            {
                RhinoApp.WriteLine("Failed to add the polyline.");
                return Result.Failure;
            }

            // Extract the first point
            var firstPoint = ptList[0];

            // Add the first point to Rhino and get its ID
            var pointId = doc.Objects.AddPoint(firstPoint);
            if (pointId == Guid.Empty)
            {
                RhinoApp.WriteLine("Failed to add the first point to Rhino.");
                return Result.Failure;
            }

            // Use the user-defined options
            bool unit = unit_value == 0;
            bool dynamicMode = mode_value == 0;
            int decimals = int.Parse(decimals_list[decimals_value]);

            // Convert the unit to a string and set the conversion factor
            string unitStr = unit ? "m" : "mm";
            double conversionFactorBasePoint = unit ? 0.001 : 1;

            // Get the model base point
            var basePoint = doc.ModelBasepoint;
            if (basePoint == Point3d.Unset)
            {
                RhinoApp.WriteLine("Model Base Point is not defined.");
                return Result.Failure;
            }

            // Convert the X and Y coordinates of the base point
            double basePointX = basePoint.X * conversionFactorBasePoint;
            double basePointY = basePoint.Y * conversionFactorBasePoint;

            // Format the text based on the mode
            string text;
            if (dynamicMode)
            {
                string decimalsFormat = "." + decimals + "f";
                text = string.Format("X = %<format(float(PointCoordinate(\"{0}\",\"X\"))*{1}+{2},\"{3}\")>%\nY = %<format(float(PointCoordinate(\"{0}\",\"Y\"))*{1}+{4},\"{3}\")>%", pointId, conversionFactorBasePoint, basePointX, decimalsFormat, basePointY);
            }
            else
            {
                double pointX = firstPoint.X * conversionFactorBasePoint;
                double pointY = firstPoint.Y * conversionFactorBasePoint;
                text = string.Format("X = {0:F" + decimals + "}\nY = {1:F" + decimals + "}", pointX, pointY);
            }

            // Add the leader with the selected points and the formatted text
            var leaderId = doc.Objects.AddLeader(text, ptList);
            if (leaderId == Guid.Empty)
            {
                RhinoApp.WriteLine("Failed to create leader.");
                return Result.Failure;
            }

            RhinoApp.WriteLine("Leader created successfully!");

            // Delete the temporary polyline
            doc.Objects.Delete(polylineId, true);

            // Save the current settings as the new previous settings
            Settings.SetInteger(prevDecimalsKey, decimals);
            Settings.SetBool(prevUnitKey, unit);
            Settings.SetBool(prevDynamicModeKey, dynamicMode);

            return Result.Success;
        }
    }
}