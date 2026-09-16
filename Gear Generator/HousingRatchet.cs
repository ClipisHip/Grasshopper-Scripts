// ===========================================================================
//  HOUSING - RATCHET VARIANT  -  Grasshopper C# component (RhinoCommon)
//  Same oval shell and belly as Housing.cs, but carrying the PAWL instead of
//  the wrap spring's anchor slot and lever gap.
//
//  Housing.cs is the wrap-spring version and is left alone. Use one or the
//  other, not both.
//
//  THE PAWL PIVOT. It sits at (16, 0), 9.4 mm clear of the nearest bore wall,
//  so it needs a web to hold it. A web beside the pawl would fight the
//  release tail for the same corridor, so the web is SPLIT instead: one below
//  the pawl and one above, leaving the pawl free to swing between them like a
//  clevis. The tail then has the whole z 32..38 band to itself.
//
//  Load path: ratchet wheel -> pawl -> pin -> post -> webs -> shell.
//  Use a METAL pin. A printed 3.2 pin sees ~21 MPa in double shear.
// ===========================================================================


// ===========================================================================
//  THIS BUILD - 8 inputs to create on the component:
//     BoreWaist 26.8   BoreLength 50.8   WallThk 2   Height 42
//     BellyWaist 36    RelSlotDeg -29    RackSlotW 7   RackSlotH 7
//     BasePlane <- the RACK script's GearPlane
//  Every constant below is already set for this build.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double BoreWaist, double BoreLength, double WallThk,
//                           double Height, double BellyWaist, double RelSlotDeg,
//                           double RackSlotW, double RackSlotH, Plane BasePlane,
//                           ref object Body, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double Bw = BoreWaist    > 1e-9 ? BoreWaist    : 26.8;
    double Bl = BoreLength   > 1e-9 ? BoreLength   : 50.8;
    double wt = WallThk      > 1e-9 ? WallThk      :  2.0;
    double Hh = Height       > 1e-9 ? Height       : 42.0;
    double Yw = BellyWaist   > 1e-9 ? BellyWaist   : 36.0;
    double rel = Math.Abs(RelSlotDeg) > 1e-9 ? RelSlotDeg : -29.0;   // pawl tail's line
    double rw = RackSlotW    > 1e-9 ? RackSlotW    :  7.0;
    double rh = RackSlotH    > 1e-9 ? RackSlotH    :  7.0;

    // --- shell, as Housing.cs ------------------------------------------------
    double bellyHi  = 7.0;
    int    lugN     = 2;
    double lugPhi   = 90.0;
    double lugR     = 24.0;
    double lugW     = 9.0;
    double lugT     = 3.0;
    double scDia    = 2.5;

    int    ribN     = 2;
    double ribW     = 5.6;
    double ribDep   = 2.2;
    double ribZlo   = 2.0;
    double ribZhi   = 30.0;   // sleeve tops out at 30; stops short of the pawl

    int    rackN    = 2;
    double rackOff  = 13.87;
    double rackZlo  = 0.0;

    // --- pawl pivot ----------------------------------------------------------
    double pawlX    = 16.0;   // MUST equal the pawl's PivotDist
    double postDia  = 6.0;    // 3.0 radius vs the 12.0 wheel -> 1.0 clear
    double pinDia   = 3.2;    // METAL pin
    double postZlo  = 30.0;
    double postZhi  = 40.0;
    double pawlZlo  = 31.8;   // the gap the pawl swings in - pawl is 6 thick
    double pawlZhi  = 38.2;   // at z 32..38, so 0.2 clearance each face
    double webW     = 7.0;    // web width, tangential. Was 5.0 - with the pawl at
                              // 25 deg the pin reaction swings mostly SIDEWAYS
                              // (70 N radial, 150 N across), which BENDS the webs.
                              // Stress goes as 1/webW^2: 5.0 -> 64 MPa, 7.0 -> 33.

    // --- release gap ---------------------------------------------------------
    double relW     = 8.0;    // wide enough for the tail plus its small swing
    double relIn    = 5.0;    // how far INSIDE the lobe the gap starts. The tail
                              // tip only reaches radius 23.16 and the wall is at
                              // 22.57, so the cut has to start inboard of both.
    double relZlo   = 31.0;
    double relZhi   = 39.0;

    double zOff     = 0.0;

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    double ab = 0.5 * Bw,  db = 0.5 * Bl - ab;
    double ao = ab + wt;
    double yb = 0.5 * Yw,  dy = 0.5 * Bl - yb;
    double yo = yb + wt;
    double zBot = 0.0;
    double lobeB = db + ab;

    // ---------- checks -------------------------------------------------------
    if (db <= 0.0 || dy <= 0.0) { Info = "BoreLength must exceed both waists."; return; }
    if (postZhi > Hh) log.Add("Pawl post runs past the top of the housing.");
    if (pawlX - 0.5 * postDia <= 12.0)
      log.Add("Pawl post fouls the 24 mm ratchet wheel - move pawlX out or thin the post.");
    if (pawlX + 0.5 * postDia >= lobeB)
      log.Add("Pawl post runs into the bore wall.");
    if (pawlZlo <= postZlo || pawlZhi >= postZhi)
      log.Add("Pawl gap is not inside the post - the webs will not form.");
    if (ribZhi > pawlZlo) log.Add("Ribs reach into the pawl's swing.");

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

    // ---------- shell --------------------------------------------------------
    Extrusion exU = Extrusion.Create(stad(ao, db, zBot), Hh - zBot, true);
    Extrusion exY = Extrusion.Create(stad(yo, dy, zBot), bellyHi - zBot, true);
    if (exU == null || exY == null) { Info = "Could not extrude the shell."; return; }

    List<Brep> add = new List<Brep>();
    add.Add(exU.ToBrep());
    add.Add(exY.ToBrep());
    for (int i = 0; i < lugN; i++)
    {
      double ang = RhinoMath.ToRadians(lugPhi) + 2.0 * Math.PI * i / lugN;
      Vector3d rad = new Vector3d(Math.Cos(ang), Math.Sin(ang), 0.0);
      Vector3d tng = Vector3d.CrossProduct(Vector3d.ZAxis, rad); tng.Unitize();
      add.Add(new Box(new Plane(Point3d.Origin, rad, tng),
                      new Interval(yo - 1.0, lugR + 5.0),
                      new Interval(-0.5 * lugW, 0.5 * lugW),
                      new Interval(zBot, zBot + lugT)).ToBrep());
    }
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
    Extrusion bU = Extrusion.Create(stad(ab, db, zBot - 1.0), Hh - zBot + 2.0, true);
    Extrusion bY = Extrusion.Create(stad(yb, dy, zBot - 1.0), bellyHi - zBot + 1.0, true);
    if (bU == null || bY == null) { Info = "Could not extrude the bore."; return; }
    cut.Add(bU.ToBrep()); cut.Add(bY.ToBrep());

    for (int i = 0; i < lugN; i++)
    {
      double a2 = RhinoMath.ToRadians(lugPhi) + 2.0 * Math.PI * i / lugN;
      Point3d o = new Point3d(lugR * Math.Cos(a2), lugR * Math.Sin(a2), zBot - 1.0);
      cut.Add(new Cylinder(new Circle(new Plane(o, Vector3d.ZAxis), 0.5 * scDia), lugT + 2.0)
              .ToBrep(true, true));
    }

    for (int i = 0; i < rackN; i++)
    {
      double sgn = i == 0 ? 1.0 : -1.0;
      double y0  = sgn * rackOff;
      double yHi = Math.Abs(rackOff) + 0.5 * rw;
      double yLo = Math.Abs(rackOff) - 0.5 * rw;
      double xIn  = dy + Math.Sqrt(Math.Max(0.0, yb * yb - yHi * yHi));
      double xOut = dy + Math.Sqrt(Math.Max(0.0, yo * yo - yLo * yLo));
      cut.Add(new Box(Plane.WorldXY,
                      new Interval(sgn > 0 ? xIn - 1.0 : -(xOut + 2.0),
                                   sgn > 0 ? xOut + 2.0 : -(xIn - 1.0)),
                      new Interval(y0 - 0.5 * rw, y0 + 0.5 * rw),
                      new Interval(rackZlo, rackZlo + rh)).ToBrep());
    }

    // release gap - a radial box on the tail's line, out through the wall
    {
      double ra = RhinoMath.ToRadians(rel);
      Vector3d rad = new Vector3d(Math.Cos(ra), Math.Sin(ra), 0.0);
      Vector3d tng = Vector3d.CrossProduct(Vector3d.ZAxis, rad); tng.Unitize();
      cut.Add(new Box(new Plane(Point3d.Origin, rad, tng),
                      new Interval(lobeB - relIn, lobeB + wt + 6.0),
                      new Interval(-0.5 * relW, 0.5 * relW),
                      new Interval(relZlo, relZhi)).ToBrep());
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

    // ---------- ribs, post and webs - AFTER the bore -------------------------
    // All three stand proud INTO the bore, so the bore cut would swallow them
    // if they were added before it.
    List<Brep> inward = new List<Brep>();
    inward.Add(body);

    for (int i = 0; i < ribN; i++)
    {
      double ang = 2.0 * Math.PI * i / ribN;
      Vector3d rad = new Vector3d(Math.Cos(ang), Math.Sin(ang), 0.0);
      Vector3d tng = Vector3d.CrossProduct(Vector3d.ZAxis, rad); tng.Unitize();
      Brep rb = new Box(new Plane(Point3d.Origin, rad, tng),
                        new Interval(lobeB - ribDep, lobeB + wt),
                        new Interval(-0.5 * ribW, 0.5 * ribW),
                        new Interval(ribZlo, ribZhi)).ToBrep();
      if (rb.SolidOrientation == BrepSolidOrientation.Inward) rb.Flip();
      inward.Add(rb);
    }

    // THE POST IS SPLIT TOO, not just the webs. A single column from postZlo to
    // postZhi would fill the very gap the pawl swings in - the pawl has a 3.2
    // bore and could not pass a 6.0 column. Only the METAL PIN crosses the gap.
    Brep postLo = new Cylinder(new Circle(new Plane(new Point3d(pawlX, 0, postZlo), Vector3d.ZAxis),
                                          0.5 * postDia), pawlZlo - postZlo).ToBrep(true, true);
    Brep postHi = new Cylinder(new Circle(new Plane(new Point3d(pawlX, 0, pawlZhi), Vector3d.ZAxis),
                                          0.5 * postDia), postZhi - pawlZhi).ToBrep(true, true);
    if (postLo.SolidOrientation == BrepSolidOrientation.Inward) postLo.Flip();
    if (postHi.SolidOrientation == BrepSolidOrientation.Inward) postHi.Flip();
    inward.Add(postLo); inward.Add(postHi);

    Plane wp = new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis);
    Brep webLo = new Box(wp, new Interval(pawlX - 0.5 * postDia, lobeB + wt),
                             new Interval(-0.5 * webW, 0.5 * webW),
                             new Interval(postZlo, pawlZlo)).ToBrep();
    Brep webHi = new Box(wp, new Interval(pawlX - 0.5 * postDia, lobeB + wt),
                             new Interval(-0.5 * webW, 0.5 * webW),
                             new Interval(pawlZhi, postZhi)).ToBrep();
    if (webLo.SolidOrientation == BrepSolidOrientation.Inward) webLo.Flip();
    if (webHi.SolidOrientation == BrepSolidOrientation.Inward) webHi.Flip();
    inward.Add(webLo); inward.Add(webHi);

    string inState;
    Brep[] iu = Brep.CreateBooleanUnion(inward, tol);
    if (iu != null && iu.Length > 0)
    {
      Brep best = iu[0]; double bv = -1.0;
      foreach (Brep b in iu) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best; inState = "OK (ribs + post + 2 webs)";
    }
    else { inState = "FAILED"; log.Add("Rib / post / web union failed."); }

    // pin hole last, through the finished post
    Brep pin = new Cylinder(new Circle(new Plane(new Point3d(pawlX, 0, postZlo - 1.0), Vector3d.ZAxis),
                                       0.5 * pinDia), postZhi - postZlo + 2.0).ToBrep(true, true);
    if (pin.SolidOrientation == BrepSolidOrientation.Inward) pin.Flip();
    Brep[] pd = Brep.CreateBooleanDifference(new Brep[] { body }, new Brep[] { pin }, tol);
    string pinState;
    if (pd != null && pd.Length > 0)
    {
      Brep best = pd[0]; double bv = -1.0;
      foreach (Brep b in pd) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best; pinState = "OK";
    }
    else { pinState = "FAILED"; log.Add("Pin hole boolean failed."); }

    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
    body.Transform(Transform.PlaneToPlane(Plane.WorldXY, bp));

    // ---------- report -------------------------------------------------------
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("upper  z " + bellyHi.ToString("0.##") + " .. " + Hh.ToString("0.##") +
                  "   bore " + Bl.ToString("0.###") + " x " + Bw.ToString("0.###"));
    sb.AppendLine("belly  z " + zBot.ToString("0.##") + " .. " + bellyHi.ToString("0.##") +
                  "   bore " + Bl.ToString("0.###") + " x " + Yw.ToString("0.###"));
    sb.AppendLine("screw lugs          " + lugN + " at radius " + lugR.ToString("0.###"));
    sb.AppendLine("ribs                " + ribN + ", z " + ribZlo.ToString("0.##") + " .. " +
                  ribZhi.ToString("0.##"));
    sb.AppendLine("--");
    sb.AppendLine("PAWL POST           dia " + postDia.ToString("0.###") + " at (" +
                  pawlX.ToString("0.##") + ", 0), SPLIT: z " + postZlo.ToString("0.##") + ".." +
                  pawlZlo.ToString("0.##") + " and " + pawlZhi.ToString("0.##") + ".." +
                  postZhi.ToString("0.##"));
    sb.AppendLine("  only the PIN crosses the pawl's band - a solid post there");
    sb.AppendLine("  would not pass the pawl's 3.2 bore.");
    sb.AppendLine("  pin hole          " + pinDia.ToString("0.###") + " - USE A METAL PIN");
    sb.AppendLine("  clear of wheel    " + (pawlX - 0.5 * postDia - 12.0).ToString("0.##"));
    sb.AppendLine("  webs              z " + postZlo.ToString("0.##") + ".." + pawlZlo.ToString("0.##") +
                  " and " + pawlZhi.ToString("0.##") + ".." + postZhi.ToString("0.##") +
                  ", " + webW.ToString("0.##") + " wide");
    sb.AppendLine("  pawl swings in    z " + pawlZlo.ToString("0.##") + " .. " + pawlZhi.ToString("0.##") +
                  "   (" + (pawlZhi - pawlZlo).ToString("0.##") + " for a 6 thick pawl)");
    sb.AppendLine("  SPLIT webs keep the corridor at pawl level clear, so the");
    sb.AppendLine("  release tail has the whole band to itself.");
    sb.AppendLine("release gap         " + relW.ToString("0.##") + " wide at " + rel.ToString("0.#") +
                  " deg, z " + relZlo.ToString("0.##") + " .. " + relZhi.ToString("0.##"));
    sb.AppendLine("rack windows        " + rackN + " at +/-" + rackOff.ToString("0.##"));
    sb.AppendLine("--");
    sb.AppendLine("STILL NEEDED: a light spring holding the pawl into the teeth.");
    sb.AppendLine("--");
    sb.AppendLine("DIAGNOSTICS");
    sb.AppendLine("  shell union       " + uniState);
    sb.AppendLine("  bore / openings   " + cutState);
    sb.AppendLine("  inward features   " + inState);
    sb.AppendLine("  pin hole          " + pinState);
    sb.AppendLine("  body solid        " + body.IsSolid);
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Body = body;
    Info = sb.ToString();
