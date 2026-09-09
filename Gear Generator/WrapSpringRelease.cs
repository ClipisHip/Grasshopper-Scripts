// ===========================================================================
//  WRAP SPRING RELEASE COLLAR  -  Grasshopper C# component (RhinoCommon)
//  Ring around the clutch spring with a slot that captures the CONTROL tang
//  and a lever to twist it.  Twisting the tang the unwinding way lifts the
//  spring off the hub and everything spins free.
//  The other (anchor) tang goes into a matching slot in your housing.
//  ---------------------------------------------------------------------
//  INPUTS (Item access):
//     CoilMeanDia  double  mean coil diameter = HubDia + WireDia
//     WireDia      double  spring wire diameter
//     Clearance    double  radial gap between the coil OD and this bore
//     WallThk      double  collar wall thickness
//     Height       double  collar height
//     LeverLength  double  lever reach beyond the collar OD
//     LeverWidth   double  lever width
//     BasePlane    Plane   base of the collar
//  OUTPUTS:
//     Body         Brep
//     SlotAxis     the tangent line the tang follows, to check the fit
//     Info         report + warnings
// ===========================================================================


// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double CoilMeanDia, double WireDia, double Clearance,
//                           double WallThk, double Height, double LeverLength,
//                           double LeverWidth, Plane BasePlane,
//                           ref object Body, ref object SlotAxis, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double Dm = CoilMeanDia > 1e-9 ? CoilMeanDia : 21.5;
    double d  = WireDia     > 1e-9 ? WireDia     : 1.5;
    double cl = Clearance   > 0.0  ? Clearance   : 0.4;
    double wt = WallThk     > 1e-9 ? WallThk     : 4.0;
    double Hc = Height      > 1e-9 ? Height      : 8.0;
    double Ll = LeverLength > 1e-9 ? LeverLength : 25.0;
    double Lw = LeverWidth  > 1e-9 ? LeverWidth  : 8.0;

    // --- extras --------------------------------------------------------------
    double slotFit  = 0.3;    // slop around the wire in the slot
    double tangAng  = 0.0;    // where the control tang leaves the coil [deg]
    double leverAng = 90.0;   // lever direction [deg]
    double slotZ    = 0.0;    // slot height above the collar base, 0 -> Height/2
    double zOff     = 0.0;    // slide along the axis, to sit level with the spring
    bool   openTop  = true;   // cut the slot through to the collar's TOP edge, so the
                              // collar can drop on over the tang during assembly.
                              // The notch sides still drive the tang; a retaining cap
                              // above the collar stops it lifting off.

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

    double rc   = 0.5 * Dm;                  // coil centreline radius
    double Rin  = rc + 0.5 * d + cl;         // bore radius, clears the coil
    double Rout = Rin + wt;
    if (slotZ <= 1e-9) slotZ = 0.5 * Hc;

    // a tangential tang leaves the coil at radius rc and moves outward along the
    // tangent, so the slot must follow that line - a purely radial slot misses it
    double L2 = Math.Sqrt(Math.Max(0.0, Rout * Rout - rc * rc));
    double exitDeg = RhinoMath.ToDegrees(Math.Atan2(L2, rc));
    if (L2 <= 1e-9) { Info = "Collar bore does not clear the coil."; return; }

    double sw = d + slotFit;
    if (sw >= wt) log.Add("Slot is as wide as the wall (" + wt.ToString("0.##") +
                          ") - thicken WallThk or the collar will split.");

    // ---------- ring ---------------------------------------------------------
    Brep ring = new Cylinder(new Circle(Plane.WorldXY, Rout), Hc).ToBrep(true, true);
    Brep bore = new Cylinder(new Circle(new Plane(new Point3d(0, 0, -1.0), Vector3d.ZAxis), Rin),
                             Hc + 2.0).ToBrep(true, true);

    // ---------- lever --------------------------------------------------------
    double la = RhinoMath.ToRadians(leverAng);
    Vector3d ld = new Vector3d(Math.Cos(la), Math.Sin(la), 0.0);
    Plane lp = new Plane(new Point3d(0, 0, 0), ld, Vector3d.ZAxis);
    // box spanning from the axis out past the collar, so it fuses with the ring
    Box lever = new Box(lp, new Interval(0.0, Rout + Ll),
                            new Interval(0.0, Hc),
                            new Interval(-0.5 * Lw, 0.5 * Lw));
    Brep leverB = lever.ToBrep();

    Brep[] uni = Brep.CreateBooleanUnion(new Brep[] { ring, leverB }, tol);
    Brep body = ring;
    if (uni != null && uni.Length > 0)
    {
      Brep best = uni[0]; double bv = -1.0;
      foreach (Brep b in uni) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best;
    }
    else log.Add("Lever union failed.");

    // ---------- tang slot, cut along the tangent line ------------------------
    double ta = RhinoMath.ToRadians(tangAng);
    Point3d  p0 = new Point3d(rc * Math.Cos(ta), rc * Math.Sin(ta), slotZ);
    Vector3d td = new Vector3d(-Math.Sin(ta), Math.Cos(ta), 0.0);      // tangent
    Vector3d nd = new Vector3d(Math.Cos(ta), Math.Sin(ta), 0.0);       // radial
    Plane sp = new Plane(p0, td, nd);
    // the slot plane's own z runs along WORLD -Z, so opening the notch upward
    // means extending the interval negative, out past the collar's top face
    double zLo = openTop ? (slotZ - Hc - 1.0) : -0.5 * sw;
    Box slot = new Box(sp, new Interval(-0.5 * sw, L2 + 5.0),
                           new Interval(-0.5 * sw, 0.5 * sw),
                           new Interval(zLo, 0.5 * sw));
    Line slotLine = new Line(p0, p0 + td * (L2 + 5.0));

    Brep[] diff = Brep.CreateBooleanDifference(new Brep[] { body },
                                               new Brep[] { bore, slot.ToBrep() }, tol);
    if (diff != null && diff.Length > 0)
    {
      Brep best = diff[0]; double bv = -1.0;
      foreach (Brep b in diff) { double v = b.GetBoundingBox(true).Volume; if (v > bv) { bv = v; best = b; } }
      body = best;
    }
    else log.Add("Bore / slot boolean failed.");

    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
    Transform xf = Transform.PlaneToPlane(Plane.WorldXY, bp);
    body.Transform(xf);
    slotLine.Transform(xf);

    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("bore                " + (2.0 * Rin).ToString("0.###") +
                  "   (coil OD " + (Dm + d).ToString("0.###") + " + " + cl.ToString("0.##") + " gap)");
    sb.AppendLine("outer dia           " + (2.0 * Rout).ToString("0.###") + ", wall " + wt.ToString("0.###"));
    sb.AppendLine("height              " + Hc.ToString("0.###"));
    sb.AppendLine("--");
    sb.AppendLine("tang slot           " + sw.ToString("0.###") + " square, along the tangent at " +
                  tangAng.ToString("0.#") + " deg");
    sb.AppendLine("tang exits the wall " + exitDeg.ToString("0.#") + " deg round from where it leaves the coil");
    sb.AppendLine("min tang length     " + L2.ToString("0.##") + "   (to reach through the wall)");
    sb.AppendLine("lever               " + Ll.ToString("0.#") + " long x " + Lw.ToString("0.#") +
                  " wide, at " + leverAng.ToString("0.#") + " deg");
    sb.AppendLine("--");
    sb.AppendLine("Set TangLength on the spring to at least " + (L2 + 3.0).ToString("0.#") + ".");
    sb.AppendLine("Twist the lever the UNWINDING way to release.");
    sb.AppendLine(openTop ? "Slot is OPEN to the top - collar drops on over the tang."
                          : "Slot is CLOSED - the collar cannot pass the tang on assembly.");
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Body     = body;
    SlotAxis = slotLine;
    Info     = sb.ToString();
