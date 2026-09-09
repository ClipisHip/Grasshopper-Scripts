// ===========================================================================
//  RATCHET PAWL  -  Grasshopper C# component (RhinoCommon)
//  Pivoting pawl for the ratchet wheel.  Built in the WHEEL's coordinates:
//  the wheel centre is at the base plane origin, the pawl pivots at
//  (PivotDist, 0).  Includes a self-engagement check.
//  ---------------------------------------------------------------------
//  INPUTS (Item access):
//     RatchetDia    double  wheel tip diameter (match the wheel)
//     Teeth         int     wheel tooth count (match the wheel)
//     ToothDepth    double  wheel tooth depth  (match the wheel)
//     PivotDist     double  wheel centre -> pawl pivot
//     ContactAngle  double  where the tip engages, [deg] round the wheel
//     ArmWidth      double  arm width
//     Thickness     double  extrusion thickness
//     PivotHoleDia  double  pivot pin hole
//     TailLength    double  tail beyond the pivot, for the spring / release
//     BasePlane     Plane
//  OUTPUTS:
//     Body          Brep
//     Profile       closed outline
//     Info          report, incl. the self-engagement check
// ===========================================================================


// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double RatchetDia, int Teeth, double ToothDepth, double PivotDist,
//                           double ContactAngle, double ArmWidth, double Thickness,
//                           double PivotHoleDia, double TailLength, Plane BasePlane,
//                           ref object Body, ref object Profile, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double Dr = RatchetDia   > 1e-9 ? RatchetDia   : 30.0;
    int    nz = Teeth        > 3    ? Teeth        : 60;
    double h  = ToothDepth   > 1e-9 ? ToothDepth   : 1.2;
    double Dp = PivotDist    > 1e-9 ? PivotDist    : 26.0;
    double ca = ContactAngle;
    double aw = ArmWidth     > 1e-9 ? ArmWidth     : 5.0;
    double th = Thickness    > 1e-9 ? Thickness    : 5.0;
    double ph = PivotHoleDia > 1e-9 ? PivotHoleDia : 3.2;
    double tl = TailLength   > 0.0  ? TailLength   : 12.0;

    // --- extras --------------------------------------------------------------
    double bossDia = 0.0;    // pivot boss diameter, 0 -> PivotHoleDia + 2*3
    double tipFrac = 0.75;   // how far into the tooth the tip reaches (0-1)
    double Treq    = 2.93;   // holding torque [N.m], for the force report

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    double R  = 0.5 * Dr, Rr = R - h;
    double bd = bossDia > 1e-9 ? bossDia : ph + 6.0;
    double cr = RhinoMath.ToRadians(ca);

    Point3d P0 = new Point3d(Dp, 0.0, 0.0);                       // pivot
    double  Rc = R - tipFrac * h;                                  // contact radius
    Point3d C  = new Point3d(Rc * Math.Cos(cr), Rc * Math.Sin(cr), 0.0);   // tip

    double reach = P0.DistanceTo(C);
    if (reach < 1e-6) { Info = "Pivot sits on the contact point."; return; }
    if (Dp <= R) log.Add("Pivot is inside the wheel tip circle - the arm will foul the teeth.");

    // ---------- self-engagement check ---------------------------------------
    // the tooth pushes the pawl tangentially; does that moment drive the tip IN?
    Vector3d fDir = new Vector3d(-Math.Sin(cr), Math.Cos(cr), 0.0);   // tangential load
    Vector3d arm  = C - P0;
    double   mom  = arm.X * fDir.Y - arm.Y * fDir.X;                  // moment about the pivot
    Vector3d tipV = new Vector3d(-arm.Y, arm.X, 0.0);                 // tip motion for +moment
    Vector3d rIn  = new Vector3d(-Math.Cos(cr), -Math.Sin(cr), 0.0);  // inward radial
    double   bite = Math.Sign(mom) * (tipV.X * rIn.X + tipV.Y * rIn.Y);
    bool selfEng  = bite > 0.0;
    if (!selfEng)
      log.Add("NOT self-engaging - the load would lever the pawl out of the tooth. " +
              "Move the pivot to the other side of the contact, or change ContactAngle.");

    // ---------- outline: boss + arm + tip + tail -----------------------------
    // every piece MUST lie in the same horizontal plane or the 2D union fails.
    // Plane(origin, xDir, yDir) takes the normal as xDir X yDir, so pair each
    // in-plane direction with Z X itself - never with ZAxis directly.
    List<Curve> parts = new List<Curve>();
    parts.Add(new ArcCurve(new Circle(new Plane(P0, Vector3d.ZAxis), 0.5 * bd)));

    Vector3d u = arm; u.Unitize();
    Vector3d uP = Vector3d.CrossProduct(Vector3d.ZAxis, u); uP.Unitize();
    Plane ap = new Plane(P0, u, uP);
    parts.Add(new Rectangle3d(ap, new Interval(0.0, reach), new Interval(-0.5 * aw, 0.5 * aw))
              .ToNurbsCurve());

    // tip: sits in a tooth space, so keep it under the tooth pitch at that radius
    double toothPitch = 2.0 * Math.PI * Rc / nz;
    double tw = Math.Min(aw, 0.65 * toothPitch);
    Vector3d ri = new Vector3d(-Math.Cos(cr), -Math.Sin(cr), 0.0);
    Vector3d rp2 = Vector3d.CrossProduct(Vector3d.ZAxis, ri); rp2.Unitize();
    Plane tp = new Plane(C, ri, rp2);
    parts.Add(new Rectangle3d(tp, new Interval(-1.5 * h, tipFrac * h),
                                  new Interval(-0.5 * tw, 0.5 * tw)).ToNurbsCurve());

    if (tl > 1e-9)
    {
      Vector3d ub = -u;
      Vector3d ubP = Vector3d.CrossProduct(Vector3d.ZAxis, ub); ubP.Unitize();
      Plane tlp = new Plane(P0, ub, ubP);
      parts.Add(new Rectangle3d(tlp, new Interval(0.0, tl), new Interval(-0.5 * aw, 0.5 * aw))
                .ToNurbsCurve());
    }

    foreach (Curve c in parts)
      if (!c.IsClosed || !c.IsPlanar(tol))
        log.Add("A pawl outline piece is not closed / planar - union will fail.");

    Curve[] uni = Curve.CreateBooleanUnion(parts, tol);
    if (uni == null || uni.Length == 0)
    {
      Info = "Could not union the pawl outline - pieces are not overlapping or not coplanar.\n" +
             "boss d " + bd.ToString("0.##") + ", arm reach " + reach.ToString("0.##") +
             ", tip w " + tw.ToString("0.##") + ", tooth pitch " + toothPitch.ToString("0.##");
      return;
    }
    Curve prof = uni[0];
    foreach (Curve c in uni)
    {
      AreaMassProperties am = AreaMassProperties.Compute(c);
      AreaMassProperties ab = AreaMassProperties.Compute(prof);
      if (am != null && ab != null && am.Area > ab.Area) prof = c;
    }

    Extrusion ex = Extrusion.Create(prof, th, true);
    if (ex == null) { Info = "Could not extrude the pawl."; return; }
    Brep body = ex.ToBrep();
    if (body.SolidOrientation == BrepSolidOrientation.Inward) body.Flip();

    Brep hole = new Cylinder(new Circle(new Plane(new Point3d(P0.X, P0.Y, -1.0), Vector3d.ZAxis),
                                        0.5 * ph), th + 2.0).ToBrep(true, true);
    Brep[] diff = Brep.CreateBooleanDifference(body, hole, tol);
    if (diff != null && diff.Length > 0)
    {
      Brep best = diff[0]; double bv = -1.0;
      foreach (Brep b in diff) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best;
    }
    else log.Add("Pivot hole boolean failed.");

    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    Transform xf = Transform.PlaneToPlane(Plane.WorldXY, bp);
    body.Transform(xf);
    prof.Transform(xf);

    // ---------- report -------------------------------------------------------
    double F = Treq * 1000.0 / Rc;
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("wheel               tip dia " + Dr.ToString("0.###") +
                  ", root dia " + (2.0 * Rr).ToString("0.###"));
    sb.AppendLine("pivot at            (" + Dp.ToString("0.###") + ", 0)");
    sb.AppendLine("contact at          " + ca.ToString("0.#") + " deg, radius " + Rc.ToString("0.###"));
    sb.AppendLine("tip width           " + tw.ToString("0.###") + "   (tooth pitch " + toothPitch.ToString("0.###") + ")");
    sb.AppendLine("arm reach           " + reach.ToString("0.###") + ", width " + aw.ToString("0.###"));
    sb.AppendLine("tail                " + tl.ToString("0.###"));
    sb.AppendLine("--");
    sb.AppendLine("self-engaging       " + (selfEng ? "YES - load pulls the tip in" :
                                                      "NO - load levers the tip out"));
    sb.AppendLine("contact force       " + F.ToString("0") + " N at " + Treq.ToString("0.##") + " N.m");
    sb.AppendLine("--");
    sb.AppendLine("A spring must hold the tip against the wheel; the tail is for");
    sb.AppendLine("that spring and for the release lever.");
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string s2 in log) sb.AppendLine("! " + s2); }

    Body    = body;
    Profile = prof;
    Info    = sb.ToString();
