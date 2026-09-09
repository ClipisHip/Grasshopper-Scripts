// ===========================================================================
//  WRAP SPRING HUB  -  Grasshopper C# component (RhinoCommon)
//  The cylinder the clutch spring grips.  Turns with the gear shaft.
//  The gripped surface must stay smooth and truly cylindrical - no flats,
//  no seams, no layer scarring, or the spring will chatter and slip.
//  ---------------------------------------------------------------------
//  INPUTS (Item access):
//     HubDia       double  gripped diameter (match the spring script)
//     HubLength    double  gripped length (spring body + a little)
//     BoreDia      double  shaft bore
//     FlatDepth    double  D-flat on the bore, 0 = round bore
//     FlangeDia    double  flange diameter, 0 = none
//     FlangeThk    double  flange thickness
//     BasePlane    Plane   feed the rack script's GearPlane
//  OUTPUTS:
//     Body         Brep
//     Info         report + warnings
// ===========================================================================



// ===========================================================================
//  THIS BUILD - set on the component:
//     HubDia 17.15   HubLength 10   BoreDia 6.4
//     FlatDepth 0   FlangeDia 0   FlangeThk 0  (unused while FlangeDia is 0)
//     BasePlane <- the RACK script's GearPlane
//  BoreDia is 6.4, not 6: the rotor TURNS on the 6.0 shaft and needs a
//  running clearance. See the FITS table in SPEC.md.
//  Every constant below is already set for this build.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double HubDia, double HubLength, double BoreDia,
//                           double FlatDepth, double FlangeDia, double FlangeThk,
//                           Plane BasePlane, ref object Body, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double Dh = HubDia    > 1e-9 ? HubDia    : 17.15;
    double Lh = HubLength > 1e-9 ? HubLength : 10.0;
    double Db = BoreDia   > 1e-9 ? BoreDia   : 6.4;   // 0.4 clearance on the 6.0 shaft
    double fd = FlatDepth > 0.0  ? FlatDepth : 0.0;
    double Df = FlangeDia > 1e-9 ? FlangeDia : 0.0;
    double Tf = FlangeThk > 1e-9 ? FlangeThk : 3.0;

    // --- extras --------------------------------------------------------------
    double bcDia   = 0.0;   // bolt circle diameter, 0 = no bolt holes
    double boltDia = 3.2;
    int    boltN   = 3;
    double zOff    = 30.0;   // slide along the axis.  For a one-piece rotor with the hub
                            // hanging BELOW the gear, set this to -(HubLength + FlangeThk).

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    if (Db >= Dh) { Info = "Bore is larger than the hub."; return; }
    double wall = 0.5 * (Dh - Db);
    if (wall < 2.0) log.Add("Hub wall is only " + wall.ToString("0.##") +
                            " thick - the spring pressure may collapse it.");
    if (fd >= 0.5 * Db) { Info = "FlatDepth removes more than half the bore."; return; }

    double zTop = Tf + Lh;
    List<Brep> add = new List<Brep>();

    // flange first, hub sits on top of it
    double zHub = 0.0;
    if (Df > 1e-9)
    {
      if (Df <= Dh) log.Add("FlangeDia is not bigger than HubDia - no shoulder to locate the spring.");
      add.Add(new Cylinder(new Circle(Plane.WorldXY, 0.5 * Df), Tf).ToBrep(true, true));
      zHub = Tf;
    }
    add.Add(new Cylinder(new Circle(new Plane(new Point3d(0, 0, zHub), Vector3d.ZAxis),
                                    0.5 * Dh), Lh).ToBrep(true, true));

    Brep body = add[0];
    if (add.Count > 1)
    {
      Brep[] u = Brep.CreateBooleanUnion(add, tol);
      if (u != null && u.Length > 0)
      {
        Brep best = u[0]; double bv = -1.0;
        foreach (Brep b in u) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
        body = best;
      }
      else log.Add("Flange union failed.");
    }

    // ---------- bore, round or D --------------------------------------------
    double zLo = -1.0, zHi = zHub + Lh + 1.0;
    Brep boreB;
    if (fd > 1e-9)
    {
      double r = 0.5 * Db, xf2 = r - fd;
      double yh = Math.Sqrt(Math.Max(0.0, r * r - xf2 * xf2));
      Arc  a = new Arc(new Point3d(xf2, -yh, zLo), new Point3d(-r, 0, zLo), new Point3d(xf2, yh, zLo));
      PolyCurve pc = new PolyCurve();
      pc.Append(new ArcCurve(a));
      pc.Append(new LineCurve(new Point3d(xf2, yh, zLo), new Point3d(xf2, -yh, zLo)));
      pc.MakeClosed(tol);
      Extrusion ex = Extrusion.Create(pc, zHi - zLo, true);
      if (ex == null) { Info = "Could not extrude the D-bore."; return; }
      boreB = ex.ToBrep();
    }
    else
    {
      boreB = new Cylinder(new Circle(new Plane(new Point3d(0, 0, zLo), Vector3d.ZAxis),
                                      0.5 * Db), zHi - zLo).ToBrep(true, true);
    }

    List<Brep> cut = new List<Brep>();
    cut.Add(boreB);

    if (bcDia > 1e-9 && boltN > 0 && Df > 1e-9)
      for (int i = 0; i < boltN; i++)
      {
        double a2 = 2.0 * Math.PI * i / boltN;
        Point3d o = new Point3d(0.5 * bcDia * Math.Cos(a2), 0.5 * bcDia * Math.Sin(a2), -1.0);
        cut.Add(new Cylinder(new Circle(new Plane(o, Vector3d.ZAxis), 0.5 * boltDia), Tf + 2.0)
                .ToBrep(true, true));
      }

    Brep[] diff = Brep.CreateBooleanDifference(new Brep[] { body }, cut, tol);
    if (diff != null && diff.Length > 0)
    {
      Brep best = diff[0]; double bv = -1.0;
      foreach (Brep b in diff) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best;
    }
    else log.Add("Bore boolean failed.");

    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
    body.Transform(Transform.PlaneToPlane(Plane.WorldXY, bp));

    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("gripped dia         " + Dh.ToString("0.###") + " x " + Lh.ToString("0.###") + " long");
    sb.AppendLine("bore                " + Db.ToString("0.###") + (fd > 1e-9 ? "  D-flat " + fd.ToString("0.##") : "  round"));
    sb.AppendLine("wall under spring   " + wall.ToString("0.###"));
    if (Df > 1e-9) sb.AppendLine("flange              " + Df.ToString("0.###") + " x " + Tf.ToString("0.###"));
    sb.AppendLine("overall height      " + (zHub + Lh).ToString("0.###") +
                  "   spanning z " + zOff.ToString("0.##") + " .. " + (zOff + zHub + Lh).ToString("0.##"));
    sb.AppendLine("--");
    sb.AppendLine("Print the gripped surface with the axis VERTICAL so no layer");
    sb.AppendLine("seam runs under the spring, and do not sand a flat on it.");
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Body = body;
    Info = sb.ToString();
