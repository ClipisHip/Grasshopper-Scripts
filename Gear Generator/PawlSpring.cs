// ===========================================================================
//  PAWL SPRING  -  Grasshopper C# component (RhinoCommon)
//  The light torsion spring that holds the pawl in the ratchet teeth: a
//  close-wound coil round the SAME 3.2 pin the pawl pivots on, with a
//  TANGENTIAL tang at each end - built exactly the way WrapSpringCoil.cs
//  builds its spring. Tang A (bottom) bears on the peg on the pawl's tail;
//  tang B (top) bears on a rib in the housing wall.
//  ---------------------------------------------------------------------
//  INPUTS (Item access):
//     WireDia     double  spring wire diameter
//     CoilOD      double  coil outside diameter
//     Coils       double  WHOLE turns. TangAngle sets the fraction on top.
//     TangAngle   double  angle between the tangs AS FITTED, seen from above,
//                         measured ANTICLOCKWISE from tang A to tang B [deg].
//                         180 = pointing straight opposite = whole turns only
//     TangA       double  length of tang A - the one on the pawl
//     TangB       double  length of tang B - the one on the housing
//     BasePlane   Plane   the RACK script's GearPlane
//  OUTPUTS:
//     Spring      Brep of the wire
//     Rail        wire centreline curve
//     AnchorB     Line along the face the housing rib must present to tang B;
//                 the rib lies to its RIGHT. Wire it into HousingRatchet's
//                 SpringAnchor input and the rib follows TangAngle.
//     Info        order spec, where the tangs land, forces, clash checks
//
//  IT DOES NOT HOLD THE GRIP. The grip is held by the pawl bearing on a tooth
//  flank - up to 169 N in compression. All this spring does is keep the wedge
//  touching the wheel so it finds the next tooth. A few newtons is plenty; a
//  stiffer spring only makes the ratchet notchy and the release lever heavy.
//
//  TANG A IS SOLVED, NOT SET. A tangential tang is a line tangent to the coil,
//  so the coil axis always sits Rm from it. For its side to bear on the peg,
//  the peg centre must sit Rm + wire radius + peg radius from the axis,
//  measured square to the tang - which fixes where tang A has to leave the
//  coil. TangAngle then places tang B relative to it.
//
//  DIRECTION. +ve rotation about the pivot (anticlockwise from above) drives
//  the wedge INTO the wheel, so the spring must push tang A anticlockwise. It
//  is fitted WOUND UP by windUp degrees - tang B turned anticlockwise relative
//  to tang A, which tightens the coil - and pushes back. Fitted the other way
//  it holds the pawl OFF the wheel, which looks the same in CAD and does
//  nothing on the bench.
// ===========================================================================


