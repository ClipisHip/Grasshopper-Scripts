// ===========================================================================
//  HOUSING  -  Grasshopper C# component (RhinoCommon)
//  Oval shell that screws down onto the base plate.
//
//  It has a BELLY at gear level. A rack meshes TANGENTIALLY: its pitch line
//  is tangent to the gear's pitch circle, so it sits 12.18 from the axis and
//  slides along its own length. Its root line alone reaches 14.06, past the
//  26.8 bore waist, so the shell has to bulge out over z 0..7 to clear it.
//  Above that it narrows again to hug the sleeve.
//
//  ANTI-ROTATION: two ribs at the lobe tips run in the sleeve's key grooves
//  over the whole of its travel. They sit at y = 0 and the rack openings at
//  y = +/-13.87, so the two never meet and the ribs can run full height.
//
//  Both stadiums share the same overall LENGTH, so their lobe tips line up
//  at x = +/-25.4 and one rib section serves both levels.
// ===========================================================================


// ===========================================================================
//  THIS BUILD - 8 inputs to create on the component:
//     BoreWaist 26.8   BoreLength 50.8   WallThk 2   Height 44
//     BellyWaist 36    LeverSlotDeg 60   RackSlotW 7   RackSlotH 7
//     BasePlane <- the RACK script's GearPlane
//  Every constant below is already set for this build.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double BoreWaist, double BoreLength, double WallThk,
//                           double Height, double BellyWaist, double LeverSlotDeg,
//                           double RackSlotW, double RackSlotH, Plane BasePlane,
//                           ref object Body, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double Bw = BoreWaist    > 1e-9 ? BoreWaist    : 26.8;
    double Bl = BoreLength   > 1e-9 ? BoreLength   : 50.8;
    double wt = WallThk      > 1e-9 ? WallThk      :  2.0;
    double Hh = Height       > 1e-9 ? Height       : 44.0;
    double Yw = BellyWaist   > 1e-9 ? BellyWaist   : 36.0;
    double lv = LeverSlotDeg > 1e-9 ? LeverSlotDeg : 60.0;
    double rw = RackSlotW    > 1e-9 ? RackSlotW    :  7.0;
    double rh = RackSlotH    > 1e-9 ? RackSlotH    :  7.0;

    // --- match these to the base plate, sleeve and racks ---------------------
    double plateThk = 2.0;    // housing starts at -2, sitting on the plate
    double bellyHi  = 7.0;    // belly runs z -2 .. 7 (gear is z 0..6)
    double flangeD  = 62.0;
    double flangeT  = 3.0;

    int    scN      = 3;
    double scR      = 28.0;
    double scDia    = 2.5;    // PILOT - the plate has the clearance hole
    double scPhi    = 90.0;

    int    ribN     = 2;      // ANTI-ROTATION ribs, at the lobe tips
    double ribW     = 5.6;    // sleeve key groove 6.0 - 0.4 play
    double ribDep   = 2.2;    // protrudes in from the bore
    double ribZlo   = 2.0;    // sleeve bottoms out at z 6, so this is early
    double ribZhi   = 32.0;   // sleeve top reaches z 30

    double leverPhi = 90.0;   // on the waist axis, clear of the ribs
    double leverZlo = 35.0;
    double leverZhi = 41.0;

    int    rackN    = 2;      // rack openings - TANGENTIAL, not radial
    double rackOff  = 13.87;  // centre of the rack's radial span (10.68..17.05)
    double rackZlo  = 0.0;

    // --- CLUTCH ANCHOR SLOT --------------------------------------------------
    // The wrap spring only works because ONE tang is held still while the hub
    // turns under it. That tang has to be anchored to the HOUSING - without
    // this slot the spring just spins with the hub and never grips.
    // Take these from the coil script's report ("anchor tip").
    bool   ancOn   = true;
    double ancX    = 19.0;    // slot starts here and runs out through the wall
    double ancY    = 9.33;    // = coil centreline radius, tang runs along +X
    double ancZ    = 31.0;    // = coil zOff, the helix start
    double ancW    = 1.9;     // wire 1.5 + fit

    double zOff     = 0.0;

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    double ab = 0.5 * Bw,  db = 0.5 * Bl - ab;      // upper bore stadium
    double ao = ab + wt;
    double yb = 0.5 * Yw,  dy = 0.5 * Bl - yb;      // belly bore stadium, same LENGTH
    double yo = yb + wt;
    double zBot = -plateThk;
    double lobeB = db + ab;                          // = dy + yb, shared lobe tip

    // ---------- checks -------------------------------------------------------
    if (db <= 0.0 || dy <= 0.0) { Info = "BoreLength must exceed both waists."; return; }
    if (Yw <= Bw) log.Add("BellyWaist is no wider than the bore - the racks will not clear.");
    if (rackOff + 0.5 * rw > yb)
      log.Add("Rack needs " + (rackOff + 0.5 * rw).ToString("0.##") +
              " of half-width but the belly bore only gives " + yb.ToString("0.##") +
              " - widen BellyWaist to " + (2.0 * (rackOff + 0.5 * rw + 1.0)).ToString("0.#") + ".");
    if (ribZhi > Hh || leverZhi > Hh) log.Add("A feature runs past the top of the housing.");
    if (rackOff - 0.5 * rw < 0.5 * ribW)
      log.Add("Rack opening overlaps the rib at the lobe tip.");
    if (flangeD < 2.0 * (dy + yo)) log.Add("Flange is smaller than the belly.");

    // ---------- stadium helper -----------------------------------------------
    Func<double, double, double, Curve> stad = delegate(double aa, double dd, double zz)
    {
      PolyCurve p = new PolyCurve();
      p.Append(new ArcCurve(new Arc(new Point3d(dd, -aa, zz), new Point3d(dd + aa, 0, zz),
                                    new Point3d(dd, aa, zz))));
      p.Append(new LineCurve(new Point3d(dd, aa, zz), new Point3d(-dd, aa, zz)));
      p.Append(new ArcCurve(new Arc(new Point3d(-dd, aa, zz), new Point3d(-dd - aa, 0, zz),
                                    new Point3d(-dd, -aa, zz))));
      p.Append(new LineCurve(new Point3d(-dd, -aa, zz), new Point3d(dd, -aa, zz)));
      p.MakeClosed(tol);
      return p;
    };

    // ---------- shell: upper section + belly + flange + ribs ------------------
    Extrusion exU = Extrusion.Create(stad(ao, db, bellyHi), Hh - bellyHi, true);
    Extrusion exY = Extrusion.Create(stad(yo, dy, zBot), bellyHi - zBot, true);
    if (exU == null || exY == null) { Info = "Could not extrude the shell."; return; }

    List<Brep> add = new List<Brep>();
    add.Add(exU.ToBrep());
    add.Add(exY.ToBrep());
    add.Add(new Cylinder(new Circle(new Plane(new Point3d(0, 0, zBot), Vector3d.ZAxis),
                                    0.5 * flangeD), flangeT).ToBrep(true, true));
    // NB the ribs are NOT added here. They protrude INTO the bore, so if they
    // were unioned before the bore is cut the bore would swallow them again.
    // They go on after the cuts, further down.
    foreach (Brep b in add) if (b.SolidOrientation == BrepSolidOrientation.Inward) b.Flip();

    Brep body = add[0];
    Brep[] uni = Brep.CreateBooleanUnion(add, tol);
    string uniState;
    if (uni != null && uni.Length > 0)
    {
      Brep best = uni[0]; double bv = -1.0;
      foreach (Brep b in uni) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best; uniState = "OK";
    }
    else { uniState = "FAILED"; log.Add("Shell union failed."); }

    // ---------- cuts ---------------------------------------------------------
    List<Brep> cut = new List<Brep>();

    // upper bore over the full height, then the wider belly bore on top of it
    Extrusion bU = Extrusion.Create(stad(ab, db, zBot - 1.0), Hh - zBot + 2.0, true);
    Extrusion bY = Extrusion.Create(stad(yb, dy, zBot - 1.0), bellyHi - zBot + 1.0, true);
    if (bU == null || bY == null) { Info = "Could not extrude the bore."; return; }
    cut.Add(bU.ToBrep()); cut.Add(bY.ToBrep());

    for (int i = 0; i < scN; i++)
    {
      double a2 = RhinoMath.ToRadians(scPhi) + 2.0 * Math.PI * i / scN;
      Point3d o = new Point3d(scR * Math.Cos(a2), scR * Math.Sin(a2), zBot - 1.0);
      cut.Add(new Cylinder(new Circle(new Plane(o, Vector3d.ZAxis), 0.5 * scDia), flangeT + 2.0)
              .ToBrep(true, true));
    }

    // rack openings: boxes running ALONG the rack, offset from the axis.
    // rack i exits the opposite end, so the pair sits diagonally.
    for (int i = 0; i < rackN; i++)
    {
      double sgn = i == 0 ? 1.0 : -1.0;
      double y0  = sgn * rackOff;
      double xLo = sgn > 0 ? 0.0 : -(0.5 * flangeD + 4.0);
      double xHi = sgn > 0 ? (0.5 * flangeD + 4.0) : 0.0;
      cut.Add(new Box(Plane.WorldXY,
                      new Interval(xLo, xHi),
                      new Interval(y0 - 0.5 * rw, y0 + 0.5 * rw),
                      new Interval(rackZlo, rackZlo + rh)).ToBrep());
    }

    if (ancOn)
      cut.Add(new Box(Plane.WorldXY,
                      new Interval(ancX, db + ao + 4.0),
                      new Interval(ancY - 0.5 * ancW, ancY + 0.5 * ancW),
                      new Interval(ancZ - 0.5 * ancW, ancZ + 0.5 * ancW)).ToBrep());

    {
      double a1 = RhinoMath.ToRadians(leverPhi - 0.5 * lv);
      double a2 = RhinoMath.ToRadians(leverPhi + 0.5 * lv);
      double Rc = db + ao + 2.0;
      Circle c = new Circle(new Plane(new Point3d(0, 0, leverZlo), Vector3d.ZAxis), Rc);
      Arc arc = new Arc(c, new Interval(a1, a2));
      PolyCurve pc = new PolyCurve();
      pc.Append(new ArcCurve(arc));
      pc.Append(new LineCurve(arc.EndPoint, new Point3d(0, 0, leverZlo)));
      pc.Append(new LineCurve(new Point3d(0, 0, leverZlo), arc.StartPoint));
      pc.MakeClosed(tol);
      Extrusion exL = Extrusion.Create(pc, leverZhi - leverZlo, true);
      if (exL != null) cut.Add(exL.ToBrep());
      else log.Add("Could not build the lever gap.");
    }

    Brep[] diff = Brep.CreateBooleanDifference(new Brep[] { body }, cut, tol);
    string cutState;
    if (diff != null && diff.Length > 0)
    {
      Brep best = diff[0]; double bv = -1.0;
      foreach (Brep b in diff) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best;
      cutState = "OK (" + diff.Length + " piece" + (diff.Length == 1 ? ")" : "s, kept the largest)");
    }
    else { cutState = "FAILED"; log.Add("Bore / opening boolean failed."); }

    // ---------- ribs, AFTER the bore ----------------------------------------
    // They stand proud into the bore, so they have to be added once the bore
    // has been taken out - otherwise the bore cut removes them again.
    string ribState = "none";
    if (ribN > 0)
    {
      List<Brep> ribs = new List<Brep>();
      ribs.Add(body);
      for (int i = 0; i < ribN; i++)
      {
        double ang = 2.0 * Math.PI * i / ribN;           // lobe tips, 0 and 180
        Vector3d rad = new Vector3d(Math.Cos(ang), Math.Sin(ang), 0.0);
        Vector3d tng = Vector3d.CrossProduct(Vector3d.ZAxis, rad); tng.Unitize();
        Brep rb = new Box(new Plane(Point3d.Origin, rad, tng),
                          new Interval(lobeB - ribDep, lobeB + wt),
                          new Interval(-0.5 * ribW, 0.5 * ribW),
                          new Interval(ribZlo, ribZhi)).ToBrep();
        if (rb.SolidOrientation == BrepSolidOrientation.Inward) rb.Flip();
        ribs.Add(rb);
      }
      Brep[] ru = Brep.CreateBooleanUnion(ribs, tol);
      if (ru != null && ru.Length > 0)
      {
        Brep best = ru[0]; double bv = -1.0;
        foreach (Brep b in ru) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
        body = best;
        ribState = "OK (" + ribN + " added after the bore)";
      }
      else { ribState = "FAILED"; log.Add("Rib union failed - the sleeve will be free to spin."); }
    }

    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
    body.Transform(Transform.PlaneToPlane(Plane.WorldXY, bp));

    // ---------- report -------------------------------------------------------
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("upper  z " + bellyHi.ToString("0.##") + " .. " + Hh.ToString("0.##") +
                  "   bore " + Bl.ToString("0.###") + " x " + Bw.ToString("0.###") +
                  ",  outer " + (2.0 * (db + ao)).ToString("0.###") + " x " + (2.0 * ao).ToString("0.###"));
    sb.AppendLine("belly  z " + zBot.ToString("0.##") + " .. " + bellyHi.ToString("0.##") +
                  "   bore " + Bl.ToString("0.###") + " x " + Yw.ToString("0.###") +
                  ",  outer " + (2.0 * (dy + yo)).ToString("0.###") + " x " + (2.0 * yo).ToString("0.###"));
    sb.AppendLine("  both stadiums share the length, so the lobe tips line up at +/-" +
                  lobeB.ToString("0.##"));
    sb.AppendLine("flange              dia " + flangeD.ToString("0.###") + " x " + flangeT.ToString("0.###") +
                  ",  " + scN + " pilot holes dia " + scDia.ToString("0.###") + " at radius " + scR.ToString("0.###"));
    sb.AppendLine("--");
    sb.AppendLine("ANTI-ROTATION       " + ribN + " ribs at the lobe tips, " + ribW.ToString("0.###") +
                  " wide x " + ribDep.ToString("0.###") + " proud");
    sb.AppendLine("                    z " + ribZlo.ToString("0.##") + " .. " + ribZhi.ToString("0.##") +
                  "  (sleeve travels 6..30, so it is keyed throughout)");
    sb.AppendLine("rack openings       " + rackN + " TANGENTIAL, offset +/-" + rackOff.ToString("0.##") +
                  " from the axis, " + rw.ToString("0.###") + " x " + rh.ToString("0.###"));
    sb.AppendLine("                    one exits each end - they sit diagonally, not opposed");
    sb.AppendLine("lever gap           " + lv.ToString("0.#") + " deg at " + leverPhi.ToString("0.#") +
                  " deg, z " + leverZlo.ToString("0.##") + " .. " + leverZhi.ToString("0.##"));
    sb.AppendLine("clutch anchor slot  " + (ancOn ? ancW.ToString("0.##") + " square at (" +
                  ancX.ToString("0.#") + "+, " + ancY.ToString("0.##") + ", " + ancZ.ToString("0.##") + ")"
                  : "OFF - the spring will just spin with the hub"));
    sb.AppendLine("--");
    sb.AppendLine("DIAGNOSTICS");
    sb.AppendLine("  shell union       " + uniState);
    sb.AppendLine("  bore / openings   " + cutState);
    sb.AppendLine("  ribs              " + ribState);
    sb.AppendLine("  body solid        " + body.IsSolid);
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Body = body;
    Info = sb.ToString();
