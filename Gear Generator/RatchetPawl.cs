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


// ===========================================================================
//  THIS BUILD - 10 inputs to create on the component:
//     RatchetDia 24   Teeth 40   ToothDepth 0.8   PivotDist 16
//     ContactAngle 25   ArmWidth 5   Thickness 6   PivotHoleDia 3.2
//     TailLength 12
//     BasePlane <- the RACK script's GearPlane
//  Every constant below is already set for this build.
//
//  THE PAWL IS A HOOK. A straight arm running from the pivot TO the contact
//  point can NEVER clear the wheel - the perpendicular distance from the
//  centre to such a chord is at most the contact radius, which is inside the
//  tip circle by definition. So the arm stops outside the teeth and a narrow
//  finger carries on down into the tooth space. The Info output reports the
//  arm and boss clearances; both must stay positive.
//
//  CONTACT ANGLE 25, NOT 60. Self-engagement flips sense at about 44 deg,
//  where the contact force line passes through the pivot. Either side of that
//  works - but below it the arm runs almost straight at the contact, so 99.7%
//  of the load is COMPRESSION along the arm instead of bending it, and the
//  whole pawl is far more compact. Which sense you need depends on the cam's
//  hand; flip `hand` on the WHEEL if it locks on the push.
//
//  PivotDist 16 must equal pawlX in HousingRatchet.cs, which builds the post
//  this pawl pivots on. TailLength 12 puts the tail tip at radius 23.16,
//  angle -29.0 deg - that is what sets the housing's RelSlotDeg.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double RatchetDia, int Teeth, double ToothDepth, double PivotDist,
//                           double ContactAngle, double ArmWidth, double Thickness,
//                           double PivotHoleDia, double TailLength, Plane BasePlane,
//                           ref object Body, ref object Profile, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double Dr = RatchetDia   > 1e-9 ? RatchetDia   : 24.0;   // match the wheel
    int    nz = Teeth        > 3    ? Teeth        : 40;
    double h  = ToothDepth   > 1e-9 ? ToothDepth   : 0.8;
    double Dp = PivotDist    > 1e-9 ? PivotDist    : 16.0;   // inside the housing bore
    double ca = ContactAngle > 1e-9 ? ContactAngle : 25.0;   // self-engagement flips
                                                         // sense near 44 deg - see Info.
    double aw = ArmWidth     > 1e-9 ? ArmWidth     : 5.0;
    double th = Thickness    > 1e-9 ? Thickness    : 6.0;    // match the wheel
    double ph = PivotHoleDia > 1e-9 ? PivotHoleDia : 3.2;
    double tl = TailLength   > 0.0  ? TailLength   : 12.0;

    // --- extras --------------------------------------------------------------
    double bossDia = 6.8;    // pivot boss diameter. MAX is 2*(PivotDist - R), or
                             // the boss eats into the wheel. At 16 / r12 that is 8.0
    double armClr  = 0.5;    // gap between the arm's inner edge and the tip circle
    double rootClr = 0.15;   // gap under the finger, so it never bottoms out
    double lockAng = 8.0;    // MUST match the wheel's LockAngle
    double Treq    = 1.92;   // holding torque [N.m], for the force report
    double zOff    = 32.0;   // level with the ratchet wheel

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    double R  = 0.5 * Dr, Rr = R - h;
    double bd = bossDia > 1e-9 ? bossDia : 6.8;
    double cr = RhinoMath.ToRadians(ca);

    // THE PAWL IS A HOOK, NOT A STRAIGHT BAR.
    // A straight arm running from the pivot TO the contact point can never
    // clear the wheel: the perpendicular distance from the centre to such a
    // chord is at most the contact radius, which is inside the tip circle by
    // definition. So the ARM stops outside the teeth, at radius rt, and only a
    // narrow FINGER carries on down into the tooth space.
    Point3d P0 = new Point3d(Dp, 0.0, 0.0);                  // pivot
    double  rt = R + armClr + 0.5 * aw;                      // arm centreline, far end
    Point3d A  = new Point3d(rt * Math.Cos(cr), rt * Math.Sin(cr), 0.0);
    double  Rc = R - 0.5 * h;                                // contact, mid-face
    Point3d C  = new Point3d(Rc * Math.Cos(cr), Rc * Math.Sin(cr), 0.0);

    Vector3d u = A - P0;
    double reach = u.Length;
    if (reach < 1e-6) { Info = "Arm has no length - check PivotDist and ContactAngle."; return; }
    u.Unitize();

    // ---------- does anything cut into the wheel? ----------------------------
    double Dmin  = Math.Abs(P0.X * (A.Y - P0.Y) - P0.Y * (A.X - P0.X)) / reach;
    double tFoot = -(P0.X * u.X + P0.Y * u.Y);
    if (tFoot < 0.0 || tFoot > reach)
      Dmin = Math.Min(P0.DistanceTo(Point3d.Origin), A.DistanceTo(Point3d.Origin));
    double armGap  = Dmin - 0.5 * aw - R;
    double bossGap = Dp - 0.5 * bd - R;
    if (armGap < 0.0)
      log.Add("ARM CUTS THE WHEEL by " + (-armGap).ToString("0.###") +
              " - raise armClr, move the pivot out, or reduce ContactAngle.");
    if (bossGap < 0.0)
      log.Add("BOSS CUTS THE WHEEL by " + (-bossGap).ToString("0.###") +
              " - bossDia must be under " + (2.0 * (Dp - R)).ToString("0.##") + ".");
    if (Dp <= R) log.Add("Pivot is inside the wheel tip circle.");

    // ---------- self-engagement, off the REAL locking face -------------------
    // The old check assumed a purely tangential load and ignored LockAngle.
    // The face is undercut, so the contact normal leans - which is the whole
    // point of the undercut, and it flips the answer near the neutral angle.
    double tLean = h * Math.Tan(RhinoMath.ToRadians(lockAng));
    double dPhi  = Math.Asin(Math.Max(-0.99, Math.Min(0.99, tLean / R)));
    Point3d fA = new Point3d(Rr * Math.Cos(cr), Rr * Math.Sin(cr), 0.0);
    Point3d fB = new Point3d(R * Math.Cos(cr + dPhi), R * Math.Sin(cr + dPhi), 0.0);
    Vector3d fu = fB - fA; fu.Unitize();
    Vector3d nrm = new Vector3d(fu.Y, -fu.X, 0.0);        // out of the tooth
    Vector3d arm = C - P0;
    double   mom = arm.X * nrm.Y - arm.Y * nrm.X;
    double   sgn = mom >= 0.0 ? 1.0 : -1.0;
    Vector3d tipV = new Vector3d(-arm.Y * sgn, arm.X * sgn, 0.0);
    Vector3d rIn  = new Vector3d(-C.X / Rc, -C.Y / Rc, 0.0);
    bool selfEng  = (tipV.X * rIn.X + tipV.Y * rIn.Y) > 0.0;

    // how much of the load runs ALONG the arm - the whole design aim
    Vector3d fDir = new Vector3d(-Math.Sin(cr), Math.Cos(cr), 0.0);
    double along  = Math.Abs(fDir.X * u.X + fDir.Y * u.Y);

    // ---------- outline: boss + arm + finger + tail --------------------------
    // every piece MUST lie in the same horizontal plane or the 2D union fails.
    // Plane(origin, xDir, yDir) takes the normal as xDir X yDir, so pair each
    // in-plane direction with Z X itself - never with ZAxis directly.
    List<Curve> parts = new List<Curve>();
    parts.Add(new ArcCurve(new Circle(new Plane(P0, Vector3d.ZAxis), 0.5 * bd)));

    Vector3d uP = Vector3d.CrossProduct(Vector3d.ZAxis, u); uP.Unitize();
    parts.Add(new Rectangle3d(new Plane(P0, u, uP), new Interval(0.0, reach),
                              new Interval(-0.5 * aw, 0.5 * aw)).ToNurbsCurve());

    // finger: narrow enough to sit in a tooth space. It starts at radius rt,
    // i.e. ON the arm, so the two overlap and actually fuse.
    double toothPitch = 2.0 * Math.PI * Rc / nz;
    double tw = Math.Min(aw, 0.65 * toothPitch);
    double rFing = Rr + rootClr;
    Vector3d ri = new Vector3d(-Math.Cos(cr), -Math.Sin(cr), 0.0);
    Vector3d rp2 = Vector3d.CrossProduct(Vector3d.ZAxis, ri); rp2.Unitize();
    parts.Add(new Rectangle3d(new Plane(A, ri, rp2), new Interval(0.0, rt - rFing),
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
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
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
    sb.AppendLine("arm                 " + reach.ToString("0.###") + " long, " +
                  aw.ToString("0.###") + " wide, ending at radius " + rt.ToString("0.###"));
    sb.AppendLine("finger              " + tw.ToString("0.###") + " wide, radius " +
                  rFing.ToString("0.###") + " .. " + rt.ToString("0.###") +
                  "   (tooth pitch " + toothPitch.ToString("0.###") + ")");
    sb.AppendLine("tail                " + tl.ToString("0.###"));
    sb.AppendLine("--");
    sb.AppendLine("CLEARANCE OFF THE WHEEL   tip circle is radius " + R.ToString("0.##"));
    sb.AppendLine("  arm inner edge    " + armGap.ToString("0.###") +
                  (armGap < 0.0 ? "   *** CUTS THE WHEEL ***" : ""));
    sb.AppendLine("  boss              " + bossGap.ToString("0.###") +
                  (bossGap < 0.0 ? "   *** CUTS THE WHEEL ***" : ""));
    sb.AppendLine("  under the finger  " + rootClr.ToString("0.###") + " off the root");
    sb.AppendLine("--");
    sb.AppendLine("self-engaging       " + (selfEng ? "YES - load pulls the finger in" :
                                                      "NO - load levers the finger out"));
    sb.AppendLine("contact force       " + F.ToString("0") + " N at " + Treq.ToString("0.##") + " N.m");
    sb.AppendLine("  " + (100.0 * along).ToString("0.#") + "% of it runs ALONG the arm -> " +
                  (F * along / (aw * th)).ToString("0.#") + " MPa compression, " +
                  (F * Math.Sqrt(Math.Max(0.0, 1.0 - along * along))).ToString("0") +
                  " N transverse");
    if (along < 0.9)
      sb.AppendLine("  under 90% - the arm is being bent, not compressed. Retune ContactAngle.");
    sb.AppendLine("--");
    Point3d tTip = P0 - u * tl;
    sb.AppendLine("TAIL TIP            radius " + tTip.DistanceTo(Point3d.Origin).ToString("0.###") +
                  " at " + RhinoMath.ToDegrees(Math.Atan2(tTip.Y, tTip.X)).ToString("0.#") + " deg");
    sb.AppendLine("  -> set the housing's RelSlotDeg to that angle.");
    sb.AppendLine("--");
    sb.AppendLine("A spring must hold the finger against the wheel; the tail is for");
    sb.AppendLine("that spring and for the release lever.");
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string s2 in log) sb.AppendLine("! " + s2); }

    Body    = body;
    Profile = prof;
    Info    = sb.ToString();
