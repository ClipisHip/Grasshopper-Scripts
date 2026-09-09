// ===========================================================================
//  CAM FOLLOWER SLEEVE  -  Grasshopper C# component (RhinoCommon)
//  The pushed counterpart: a sleeve that slides over the cam cylinder with
//  ball-nose pins running in the helical groove(s).
//  ---------------------------------------------------------------------
//  Component INPUTS  (all "Item" access):
//     CamDiameter    double  outside diameter of the cam cylinder
//     Clearance      double  radial gap between the cam and this bore
//     OuterDiameter  double  outside diameter of this sleeve
//     Height         double  sleeve height
//     GrooveWidth    double  cam groove width  (sizes the pin)
//     GrooveDepth    double  cam groove depth  (sets how far the pin reaches)
//     PinCount       int     number of pins (2 = balanced)
//     BasePlane      Plane   feed the RACK script's GearPlane here
//  Component OUTPUTS:
//     Body           sleeve with the pins unioned on
//     Pins           the pin solids on their own
//     Info           report + warnings
//
//  Pin form: a cylinder of the groove width (less clearance) capped with a
//  hemisphere, seating in the groove's rounded bottom.  The sphere is what
//  stops it wedging as the groove twists.
//
//  IMPORTANT - pin phasing.  In a groove with `starts` starts, a pin at angle
//  phi sits at z = zRef + lead*phi/2pi, modulo lead/starts.  So:
//     PinCount == starts  -> all pins at the SAME height (what you want)
//     starts == 1, 2 pins -> the second pin must sit lead/2 higher, which
//                            usually will not fit.  Set starts = 2 on the cam.
// ===========================================================================



