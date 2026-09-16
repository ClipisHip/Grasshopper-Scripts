// ===========================================================================
//  HELICAL CAM - TWO STAGE  -  Grasshopper C# component (RhinoCommon)
//  Same barrel cam as HelicalCamCylinder.cs, but the groove's LEAD changes
//  along the stroke: shallow where the jaws are just travelling, steep where
//  they are squeezing the cloth.
//
//  HelicalCamCylinder.cs is the constant-lead version and is LEFT ALONE.
//  Build one or the other, not both.
//
//  WHY. F_rack x travel = F_push x stroke. Both sides are fixed, so doubling
//  the jaw travel to 30 mm halved the grip to 79 N and no constant-lead cam
//  can avoid that. But the jaws only need FORCE at the very end - for most of
//  the travel they are moving through air against nothing but the return
//  springs. Spending most of the stroke on fast, cheap travel and the last
//  part on a steep lead puts the force where the cloth actually is.
//
//      phase           jaw travel   push stroke   lead    force
//      fast (upper)      24 mm        9.7 mm      30.4     60 N
//      squeeze (lower)    6 mm        6.3 mm      79.4    158 N
//
//  The identity still holds - it is just no longer spent evenly.
//
//  THE SQUEEZE IS AT THE BOTTOM. Pushing DOWN closes the jaws, so the last
//  part of the push is the low-z end of the groove. That is where the steep
//  lead goes.
//
//  THE TRANSITION IS BLENDED, not a corner. A step change in lead is a kink
//  in the groove, and a kink is exactly what jams a follower pin. The lead
//  ramps over BlendFrac of the total rotation either side of the split.
// ===========================================================================