// ===========================================================================
//  THIS BUILD - 7 inputs and 4 outputs to create on the component:
//     WireDia 0.7   CoilOD 6.2   Coils 3   TangAngle 190
//     TangA 9   TangB 6
//     BasePlane <- the RACK script's GearPlane
//     outputs: Spring, Rail, AnchorB, Info
//  Every constant below is already set for this build.
//
//  COILS 3, NOT 4. A close-wound coil of N turns is (N + 1) wires tall - the
//  helix climbs one wire per turn, plus half a wire of envelope at each end.
//  4 turns is 3.52 tall and the band is 3.0. (The previous version of this
//  script squeezed the pitch to 0.54 on a 0.7 wire to hide that, so its turns
//  ran through each other.)
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double WireDia, double CoilOD, double Coils,
//                           double TangAngle, double TangA, double TangB,
//                           Plane BasePlane,
//                           ref object Spring, ref object Rail,
//                           ref object AnchorB, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double d   = WireDia   > 1e-9 ? WireDia   : 0.7;
    double Do  = CoilOD    > 1e-9 ? CoilOD    : 6.2;
    double nc  = Coils     > 0.5  ? Math.Floor(Coils + 1e-6) : 3.0;
    double ang = TangAngle > 1e-9 ? TangAngle : 190.0;
    double lA  = TangA     > 1e-9 ? TangA     : 9.0;
    double lB  = TangB     > 1e-9 ? TangB     : 6.0;
    if (Coils > 0.5 && Math.Abs(Coils - nc) > 1e-6)
      log.Add("Coils is WHOLE turns - " + Coils.ToString("0.###") + " read as " +
              nc.ToString("0") + ". Set the fraction with TangAngle.");

    // --- where it sits, and what it works against ----------------------------
    double pinX     = 16.0;    // the pawl pivot - MUST match the pawl's PivotDist
    double pinDia   = 3.2;     // the pin it wraps
    double zOff     = 38.2;    // bottom of its band, just clear of the pawl top
    double bandTop  = 41.2;    // top of its band - the housing's sprZhi
    double pawlTop  = 38.0;    // tang A runs just above this
    double pegX     = 18.84;   // the pawl's spring peg, from the pawl's report.
    double pegY     = -7.48;   // Tang A is SOLVED to bear on its flank.
    double pegRad   = 1.0;     // must match the pawl's pegDia / 2
    double hand     = 1.0;     // +1 right hand. The anchors are solved for +1.
    bool   flatTangs = true;   // tangs tangent to the coil in PLAN and lying flat.
                               // false follows the helix slope exactly, as
                               // WrapSpringCoil does - but then tang A dips
                               // toward the pawl top and tang B climbs out of
                               // the band. The slope here is 2.3 deg.
    double windUp   = 35.0;    // how far it is wound up to fit it [deg]
    double pawlLift = 10.0;    // the pawl's full lift, from its report [deg]
    double armRch   = 7.55;    // pivot to the wedge contact, for the force there
    double ribS1    = 1.5;     // the housing rib bears on tang B from here ...
    double ribLen   = 3.0;     // ... for this long, measured from where it leaves
                               // the coil. s1 keeps the rib off the coil.
    double capR     = 10.0;    // retainer cap radius, round the WHEEL axis
    double Emod     = 207000.0;// music wire, ASTM A228 [MPa]
    double sAllow   = 1700.0;  // allowable bending stress for that wire [MPa]

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
    double aTol = 0.01;

    double Dm = Do - d;               // MEAN coil diameter
    double Rm = 0.5 * Dm;
    double Id = Dm - d;               // free inside diameter
    if (Rm <= 0.5 * d) { Info = "CoilOD is too small for that wire."; return; }

    // ---------- tang A: solved so its side bears on the pawl peg -------------
    double qx = pegX - pinX, qy = pegY;
    double qd = Math.Sqrt(qx * qx + qy * qy), qa = Math.Atan2(qy, qx);
    double reachA = Rm + 0.5 * d + pegRad;
    if (qd <= reachA) { Info = "The pawl peg is too close to the pin for a tangential tang."; return; }
    double thA = qa + Math.Acos(reachA / qd);        // where tang A leaves the coil

    // ---------- tang B: TangAngle anticlockwise from tang A -------------------
    // Tang A leaves BACKWARDS, pointing at thA - 90; tang B FORWARDS, at
    // thB + 90. So the angle from A to B is (thB - thA) + 180, and a whole
    // number of coils leaves the tangs pointing straight opposite.
    double dTh  = ((ang - 180.0) % 360.0 + 360.0) % 360.0;
    double nTot = nc + dTh / 360.0;                  // turns, as fitted
    double thB  = thA + RhinoMath.ToRadians(dTh);

    // ---------- coil, exactly as WrapSpringCoil builds it ---------------------
    double pitch = d;                                // close wound
    Point3d ax0 = new Point3d(pinX, 0.0, zOff + 0.5 * d);          // wire centre, bottom
    Point3d r0  = new Point3d(pinX + Rm * Math.Cos(thA), Rm * Math.Sin(thA), ax0.Z);
    NurbsCurve helix = NurbsCurve.CreateSpiral(ax0, Vector3d.ZAxis, r0, hand * pitch, nTot, Rm, Rm);
    if (helix == null) { Info = "Could not build the helix - check Coils / WireDia."; return; }

    Point3d  ps = helix.PointAtStart, pe = helix.PointAtEnd;
    Vector3d ts = helix.TangentAtStart, te = helix.TangentAtEnd;
    if (flatTangs) { ts.Z = 0.0; te.Z = 0.0; }
    ts.Unitize(); te.Unitize();
    if (ts.X * -Math.Sin(thA) + ts.Y * Math.Cos(thA) <= 0.0)
      log.Add("The coil winds clockwise - the anchors are solved for anticlockwise, " +
              "so both tangs will miss.");

    PolyCurve rail = new PolyCurve();
    rail.Append(new LineCurve(ps - ts * lA, ps));    // tang A -> the pawl peg
    rail.Append(helix);
    rail.Append(new LineCurve(pe, pe + te * lB));    // tang B -> the housing rib

    Brep[] pipe = Brep.CreatePipe(rail, 0.5 * d, false, PipeCapMode.Round, true, tol, aTol);
    if (pipe == null || pipe.Length == 0) { Info = "Pipe failed - try a coarser tolerance."; return; }
    Brep spring = pipe[0];
    if (pipe.Length > 1) { Brep[] j = Brep.JoinBreps(pipe, tol); if (j != null && j.Length > 0) spring = j[0]; }

    // ---------- where the tangs actually land --------------------------------
    Vector3d dA = new Vector3d(-ts.X, -ts.Y, 0.0); dA.Unitize();   // tang A, in plan
    double vx = pegX - ps.X, vy = pegY - ps.Y;
    double sA   = vx * dA.X + vy * dA.Y;                          // along tang A to the peg
    double offA = Math.Abs(vx * dA.Y - vy * dA.X);                // peg centre off its line
    if (sA + pegRad > lA)
      log.Add("Tang A is too short to reach the pawl peg - make it at least " +
              (sA + pegRad + 0.5).ToString("0.#") + ".");
    if (Math.Abs(offA - (0.5 * d + pegRad)) > 0.05)
      log.Add("Tang A misses the peg's flank by " + (offA - 0.5 * d - pegRad).ToString("0.##") + ".");

    // the stretch of tang B the housing rib bears on. The rib sits to the
    // tang's RIGHT, which for a forward tang is always away from the coil.
    Vector3d tB = new Vector3d(te.X, te.Y, 0.0); tB.Unitize();
    Vector3d nB = new Vector3d(tB.Y, -tB.X, 0.0);
    double s1 = ribS1, s2 = Math.Min(ribS1 + ribLen, lB - 0.3);
    if (s2 - s1 < 1.0)
      log.Add("Tang B is too short to bear on the housing rib - make it at least " +
              (ribS1 + 1.3).ToString("0.#") + ".");
    if ((pe.X - pinX) * nB.X + pe.Y * nB.Y <= 0.0)
      log.Add("Tang B's rib side faces the coil, not away from it - check hand.");
    Point3d f0 = new Point3d(pe.X + tB.X * s1 + nB.X * 0.5 * d, pe.Y + tB.Y * s1 + nB.Y * 0.5 * d, pe.Z);
    Point3d f1 = new Point3d(pe.X + tB.X * s2 + nB.X * 0.5 * d, pe.Y + tB.Y * s2 + nB.Y * 0.5 * d, pe.Z);
    Line anchor = new Line(f0, f1);

    // ---------- clash checks: the rib against everything that moves ---------
    // distance from a point to the rib's footprint, run 8 out toward the wall
    Func<double, double, double> ribDist = delegate(double ux, double uy)
    {
      double lx = (ux - f0.X) * tB.X + (uy - f0.Y) * tB.Y;
      double ly = (ux - f0.X) * nB.X + (uy - f0.Y) * nB.Y;
      double ex = Math.Max(Math.Max(-lx, 0.0), lx - (s2 - s1));
      double ey = Math.Max(Math.Max(-ly, 0.0), ly - 8.0);
      return Math.Sqrt(ex * ex + ey * ey);
    };
    double pegClr = 1e9, tangClr = 1e9;
    for (int kS = 0; kS <= 10; kS++)                  // the pawl swings through its lift
    {
      double aS = -RhinoMath.ToRadians(pawlLift * kS / 10.0);
      double cS = Math.Cos(aS), sS = Math.Sin(aS);
      double gx = pinX + qx * cS - qy * sS, gy = qx * sS + qy * cS;
      pegClr = Math.Min(pegClr, ribDist(gx, gy) - pegRad);
      for (int jS = 0; jS <= 20; jS++)
      {
        double tx = ps.X + dA.X * lA * jS / 20.0 - pinX, ty = ps.Y + dA.Y * lA * jS / 20.0;
        tangClr = Math.Min(tangClr, ribDist(pinX + tx * cS - ty * sS, tx * sS + ty * cS) - 0.5 * d);
      }
    }
    double coilClr = ribDist(pinX, 0.0) - 0.5 * Do;
    double capClr  = ribDist(0.0, 0.0) - capR;
    if (pegClr  < 0.2) log.Add("The housing rib would catch the pawl peg as the pawl lifts - change TangAngle.");
    if (tangClr < 0.2) log.Add("The housing rib would catch tang A as the pawl lifts - change TangAngle.");
    if (coilClr < 0.2) log.Add("The housing rib crowds the coil - raise ribS1.");
    if (capClr  < 0.3) log.Add("The housing rib would hit the retainer cap - change TangAngle.");

    double zAlow = Math.Min(ps.Z, (ps - ts * lA).Z) - 0.5 * d;      // lowest wire, tang A
    double zBtop = Math.Max(pe.Z, (pe + te * lB).Z) + 0.5 * d;      // highest wire, tang B
    if (zAlow < pawlTop + 0.05)
      log.Add("Tang A dips to z " + zAlow.ToString("0.##") + " - into the pawl top at " +
              pawlTop.ToString("0.#") + ".");
    if (zBtop > bandTop)
      log.Add("The spring reaches z " + zBtop.ToString("0.##") + ", above its band (" +
              bandTop.ToString("0.#") + ") - fewer Coils, or thinner wire.");

    // ---------- rate and load ------------------------------------------------
    // Na counts the tangs too: long legs add bending of their own.
    double Na      = nTot + (lA + lB) / (3.0 * Math.PI * Dm);
    double kRev    = Emod * Math.Pow(d, 4) / (10.8 * Dm * Na);      // N.mm per revolution
    double M       = kRev * windUp / 360.0;                         // N.mm, as fitted
    double sig     = 32.0 * M / (Math.PI * Math.Pow(d, 3));
    double sigMax  = sig * (windUp + pawlLift) / windUp;            // pawl fully lifted
    double Fwedge  = M / armRch;
    double IdWound = Id * nTot / (nTot + (windUp + pawlLift) / 360.0);   // bore shrinks as it winds
    if (sigMax > sAllow)
      log.Add("Wire reaches " + sigMax.ToString("0") + " MPa at full lift vs " +
              sAllow.ToString("0") + " allowable - thicker wire, or less windUp.");
    if (Fwedge < 1.0)
      log.Add("Only " + Fwedge.ToString("0.##") + " N at the wedge - it may not follow the teeth.");
    if (Fwedge > 8.0)
      log.Add("More than 8 N at the wedge - notchy ratchet, heavy release, no benefit.");
    if (IdWound <= pinDia + 0.3)
      log.Add("The bore closes to " + IdWound.ToString("0.##") + " on the " +
              pinDia.ToString("0.#") + " pin - it will bind. Open up CoilOD.");

    // ---------- place it -----------------------------------------------------
    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    Transform xf = Transform.PlaneToPlane(Plane.WorldXY, bp);
    spring.Transform(xf);
    rail.Transform(xf);
    anchor.Transform(xf);

    // ---------- report -------------------------------------------------------
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("ORDER THIS SPRING");
    sb.AppendLine("  wire              " + d.ToString("0.##") + " music wire (ASTM A228)");
    sb.AppendLine("  coil OD / ID      " + Do.ToString("0.##") + " / " + Id.ToString("0.##"));
    sb.AppendLine("  body turns        " + (nTot - windUp / 360.0).ToString("0.###") + " free");
    sb.AppendLine("  tangs             tangential, " + lA.ToString("0.#") + " and " +
                  lB.ToString("0.#") + " long");
    sb.AppendLine("  FREE tang angle   " + (((ang - windUp) % 360.0 + 360.0) % 360.0).ToString("0.#") +
                  " deg, anticlockwise from A to B seen from above");
    sb.AppendLine("--");
    sb.AppendLine("AS FITTED");
    sb.AppendLine("  tang angle        " + ang.ToString("0.#") + " deg - wound up " +
                  windUp.ToString("0.#") + " to fit, which TIGHTENS the coil");
    sb.AppendLine("  CHECK: fitting it turns tang B ANTICLOCKWISE relative to tang A.");
    sb.AppendLine("  If it has to go the other way, it is the wrong spring.");
    sb.AppendLine("  coil              " + nTot.ToString("0.###") + " turns, z " +
                  (zOff).ToString("0.##") + " .. " + zBtop.ToString("0.##") +
                  "  (band to " + bandTop.ToString("0.#") + ")");
    sb.AppendLine("  bore, fully wound " + IdWound.ToString("0.##") + " on the " +
                  pinDia.ToString("0.#") + " pin");
    sb.AppendLine("--");
    sb.AppendLine("TANG A  -> the pawl peg");
    sb.AppendLine("  leaves the coil   " + RhinoMath.ToDegrees(thA).ToString("0.##") +
                  " deg from the pin  (solved from the peg)");
    sb.AppendLine("  meets the peg     " + sA.ToString("0.##") + " along it, peg centre " +
                  offA.ToString("0.###") + " off it  (wire r + peg r = " +
                  (0.5 * d + pegRad).ToString("0.###") + ")");
    sb.AppendLine("TANG B  -> the housing rib");
    sb.AppendLine("  leaves the coil   " + RhinoMath.ToDegrees(thB).ToString("0.##") + " deg from the pin");
    sb.AppendLine("  rib bears on it   " + s1.ToString("0.#") + " .. " + s2.ToString("0.#") +
                  " along it, on its outer side");
    sb.AppendLine("  ANCHOR B          (" + f0.X.ToString("0.###") + ", " + f0.Y.ToString("0.###") +
                  ") -> (" + f1.X.ToString("0.###") + ", " + f1.Y.ToString("0.###") +
                  ")  in the gear frame");
    sb.AppendLine("  Wire AnchorB into HousingRatchet's SpringAnchor - or copy these");
    sb.AppendLine("  four numbers into its ancX0 / ancY0 / ancX1 / ancY1.");
    sb.AppendLine("--");
    sb.AppendLine("CLEARANCES FROM THE HOUSING RIB, over the whole pawl lift");
    sb.AppendLine("  pawl peg          " + pegClr.ToString("0.##"));
    sb.AppendLine("  tang A            " + tangClr.ToString("0.##"));
    sb.AppendLine("  coil              " + coilClr.ToString("0.##"));
    sb.AppendLine("  retainer cap      " + capClr.ToString("0.##"));
    sb.AppendLine("--");
    sb.AppendLine("rate                " + kRev.ToString("0.#") + " N.mm per revolution  (" +
                  Na.ToString("0.##") + " active turns, tangs included)");
    sb.AppendLine("moment at the pin   " + M.ToString("0.##") + " N.mm");
    sb.AppendLine("  at the wedge      " + Fwedge.ToString("0.##") + " N");
    sb.AppendLine("  on the pawl peg   " + (M / sA).ToString("0.##") + " N");
    sb.AppendLine("  on the housing rib " + (M / (0.5 * (s1 + s2))).ToString("0.##") + " N");
    sb.AppendLine("wire stress         " + sig.ToString("0") + " MPa, " + sigMax.ToString("0") +
                  " at full lift   vs " + sAllow.ToString("0") + "  (" +
                  (sAllow / sigMax).ToString("0.#") + "x)");
    sb.AppendLine("--");
    sb.AppendLine("The ratchet holds up to 169 N on the tooth flank. This spring is NOT");
    sb.AppendLine("part of that load path - it only keeps the wedge in contact.");
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Spring  = spring;
    Rail    = rail;
    AnchorB = anchor;
    Info    = sb.ToString();
