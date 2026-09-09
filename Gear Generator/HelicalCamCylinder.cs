// ===========================================================================
//  HELICAL CAM CYLINDER  -  Grasshopper C# component (RhinoCommon)
//  Barrel cam: pushing a pin down the groove rotates the cylinder, which
//  drives the gear, which drives the rack that opens the clamps.
//  ---------------------------------------------------------------------
//  Component INPUTS  (all "Item" access):
//     Diameter     double   cylinder outside diameter
//     Height       double   total cylinder height
//     Stroke       double   pin travel.  0 (or > Height) = use the WHOLE
//                           height, so the helix stretches to fit the cylinder
//     GrooveWidth  double   width MEASURED PERPENDICULAR to the groove path
//     GrooveDepth  double   radial depth
//     RackTeeth    double   rack teeth the gear must travel to open the clamps
//     GearTeeth    int      teeth on the gear
//     GearPlane    Plane    feed the RACK script's GearPlane here - the
//                           cylinder is then coaxial with the gear
//  Component OUTPUTS:
//     Body         cylinder with the groove cut (falls back to the blank)
//     Cutter       the groove solid(s), for a manual Solid Difference
//     Path         groove centreline curve(s)
//     Axis         the shared cylinder/gear axis, to check alignment
//     Info         kinematics report, diagnostics + warnings
//
//  Core relation, module-independent:  turns = RackTeeth / GearTeeth
//  (the gear rolls one full turn per GearTeeth of rack).
//  Lead (axial rise per turn) = Stroke / turns.
//  Force: push x Stroke = rack force x rack travel.
// ===========================================================================


