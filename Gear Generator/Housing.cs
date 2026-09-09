// ===========================================================================
//  HOUSING  -  Grasshopper C# component (RhinoCommon)
//  The shell that screws down onto the base plate. It does four jobs:
//     - guides the sleeve and STOPS IT ROTATING, via slots its tabs run in
//     - those same slots are the clearance for the return springs
//     - an angular gap lets the release lever swing and reach outside
//     - openings at gear level let the two racks travel out to the jaws
//
//  Measured from the gear plane (z = 0), same as everything else. The bottom
//  flange sits on the base plate, so the housing starts at z = -PlateThk.
// ===========================================================================


// ===========================================================================
//  THIS BUILD - 7 inputs to create on the component:
//     InnerDia 42   WallThk 2   Height 44
//     LeverSlotDeg 60   RackSlotW 7   RackSlotH 7
//     BasePlane <- the RACK script's GearPlane
//  Every constant below is already set for this build.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double InnerDia, double WallThk, double Height,
//                           double LeverSlotDeg, double RackSlotW, double RackSlotH,
//                           Plane BasePlane, ref object Body, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double Di = InnerDia     > 1e-9 ? InnerDia     : 42.0;
    double wt = WallThk      > 1e-9 ? WallThk      : 2.0;
    double Hh = Height       > 1e-9 ? Height       : 44.0;
    double lv = LeverSlotDeg > 1e-9 ? LeverSlotDeg : 60.0;
    double rw = RackSlotW    > 1e-9 ? RackSlotW    : 7.0;
    double rh = RackSlotH    > 1e-9 ? RackSlotH    : 7.0;

    // --- match these to the base plate and the sleeve ------------------------
    double plateThk = 2.0;    // housing starts at -plateThk, sitting on the plate
    double flangeD  = 54.0;   // bottom mounting flange diameter
    double flangeT  = 3.0;    // flange thickness

    int    scN      = 3;      // screws, matching the base plate
    double scR      = 25.0;   // screw circle RADIUS
    double scDia    = 2.5;    // PILOT hole - the plate has the clearance hole
    double scPhi    = 60.0;

    int    tabN     = 2;      // sleeve tabs / spring channels
    double tabPhi   = 0.0;    // angle of the first one [deg]
    double tabSlotW = 7.0;    // slot width, = tab width + clearance
    double tabZlo   = 10.0;   // slot runs from here ...
    double tabZhi   = 32.0;   // ... to here (tab travels z 12..30)

    double leverPhi = 90.0;   // centre of the lever gap [deg]
    double leverZlo = 35.0;   // collar sits z 36..40
    double leverZhi = 41.0;

    int    rackN    = 2;      // rack openings, opposed
    double rackPhi  = 0.0;    // angle of the first [deg]
    double rackZlo  = 0.0;    // gear sits z 0..6

    double zOff     = 0.0;

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    double Ri = 0.5 * Di, Ro = Ri + wt;
    double zBot = -plateThk;

    // ---------- checks -------------------------------------------------------
    if (flangeD < 2.0 * Ro) { Info = "Flange is smaller than the housing OD."; return; }
    if (scR + 0.5 * scDia > 0.5 * flangeD)
      log.Add("Screw circle runs off the flange.");
    if (scR - 0.5 * scDia < Ro)
      log.Add("Screw circle overlaps the housing wall - push scR out past " +
              (Ro + 0.5 * scDia + 1.0).ToString("0.##") + ".");
    if (tabZhi > Hh) log.Add("Tab slot runs past the top of the housing.");
    if (leverZhi > Hh) log.Add("Lever gap runs past the top of the housing.");
    if (lv >= 360.0 / Math.Max(1, tabN))
      log.Add("Lever gap is wide enough to swallow a tab slot - reduce LeverSlotDeg.");

    // ---------- shell + flange ----------------------------------------------
    Brep outer = new Cylinder(new Circle(new Plane(new Point3d(0, 0, zBot), Vector3d.ZAxis), Ro),
                              Hh - zBot).ToBrep(true, true);
    Brep flange = new Cylinder(new Circle(new Plane(new Point3d(0, 0, zBot), Vector3d.ZAxis), 0.5 * flangeD),
                               flangeT).ToBrep(true, true);
    List<Brep> add = new List<Brep>();
    add.Add(outer); add.Add(flange);
    Brep body = outer;
    Brep[] uni = Brep.CreateBooleanUnion(add, tol);
    string uniState;
    if (uni != null && uni.Length > 0)
    {
      Brep best = uni[0]; double bv = -1.0;
      foreach (Brep b in uni) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best; uniState = "OK";
    }
    else { uniState = "FAILED"; log.Add("Flange union failed."); }

    // ---------- everything that gets cut away --------------------------------
    List<Brep> cut = new List<Brep>();

    // the bore
    cut.Add(new Cylinder(new Circle(new Plane(new Point3d(0, 0, zBot - 1.0), Vector3d.ZAxis), Ri),
                         Hh - zBot + 2.0).ToBrep(true, true));

    // screw pilot holes in the flange
    for (int i = 0; i < scN; i++)
    {
      double a = RhinoMath.ToRadians(scPhi) + 2.0 * Math.PI * i / scN;
      Point3d o = new Point3d(scR * Math.Cos(a), scR * Math.Sin(a), zBot - 1.0);
      cut.Add(new Cylinder(new Circle(new Plane(o, Vector3d.ZAxis), 0.5 * scDia), flangeT + 2.0)
              .ToBrep(true, true));
    }

    // tab / spring slots, and the rack openings - both are radial boxes
    for (int i = 0; i < tabN; i++)
    {
      double a = RhinoMath.ToRadians(tabPhi) + 2.0 * Math.PI * i / tabN;
      Vector3d rad = new Vector3d(Math.Cos(a), Math.Sin(a), 0.0);
      Vector3d tan = Vector3d.CrossProduct(Vector3d.ZAxis, rad); tan.Unitize();
      Plane bp2 = new Plane(Point3d.Origin, rad, tan);
      cut.Add(new Box(bp2, new Interval(Ri - 1.0, Ro + 1.0),
                           new Interval(-0.5 * tabSlotW, 0.5 * tabSlotW),
                           new Interval(tabZlo, tabZhi)).ToBrep());
    }
    for (int i = 0; i < rackN; i++)
    {
      double a = RhinoMath.ToRadians(rackPhi) + 2.0 * Math.PI * i / rackN;
      Vector3d rad = new Vector3d(Math.Cos(a), Math.Sin(a), 0.0);
      Vector3d tan = Vector3d.CrossProduct(Vector3d.ZAxis, rad); tan.Unitize();
      Plane bp2 = new Plane(Point3d.Origin, rad, tan);
      cut.Add(new Box(bp2, new Interval(Ri - 1.0, 0.5 * flangeD + 2.0),
                           new Interval(-0.5 * rw, 0.5 * rw),
                           new Interval(rackZlo, rackZlo + rh)).ToBrep());
    }

    // lever gap - a pie slice, so the lever can swing through it
    {
      double a1 = RhinoMath.ToRadians(leverPhi - 0.5 * lv);
      double a2 = RhinoMath.ToRadians(leverPhi + 0.5 * lv);
      double Rc = Ro + 2.0;
      Circle c = new Circle(new Plane(new Point3d(0, 0, leverZlo), Vector3d.ZAxis), Rc);
      Arc arc = new Arc(c, new Interval(a1, a2));
      PolyCurve pc = new PolyCurve();
      pc.Append(new ArcCurve(arc));
      pc.Append(new LineCurve(arc.EndPoint, new Point3d(0, 0, leverZlo)));
      pc.Append(new LineCurve(new Point3d(0, 0, leverZlo), arc.StartPoint));
      pc.MakeClosed(tol);
      Extrusion ex = Extrusion.Create(pc, leverZhi - leverZlo, true);
      if (ex != null) cut.Add(ex.ToBrep());
      else log.Add("Could not build the lever gap.");
    }

    Brep[] diff = Brep.CreateBooleanDifference(new Brep[] { body }, cut, tol);
    string cutState;
    if (diff != null && diff.Length > 0)
    {
      Brep best = diff[0]; double bv = -1.0;
      foreach (Brep b in diff) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best;
      cutState = "OK (" + diff.Length + " piece" + (diff.Length == 1 ? ")" : "s, kept the largest)");
    }
    else { cutState = "FAILED"; log.Add("Slot / bore boolean failed."); }

    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
    body.Transform(Transform.PlaneToPlane(Plane.WorldXY, bp));

    // ---------- report -------------------------------------------------------
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("shell               bore " + Di.ToString("0.###") + ", OD " + (2.0 * Ro).ToString("0.###") +
                  ", wall " + wt.ToString("0.###"));
    sb.AppendLine("                    z " + zBot.ToString("0.##") + " .. " + Hh.ToString("0.##"));
    sb.AppendLine("flange              dia " + flangeD.ToString("0.###") + " x " + flangeT.ToString("0.###"));
    sb.AppendLine("screws              " + scN + " pilot holes dia " + scDia.ToString("0.###") +
                  " at radius " + scR.ToString("0.###"));
    sb.AppendLine("--");
    sb.AppendLine("tab / spring slots  " + tabN + " x " + tabSlotW.ToString("0.###") +
                  " wide, z " + tabZlo.ToString("0.##") + " .. " + tabZhi.ToString("0.##"));
    sb.AppendLine("   these stop the sleeve rotating AND clear the return springs");
    sb.AppendLine("lever gap           " + lv.ToString("0.#") + " deg at " + leverPhi.ToString("0.#") +
                  " deg, z " + leverZlo.ToString("0.##") + " .. " + leverZhi.ToString("0.##"));
    sb.AppendLine("rack openings       " + rackN + " x " + rw.ToString("0.###") + " wide x " +
                  rh.ToString("0.###") + " tall, z " + rackZlo.ToString("0.##"));
    sb.AppendLine("--");
    sb.AppendLine("DIAGNOSTICS");
    sb.AppendLine("  shell union       " + uniState);
    sb.AppendLine("  slots / bore      " + cutState);
    sb.AppendLine("  body solid        " + body.IsSolid);
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Body = body;
    Info = sb.ToString();
