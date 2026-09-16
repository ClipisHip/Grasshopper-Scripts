// ===========================================================================
//  PAWL SPRING  -  Grasshopper C# component (RhinoCommon)
//  The light torsion spring that holds the pawl in the ratchet teeth.
//  Sits around the SAME 3.2 pin the pawl pivots on, in its own band above the
//  pawl, with one leg on a peg on the pawl's tail and the other on a rib in
//  the housing wall.
//
//  IT DOES NOT HOLD THE GRIP. The grip is held by the pawl bearing on a tooth
//  flank - 84 N in compression. All this spring does is keep the finger
//  touching the wheel so it finds the next tooth. A few newtons is plenty, and
//  a stiffer spring only makes the ratchet harder to push and the release
//  lever heavier.
//
//  WHY A TORSION SPRING AND NOT A LEAF. There is no room in-plane: at pawl
//  level the bore wall closes from radius 22.6 at -29 deg to 16.4 at -54 deg,
//  and a printed leaf needs about 12 mm of length to stay under 25 MPa and not
//  creep. Going around the pin uses the one axis that still has space.
//
//  DIRECTION - THE WHOLE POINT. +ve rotation about the pivot (CCW seen from
//  above) drives the finger INTO the wheel. The spring must therefore pull its
//  two legs TOGETHER, so it is fitted wound OPEN by WindUp degrees and tries
//  to close. Fit it the other way and it holds the pawl off the wheel, which
//  looks almost identical in CAD and does nothing at all on the bench.
// ===========================================================================