// ===========================================================================
//  THIS BUILD - set on the component:
//     CamDiameter 18   Clearance 0.25   OuterDiameter 26   Height 8
//     GrooveWidth 4   GrooveDepth 2   PinCount 2
//     BasePlane <- the RACK script's GearPlane
//  zOff below is the TRAVEL:  6 = closed (pushed down),  22 = open (up).
//  Every other constant is already set for this build.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste this into  RunScript( ... )
//  signature:
//    private void RunScript(double CamDiameter, double Clearance,
//                           double OuterDiameter, double Height,
//                           double GrooveWidth, double GrooveDepth,
//                           int PinCount, Plane BasePlane,
//                           ref object Body, ref object Pins, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double Dc = CamDiameter   > 1e-9 ? CamDiameter   : 18.0;
    double cl = Clearance     > 0.0  ? Clearance     : 0.25;
    double Do = OuterDiameter > 1e-9 ? OuterDiameter : 26.0;
    double Hs = Height        > 1e-9 ? Height        :  8.0;
    double gw = GrooveWidth   > 1e-9 ? GrooveWidth   : 4.0;
    double gd = GrooveDepth   > 1e-9 ? GrooveDepth   : 2.0;
    int    np = PinCount      > 0    ? PinCount      : 2;

    // --- match these to the cam script ---------------------------------------
    double pinClr = 0.2;          // pin-to-groove clearance ('suggested pin' in the cam report)
    int    starts = 2;            // groove starts on the cam - set the cam to match
    double lead   = 80.4;      // 'lead (rise/turn)' from the cam report.
                                  // only matters when PinCount != starts
    double pinZ   = 4.0;          // first pin height above the base, 0 -> Height/2
    double hand   = 1.0;          // +1 right-hand, must match the cam
    double phi0   = 0.0;          // angle of the first pin [deg]
    double zOff   = 22.0;         // travel: 6 = closed (down), 22 = open (up)
    bool   pinsSeparate = false;  // true = press-in pins: the sleeve gets radial
                                  // holes and the pins come out on their own.
                                  // REQUIRED for assembly - the pins reach in to
                                  // radius 7.10 and would foul the 8.575 hub if
                                  // they were moulded in.

    // --- tabs: return-spring seats AND the anti-rotation keys -----------------
    // A spring rising from the base cannot reach the sleeve body: anything that
    // clears the gear (radius 13.68) is further out than the sleeve is wide.
    // So the sleeve grows tabs that reach past it. They sit at the TOP of the
    // sleeve, which leaves the spring 12 mm to go solid in rather than 6.
    int    tabN   = 2;            // one per return spring; 0 = no tabs
    double tabR   = 20.0;         // tab OUTER radius
    double tabW   = 6.0;          // tab width (tangential)
    double tabThk = 2.0;          // tab thickness (axial)
    double tabPhi = 0.0;          // angle of the first tab [deg]
    double tabTop = 0.0;          // gap from the sleeve top down to the tab top

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    if (starts < 1) starts = 1;

    double Rc   = 0.5 * Dc;                 // cam radius
    double Rb   = Rc + cl;                  // bore radius
    double Ro   = 0.5 * Do;                 // outer radius
    double rw   = 0.5 * gw;                 // groove bottom radius
    double rp   = 0.5 * (gw - pinClr);      // pin shaft / ball radius
    double rcen = Rc - (gd - rw);           // radius of the groove U centre = ball centre
    if (pinZ <= 1e-9) pinZ = 0.5 * Hs;

    // ---------- checks -------------------------------------------------------
    if (Ro <= Rb) { Info = "OuterDiameter must exceed CamDiameter + 2*Clearance."; return; }
    if (rp <= 0.0) { Info = "Pin clearance is larger than the groove width."; return; }
    if (rp > rw)   { Info = "Pin is wider than the groove bottom radius."; return; }
    if (rcen - rp <= 0.0) { Info = "Pin reaches the cam axis - reduce GrooveDepth."; return; }

    double wall = Ro - Rb;
    if (wall < rp)
      log.Add("Wall " + wall.ToString("0.###") + " is thinner than the pin radius " +
              rp.ToString("0.###") + " - the pin boss will show on the outside.");

    // ---------- pin placement ------------------------------------------------
    double wrapZ = lead / starts;
    List<double> pz = new List<double>();
    List<double> pa = new List<double>();
    for (int i = 0; i < np; i++)
    {
      double phi = RhinoMath.ToRadians(phi0) + 2.0 * Math.PI * i / np;
      double dz  = hand * lead * i / np;
      dz = dz - Math.Floor(dz / wrapZ) * wrapZ;    // drop onto the nearest groove pass
      pa.Add(phi);
      pz.Add(pinZ + dz);
    }
    double zLo = pz[0], zHi = pz[0];
    foreach (double v in pz) { if (v < zLo) zLo = v; if (v > zHi) zHi = v; }
    if (zHi - zLo + 2.0 * rp > Hs)
      log.Add("Pins span " + (zHi - zLo).ToString("0.###") + " but the sleeve is only " +
              Hs.ToString("0.###") + " tall.  Set starts = " + np + " on the cam so all pins sit level.");
    if (zLo - rp < 0.0 || zHi + rp > Hs)
      log.Add("A pin runs past the end of the sleeve - adjust pinZ or Height.");

    // ---------- sleeve -------------------------------------------------------
    Brep sleeve = new Cylinder(new Circle(Plane.WorldXY, Ro), Hs).ToBrep(true, true);
    if (sleeve.SolidOrientation == BrepSolidOrientation.Inward) sleeve.Flip();

    Brep boreB = new Cylinder(new Circle(new Plane(new Point3d(0, 0, -1.0), Vector3d.ZAxis), Rb),
                              Hs + 2.0).ToBrep(true, true);
    if (boreB.SolidOrientation == BrepSolidOrientation.Inward) boreB.Flip();

    Brep[] rr = Brep.CreateBooleanDifference(sleeve, boreB, tol);
    if (rr != null && rr.Length > 0) sleeve = rr[0];
    else log.Add("Bore boolean failed.");

    // ---------- tabs ---------------------------------------------------------
    if (tabN > 0 && tabR > Ro)
    {
      double tz = Hs - tabTop - tabThk;             // tab underside = spring seat
      if (tz < 0.0) { tz = 0.0; log.Add("Tab is taller than the sleeve - dropped to the base."); }
      List<Brep> tabs = new List<Brep>();
      tabs.Add(sleeve);
      for (int i = 0; i < tabN; i++)
      {
        double a = RhinoMath.ToRadians(tabPhi) + 2.0 * Math.PI * i / tabN;
        Vector3d rad = new Vector3d(Math.Cos(a), Math.Sin(a), 0.0);
        Vector3d tng = Vector3d.CrossProduct(Vector3d.ZAxis, rad); tng.Unitize();
        Plane tp2 = new Plane(Point3d.Origin, rad, tng);
        tabs.Add(new Box(tp2, new Interval(Rb, tabR),
                              new Interval(-0.5 * tabW, 0.5 * tabW),
                              new Interval(tz, tz + tabThk)).ToBrep());
      }
      Brep[] tu = Brep.CreateBooleanUnion(tabs, tol);
      if (tu != null && tu.Length > 0)
      {
        Brep best = tu[0]; double bv = -1.0;
        foreach (Brep b in tu) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
        sleeve = best;
      }
      else log.Add("Tab union failed.");
    }
    else if (tabN > 0)
      log.Add("tabR must be larger than the sleeve OD radius " + Ro.ToString("0.##") + " - no tabs made.");

    // ---------- pins ---------------------------------------------------------
    List<Brep> pins = new List<Brep>();
    for (int i = 0; i < np; i++)
    {
      Vector3d rad   = new Vector3d(Math.Cos(pa[i]), Math.Sin(pa[i]), 0.0);
      Point3d  ballC = new Point3d(rad.X * rcen, rad.Y * rcen, pz[i]);

      Brep shaft = new Cylinder(new Circle(new Plane(ballC, rad), rp), Ro - rcen).ToBrep(true, true);
      Brep ball  = new Sphere(ballC, rp).ToBrep();
      if (shaft.SolidOrientation == BrepSolidOrientation.Inward) shaft.Flip();
      if (ball.SolidOrientation  == BrepSolidOrientation.Inward) ball.Flip();

      Brep[] u = Brep.CreateBooleanUnion(new Brep[] { shaft, ball }, tol);
      pins.Add(u != null && u.Length > 0 ? u[0] : shaft);
    }

    // ---------- union it all -------------------------------------------------
    Brep body = sleeve;
    string uniState;
    Brep[] uni;
    if (pinsSeparate)
    {
      // bore the radial holes, leave the pins as loose parts
      List<Brep> holes = new List<Brep>();
      for (int i = 0; i < np; i++)
      {
        Vector3d rad = new Vector3d(Math.Cos(pa[i]), Math.Sin(pa[i]), 0.0);
        Point3d  bc  = new Point3d(rad.X * rcen, rad.Y * rcen, pz[i]);
        holes.Add(new Cylinder(new Circle(new Plane(bc, rad), rp), Ro - rcen + 1.0)
                  .ToBrep(true, true));
      }
      uni = Brep.CreateBooleanDifference(new Brep[] { sleeve }, holes, tol);
    }
    else
    {
      List<Brep> all = new List<Brep>();
      all.Add(sleeve);
      all.AddRange(pins);
      uni = Brep.CreateBooleanUnion(all, tol);
    }
    if (uni != null && uni.Length > 0)
    {
      Brep best = uni[0]; double bv = -1.0;
      foreach (Brep b in uni)
      { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best;
      uniState = (pinsSeparate ? "holes bored, pins loose" : "pins unioned") +
                 " - OK (" + uni.Length + " piece" + (uni.Length == 1 ? ")" : "s, kept the largest)");
    }
    else
    {
      uniState = "FAILED - union Body with Pins manually";
      log.Add("Pin union failed - use a Solid Union on the Body and Pins outputs.");
    }

    // ---------- place on the gear axis ---------------------------------------
    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
    Transform xf = Transform.PlaneToPlane(Plane.WorldXY, bp);
    body.Transform(xf);
    foreach (Brep b in pins) b.Transform(xf);

    // ---------- report -------------------------------------------------------
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("sleeve              bore dia " + (2.0 * Rb).ToString("0.###") +
                  ", outer dia " + Do.ToString("0.###") + ", " + Hs.ToString("0.###") + " tall");
    sb.AppendLine("wall thickness      " + wall.ToString("0.###"));
    sb.AppendLine("radial clearance    " + cl.ToString("0.###") + " on the cam");
    sb.AppendLine("--");
    sb.AppendLine("pins                " + np + ", dia " + (2.0 * rp).ToString("0.###") +
                  " with a " + rp.ToString("0.###") + " ball tip");
    sb.AppendLine("ball centre radius  " + rcen.ToString("0.###") +
                  "   (groove U centre - the ball seats here)");
    sb.AppendLine("pin reach past bore " + (Rb - (rcen - rp)).ToString("0.###"));
    for (int i = 0; i < np; i++)
      sb.AppendLine("  pin " + (i + 1) + "             " +
                    RhinoMath.ToDegrees(pa[i]).ToString("0.#") + " deg,  z = " + pz[i].ToString("0.###"));
    sb.AppendLine("cam starts assumed  " + starts +
                  (np == starts ? "   (pins level - good)" : "   (pins staggered in z)"));
    sb.AppendLine("--");
    sb.AppendLine("DIAGNOSTICS");
    sb.AppendLine("  sleeve solid      " + sleeve.IsSolid);
    sb.AppendLine("  union             " + uniState);
    sb.AppendLine("  body solid        " + body.IsSolid);
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Body = body;
    Pins = pins;
    Info = sb.ToString();