// ---------------------------------------------------------------------------
//  paste this into  RunScript( ... )
//  signature:
//    private void RunScript(double Diameter, double Height, double Stroke,
//                           double GrooveWidth, double GrooveDepth,
//                           double RackTeeth, int GearTeeth, Plane GearPlane,
//                           ref object Body, ref object Cutter,
//                           ref object Path, ref object Axis, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double D  = Diameter    > 1e-9 ? Diameter    : 30.0;
    double H  = Height      > 1e-9 ? Height      : 60.0;
    double gw = GrooveWidth > 1e-9 ? GrooveWidth : 4.0;
    double gd = GrooveDepth > 1e-9 ? GrooveDepth : 2.0;
    double nT = RackTeeth   > 1e-9 ? RackTeeth   : 6.0;
    int    zg = GearTeeth   > 2    ? GearTeeth   : 24;

    // --- match these to the gear / rack scripts ------------------------------
    double mod    = 1.5;          // module - only used to report travel in mm
    double zOff   = 0.0;          // slide the cylinder along the gear axis.
                                  //   0        = base sits on the gear plane
                                  //   -0.5*H   = cylinder centred on the gear
    double margin = 0.0;          // groove inset from each end of the cylinder
    double turnsO = 0.0;          // > 0 overrides the rack-derived turn count
    int    starts = 1;            // grooves around the cylinder (2 = balanced pin load)
    double bore   = 0.0;          // central hole diameter, 0 = solid
    double phi0   = 0.0;          // groove start angle [deg]
    double hand   = 1.0;          // +1 right-hand helix, -1 left-hand
    int    spt    = 64;           // cutter sections per turn
    double over   = 0.0;          // radial overshoot for a clean cut (0 -> 5% of D)
    double botR   = 0.0;          // groove bottom corner radius.  0 -> GrooveWidth/2,
                                  // i.e. a full U that a ball-nose pin seats in
    double pinClr = 0.2;          // pin-to-groove clearance, for the report
    double endOverTop = -1.0;     // axial overshoot past the TOP of the cylinder, so the
                                  // groove breaks out and the sleeve threads on from above.
                                  // <0 = auto (one groove width when Stroke = 0, else 0)
    double endOverBot = 1.0;      // overshoot past the BOTTOM.  Keep this small when the
                                  // cam is fused to a gear below - it notches the gear face.

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    double R = 0.5 * D;
    if (over <= 1e-9) over = 0.05 * D;
    if (starts < 1) starts = 1;

    // stroke: 0 or oversized -> stretch the helix over the whole cylinder
    double S = Stroke;
    if (S <= 1e-9 || S > H - 2.0 * margin)
    {
      S = H - 2.0 * margin;
      if (Stroke > 1e-9) log.Add("Stroke exceeds the cylinder - stretched to the full height instead.");
    }
    if (S <= 1e-9) { Info = "No room for the groove - check Height and the end margin."; return; }

    if (gd >= R) { Info = "Groove depth reaches the cylinder axis."; return; }
    if (bore > 1e-9 && 2.0 * (R - gd) <= bore)
      log.Add("Groove breaks into the bore (wall " + (R - gd - 0.5 * bore).ToString("0.###") +
              ") - reduce GrooveDepth or the bore.");

    // ---------- kinematics ---------------------------------------------------
    double turns = turnsO > 1e-9 ? turnsO : nT / (double) zg;
    if (turns <= 1e-9) { Info = "Zero rotation - check RackTeeth / GearTeeth."; return; }

    double lead = S / turns;                              // axial rise per turn
    double lam  = Math.Atan2(lead, 2.0 * Math.PI * R);    // lead angle from horizontal
    double land = (lead / starts) * Math.Cos(lam) - gw;   // material between passes
    double rp   = 0.5 * mod * zg;
    double L    = nT * Math.PI * mod;                     // rack travel

    if (land <= 0.0)
      log.Add("Adjacent groove passes overlap (land " + land.ToString("0.###") +
              ") - use fewer turns, a narrower groove, or a bigger cylinder.");
    if (RhinoMath.ToDegrees(lam) < 15.0)
      log.Add("Lead angle " + RhinoMath.ToDegrees(lam).ToString("0.#") +
              " deg is shallow - the cam may jam instead of turning. Aim for 20 deg or more.");

    // ---------- groove cross-section, built once -----------------------------
    // (u across the path, v radial: v = 0 on the skin, -gd at the bottom).
    // The bottom is rounded so a ball-nose pin cannot wedge into a corner.
    double rw = 0.5 * gw;
    double q  = botR > 1e-9 ? botR : rw;      // 0 -> full semicircle
    if (q > rw) q = rw;
    if (q > gd) q = gd;
    int    nq = 8;                            // points per bottom corner

    List<double> pu = new List<double>();
    List<double> pv = new List<double>();
    pu.Add(-rw); pv.Add(over);                                  // outside the skin
    pu.Add(-rw); pv.Add(-gd + q);                               // down the left wall
    for (int i = 1; i < nq; i++)                                // left bottom corner
    {
      double th = Math.PI + 0.5 * Math.PI * i / nq;
      pu.Add(-(rw - q) + q * Math.Cos(th));
      pv.Add((-gd + q) + q * Math.Sin(th));
    }
    if (rw - q > 1e-9)                                          // flat between the corners
    { pu.Add(-(rw - q)); pv.Add(-gd); pu.Add(rw - q); pv.Add(-gd); }
    else
    { pu.Add(0.0); pv.Add(-gd); }
    for (int i = 1; i < nq; i++)                                // right bottom corner
    {
      double th = 1.5 * Math.PI + 0.5 * Math.PI * i / nq;
      pu.Add((rw - q) + q * Math.Cos(th));
      pv.Add((-gd + q) + q * Math.Sin(th));
    }
    pu.Add(rw); pv.Add(-gd + q);                                // up the right wall
    pu.Add(rw); pv.Add(over);

    // ---------- groove cutters ----------------------------------------------
    double z0   = margin + 0.5 * ((H - 2.0 * margin) - S);   // centre a short stroke
    double k    = hand * 2.0 * Math.PI * turns;

    // extend the cutter past both ends of the cylinder: a groove that stops
    // inside the blank leaves a sliver face and traps the pin at the top
    double eoT = endOverTop >= 0.0 ? endOverTop : (Stroke <= 1e-9 ? gw : 0.0);
    double eoB = endOverBot >= 0.0 ? endOverBot : (Stroke <= 1e-9 ? gw : 0.0);
    double uLo = -eoB / S, uHi = 1.0 + eoT / S;
    int    nSec = Math.Max(8, (int) (spt * turns * (uHi - uLo)) + 1);

    List<Brep>  cutters = new List<Brep>();
    List<Curve> paths   = new List<Curve>();
    int  nFlip = 0, nNotSolid = 0;

    for (int s = 0; s < starts; s++)
    {
      double off = RhinoMath.ToRadians(phi0) + s * 2.0 * Math.PI / starts;
      List<Point3d> mid  = new List<Point3d>();
      List<Curve>   sect = new List<Curve>();

      for (int i = 0; i <= nSec; i++)
      {
        double u   = uLo + (uHi - uLo) * i / nSec;
        double z   = z0 + S * u;
        double phi = off + k * u;
        double cf  = Math.Cos(phi), sf = Math.Sin(phi);

        Vector3d rad = new Vector3d(cf, sf, 0.0);
        Vector3d tng = new Vector3d(-R * sf * k, R * cf * k, S);   // helix tangent
        Vector3d w   = Vector3d.CrossProduct(rad, tng);            // across the path, on the surface
        w.Unitize();

        Point3d c = new Point3d(R * cf, R * sf, z);
        mid.Add(c);

        Point3d[] sp = new Point3d[pu.Count + 1];
        for (int j = 0; j < pu.Count; j++)
          sp[j] = c + w * pu[j] + rad * pv[j];
        sp[pu.Count] = sp[0];
        sect.Add(new Polyline(sp).ToPolylineCurve());
      }

      Brep[] lofts = Brep.CreateFromLoft(sect, Point3d.Unset, Point3d.Unset, LoftType.Straight, false);
      if (lofts == null || lofts.Length == 0) { Info = "Could not loft the groove cutter."; return; }
      Brep gb = lofts[0];
      if (lofts.Length > 1)
      { Brep[] j = Brep.JoinBreps(lofts, tol); if (j != null && j.Length > 0) gb = j[0]; }

      Brep capped = gb.CapPlanarHoles(tol);
      if (capped != null) gb = capped;

      // a lofted tube can come out inside-out; an inverted cutter makes the
      // boolean keep the groove and throw away the cylinder
      if (!gb.IsSolid) nNotSolid++;
      if (gb.SolidOrientation == BrepSolidOrientation.Inward) { gb.Flip(); nFlip++; }

      cutters.Add(gb);
      paths.Add(Curve.CreateInterpolatedCurve(mid, 3));
    }

    // ---------- blank + booleans --------------------------------------------
    Brep blank = new Cylinder(new Circle(Plane.WorldXY, R), H).ToBrep(true, true);
    if (blank.SolidOrientation == BrepSolidOrientation.Inward) blank.Flip();

    if (bore > 1e-9)
    {
      Brep bb = new Cylinder(new Circle(new Plane(new Point3d(0, 0, -1.0), Vector3d.ZAxis),
                                        0.5 * bore), H + 2.0).ToBrep(true, true);
      if (bb.SolidOrientation == BrepSolidOrientation.Inward) bb.Flip();
      Brep[] r = Brep.CreateBooleanDifference(blank, bb, tol);
      if (r != null && r.Length > 0) blank = r[0];
      else log.Add("Bore boolean failed.");
    }

    Brep body = blank;
    string boolState;
    Brep[] diff = Brep.CreateBooleanDifference(new Brep[] { blank }, cutters, tol);
    if (diff != null && diff.Length > 0)
    {
      // keep the biggest piece - a stray fragment is never the part we want
      Brep best = diff[0]; double bv = -1.0;
      foreach (Brep b in diff)
      {
        double v = b.GetBoundingBox(true).Volume;
        if (v > bv) { bv = v; best = b; }
      }
      body = best;
      boolState = "OK (" + diff.Length + " piece" + (diff.Length == 1 ? ")" : "s, kept the largest)");
    }
    else
    {
      boolState = "FAILED - use the Cutter output with a Solid Difference component";
      log.Add("Groove boolean failed - feed Cutter into a Solid Difference instead.");
    }

    // ---------- put it on the gear axis --------------------------------------
    Plane bp = GearPlane.IsValid ? GearPlane : Plane.WorldXY;
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
    Transform xf = Transform.PlaneToPlane(Plane.WorldXY, bp);

    body.Transform(xf);
    foreach (Brep b in cutters) b.Transform(xf);
    foreach (Curve c in paths)  c.Transform(xf);
    Line axis = new Line(bp.Origin, bp.Origin + bp.ZAxis * H);

    // ---------- report -------------------------------------------------------
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("cylinder            dia " + D.ToString("0.###") + " x " + H.ToString("0.###") + " high");
    sb.AppendLine("groove              " + gw.ToString("0.###") + " wide x " + gd.ToString("0.###") + " deep");
    sb.AppendLine("bottom radius       " + q.ToString("0.###") +
                  (q >= rw - 1e-9 ? "   (full U)" : ""));
    sb.AppendLine("suggested pin       dia " + (gw - pinClr).ToString("0.###") +
                  ", ball tip radius " + (0.5 * (gw - pinClr)).ToString("0.###"));
    sb.AppendLine("starts              " + starts + (hand > 0 ? "   right-hand" : "   left-hand"));
    sb.AppendLine("--");
    sb.AppendLine("stroke (groove run) " + S.ToString("0.###") +
                  (Stroke <= 1e-9 ? "   (full height)" : ""));
    sb.AppendLine("turns over stroke   " + turns.ToString("0.####") + "   (" +
                  (turns * 360.0).ToString("0.##") + " deg)");
    sb.AppendLine("lead (rise/turn)    " + lead.ToString("0.###"));
    sb.AppendLine("lead angle          " + RhinoMath.ToDegrees(lam).ToString("0.##") + " deg from horizontal");
    sb.AppendLine("land between passes " + land.ToString("0.###"));
    sb.AppendLine("groove overshoot    " + eoB.ToString("0.###") + " below, " +
                  eoT.ToString("0.###") + " above" +
                  (eoT > 1e-9 ? "   (breaks out on top - sleeve can slide on)" : "   (blind top)"));
    // largest through-bore the groove leaves, for a one-piece rotor
    double coreR = R - gd;
    sb.AppendLine("max bore (2.5 wall) " + Math.Max(0.0, 2.0 * (coreR - 2.5)).ToString("0.##") +
                  "   (core radius at the groove bottom " + coreR.ToString("0.##") + ")");
    sb.AppendLine("--");
    sb.AppendLine("gear z              " + zg + "   (module " + mod.ToString("0.###") +
                  ", pitch radius " + rp.ToString("0.###") + ")");
    sb.AppendLine("rack travel         " + L.ToString("0.###") + "   (" + nT.ToString("0.###") + " teeth)");
    sb.AppendLine("mech advantage      " + (L > 1e-9 ? (S / L).ToString("0.###") : "-") +
                  "   (rack force / push force)");
    sb.AppendLine("axis at             " + bp.Origin.ToString());
    sb.AppendLine("--");
    sb.AppendLine("DIAGNOSTICS");
    sb.AppendLine("  blank solid       " + blank.IsSolid);
    sb.AppendLine("  cutters           " + cutters.Count + ", not-solid " + nNotSolid + ", flipped " + nFlip);
    sb.AppendLine("  boolean           " + boolState);
    sb.AppendLine("  body solid        " + body.IsSolid);
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Body   = body;
    Cutter = cutters;
    Path   = paths;
    Axis   = axis;
    Info   = sb.ToString();