// ===========================================================================
//  THIS BUILD - 7 inputs to create on the component:
//     WireDia 0.7   CoilOD 6.2   Coils 4   LegSpan 95.2
//     LegA 9   LegB 7.5
//     BasePlane <- the RACK script's GearPlane
//  Every constant below is already set for this build.
//
//  Coils / LegSpan describe the INSTALLED shape - what you see in the model.
//  The spring you order is the same coil with its legs FREE span apart, which
//  the report gives.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double WireDia, double CoilOD, double Coils,
//                           double LegSpan, double LegA, double LegB,
//                           Plane BasePlane,
//                           ref object Body, ref object Path, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double d  = WireDia > 1e-9 ? WireDia : 0.7;
    double Do = CoilOD  > 1e-9 ? CoilOD  : 6.2;
    double nc = Coils   > 0.1  ? Coils   : 4.0;
    double ls = Math.Abs(LegSpan) > 1e-9 ? LegSpan : 95.2;
    double La = LegA    > 1e-9 ? LegA    : 9.0;
    double Lb = LegB    > 1e-9 ? LegB    : 7.5;

    // --- where it sits, and what it works against ----------------------------
    double pinX    = 16.0;    // the pawl pivot - MUST match the pawl's PivotDist
    double pinDia  = 3.2;     // the pin it wraps
    double zOff    = 38.2;    // its own band: clear above the pawl top at 38
    double aA      = -78.91;  // BOTTOM leg [deg from the pin]. NOT the peg's own
                              // angle: a radial leg aimed straight at a peg would
                              // run THROUGH it. The leg lies alongside, offset by
                              // wire radius + peg radius, and pushes on its flank.
                              // -78.91 + 9.71 puts the peg on the tail centreline.
    double pegRad  = 1.0;     // pawl peg RADIUS   - must match the pawl
    double ancHalf = 2.0;     // housing rib HALF width - must match the housing
    double hand    = 1.0;     // +1 right hand
    double windUp  = 35.0;    // how far it is wound OPEN to install it [deg]
    double armRch  = 6.78;    // pawl arm reach, to report the force at the finger
    double Emod    = 207000.0;// music wire, ASTM A228 [MPa]
    double sAllow  = 1700.0;  // allowable bending stress for that wire [MPa]
    int    seg     = 24;      // helix points per turn

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    double Dm = Do - d;              // MEAN coil diameter
    double Rm = 0.5 * Dm;
    double Id = Dm - d;              // free inside diameter

    // ---------- checks -------------------------------------------------------
    if (Rm <= 0.5 * d) { Info = "CoilOD is too small for that wire."; return; }
    double nTot  = nc + ls / 360.0;          // total active turns, legs included
    double coilH = nTot * d;                 // close wound
    // winding a torsion spring CLOSED shrinks its bore
    double IdWound = Id * nTot / (nTot + windUp / 360.0);
    if (IdWound <= pinDia + 0.3)
      log.Add("Coil bore closes to " + IdWound.ToString("0.##") + " on a " +
              pinDia.ToString("0.#") + " pin - it will bind. Open up CoilOD.");
    if (La + Rm >= 12.0)
      log.Add("Leg A is long enough to reach past the pawl tail - check the peg.");

    // ---------- rate and load ------------------------------------------------
    double kRev = Emod * Math.Pow(d, 4) / (10.8 * Dm * nTot);   // N.mm per revolution
    double M    = kRev * windUp / 360.0;                        // N.mm at the pin
    double sig  = 32.0 * M / (Math.PI * Math.Pow(d, 3));        // wire bending
    double Ffin = M / armRch;                                   // at the pawl finger
    double Fpeg = M / La;                                       // on the pawl peg

    if (sig > sAllow)
      log.Add("Wire at " + sig.ToString("0") + " MPa vs " + sAllow.ToString("0") +
              " allowable - thicker wire, or fewer degrees of wind-up.");
    if (Ffin < 1.0)
      log.Add("Only " + Ffin.ToString("0.##") + " N at the finger - it may not follow the " +
              "teeth reliably. More wind-up, or thicker wire.");
    if (Ffin > 8.0)
      log.Add("More than 8 N at the finger - the ratchet will feel notchy and the " +
              "release lever heavy for no benefit.");

    // ---------- path: leg A, helix, leg B ------------------------------------
    double aAr = RhinoMath.ToRadians(aA);
    double aBr = aAr + hand * RhinoMath.ToRadians(360.0 * nc + ls);
    double zLo = zOff + 0.5 * d;
    double zHi = zOff + coilH - 0.5 * d;

    List<Point3d> hp = new List<Point3d>();
    int np = Math.Max(8, (int) (seg * nTot));
    for (int i = 0; i <= np; i++)
    {
      double s = (double) i / np;
      double a = aAr + hand * RhinoMath.ToRadians(360.0 * nTot) * s;
      hp.Add(new Point3d(pinX + Rm * Math.Cos(a), Rm * Math.Sin(a), zLo + (zHi - zLo) * s));
    }
    Curve helix = Curve.CreateInterpolatedCurve(hp, 3);
    if (helix == null) { Info = "Could not build the coil."; return; }

    // radial legs, each in the horizontal plane of its own end of the coil
    Point3d a0 = hp[0];
    Point3d a1 = new Point3d(pinX + La * Math.Cos(aAr), La * Math.Sin(aAr), zLo);
    Point3d b0 = hp[hp.Count - 1];
    Point3d b1 = new Point3d(pinX + Lb * Math.Cos(aBr), Lb * Math.Sin(aBr), zHi);

    PolyCurve path = new PolyCurve();
    path.Append(new LineCurve(a1, a0));
    path.Append(helix);
    path.Append(new LineCurve(b0, b1));

    // ---------- solid --------------------------------------------------------
    Brep body = null;
    Brep[] pipe = Brep.CreatePipe(path, 0.5 * d, false, PipeCapMode.Round, true, tol, 0.01);
    if (pipe != null && pipe.Length > 0)
    {
      body = pipe[0];
      if (body.SolidOrientation == BrepSolidOrientation.Inward) body.Flip();
    }
    else log.Add("Pipe failed - the wire is drawn as a path only.");

    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    Transform xf = Transform.PlaneToPlane(Plane.WorldXY, bp);
    if (body != null) body.Transform(xf);
    path.Transform(xf);

    // ---------- report -------------------------------------------------------
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("ORDER THIS SPRING");
    sb.AppendLine("  wire              " + d.ToString("0.##") + " music wire (ASTM A228)");
    sb.AppendLine("  coil OD / ID      " + Do.ToString("0.##") + " / " + Id.ToString("0.##"));
    sb.AppendLine("  active turns      " + nTot.ToString("0.###"));
    sb.AppendLine("  FREE leg span     " + (ls - windUp).ToString("0.#") + " deg");
    sb.AppendLine("  legs              " + La.ToString("0.#") + " and " + Lb.ToString("0.#") +
                  " long, radial");
    sb.AppendLine("--");
    sb.AppendLine("AS INSTALLED");
    sb.AppendLine("  leg span          " + ls.ToString("0.#") + " deg - wound OPEN by " +
                  windUp.ToString("0.#"));
    sb.AppendLine("  so it pulls its legs TOGETHER, which drives the finger IN.");
    sb.AppendLine("  coil              z " + zOff.ToString("0.##") + " .. " +
                  (zOff + coilH).ToString("0.##") + "  (" + coilH.ToString("0.##") + " tall)");
    sb.AppendLine("  bore when wound   " + IdWound.ToString("0.##") + " on the " +
                  pinDia.ToString("0.#") + " pin");
    sb.AppendLine("--");
    // where the two anchors must sit. A radial leg pushes on the FLANK of its
    // anchor, so each is rotated off the leg's own line by the contact offset.
    double rPegA = 8.0;                                   // peg centre, from the pin
    double offA  = RhinoMath.ToDegrees(Math.Asin(Math.Min(0.99, (0.5 * d + pegRad) / rPegA)));
    double offB  = RhinoMath.ToDegrees(Math.Asin(Math.Min(0.99, (0.5 * d + ancHalf) / Lb)));
    double pegAdeg = aA + offA;                           // CCW of the leg: it is pushed CCW
    double ancBdeg = RhinoMath.ToDegrees(aBr) - offB;     // CW of the leg: it takes the reaction
    Point3d pA = new Point3d(pinX + rPegA * Math.Cos(RhinoMath.ToRadians(pegAdeg)),
                             rPegA * Math.Sin(RhinoMath.ToRadians(pegAdeg)), 0);

    sb.AppendLine("PEG A  on the PAWL TAIL");
    sb.AppendLine("  " + rPegA.ToString("0.##") + " from the pin at " + pegAdeg.ToString("0.##") +
                  " deg  =  (" + pA.X.ToString("0.##") + ", " + pA.Y.ToString("0.##") + ")");
    sb.AppendLine("  leg sits at " + aA.ToString("0.##") + ", offset " + offA.ToString("0.##") +
                  " deg so it bears on the peg's flank");
    sb.AppendLine("RIB B  in the HOUSING");
    sb.AppendLine("  leg at " + RhinoMath.ToDegrees(aBr).ToString("0.##") +
                  " deg from the pin, " + Lb.ToString("0.#") + " long");
    sb.AppendLine("  rib centre " + offB.ToString("0.##") + " deg CW of it -> ancPhi " +
                  ancBdeg.ToString("0.##"));
    sb.AppendLine("  ALL FOUR ANGLES ARE FROM THE PIN at x " + pinX.ToString("0.#") +
                  ", not from the wheel axis.");
    sb.AppendLine("--");
    sb.AppendLine("rate                " + kRev.ToString("0.#") + " N.mm per revolution");
    sb.AppendLine("moment at the pin   " + M.ToString("0.##") + " N.mm");
    sb.AppendLine("  at the finger     " + Ffin.ToString("0.##") + " N");
    sb.AppendLine("  on peg A          " + Fpeg.ToString("0.##") + " N");
    sb.AppendLine("wire stress         " + sig.ToString("0") + " MPa   vs " +
                  sAllow.ToString("0") + " allowable  (" + (sAllow / sig).ToString("0.#") + "x)");
    sb.AppendLine("--");
    sb.AppendLine("The ratchet holds 84 N on the tooth flank. This spring is NOT");
    sb.AppendLine("part of that load path - it only keeps the finger in contact.");
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Body = body;
    Path = path;
    Info = sb.ToString();
