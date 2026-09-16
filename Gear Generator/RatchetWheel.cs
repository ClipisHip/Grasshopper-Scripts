// ===========================================================================
//  RATCHET WHEEL  -  Grasshopper C# component (RhinoCommon)
//  Asymmetric external ratchet ring: a steep locking face and a shallow ramp
//  the pawl rides over.  Sits on the gear shaft.
//  ---------------------------------------------------------------------
//  INPUTS (Item access):
//     OuterDia    double  tip diameter
//     Teeth       int     tooth count (40 gives 1.885 mm rack steps on m1.5 z16)
//     ToothDepth  double  radial depth of a tooth
//     Thickness   double  extrusion thickness
//     BoreDia     double  shaft bore
//     LockAngle   double  undercut of the locking face [deg].  POSITIVE leans
//                         the tip back so the pawl is pulled in.  5-10 typical
//     RootFillet  double  corner radius, 0 = sharp
//     BasePlane   Plane
//  OUTPUTS:
//     Body        Brep
//     Profile     closed tooth profile curve
//     Info        report + warnings
// ===========================================================================


// ===========================================================================
//  THIS BUILD - 8 inputs to create on the component:
//     OuterDia 24   Teeth 40   ToothDepth 0.8   Thickness 8
//     BoreDia 6.4   LockAngle 8   RootFillet 0
//     BasePlane <- the RACK script's GearPlane
//  Every constant below is already set for this build.
//
//  WHY 8 THICK AND zOff 30. The pawl swings at z 32..38, so the TEETH must be
//  there. But the cam tops out at z 30, and a wheel starting at 32 would leave
//  the rotor in two disconnected pieces. Spanning 30..38 butts the cam and
//  still puts 6 mm of tooth in front of the 6 mm pawl. The spare 30..32 band
//  doubles as the sleeve's top end-stop: the sleeve tops out at exactly 30
//  and the wheel's 24 dia overhangs its 18.5 bore.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double OuterDia, int Teeth, double ToothDepth,
//                           double Thickness, double BoreDia, double LockAngle,
//                           double RootFillet, Plane BasePlane,
//                           ref object Body, ref object Profile, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double Do = OuterDia   > 1e-9 ? OuterDia   : 24.0;   // fits the 26.8 bore waist
    int    n  = Teeth      > 3    ? Teeth      : 40;     // 1.88 mm of rack per click
    double h  = ToothDepth > 1e-9 ? ToothDepth : 0.8;
    double th = Thickness  > 1e-9 ? Thickness  : 8.0;   // 30..38: butts the cam
                                                        // at 30, teeth face the
                                                        // pawl over 32..38
    double Db = BoreDia    > 0.0  ? BoreDia    : 6.4;    // 0.4 clearance on the 6.0 shaft
    double la = LockAngle > 1e-9 ? LockAngle : 8.0;      // undercut - 0 lets the pawl climb out
    double rf = RootFillet > 0.0  ? RootFillet : 0.0;

    // --- for the report only -------------------------------------------------
    double mod   = 1.5;    // gear module
    int    zGear = 16;     // gear teeth
    double Treq  = 0.96;   // holding torque [N.m]
    double hand  = 1.0;    // -1 mirrors the teeth if it locks the wrong way
    double zOff  = 30.0;   // sits ON the cam top, so the rotor stays one piece

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    double R  = 0.5 * Do;
    double Rr = R - h;
    if (Rr <= 0.5 * Db) { Info = "ToothDepth leaves no rim over the bore."; return; }

    double pitchMM = 2.0 * Math.PI * R / n;
    if (pitchMM < 0.8) log.Add("Tooth pitch " + pitchMM.ToString("0.##") +
                               " is very fine - hard to print on FDM.");

    // locking face lean: positive LockAngle leans the tip backwards (undercut)
    double t = h * Math.Tan(RhinoMath.ToRadians(la));
    double dPhi = Math.Asin(Math.Max(-0.99, Math.Min(0.99, t / R)));

    // ---------- profile ------------------------------------------------------
    List<Point3d> pts = new List<Point3d>();
    for (int i = 0; i < n; i++)
    {
      double a0 = hand * 2.0 * Math.PI * i / n;
      double a1 = a0 + hand * dPhi;
      pts.Add(new Point3d(Rr * Math.Cos(a0), Rr * Math.Sin(a0), 0.0));   // root
      pts.Add(new Point3d(R  * Math.Cos(a1), R  * Math.Sin(a1), 0.0));   // tip
    }
    pts.Add(pts[0]);
    Curve prof = new Polyline(pts).ToPolylineCurve();

    if (rf > 1e-9)
    {
      double rfMax = 0.35 * Math.Min(h, pitchMM);
      if (rf > rfMax) { rf = rfMax; log.Add("RootFillet clamped to " + rf.ToString("0.###") + "."); }
      Curve f = Curve.CreateFilletCornersCurve(prof, rf, tol, 0.01);
      if (f != null) prof = f;
      else log.Add("Fillet failed - profile left sharp.");
    }
    if (!prof.IsClosed) prof.MakeClosed(tol);

    // ---------- solid --------------------------------------------------------
    Extrusion ex = Extrusion.Create(prof, th, true);
    if (ex == null) { Info = "Could not extrude the profile."; return; }
    Brep body = ex.ToBrep();
    if (body.SolidOrientation == BrepSolidOrientation.Inward) body.Flip();

    if (Db > 1e-9)
    {
      Brep bore = new Cylinder(new Circle(new Plane(new Point3d(0, 0, -1.0), Vector3d.ZAxis),
                                          0.5 * Db), th + 2.0).ToBrep(true, true);
      Brep[] diff = Brep.CreateBooleanDifference(body, bore, tol);
      if (diff != null && diff.Length > 0)
      {
        Brep best = diff[0]; double bv = -1.0;
        foreach (Brep b in diff) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
        body = best;
      }
      else log.Add("Bore boolean failed.");
    }

    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
    Transform xf = Transform.PlaneToPlane(Plane.WorldXY, bp);
    body.Transform(xf);
    prof.Transform(xf);

    // ---------- report -------------------------------------------------------
    double stepDeg = 360.0 / n;
    double rackStep = Math.PI * mod * zGear * stepDeg / 360.0;
    double pawlF = Treq * 1000.0 / (0.5 * (R + Rr));

    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("tip dia             " + Do.ToString("0.###") + ", root dia " + (2.0 * Rr).ToString("0.###"));
    sb.AppendLine("teeth               " + n + ",  pitch " + pitchMM.ToString("0.###") +
                  ",  depth " + h.ToString("0.###"));
    sb.AppendLine("lock face undercut  " + la.ToString("0.#") + " deg" +
                  (la > 0.5 ? "   (pulls the pawl in - good)" : "   (radial or positive-rake: pawl may climb out)"));
    sb.AppendLine("thickness           " + th.ToString("0.###") + ",  bore " + Db.ToString("0.###"));
    sb.AppendLine("--");
    sb.AppendLine("step                " + stepDeg.ToString("0.###") + " deg  =  " +
                  rackStep.ToString("0.###") + " mm of rack (m" + mod.ToString("0.##") + " z" + zGear + ")");
    sb.AppendLine("pawl contact force  " + pawlF.ToString("0") + " N at " + Treq.ToString("0.##") + " N.m");
    if (pawlF > 150.0)
      sb.AppendLine("   -> load the pawl in COMPRESSION along its arm, not bending.");
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string s2 in log) sb.AppendLine("! " + s2); }

    Body    = body;
    Profile = prof;
    Info    = sb.ToString();
