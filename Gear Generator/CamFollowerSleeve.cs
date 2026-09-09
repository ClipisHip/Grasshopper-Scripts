// ===========================================================================
//  CAM FOLLOWER SLEEVE  -  Grasshopper C# component (RhinoCommon)
//  The pushed part. An OVAL (stadium) body so its wide ends can swallow the
//  return springs in blind pockets, with vertical key grooves down the full
//  height that the housing's ribs run in to stop it rotating.
//
//  Why oval: a spring rising from the base cannot reach a round sleeve.
//  Anything that clears the gear (radius 13.68) sits further out than a round
//  sleeve is wide (radius 13). Stretching the body along one axis puts real
//  material out past the gear, and the pockets go there.
//
//  Layout, looking down:
//     lobes  (long axis)  spring pockets, and the key grooves at the tips
//     waist  (short axis) the cam pins
//  They must not share an angle, or the pin shaft would break into a pocket.
// ===========================================================================


// ===========================================================================
//  THIS BUILD - 9 inputs to create on the component:
//     CamDiameter 18   Clearance 0.25
//     WaistDia 26      Length 50        Height 8
//     GrooveWidth 4    GrooveDepth 2    PinCount 2
//     BasePlane <- the RACK script's GearPlane
//  zOff below is the TRAVEL:  6 = closed (pushed down),  22 = open (up).
//  Every other constant is already set for this build.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double CamDiameter, double Clearance, double WaistDia,
//                           double Length, double Height, double GrooveWidth,
//                           double GrooveDepth, int PinCount, Plane BasePlane,
//                           ref object Body, ref object Pins, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double Dc = CamDiameter > 1e-9 ? CamDiameter : 18.0;
    double cl = Clearance   > 0.0  ? Clearance   : 0.25;
    double Dw = WaistDia    > 1e-9 ? WaistDia    : 26.0;
    double Lo = Length      > 1e-9 ? Length      : 50.0;
    double Hs = Height      > 1e-9 ? Height      :  8.0;
    double gw = GrooveWidth > 1e-9 ? GrooveWidth :  4.0;
    double gd = GrooveDepth > 1e-9 ? GrooveDepth :  2.0;
    int    np = PinCount    > 0    ? PinCount    :  2;

    // --- match these to the cam script ---------------------------------------
    double pinClr = 0.2;          // pin-to-groove clearance
    int    starts = 2;            // groove starts on the cam
    double lead   = 80.4;         // 'lead (rise/turn)' from the cam report
    double pinZ   = 4.0;          // pin height above the sleeve base
    double hand   = 1.0;          // +1 right-hand, must match the cam
    double phi0   = 90.0;         // pins on the WAIST axis. Must equal the cam's phi0.
    double zOff   = 22.0;         // travel: 6 = closed (down), 22 = open (up)
    bool   pinsSeparate = false;  // true = press-in pins, sleeve gets radial holes

    // --- spring pockets, in the lobes ----------------------------------------
    double pockR   = 18.0;        // pocket centre RADIUS - must clear the gear
    double pockDia = 6.6;         // spring OD 6 + 0.6
    double pockDep = 6.0;         // blind, up from the sleeve base
    double gearR   = 13.68;       // for the clearance check

    // --- key grooves, at the lobe tips ---------------------------------------
    int    keyN    = 2;
    double keyW    = 6.0;         // groove width
    double keyDep  = 2.0;         // groove depth, cut inward from the tip

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
    if (starts < 1) starts = 1;

    double Rc   = 0.5 * Dc;
    double Rb   = Rc + cl;                  // bore radius
    double a    = 0.5 * Dw;                 // waist half-width = lobe radius
    double d    = 0.5 * Lo - a;             // lobe centre offset
    double rw   = 0.5 * gw;
    double rp   = 0.5 * (gw - pinClr);      // pin / ball radius
    double rcen = Rc - (gd - rw);           // ball centre radius

    // ---------- checks -------------------------------------------------------
    if (d <= 0.0) { Info = "Length must exceed WaistDia - that is what makes it oval."; return; }
    if (a <= Rb)  { Info = "WaistDia leaves no wall around the bore."; return; }
    if (rp > rw)  { Info = "Pin is wider than the groove."; return; }

    double pockOut = pockR + 0.5 * pockDia;
    double lobeOut = d + a;
    if (pockR - 0.5 * pockDia <= gearR)
      log.Add("Spring pocket at radius " + pockR.ToString("0.##") + " fouls the gear (" +
              gearR.ToString("0.##") + ") - move it out past " +
              (gearR + 0.5 * pockDia + 0.5).ToString("0.##") + ".");
    double pockWall = a - (Math.Abs(pockR - d) + 0.5 * pockDia);
    if (pockWall < 1.0)
      log.Add("Only " + pockWall.ToString("0.##") + " of wall around the pocket - lengthen the oval.");
    if (pockDep >= Hs)
      log.Add("Pocket is as deep as the sleeve is tall - leave a ceiling for the spring to push on.");
    if (lobeOut - keyDep <= pockOut)
      log.Add("Key groove cuts into the spring pocket - deepen the oval or narrow the groove.");
    if (pockR > lobeOut) { Info = "Spring pocket falls outside the body."; return; }

    // ---------- oval body ----------------------------------------------------
    PolyCurve oval = new PolyCurve();
    oval.Append(new ArcCurve(new Arc(new Point3d(d, -a, 0), new Point3d(d + a, 0, 0),
                                     new Point3d(d, a, 0))));
    oval.Append(new LineCurve(new Point3d(d, a, 0), new Point3d(-d, a, 0)));
    oval.Append(new ArcCurve(new Arc(new Point3d(-d, a, 0), new Point3d(-d - a, 0, 0),
                                     new Point3d(-d, -a, 0))));
    oval.Append(new LineCurve(new Point3d(-d, -a, 0), new Point3d(d, -a, 0)));
    oval.MakeClosed(tol);

    Extrusion ex = Extrusion.Create(oval, Hs, true);
    if (ex == null) { Info = "Could not extrude the oval body."; return; }
    Brep sleeve = ex.ToBrep();
    if (sleeve.SolidOrientation == BrepSolidOrientation.Inward) sleeve.Flip();

    // ---------- bore, pockets, key grooves ------------------------------------
    List<Brep> cut = new List<Brep>();
    cut.Add(new Cylinder(new Circle(new Plane(new Point3d(0, 0, -1.0), Vector3d.ZAxis), Rb),
                         Hs + 2.0).ToBrep(true, true));

    for (int i = 0; i < 2; i++)                         // spring pockets, blind from below
    {
      double sgn = i == 0 ? 1.0 : -1.0;
      cut.Add(new Cylinder(new Circle(new Plane(new Point3d(sgn * pockR, 0, -1.0), Vector3d.ZAxis),
                                      0.5 * pockDia), pockDep + 1.0).ToBrep(true, true));
    }

    for (int i = 0; i < keyN; i++)                      // key grooves, full height
    {
      double ang = 2.0 * Math.PI * i / keyN;            // lobe tips: 0 and 180
      Vector3d rad = new Vector3d(Math.Cos(ang), Math.Sin(ang), 0.0);
      Vector3d tng = Vector3d.CrossProduct(Vector3d.ZAxis, rad); tng.Unitize();
      Plane kp = new Plane(Point3d.Origin, rad, tng);
      cut.Add(new Box(kp, new Interval(lobeOut - keyDep, lobeOut + 2.0),
                          new Interval(-0.5 * keyW, 0.5 * keyW),
                          new Interval(-1.0, Hs + 1.0)).ToBrep());
    }

    Brep[] rr = Brep.CreateBooleanDifference(new Brep[] { sleeve }, cut, tol);
    string cutState;
    if (rr != null && rr.Length > 0)
    {
      Brep best = rr[0]; double bv = -1.0;
      foreach (Brep b in rr) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      sleeve = best; cutState = "OK";
    }
    else { cutState = "FAILED"; log.Add("Bore / pocket / key boolean failed."); }

    // ---------- pin placement, on the waist axis -----------------------------
    double wrapZ = lead / starts;
    List<double> pz = new List<double>();
    List<double> pa = new List<double>();
    for (int i = 0; i < np; i++)
    {
      double phi = RhinoMath.ToRadians(phi0) + 2.0 * Math.PI * i / np;
      double dz  = hand * lead * i / np;
      dz = dz - Math.Floor(dz / wrapZ) * wrapZ;
      pa.Add(phi);
      pz.Add(pinZ + dz);
    }
    double zLo = pz[0], zHi = pz[0];
    foreach (double v in pz) { if (v < zLo) zLo = v; if (v > zHi) zHi = v; }
    if (zHi - zLo + 2.0 * rp > Hs)
      log.Add("Pins span " + (zHi - zLo).ToString("0.###") + " but the sleeve is only " +
              Hs.ToString("0.###") + " tall. Set starts = " + np + " on the cam.");

    // ---------- pins ---------------------------------------------------------
    List<Brep> pins = new List<Brep>();
    for (int i = 0; i < np; i++)
    {
      Vector3d rad = new Vector3d(Math.Cos(pa[i]), Math.Sin(pa[i]), 0.0);
      Point3d  bc  = new Point3d(rad.X * rcen, rad.Y * rcen, pz[i]);
      double   len = a - rcen;                         // out to the waist surface
      if (len <= 0.0) { Info = "Pin has no length - waist is inside the ball centre."; return; }
      Brep shaft = new Cylinder(new Circle(new Plane(bc, rad), rp), len).ToBrep(true, true);
      Brep ball  = new Sphere(bc, rp).ToBrep();
      if (shaft.SolidOrientation == BrepSolidOrientation.Inward) shaft.Flip();
      if (ball.SolidOrientation  == BrepSolidOrientation.Inward) ball.Flip();
      Brep[] u = Brep.CreateBooleanUnion(new Brep[] { shaft, ball }, tol);
      pins.Add(u != null && u.Length > 0 ? u[0] : shaft);
    }

    Brep body = sleeve;
    string pinState;
    Brep[] res;
    if (pinsSeparate)
    {
      List<Brep> holes = new List<Brep>();
      for (int i = 0; i < np; i++)
      {
        Vector3d rad = new Vector3d(Math.Cos(pa[i]), Math.Sin(pa[i]), 0.0);
        Point3d  bc  = new Point3d(rad.X * rcen, rad.Y * rcen, pz[i]);
        holes.Add(new Cylinder(new Circle(new Plane(bc, rad), rp), a - rcen + 1.0)
                  .ToBrep(true, true));
      }
      res = Brep.CreateBooleanDifference(new Brep[] { sleeve }, holes, tol);
      pinState = "holes bored, pins loose";
    }
    else
    {
      List<Brep> all = new List<Brep>();
      all.Add(sleeve); all.AddRange(pins);
      res = Brep.CreateBooleanUnion(all, tol);
      pinState = "pins unioned";
    }
    if (res != null && res.Length > 0)
    {
      Brep best = res[0]; double bv = -1.0;
      foreach (Brep b in res) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best;
    }
    else { pinState += " - FAILED"; log.Add("Pin boolean failed - use the Pins output separately."); }

    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
    Transform xf = Transform.PlaneToPlane(Plane.WorldXY, bp);
    body.Transform(xf);
    foreach (Brep b in pins) b.Transform(xf);

    // ---------- report -------------------------------------------------------
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("oval body           " + Lo.ToString("0.###") + " long x " + Dw.ToString("0.###") +
                  " wide x " + Hs.ToString("0.###") + " tall");
    sb.AppendLine("  lobe radius " + a.ToString("0.##") + " at centres +/-" + d.ToString("0.##") +
                  ", reaching " + lobeOut.ToString("0.##"));
    sb.AppendLine("bore                " + (2.0 * Rb).ToString("0.###") + "   (waist wall " +
                  (a - Rb).ToString("0.##") + ")");
    sb.AppendLine("--");
    sb.AppendLine("spring pockets      2 x dia " + pockDia.ToString("0.###") + " x " +
                  pockDep.ToString("0.###") + " deep, at radius " + pockR.ToString("0.###"));
    sb.AppendLine("  clear of the gear " + (pockR - 0.5 * pockDia - gearR).ToString("0.##") +
                  ",  wall around it " + pockWall.ToString("0.##"));
    sb.AppendLine("  ceiling is the spring seat: " + (Hs - pockDep).ToString("0.##") +
                  " below the sleeve top");
    sb.AppendLine("key grooves         " + keyN + " x " + keyW.ToString("0.###") + " wide x " +
                  keyDep.ToString("0.###") + " deep at the lobe tips, full height");
    sb.AppendLine("pins                " + np + " on the WAIST axis at " + phi0.ToString("0.#") +
                  " deg, dia " + (2.0 * rp).ToString("0.###") + ", " + (a - rcen).ToString("0.##") + " long");
    sb.AppendLine("  cam phi0 MUST also be " + phi0.ToString("0.#") + " or the pins miss the groove");
    sb.AppendLine("--");
    sb.AppendLine("DIAGNOSTICS");
    sb.AppendLine("  bore/pockets/keys " + cutState);
    sb.AppendLine("  pins              " + pinState);
    sb.AppendLine("  body solid        " + body.IsSolid);
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Body = body;
    Pins = pins;
    Info = sb.ToString();
