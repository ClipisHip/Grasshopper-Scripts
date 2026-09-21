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
//     RatchetDia 24   Teeth 30   ToothDepth 1.4   PivotDist 16
//     ContactAngle 25   ArmWidth 5   Thickness 6   PivotHoleDia 3.2
//     TailLength 12
//     BasePlane <- the RACK script's GearPlane
//  Every constant below is already set for this build.
//
//  THE PAWL IS A HOOK. A straight arm running from the pivot TO the contact
//  point can NEVER clear the wheel - the perpendicular distance from the
//  centre to such a chord is at most the contact radius, which is inside the
//  tip circle by definition. So the arm stops outside the teeth and a WEDGE
//  carries on down into the tooth space. The Info output reports the arm and
//  boss clearances; both must stay positive.
//
//  THE TIP IS A WEDGE, NOT A FINGER. The notch between two teeth is a V. The
//  old flat-bottomed finger stopped wherever the V narrowed to its own width,
//  so it bore on only ~40% of the face however deep the tooth was - 0.34 mm
//  of a 0.8 mm tooth. The wedge is built from the tooth's own lines: loaded
//  flank ON the locking face, back flank along the ramp of the tooth behind,
//  so it bears on nearly the whole face. It is also far stronger at its root.
//
//  Checked by simulation against the wheel: no overlap when engaged; turning
//  the wheel the free way lifts the pawl smoothly to 8.9 deg and drops it into
//  the next notch; turning it the loaded way, any lift only deepens the overlap
//  - the load cannot lever the pawl out.
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
    int    nz = Teeth        > 3    ? Teeth        : 30;     // match the wheel
    double h  = ToothDepth   > 1e-9 ? ToothDepth   : 1.4;    // match the wheel
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
    double rootClr = 0.15;   // the wedge tip must stay at least this far off the root
    double rampClr = 0.15;   // gap between the wedge's back flank and the ramp
                             // of the tooth behind - it rides this when ratcheting
    double wedgeTop = 0.85;  // wedge width at the tip circle, as a fraction of pitch
    double lockAng = 8.0;    // MUST match the wheel's LockAngle
    double mu      = 0.25;   // friction on the face, for the release-force report
    double pegR    = 8.0;    // spring peg, along the TAIL from the pivot.
                             // MUST match the spring's LegA.
    double pegDia  = 2.0;
    double pegH    = 2.0;    // up off the pawl top, into the spring's band
    double Treq    = 1.92;   // holding torque [N.m] - WORST case, the two-stage
                             // cam's squeeze. The constant-lead cam is half this.
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
    // definition. So the ARM stops outside the teeth, at radius rt, and a
    // WEDGE carries on down into the tooth space.
    Point3d P0 = new Point3d(Dp, 0.0, 0.0);                  // pivot
    double  rt = R + armClr + 0.5 * aw;                      // arm centreline, far end
    Point3d A  = new Point3d(rt * Math.Cos(cr), rt * Math.Sin(cr), 0.0);

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

    // ---------- the wedge, built from the tooth's OWN lines -------------------
    // The wheel is phased so a tooth ROOT sits at ContactAngle; this is drawn
    // against that tooth. Same lines the wheel script generates, same lean.
    double tLean = h * Math.Tan(RhinoMath.ToRadians(lockAng));
    double dPhi  = Math.Asin(Math.Max(-0.99, Math.Min(0.99, tLean / R)));
    double pa    = 2.0 * Math.PI / nz;                                  // one tooth
    Point3d F0 = new Point3d(Rr * Math.Cos(cr), Rr * Math.Sin(cr), 0.0);              // face root
    Point3d F1 = new Point3d(R * Math.Cos(cr + dPhi), R * Math.Sin(cr + dPhi), 0.0);  // face tip
    Point3d G1 = new Point3d(R * Math.Cos(cr - pa + dPhi), R * Math.Sin(cr - pa + dPhi), 0.0);

    // the ramp of the tooth behind (G1 -> F0), pushed rampClr into the notch
    Vector3d rv = F0 - G1; rv.Unitize();
    Vector3d rn = new Vector3d(rv.Y, -rv.X, 0.0);
    if (rn.X * Math.Cos(cr) + rn.Y * Math.Sin(cr) < 0.0) rn = -rn;
    Line faceLn = new Line(F0, F1);
    Line rampLn = new Line(G1 + rn * rampClr, F0 + rn * rampClr);

    double tA, tB;
    if (!Rhino.Geometry.Intersect.Intersection.LineLine(faceLn, rampLn, out tA, out tB, 0.0, false))
    { Info = "Face and ramp are parallel - check Teeth / ToothDepth."; return; }
    Point3d Pb = faceLn.PointAt(tA);                                    // wedge tip

    double aTop = cr + dPhi;                                            // face tip angle
    double aBack = aTop - wedgeTop * pa;                                // back edge angle
    Point3d P2 = new Point3d(rt * Math.Cos(aTop),  rt * Math.Sin(aTop),  0.0);
    Point3d P3 = new Point3d(rt * Math.Cos(aBack), rt * Math.Sin(aBack), 0.0);
    Line backLn = new Line(Point3d.Origin, P3);
    if (!Rhino.Geometry.Intersect.Intersection.LineLine(backLn, rampLn, out tA, out tB, 0.0, false))
    { Info = "Could not place the wedge's back edge."; return; }
    Point3d P4 = backLn.PointAt(tA);

    // tip -> up the loaded flank -> into the arm -> back edge -> down the ramp flank
    Polyline wedgePl = new Polyline(new Point3d[] { Pb, F1, P2, P3, P4, Pb });

    double tipGap  = Pb.DistanceTo(Point3d.Origin) - Rr;               // wedge tip off the root
    double engaged = Pb.DistanceTo(F1);                                // face it bears on
    double wTop    = wedgeTop * pa * R;                                // width at the tip circle
    if (tipGap < rootClr)
      log.Add("Wedge tip is only " + tipGap.ToString("0.###") + " off the root - raise rampClr.");

    Point3d C  = 0.5 * (Pb + F1);                                      // middle of the contact
    double  Rc = C.DistanceTo(Point3d.Origin);

    // ---------- self-engagement, off the REAL locking face -------------------
    // The contact normal comes from the actual face, lean included. The face
    // leans AWAY from the pawl, so the load pushes the wedge slightly OUT; the
    // pivot geometry has to more than cancel that.
    Vector3d fu = F1 - F0; fu.Unitize();
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

    // the wedge - its top edge sits at radius rt, inside the arm, so they fuse
    parts.Add(wedgePl.ToNurbsCurve());

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
             ", wedge width " + wTop.ToString("0.##") + ", tip at r " +
             Pb.DistanceTo(Point3d.Origin).ToString("0.##");
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

    // ---------- peg for the spring's lower leg --------------------------------
    // Stands up off the TAIL, into the spring's own band above the pawl. The
    // leg is radial from the pin, so the peg sits on the tail centreline at
    // pegR from the pivot - which is what fixes the spring's aA angle.
    if (pegDia > 1e-9 && pegH > 1e-9)
    {
      if (pegR + 0.5 * pegDia > tl)
        log.Add("Spring peg at " + pegR.ToString("0.##") + " runs off the end of a " +
                tl.ToString("0.##") + " tail - lengthen TailLength.");
      Point3d pg = P0 - u * pegR;
      Brep peg = new Cylinder(new Circle(new Plane(new Point3d(pg.X, pg.Y, th), Vector3d.ZAxis),
                                         0.5 * pegDia), pegH).ToBrep(true, true);
      if (peg.SolidOrientation == BrepSolidOrientation.Inward) peg.Flip();
      Brep[] pu = Brep.CreateBooleanUnion(new List<Brep>() { body, peg }, tol);
      if (pu != null && pu.Length > 0)
      {
        Brep best = pu[0]; double bv = -1.0;
        foreach (Brep b in pu) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
        body = best;
      }
      else log.Add("Spring peg union failed.");
    }

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
    double F      = Treq * 1000.0 / Rc;
    double faceP  = F / (engaged * th);                        // pressure on the face
    double wedgeB = F * (R - Rc) / (th * wTop * wTop / 6.0);   // wedge root, at the tip circle
    double seArm  = Math.Abs(mom);                             // what keeps it in ...
    double frArm  = Math.Abs(arm.X * fu.Y - arm.Y * fu.X);     // ... and friction along the face
    double relF   = F * (seArm + mu * frArm) / tl;             // ... which the release must beat

    // how far the pawl must swing for the wedge tip to clear the teeth
    double liftDeg = 0.0;
    for (int kL = 0; kL <= 600; kL++)
    {
      double aL = -RhinoMath.ToRadians(0.05 * kL);
      double lx = Pb.X - P0.X, ly = Pb.Y - P0.Y;
      double qx = P0.X + lx * Math.Cos(aL) - ly * Math.Sin(aL);
      double qy = P0.Y + lx * Math.Sin(aL) + ly * Math.Cos(aL);
      liftDeg = 0.05 * kL;
      if (qx * qx + qy * qy >= R * R) break;
    }
    double tailMove = tl * RhinoMath.ToRadians(liftDeg);

    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("wheel               tip dia " + Dr.ToString("0.###") +
                  ", root dia " + (2.0 * Rr).ToString("0.###"));
    sb.AppendLine("pivot at            (" + Dp.ToString("0.###") + ", 0)");
    sb.AppendLine("contact at          " + ca.ToString("0.#") + " deg, radius " + Rc.ToString("0.###"));
    sb.AppendLine("arm                 " + reach.ToString("0.###") + " long, " +
                  aw.ToString("0.###") + " wide, ending at radius " + rt.ToString("0.###"));
    sb.AppendLine("wedge               bears on " + engaged.ToString("0.###") + " of the " +
                  h.ToString("0.##") + " face,  " + wTop.ToString("0.###") +
                  " wide at the tip circle  (pitch " + (pa * R).ToString("0.###") + ")");
    sb.AppendLine("tail                " + tl.ToString("0.###"));
    sb.AppendLine("--");
    sb.AppendLine("CLEARANCE OFF THE WHEEL   tip circle is radius " + R.ToString("0.##"));
    sb.AppendLine("  arm inner edge    " + armGap.ToString("0.###") +
                  (armGap < 0.0 ? "   *** CUTS THE WHEEL ***" : ""));
    sb.AppendLine("  boss              " + bossGap.ToString("0.###") +
                  (bossGap < 0.0 ? "   *** CUTS THE WHEEL ***" : ""));
    sb.AppendLine("  wedge tip         " + tipGap.ToString("0.###") + " off the root");
    sb.AppendLine("--");
    sb.AppendLine("self-engaging       " + (selfEng ? "YES - load pulls the wedge in" :
                                                      "NO - load levers the wedge out") +
                  ",  moment arm " + seArm.ToString("0.##"));
    sb.AppendLine("contact force       " + F.ToString("0") + " N at " + Treq.ToString("0.##") + " N.m");
    sb.AppendLine("  " + (100.0 * along).ToString("0.#") + "% of it runs ALONG the arm -> " +
                  (F * along / (aw * th)).ToString("0.#") + " MPa compression, " +
                  (F * Math.Sqrt(Math.Max(0.0, 1.0 - along * along))).ToString("0") +
                  " N transverse");
    if (along < 0.9)
      sb.AppendLine("  under 90% - the arm is being bent, not compressed. Retune ContactAngle.");
    sb.AppendLine("face pressure       " + faceP.ToString("0.#") + " MPa   on " +
                  engaged.ToString("0.##") + " x " + th.ToString("0.#") +
                  "   - the number that limits a ratchet this small");
    sb.AppendLine("wedge bending       " + wedgeB.ToString("0.#") + " MPa   at its root   (POM ~65)");
    sb.AppendLine("--");
    sb.AppendLine("RELEASE UNDER LOAD");
    sb.AppendLine("  lift to clear     " + liftDeg.ToString("0.#") + " deg  ->  tail moves " +
                  tailMove.ToString("0.##") + " mm");
    sb.AppendLine("  force on the tail " + relF.ToString("0") + " N   at " + Treq.ToString("0.##") +
                  " N.m, mu " + mu.ToString("0.##"));
    sb.AppendLine("  The moment that keeps the pawl in under load is the SAME one the");
    sb.AppendLine("  release has to beat. Unload it first - push the sleeve down a");
    sb.AppendLine("  touch - and the lever only has to fight the pawl spring.");
    sb.AppendLine("--");
    Point3d tTip = P0 - u * tl;
    sb.AppendLine("TAIL TIP            radius " + tTip.DistanceTo(Point3d.Origin).ToString("0.###") +
                  " at " + RhinoMath.ToDegrees(Math.Atan2(tTip.Y, tTip.X)).ToString("0.#") + " deg");
    sb.AppendLine("  -> set the housing's RelSlotDeg to that angle.");
    if (pegDia > 1e-9)
    {
      Point3d pgr = P0 - u * pegR;
      sb.AppendLine("SPRING PEG          dia " + pegDia.ToString("0.##") + " at (" +
                    pgr.X.ToString("0.##") + ", " + pgr.Y.ToString("0.##") + "), z " +
                    (zOff + th).ToString("0.##") + " .. " + (zOff + th + pegH).ToString("0.##"));
      sb.AppendLine("  = " + pegR.ToString("0.##") + " from the pin at " +
                    RhinoMath.ToDegrees(Math.Atan2(-u.Y, -u.X)).ToString("0.#") +
                    " deg -> the spring's LegA / aA.");
    }
    sb.AppendLine("--");
    sb.AppendLine("A spring must hold the wedge against the wheel; the tail is for");
    sb.AppendLine("that spring and for the release lever.");
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string s2 in log) sb.AppendLine("! " + s2); }

    Body    = body;
    Profile = prof;
    Info    = sb.ToString();
