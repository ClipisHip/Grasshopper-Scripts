// ===========================================================================
//  RETAINER CAP  -  Grasshopper C# component (RhinoCommon)
//  Presses onto the top of the fixed shaft and traps BOTH the rotor and the
//  release collar axially, while leaving both free to turn.
//
//  Why it is needed: the collar's tang notch is open at its top edge so the
//  collar can be dropped on over the tang during assembly. That is also an
//  open door - nothing else stops it lifting straight back off.
//
//  It bears on nothing. It sits a small gap above the rotor and collar tops,
//  so it stops them rising without clamping them.
//
//  NOTE the shaft must be lengthened to reach it - ShaftLen 46, not 42.
// ===========================================================================


// ===========================================================================
//  THIS BUILD - 5 inputs to create on the component:
//     CapDia 26   CapThk 3   ShaftDia 6   Gap 0.3
//     BasePlane <- the RACK script's GearPlane
//  Every constant below is already set for this build.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double CapDia, double CapThk, double ShaftDia,
//                           double Gap, Plane BasePlane,
//                           ref object Body, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double Dc = CapDia   > 1e-9 ? CapDia   : 26.0;
    double tc = CapThk   > 1e-9 ? CapThk   :  3.0;
    double Ds = ShaftDia > 1e-9 ? ShaftDia :  6.0;
    double gp = Gap      > 0.0  ? Gap      :  0.3;

    // --- match these to the rest of the stack --------------------------------
    double rotorTop  = 40.0;   // hub top, and the collar top
    double collarOD  = 24.95;  // must be covered, or it can still lift
    double boreWaist = 26.8;   // housing bore short axis - the cap must clear it
    double pressFit  = 0.10;   // bore is ShaftDia MINUS this, for a press fit.
                               // Set to a negative number for a slip fit plus
                               // a grub screw instead.
    double screwDia  = 0.0;    // >0 adds an axial hole for a screw into the
                               // shaft end, if the press fit will not hold
    double zOff      = 0.0;

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    double zLo = rotorTop + gp;          // underside, clear of both tops
    double Rc  = 0.5 * Dc;
    double Db  = Ds - pressFit;

    // ---------- checks -------------------------------------------------------
    if (Dc <= collarOD)
      log.Add("Cap " + Dc.ToString("0.##") + " does not overhang the collar (" +
              collarOD.ToString("0.##") + ") - it will not retain it.");
    if (Dc >= boreWaist)
      log.Add("Cap " + Dc.ToString("0.##") + " fouls the housing bore waist (" +
              boreWaist.ToString("0.##") + ") - reduce CapDia.");
    if (Db <= 0.0) { Info = "Press fit is larger than the shaft."; return; }
    if (screwDia > 1e-9 && screwDia >= Db)
    { Info = "Screw hole is as big as the bore."; return; }
    log.Add("Shaft must reach z " + (zLo + tc).ToString("0.##") +
            " - set ShaftLen to at least " + (zLo + tc).ToString("0.#") + " on the base plate.");

    // ---------- cap ----------------------------------------------------------
    Brep cap = new Cylinder(new Circle(new Plane(new Point3d(0, 0, zLo), Vector3d.ZAxis), Rc), tc)
               .ToBrep(true, true);
    if (cap.SolidOrientation == BrepSolidOrientation.Inward) cap.Flip();

    List<Brep> cut = new List<Brep>();
    cut.Add(new Cylinder(new Circle(new Plane(new Point3d(0, 0, zLo - 1.0), Vector3d.ZAxis),
                                    0.5 * Db), tc + 2.0).ToBrep(true, true));
    if (screwDia > 1e-9)
      cut.Add(new Cylinder(new Circle(new Plane(new Point3d(0, 0, zLo - 1.0), Vector3d.ZAxis),
                                      0.5 * screwDia), tc + 2.0).ToBrep(true, true));

    Brep body = cap;
    string cutState;
    Brep[] diff = Brep.CreateBooleanDifference(new Brep[] { cap }, cut, tol);
    if (diff != null && diff.Length > 0)
    {
      Brep best = diff[0]; double bv = -1.0;
      foreach (Brep b in diff) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best; cutState = "OK";
    }
    else { cutState = "FAILED"; log.Add("Bore boolean failed."); }

    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
    body.Transform(Transform.PlaneToPlane(Plane.WorldXY, bp));

    // ---------- report -------------------------------------------------------
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("cap                 dia " + Dc.ToString("0.###") + " x " + tc.ToString("0.###") +
                  ",  z " + zLo.ToString("0.##") + " .. " + (zLo + tc).ToString("0.##"));
    sb.AppendLine("bore                " + Db.ToString("0.###") + "   (shaft " + Ds.ToString("0.##") +
                  " less a " + pressFit.ToString("0.###") + " press fit)");
    if (screwDia > 1e-9) sb.AppendLine("screw hole          " + screwDia.ToString("0.###"));
    sb.AppendLine("--");
    sb.AppendLine("overhangs the collar by " + (0.5 * (Dc - collarOD)).ToString("0.###") +
                  " all round");
    sb.AppendLine("clears the housing bore by " + (0.5 * (boreWaist - Dc)).ToString("0.###"));
    sb.AppendLine("sits " + gp.ToString("0.##") + " above the rotor and collar tops, so it");
    sb.AppendLine("stops them rising without clamping either.");
    sb.AppendLine("--");
    sb.AppendLine("DIAGNOSTICS");
    sb.AppendLine("  bore              " + cutState);
    sb.AppendLine("  body solid        " + body.IsSolid);
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Body = body;
    Info = sb.ToString();
