// ===========================================================================
//  BASE PLATE  -  Grasshopper C# component (RhinoCommon)
//  Bottom piece: the 2 mm floor, the shaft the rotor turns on, the pins that
//  locate the return springs, and clearance holes for the screws that pull
//  the housing down onto it.
//
//  The plate top IS the gear plane (z = 0). Everything else in the assembly
//  is measured from there, so the plate itself sits at z -PlateThk .. 0.
// ===========================================================================


// ===========================================================================
//  THIS BUILD - 6 inputs to create on the component:
//     PlateDia 62   PlateThk 2   ShaftDia 6   ShaftLen 42
//     PinCount 2    BasePlane <- the RACK script's GearPlane
//  Every constant below is already set for this build.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double PlateDia, double PlateThk, double ShaftDia,
//                           double ShaftLen, int PinCount, Plane BasePlane,
//                           ref object Body, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double Dp = PlateDia > 1e-9 ? PlateDia : 62.0;
    double tp = PlateThk > 1e-9 ? PlateThk : 2.0;
    double Ds = ShaftDia > 1e-9 ? ShaftDia : 6.0;
    double Ls = ShaftLen > 1e-9 ? ShaftLen : 42.0;
    int    np = PinCount > 0    ? PinCount : 2;

    // --- match these to the housing and the sleeve ---------------------------
    double pinR    = 18.0;   // spring pin circle RADIUS - matches the sleeve pockets
    double pinDia  = 3.0;    // pin diameter, guides the spring's lower end
    double pinLen  = 8.0;    // how far the pin stands proud of the plate
    double pinPhi  = 0.0;    // angle of the first pin [deg]
    double springOD = 6.0;   // for the report / clearance check only

    int    scN     = 2;      // screws pulling the housing down - MUST match
                             // the housing's lugN / lugPhi / lugR
    double scR     = 24.0;   // screw circle RADIUS - must match the housing lugs
    double scDia   = 3.4;    // clearance hole for M3 (housing has the 2.5 pilot)
    double scPhi   = 90.0;   // angle of the first screw [deg] - must match

    double gearR   = 13.68;  // rotor gear radius, for the clearance check
    double zOff    = 0.0;    // slide the whole plate along the axis

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    double Rp = 0.5 * Dp;

    // ---------- checks -------------------------------------------------------
    if (pinR + 0.5 * springOD > Rp)
    { Info = "Spring pins fall outside the plate - grow PlateDia or pull pinR in."; return; }
    if (pinR - 0.5 * springOD <= gearR)
      log.Add("Springs at radius " + pinR.ToString("0.##") + " foul the gear (radius " +
              gearR.ToString("0.##") + ") - push pinR out to at least " +
              (gearR + 0.5 * springOD + 0.5).ToString("0.##") + ".");
    if (scR + 0.5 * scDia > Rp)
      log.Add("Screw circle runs off the edge of the plate.");
    if (Ds >= 2.0 * gearR)
    { Info = "Shaft is wider than the gear."; return; }

    // ---------- solids -------------------------------------------------------
    // plate sits BELOW the gear plane, so its top face is z = 0
    Brep plate = new Cylinder(new Circle(new Plane(new Point3d(0, 0, -tp), Vector3d.ZAxis), Rp), tp)
                 .ToBrep(true, true);
    if (plate.SolidOrientation == BrepSolidOrientation.Inward) plate.Flip();

    List<Brep> add = new List<Brep>();
    add.Add(plate);
    add.Add(new Cylinder(new Circle(Plane.WorldXY, 0.5 * Ds), Ls).ToBrep(true, true));

    List<Point3d> pinPts = new List<Point3d>();
    for (int i = 0; i < np; i++)
    {
      double a = RhinoMath.ToRadians(pinPhi) + 2.0 * Math.PI * i / np;
      Point3d o = new Point3d(pinR * Math.Cos(a), pinR * Math.Sin(a), 0.0);
      pinPts.Add(o);
      add.Add(new Cylinder(new Circle(new Plane(o, Vector3d.ZAxis), 0.5 * pinDia), pinLen)
              .ToBrep(true, true));
    }

    Brep body = add[0];
    Brep[] uni = Brep.CreateBooleanUnion(add, tol);
    string uniState;
    if (uni != null && uni.Length > 0)
    {
      Brep best = uni[0]; double bv = -1.0;
      foreach (Brep b in uni) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best;
      uniState = "OK (" + uni.Length + " piece" + (uni.Length == 1 ? ")" : "s, kept the largest)");
    }
    else { uniState = "FAILED"; log.Add("Shaft / pin union failed."); }

    // ---------- screw clearance holes ----------------------------------------
    List<Brep> cut = new List<Brep>();
    for (int i = 0; i < scN; i++)
    {
      double a = RhinoMath.ToRadians(scPhi) + 2.0 * Math.PI * i / scN;
      Point3d o = new Point3d(scR * Math.Cos(a), scR * Math.Sin(a), -tp - 1.0);
      cut.Add(new Cylinder(new Circle(new Plane(o, Vector3d.ZAxis), 0.5 * scDia), tp + 2.0)
              .ToBrep(true, true));
    }
    Brep[] diff = Brep.CreateBooleanDifference(new Brep[] { body }, cut, tol);
    if (diff != null && diff.Length > 0)
    {
      Brep best = diff[0]; double bv = -1.0;
      foreach (Brep b in diff) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best;
    }
    else log.Add("Screw hole boolean failed.");

    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
    body.Transform(Transform.PlaneToPlane(Plane.WorldXY, bp));

    // ---------- report -------------------------------------------------------
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("plate               dia " + Dp.ToString("0.###") + " x " + tp.ToString("0.###") +
                  " thick,  z " + (-tp).ToString("0.##") + " .. 0");
    sb.AppendLine("shaft               dia " + Ds.ToString("0.###") + ", z 0 .. " + Ls.ToString("0.###"));
    sb.AppendLine("--");
    sb.AppendLine("spring pins         " + np + " at radius " + pinR.ToString("0.###") +
                  ", dia " + pinDia.ToString("0.###") + ", standing " + pinLen.ToString("0.###") + " proud");
    sb.AppendLine("  clear of the gear  " + (pinR - 0.5 * springOD - gearR).ToString("0.###") +
                  " (spring OD " + springOD.ToString("0.##") + " vs gear radius " + gearR.ToString("0.##") + ")");
    sb.AppendLine("screws              " + scN + " clearance holes dia " + scDia.ToString("0.###") +
                  " at radius " + scR.ToString("0.###"));
    sb.AppendLine("--");
    sb.AppendLine("The plate TOP is the gear plane. The rotor's gear face rests on it,");
    sb.AppendLine("so the shaft carries the radial load and the plate the thrust.");
    sb.AppendLine("--");
    sb.AppendLine("DIAGNOSTICS");
    sb.AppendLine("  union             " + uniState);
    sb.AppendLine("  body solid        " + body.IsSolid);
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Body = body;
    Info = sb.ToString();
