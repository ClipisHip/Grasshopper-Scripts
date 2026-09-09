// ===========================================================================
//  HOUSING  -  Grasshopper C# component (RhinoCommon)
//  Oval shell that screws down onto the base plate. Its bore is the sleeve's
//  stadium plus a running clearance, so the wall stays a uniform thickness
//  all the way round rather than ballooning at the waist.
//
//  It does four jobs:
//     - two internal RIBS at the lobe tips run in the sleeve's key grooves
//       and stop it rotating
//     - the oval bore guides the sleeve over its whole travel
//     - an angular gap lets the release lever swing out
//     - openings at gear level let the two racks travel out to the jaws
//
//  Measured from the gear plane (z = 0). The flange sits on the base plate,
//  so the housing starts at z = -PlateThk.
// ===========================================================================


// ===========================================================================
//  THIS BUILD - 7 inputs to create on the component:
//     BoreWaist 26.8   BoreLength 50.8   WallThk 2   Height 44
//     LeverSlotDeg 60  RackSlotW 7       RackSlotH 7
//     BasePlane <- the RACK script's GearPlane
//  Bore = the sleeve's 26 x 50 oval + 0.4 clearance all round.
//  Every constant below is already set for this build.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double BoreWaist, double BoreLength, double WallThk,
//                           double Height, double LeverSlotDeg, double RackSlotW,
//                           double RackSlotH, Plane BasePlane,
//                           ref object Body, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double Bw = BoreWaist    > 1e-9 ? BoreWaist    : 26.8;
    double Bl = BoreLength   > 1e-9 ? BoreLength   : 50.8;
    double wt = WallThk      > 1e-9 ? WallThk      :  2.0;
    double Hh = Height       > 1e-9 ? Height       : 44.0;
    double lv = LeverSlotDeg > 1e-9 ? LeverSlotDeg : 60.0;
    double rw = RackSlotW    > 1e-9 ? RackSlotW    :  7.0;
    double rh = RackSlotH    > 1e-9 ? RackSlotH    :  7.0;

    // --- match these to the base plate and the sleeve ------------------------
    double plateThk = 2.0;    // housing starts at -2, sitting on the plate
    double flangeD  = 62.0;   // bottom mounting flange
    double flangeT  = 3.0;

    int    scN      = 3;      // screws, matching the base plate
    double scR      = 28.0;   // screw circle RADIUS
    double scDia    = 2.5;    // PILOT hole - the plate has the clearance hole
    double scPhi    = 90.0;

    int    ribN     = 2;      // anti-rotation ribs, at the lobe tips
    double ribW     = 5.6;    // sleeve key groove 6.0 - 0.4 play
    double ribDep   = 2.2;    // protrudes into the bore
    double ribZlo   = 8.0;    // starts above the rack openings
    double ribZhi   = 32.0;   // sleeve travels z 6..30

    double leverPhi = 90.0;   // on the waist axis, clear of the ribs
    double leverZlo = 35.0;   // collar sits z 36..40
    double leverZhi = 41.0;

    int    rackN    = 2;      // rack openings at the lobe tips
    double rackPhi  = 0.0;
    double rackZlo  = 0.0;    // gear sits z 0..6

    double zOff     = 0.0;

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    double ab = 0.5 * Bw, db = 0.5 * Bl - ab;      // bore stadium
    double ao = ab + wt;                            // outer stadium, same centres
    double outLen = 2.0 * (db + ao), outWid = 2.0 * ao;
    double zBot = -plateThk;

    // ---------- checks -------------------------------------------------------
    if (db <= 0.0) { Info = "BoreLength must exceed BoreWaist."; return; }
    if (flangeD < outLen) log.Add("Flange (" + flangeD.ToString("0.#") +
        ") is smaller than the shell length (" + outLen.ToString("0.#") + ").");
    if (scR - 0.5 * scDia < 0.5 * outLen)
      log.Add("Screw circle overlaps the shell - push scR out past " +
              (0.5 * outLen + 0.5 * scDia + 1.0).ToString("0.##") + ".");
    if (scR + 0.5 * scDia > 0.5 * flangeD) log.Add("Screw circle runs off the flange.");
    if (ribZhi > Hh || leverZhi > Hh) log.Add("A feature runs past the top of the housing.");
    if (ribZlo < rackZlo + rh) log.Add("Ribs start inside the rack openings - raise ribZlo.");

    // ---------- oval shell ---------------------------------------------------
    PolyCurve outer = new PolyCurve();
    outer.Append(new ArcCurve(new Arc(new Point3d(db, -ao, zBot), new Point3d(db + ao, 0, zBot),
                                      new Point3d(db, ao, zBot))));
    outer.Append(new LineCurve(new Point3d(db, ao, zBot), new Point3d(-db, ao, zBot)));
    outer.Append(new ArcCurve(new Arc(new Point3d(-db, ao, zBot), new Point3d(-db - ao, 0, zBot),
                                      new Point3d(-db, -ao, zBot))));
    outer.Append(new LineCurve(new Point3d(-db, -ao, zBot), new Point3d(db, -ao, zBot)));
    outer.MakeClosed(tol);

    Extrusion exO = Extrusion.Create(outer, Hh - zBot, true);
    if (exO == null) { Info = "Could not extrude the shell."; return; }
    Brep shell = exO.ToBrep();
    if (shell.SolidOrientation == BrepSolidOrientation.Inward) shell.Flip();

    Brep flange = new Cylinder(new Circle(new Plane(new Point3d(0, 0, zBot), Vector3d.ZAxis),
                                          0.5 * flangeD), flangeT).ToBrep(true, true);
    if (flange.SolidOrientation == BrepSolidOrientation.Inward) flange.Flip();

    // ribs are added material, so they join the union
    List<Brep> add = new List<Brep>();
    add.Add(shell); add.Add(flange);
    double lobeB = db + ab;
    for (int i = 0; i < ribN; i++)
    {
      double ang = RhinoMath.ToRadians(rackPhi) + 2.0 * Math.PI * i / ribN;
      Vector3d rad = new Vector3d(Math.Cos(ang), Math.Sin(ang), 0.0);
      Vector3d tng = Vector3d.CrossProduct(Vector3d.ZAxis, rad); tng.Unitize();
      Plane rp2 = new Plane(Point3d.Origin, rad, tng);
      add.Add(new Box(rp2, new Interval(lobeB - ribDep, lobeB + wt),
                           new Interval(-0.5 * ribW, 0.5 * ribW),
                           new Interval(ribZlo, ribZhi)).ToBrep());
    }

    Brep body = shell;
    Brep[] uni = Brep.CreateBooleanUnion(add, tol);
    string uniState;
    if (uni != null && uni.Length > 0)
    {
      Brep best = uni[0]; double bv = -1.0;
      foreach (Brep b in uni) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best; uniState = "OK";
    }
    else { uniState = "FAILED"; log.Add("Shell / flange / rib union failed."); }

    // ---------- everything cut away ------------------------------------------
    List<Brep> cut = new List<Brep>();

    PolyCurve bore = new PolyCurve();
    bore.Append(new ArcCurve(new Arc(new Point3d(db, -ab, zBot - 1.0), new Point3d(db + ab, 0, zBot - 1.0),
                                     new Point3d(db, ab, zBot - 1.0))));
    bore.Append(new LineCurve(new Point3d(db, ab, zBot - 1.0), new Point3d(-db, ab, zBot - 1.0)));
    bore.Append(new ArcCurve(new Arc(new Point3d(-db, ab, zBot - 1.0), new Point3d(-db - ab, 0, zBot - 1.0),
                                     new Point3d(-db, -ab, zBot - 1.0))));
    bore.Append(new LineCurve(new Point3d(-db, -ab, zBot - 1.0), new Point3d(db, -ab, zBot - 1.0)));
    bore.MakeClosed(tol);
    Extrusion exB = Extrusion.Create(bore, Hh - zBot + 2.0, true);
    if (exB == null) { Info = "Could not extrude the bore."; return; }
    cut.Add(exB.ToBrep());

    for (int i = 0; i < scN; i++)
    {
      double a2 = RhinoMath.ToRadians(scPhi) + 2.0 * Math.PI * i / scN;
      Point3d o = new Point3d(scR * Math.Cos(a2), scR * Math.Sin(a2), zBot - 1.0);
      cut.Add(new Cylinder(new Circle(new Plane(o, Vector3d.ZAxis), 0.5 * scDia), flangeT + 2.0)
              .ToBrep(true, true));
    }

    for (int i = 0; i < rackN; i++)
    {
      double a2 = RhinoMath.ToRadians(rackPhi) + 2.0 * Math.PI * i / rackN;
      Vector3d rad = new Vector3d(Math.Cos(a2), Math.Sin(a2), 0.0);
      Vector3d tng = Vector3d.CrossProduct(Vector3d.ZAxis, rad); tng.Unitize();
      Plane bp2 = new Plane(Point3d.Origin, rad, tng);
      cut.Add(new Box(bp2, new Interval(lobeB - 1.0, 0.5 * flangeD + 2.0),
                           new Interval(-0.5 * rw, 0.5 * rw),
                           new Interval(rackZlo, rackZlo + rh)).ToBrep());
    }

    {
      double a1 = RhinoMath.ToRadians(leverPhi - 0.5 * lv);
      double a2 = RhinoMath.ToRadians(leverPhi + 0.5 * lv);
      double Rc = 0.5 * outLen + 2.0;
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

    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
    body.Transform(Transform.PlaneToPlane(Plane.WorldXY, bp));

    // ---------- report -------------------------------------------------------
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("bore  (oval)        " + Bl.ToString("0.###") + " x " + Bw.ToString("0.###"));
    sb.AppendLine("outer (oval)        " + outLen.ToString("0.###") + " x " + outWid.ToString("0.###") +
                  ",  uniform wall " + wt.ToString("0.###"));
    sb.AppendLine("                    z " + zBot.ToString("0.##") + " .. " + Hh.ToString("0.##"));
    sb.AppendLine("flange              dia " + flangeD.ToString("0.###") + " x " + flangeT.ToString("0.###") +
                  ",  " + scN + " pilot holes dia " + scDia.ToString("0.###") + " at radius " + scR.ToString("0.###"));
    sb.AppendLine("--");
    sb.AppendLine("ribs                " + ribN + " x " + ribW.ToString("0.###") + " wide x " +
                  ribDep.ToString("0.###") + " proud, z " + ribZlo.ToString("0.##") + " .. " + ribZhi.ToString("0.##"));
    sb.AppendLine("   they run in the sleeve's key grooves - that is the anti-rotation");
    sb.AppendLine("lever gap           " + lv.ToString("0.#") + " deg at " + leverPhi.ToString("0.#") +
                  " deg, z " + leverZlo.ToString("0.##") + " .. " + leverZhi.ToString("0.##"));
    sb.AppendLine("rack openings       " + rackN + " x " + rw.ToString("0.###") + " wide x " +
                  rh.ToString("0.###") + " tall, z " + rackZlo.ToString("0.##"));
    sb.AppendLine("--");
    sb.AppendLine("DIAGNOSTICS");
    sb.AppendLine("  shell union       " + uniState);
    sb.AppendLine("  bore / openings   " + cutState);
    sb.AppendLine("  body solid        " + body.IsSolid);
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Body = body;
    Info = sb.ToString();