// ===========================================================================
//  THIS BUILD - 9 inputs to create on the component:
//     Diameter 18   Height 24   GrooveWidth 4   GrooveDepth 2
//     RackTeeth 6.366   GearTeeth 16
//     SqueezeTravel 6   ForceRatio 2.61
//     GearPlane <- the RACK script's GearPlane
//  Every constant below is already set for this build.
//
//  ForceRatio is how much harder the squeeze phase pulls than the fast phase.
//  2.61 is what puts the squeeze back at the 158 N the 15 mm build had.
//  Raise it for more grip and the fast phase gets shallower - watch the lead
//  angle, because under about 25 deg it stops back-driving and the return
//  springs can no longer open the jaws.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double Diameter, double Height, double GrooveWidth,
//                           double GrooveDepth, double RackTeeth, int GearTeeth,
//                           double SqueezeTravel, double ForceRatio, Plane GearPlane,
//                           ref object Body, ref object Cutter,
//                           ref object Path, ref object Axis, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double D  = Diameter      > 1e-9 ? Diameter      : 18.0;
    double H  = Height        > 1e-9 ? Height        : 24.0;
    double gw = GrooveWidth   > 1e-9 ? GrooveWidth   : 4.0;
    double gd = GrooveDepth   > 1e-9 ? GrooveDepth   : 2.0;
    double nT = RackTeeth     > 1e-9 ? RackTeeth     : 6.366;  // = 30 mm / (pi x 1.5)
    int    zg = GearTeeth     > 2    ? GearTeeth     : 16;
    double sq = SqueezeTravel > 1e-9 ? SqueezeTravel : 6.0;    // jaw travel in the squeeze
    double fr = ForceRatio    > 1.0  ? ForceRatio    : 2.61;   // lead2 / lead1

    // --- match these to the gear / rack / sleeve scripts ----------------------
    double mod    = 1.5;
    double zOff   = 6.0;
    double margin = 4.0;
    int    starts = 2;
    double bore   = 6.4;
    double phi0   = 90.0;
    double hand   = 1.0;
    int    spt    = 64;
    double over   = 0.0;
    double botR   = 0.0;
    double pinClr = 0.2;
    double blendF = 0.15;    // half-blend, as a fraction of TOTAL turns
    double Fpush  = 150.0;   // assumed push force, for the report

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    double R = 0.5 * D;
    if (over <= 1e-9) over = 0.05 * D;
    if (starts < 1) starts = 1;

    double S = H - 2.0 * margin;                 // the whole run is the stroke
    if (S <= 1e-9) { Info = "No room for the groove - check Height and margin."; return; }
    if (gd >= R)   { Info = "Groove depth reaches the cylinder axis."; return; }
    if (bore > 1e-9 && 2.0 * (R - gd) <= bore)
      log.Add("Groove breaks into the bore (wall " + (R - gd - 0.5 * bore).ToString("0.###") + ").");

    // ---------- kinematics ---------------------------------------------------
    double rp     = 0.5 * mod * zg;              // gear pitch radius
    double L      = nT * Math.PI * mod;          // TOTAL jaw travel
    double turns  = nT / (double) zg;
    if (turns <= 1e-9) { Info = "Zero rotation - check RackTeeth / GearTeeth."; return; }
    if (sq >= L) { Info = "SqueezeTravel is the whole stroke - use the constant-lead cam."; return; }

    double turns2 = sq / (2.0 * Math.PI * rp);   // squeeze, at the BOTTOM
    double tau2   = turns2 / turns;              // ... as a fraction of the rotation
    double hb     = Math.Min(blendF, 0.49 * Math.Min(tau2, 1.0 - tau2));
    double aB     = tau2 - hb, bB = tau2 + hb;   // blend runs from aB to bB

    // Solve the leads so the stroke comes out EXACTLY S, blend included:
    //   S = turns * lead1 * [ fr*bB + hb*(1-fr) + (1-bB) ]
    double shape = fr * bB + hb * (1.0 - fr) + (1.0 - bB);
    if (shape <= 1e-9) { Info = "ForceRatio / SqueezeTravel leave no stroke."; return; }
    double lead1 = S / (turns * shape);          // fast phase, upper
    double lead2 = fr * lead1;                   // squeeze, lower

    double lam1 = Math.Atan2(lead1, 2.0 * Math.PI * R);
    double lam2 = Math.Atan2(lead2, 2.0 * Math.PI * R);
    double F1   = Fpush * lead1 / (2.0 * Math.PI * rp);
    double F2   = Fpush * lead2 / (2.0 * Math.PI * rp);
    double land = (lead1 / starts) * Math.Cos(lam1) - gw;   // worst at the SHALLOW lead

    if (RhinoMath.ToDegrees(lam1) < 25.0)
      log.Add("Fast-phase lead angle " + RhinoMath.ToDegrees(lam1).ToString("0.#") +
              " deg is under 25 - the return springs may not be able to back-drive " +
              "it, and the jaws will not spring open. Lower ForceRatio.");
    if (land <= 0.0)
      log.Add("Adjacent groove passes overlap (land " + land.ToString("0.###") + ").");
    double wrapDeg = turns * 360.0 + 360.0 * gw / (2.0 * Math.PI * R);
    if (starts > 1 && wrapDeg > 360.0 / starts)
      log.Add("With " + starts + " starts the grooves collide: " + wrapDeg.ToString("0.#") +
              " deg of wrap against " + (360.0 / starts).ToString("0.#") + " available.");

    // z as a function of the turn fraction t - the integral of lead(t)
    Func<double, double> Zof = delegate(double t)
    {
      double e;
      if (t <= aB)      e = lead2 * t;
      else if (t <= bB) e = lead2 * aB + lead2 * (t - aB)
                          + 0.5 * (lead1 - lead2) * (t - aB) * (t - aB) / (bB - aB);
      else              e = lead2 * aB + 0.5 * (lead1 + lead2) * (bB - aB) + lead1 * (t - bB);
      return turns * e;
    };
    Func<double, double> Lof = delegate(double t)
    {
      if (t <= aB) return lead2;
      if (t >= bB) return lead1;
      return lead2 + (lead1 - lead2) * (t - aB) / (bB - aB);
    };

    // ---------- groove cross-section, built once -----------------------------
    double rw = 0.5 * gw;
    double q  = botR > 1e-9 ? botR : rw;
    bool fullU = Math.Abs(gd - rw) < 1e-6 && botR <= 1e-9;
    if (q > rw) q = rw;
    if (q > gd) q = gd;
    int nq = 8;

    List<double> pu = new List<double>();
    List<double> pv = new List<double>();
    pu.Add(-rw); pv.Add(over);
    pu.Add(-rw); pv.Add(-gd + q);
    for (int i = 1; i < nq; i++)
    {
      double th = Math.PI + 0.5 * Math.PI * i / nq;
      pu.Add(-(rw - q) + q * Math.Cos(th));
      pv.Add((-gd + q) + q * Math.Sin(th));
    }
    if (rw - q > 1e-9) { pu.Add(-(rw - q)); pv.Add(-gd); pu.Add(rw - q); pv.Add(-gd); }
    else               { pu.Add(0.0); pv.Add(-gd); }
    for (int i = 1; i < nq; i++)
    {
      double th = 1.5 * Math.PI + 0.5 * Math.PI * i / nq;
      pu.Add((rw - q) + q * Math.Cos(th));
      pv.Add((-gd + q) + q * Math.Sin(th));
    }
    pu.Add(rw); pv.Add(-gd + q);
    pu.Add(rw); pv.Add(over);

    // ---------- groove cutters ----------------------------------------------
    double z0 = margin;
    double k  = hand * 2.0 * Math.PI * turns;
    int nSec  = Math.Max(16, (int) (spt * turns) + 1);

    List<Brep>  cutters = new List<Brep>();
    List<Curve> paths   = new List<Curve>();
    int nFlip = 0, nNotSolid = 0;

    for (int s = 0; s < starts; s++)
    {
      double off = RhinoMath.ToRadians(phi0) + s * 2.0 * Math.PI / starts;
      List<Point3d> mid  = new List<Point3d>();
      List<Curve>   sect = new List<Curve>();

      for (int i = 0; i <= nSec; i++)
      {
        double t   = (double) i / nSec;
        double z   = z0 + Zof(t);
        double phi = off + k * t;
        double cf  = Math.Cos(phi), sf = Math.Sin(phi);

        Vector3d rad = new Vector3d(cf, sf, 0.0);
        // tangent: dphi/dt is constant, dz/dt is the LOCAL lead x turns
        Vector3d tng = new Vector3d(-R * sf * k, R * cf * k, Lof(t) * turns);
        Vector3d w   = Vector3d.CrossProduct(rad, tng);
        w.Unitize();

        Point3d c = new Point3d(R * cf, R * sf, z);
        mid.Add(c);

        Point3d[] sp = new Point3d[pu.Count + 1];
        for (int j = 0; j < pu.Count; j++)
          sp[j] = c + w * pu[j] + rad * pv[j];
        sp[pu.Count] = sp[0];
        sect.Add(new Polyline(sp).ToPolylineCurve());
      }

      Brep gb;
      if (fullU)
      {
        Curve rail = Curve.CreateInterpolatedCurve(mid, 3);
        Brep[] pipe = Brep.CreatePipe(rail, rw, false, PipeCapMode.Round, true, tol, 0.01);
        if (pipe == null || pipe.Length == 0) { Info = "Could not pipe the groove cutter."; return; }
        gb = pipe[0];
        if (pipe.Length > 1)
        { Brep[] j = Brep.JoinBreps(pipe, tol); if (j != null && j.Length > 0) gb = j[0]; }
      }
      else
      {
        Brep[] lofts = Brep.CreateFromLoft(sect, Point3d.Unset, Point3d.Unset, LoftType.Straight, false);
        if (lofts == null || lofts.Length == 0) { Info = "Could not loft the groove cutter."; return; }
        gb = lofts[0];
        if (lofts.Length > 1)
        { Brep[] j = Brep.JoinBreps(lofts, tol); if (j != null && j.Length > 0) gb = j[0]; }
        Brep capped = gb.CapPlanarHoles(tol);
        if (capped != null) gb = capped;
      }

      if (!gb.IsSolid) nNotSolid++;
      if (gb.SolidOrientation == BrepSolidOrientation.Inward) { gb.Flip(); nFlip++; }
      cutters.Add(gb);
      paths.Add(Curve.CreateInterpolatedCurve(mid, 3));
    }

    // ---------- blank, bore, grooves ----------------------------------------
    Brep blank = new Cylinder(new Circle(Plane.WorldXY, R), H).ToBrep(true, true);
    if (blank.SolidOrientation == BrepSolidOrientation.Inward) blank.Flip();

    List<Brep> cut = new List<Brep>(cutters);
    if (bore > 1e-9)
      cut.Add(new Cylinder(new Circle(new Plane(new Point3d(0, 0, -1.0), Vector3d.ZAxis),
                                      0.5 * bore), H + 2.0).ToBrep(true, true));

    Brep body = blank;
    string cutState;
    Brep[] diff = Brep.CreateBooleanDifference(new Brep[] { blank }, cut, tol);
    if (diff != null && diff.Length > 0)
    {
      Brep best = diff[0]; double bv = -1.0;
      foreach (Brep b in diff) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best;
      cutState = "OK (" + diff.Length + " piece" + (diff.Length == 1 ? ")" : "s, kept the largest)");
    }
    else { cutState = "FAILED"; log.Add("Groove / bore boolean failed."); }

    Plane gp = GearPlane.IsValid ? GearPlane : Plane.WorldXY;
    gp.Origin = gp.Origin + gp.ZAxis * zOff;
    Transform xf = Transform.PlaneToPlane(Plane.WorldXY, gp);
    body.Transform(xf);
    foreach (Brep b in cutters) b.Transform(xf);
    foreach (Curve c in paths)  c.Transform(xf);
    Line ax = new Line(gp.Origin, gp.Origin + gp.ZAxis * H);

    // ---------- report -------------------------------------------------------
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("body                dia " + D.ToString("0.###") + ", z " + zOff.ToString("0.##") +
                  " .. " + (zOff + H).ToString("0.##") + ", bore " + bore.ToString("0.##"));
    sb.AppendLine("groove              z " + (zOff + margin).ToString("0.##") + " .. " +
                  (zOff + margin + S).ToString("0.##") + ",  " + starts + " start" +
                  (starts == 1 ? "" : "s") + ", " + gw.ToString("0.##") + " x " + gd.ToString("0.##"));
    sb.AppendLine("total               " + L.ToString("0.###") + " mm of jaw travel over " +
                  S.ToString("0.###") + " mm of push, " + (turns * 360.0).ToString("0.#") + " deg");
    sb.AppendLine("--");
    sb.AppendLine("TWO STAGE                jaw travel   push    lead   lead ang   GRIP");
    sb.AppendLine("  fast    (upper)        " + (L - sq).ToString("0.##") + " mm      " +
                  (S - Zof(tau2)).ToString("0.##") + " mm   " + lead1.ToString("0.#") + "   " +
                  RhinoMath.ToDegrees(lam1).ToString("0.#") + " deg   " + F1.ToString("0") + " N");
    sb.AppendLine("  squeeze (lower)        " + sq.ToString("0.##") + " mm      " +
                  Zof(tau2).ToString("0.##") + " mm   " + lead2.ToString("0.#") + "   " +
                  RhinoMath.ToDegrees(lam2).ToString("0.#") + " deg   " + F2.ToString("0") + " N");
    sb.AppendLine("  blend over " + (2.0 * hb * turns * 360.0).ToString("0.#") +
                  " deg of rotation, so there is no kink for the pin to jam on");
    sb.AppendLine("--");
    sb.AppendLine("A CONSTANT lead over the same 30 mm would give " +
                  (Fpush * (S / turns) / (2.0 * Math.PI * rp)).ToString("0") + " N throughout.");
    sb.AppendLine("The work identity is unchanged - F x travel = " +
                  (Fpush * S).ToString("0") + " N.mm either way. It is just spent");
    sb.AppendLine("where the cloth is instead of evenly.");
    sb.AppendLine("--");
    sb.AppendLine("land between passes " + land.ToString("0.###") + "   (at the shallow lead)");
    sb.AppendLine("wrap per start      " + wrapDeg.ToString("0.#") + " deg of " +
                  (360.0 / starts).ToString("0.#") + " available");
    sb.AppendLine("core radius         " + (R - gd).ToString("0.###") +
                  "  -> max bore " + (2.0 * (R - gd) - 2.0).ToString("0.###"));
    sb.AppendLine("pin                 ball tip radius " + (rw - pinClr).ToString("0.###") +
                  " nests with " + pinClr.ToString("0.##") + " clearance");
    sb.AppendLine("--");
    sb.AppendLine("DIAGNOSTICS");
    sb.AppendLine("  cutters           " + cutters.Count + ", flipped " + nFlip +
                  ", not solid " + nNotSolid);
    sb.AppendLine("  boolean           " + cutState);
    sb.AppendLine("  body solid        " + body.IsSolid);
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Body   = body;
    Cutter = cutters;
    Path   = paths;
    Axis   = ax;
    Info   = sb.ToString();
