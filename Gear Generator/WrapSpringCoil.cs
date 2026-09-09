// ===========================================================================
//  WRAP SPRING COIL  -  Grasshopper C# component (RhinoCommon)
//  The clutch spring: close-wound coil with a tangential tang at each end.
//  One tang anchors to the housing, the other is the control (release) tang.
//  ---------------------------------------------------------------------
//  INPUTS (Item access):
//     WireDia       double  spring wire diameter
//     HubDia        double  diameter of the hub it grips
//     Interference  double  DIAMETRAL squeeze: free ID = HubDia - this
//     Coils         double  number of active coils (3-5 typical)
//     TangLength    double  straight tang length at each end
//     Torque        double  holding torque required [N.m] (report only)
//     BasePlane     Plane   base of the coil
//  OUTPUTS:
//     Spring        Brep of the wire
//     Rail          wire centreline curve
//     Info          sizing report + warnings
//
//  Direction: wound so the spring TIGHTENS when the hub tries to turn the
//  way the clamp pulls it.  Flip `hand` if it grips the wrong way.
// ===========================================================================



// ===========================================================================
//  THIS BUILD - set on the component:
//     WireDia 1.5   HubDia 17.15   Interference 0.15
//     Coils 5   TangLength 16   Torque 1.92
//     BasePlane <- the RACK script's GearPlane
//  Every constant below is already set for this build.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(double WireDia, double HubDia, double Interference,
//                           double Coils, double TangLength, double Torque,
//                           Plane BasePlane,
//                           ref object Spring, ref object Rail, ref object Info)
// ---------------------------------------------------------------------------

    List<string> log = new List<string>();

    double d   = WireDia      > 1e-9 ? WireDia      : 1.5;
    double Dh  = HubDia       > 1e-9 ? HubDia       : 17.15;
    double itf = Interference > 0.0  ? Interference : 0.15;
    double nc  = Coils        > 0.2  ? Coils        : 5.0;
    double tl  = TangLength   > 1e-9 ? TangLength   : 16.0;
    double Treq= Torque       > 1e-9 ? Torque       : 1.92;

    // --- material / fit ------------------------------------------------------
    double E     = 200000.0;   // N/mm2, music wire / stainless spring steel
    double sAll  = 1200.0;     // N/mm2 allowable combined stress in the wire
    double mu    = 0.30;       // spring on hub. ~0.15 steel/steel oiled, ~0.30 on plastic
    double pHub  = 20.0;       // N/mm2 allowable hub contact pressure (printed plastic)
    double gap   = 0.0;        // axial gap between coils, 0 = close wound
    double phi0  = 90.0;       // 90 puts the tangs on the WAIST, running along X
                               // toward the LOBES where the bore reaches 25.4.
                               // At 0 they run at the bore waist and hit the wall
                               // after 9.5 mm - before they clear the collar.
    double tlA   = 24.0;       // ANCHOR tang: must reach the slot in the housing
                               // wall, which sits 21.6 out along X
    double tlC   = 16.0;       // CONTROL tang: only has to clear the collar (11.3)
    double hand  = 1.0;        // +1 right-hand wind, -1 left-hand
    double zOff  = 31.0;        // slide along the axis, to sit on the hub

    double tol = 0.001;
    if (Rhino.RhinoDoc.ActiveDoc != null) tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
    double aTol = 0.01;

    // ---------- geometry -----------------------------------------------------
    double idFree = Dh - itf;                 // free inner diameter
    if (idFree <= 0.0) { Info = "Interference is larger than the hub."; return; }
    double rFree  = 0.5 * (idFree + d);       // free MEAN coil radius
    double rMnt   = 0.5 * (Dh + d);           // mean radius when sprung onto the hub
    double pitch  = d + gap;
    double len    = nc * pitch;

    // ---------- engineering --------------------------------------------------
    double A  = 0.25 * Math.PI * d * d;
    double I  = Math.PI * Math.Pow(d, 4.0) / 64.0;
    double dk = 1.0 / rFree - 1.0 / rMnt;     // curvature change on assembly
    double M0 = E * I * dk;                   // moment stored in the coil
    double T0 = M0 / rMnt;                    // initial wire tension
    double th = 2.0 * Math.PI * nc;           // total wrap angle
    double cap= rMnt * T0 * (Math.Exp(mu * th) - 1.0) / 1000.0;   // N.m capacity

    double dT   = Treq * 1000.0 / rMnt;       // extra tension to carry the torque
    double Tpk  = T0 + dT;                    // peak wire tension, at the anchor
    double sTen = Tpk / A;                    // direct tensile stress
    double sBnd = E * 0.5 * d * dk;           // bending stress from springing it open
    double sTot = sTen + sBnd;
    double pres = Tpk / (rMnt * pitch);       // peak radial pressure on the hub

    if (cap < Treq)
      log.Add("Capacity " + cap.ToString("0.##") + " N.m is below the required " +
              Treq.ToString("0.##") + " - add coils or interference.");
    if (sTot > sAll)
      log.Add("Wire stress " + sTot.ToString("0") + " N/mm2 exceeds " + sAll.ToString("0") +
              " - use thicker wire or a bigger hub.");
    if (pres > pHub)
      log.Add("Hub pressure " + pres.ToString("0.#") + " N/mm2 exceeds " + pHub.ToString("0.#") +
              " for plastic - increase HubDia to about " +
              (Dh * pres / pHub).ToString("0.#") + " or use a metal hub.");
    if (nc < 3.0) log.Add("Fewer than 3 coils - grip is unreliable at the tang ends.");

    // nearest standard music-wire size
    double[] std = { 0.5,0.6,0.7,0.8,0.9,1.0,1.1,1.2,1.4,1.6,1.8,2.0,2.2,2.5,2.8,3.0,3.5,4.0 };
    double near = std[0];
    foreach (double v in std) if (Math.Abs(v - d) < Math.Abs(near - d)) near = v;

    // ---------- model --------------------------------------------------------
    double rc = rMnt;   // draw it sprung onto the hub, as assembled
    NurbsCurve helix = NurbsCurve.CreateSpiral(Point3d.Origin, Vector3d.ZAxis,
                                               new Point3d(rc, 0, 0), hand * pitch, nc, rc, rc);
    if (helix == null) { Info = "Could not build the helix - check Coils / WireDia."; return; }

    Point3d  ps = helix.PointAtStart, pe = helix.PointAtEnd;
    Vector3d ts = helix.TangentAtStart, te = helix.TangentAtEnd;
    ts.Unitize(); te.Unitize();

    PolyCurve rail = new PolyCurve();
    double lenA = tlA > 1e-9 ? tlA : tl;
    double lenC = tlC > 1e-9 ? tlC : tl;
    rail.Append(new LineCurve(ps - ts * lenA, ps));    // anchor tang  -> housing slot
    rail.Append(helix);
    rail.Append(new LineCurve(pe, pe + te * lenC));    // control tang -> collar slot

    Brep[] pipe = Brep.CreatePipe(rail, 0.5 * d, false, PipeCapMode.Round, true, tol, aTol);
    if (pipe == null || pipe.Length == 0) { Info = "Pipe failed - try fewer coils or a coarser tolerance."; return; }
    Brep spring = pipe[0];
    if (pipe.Length > 1) { Brep[] j = Brep.JoinBreps(pipe, tol); if (j != null && j.Length > 0) spring = j[0]; }

    Plane bp = BasePlane.IsValid ? BasePlane : Plane.WorldXY;
    bp.Origin = bp.Origin + bp.ZAxis * zOff;
    Transform rot = Transform.Rotation(RhinoMath.ToRadians(phi0), Vector3d.ZAxis, Point3d.Origin);
    Transform xf  = Transform.PlaneToPlane(Plane.WorldXY, bp) * rot;
    spring.Transform(xf);
    rail.Transform(xf);

    // ---------- report -------------------------------------------------------
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("wire dia            " + d.ToString("0.###") +
                  (Math.Abs(near - d) > 1e-6 ? "   (nearest standard " + near.ToString("0.#") + ")" : "   (standard)"));
    sb.AppendLine("coils               " + nc.ToString("0.##") + ",  pitch " + pitch.ToString("0.###"));
    sb.AppendLine("free ID             " + idFree.ToString("0.###"));
    sb.AppendLine("ID on the hub       " + Dh.ToString("0.###") + "   (interference " + itf.ToString("0.###") + ")");
    sb.AppendLine("coil OD             " + (Dh + 2.0 * d).ToString("0.###"));
    sb.AppendLine("body length         " + len.ToString("0.###") + "   (+ tangs " + tl.ToString("0.#") + " each)");
    sb.AppendLine("--");
    sb.AppendLine("TANG POSITIONS  (z measured from the base plane)");
    sb.AppendLine("  anchor  (start)   z " + zOff.ToString("0.###") +
                  ",  angle " + (((phi0 % 360.0) + 360.0) % 360.0).ToString("0.#") + " deg  -> to the housing");
    double angC = ((phi0 + 360.0 * (nc - Math.Floor(nc))) % 360.0 + 360.0) % 360.0;
    sb.AppendLine("  control (end)     z " + (zOff + len).ToString("0.###") +
                  ",  angle " + angC.ToString("0.#") + " deg  -> to the release collar");
    sb.AppendLine("  SET ON THE COLLAR:  tangAng = " + angC.ToString("0.#") +
                  ",  slotZ = (this z) - (collar zOff)");
    {
      Point3d tipA = rail.PointAtStart, tipC = rail.PointAtEnd;
      sb.AppendLine("  anchor tip  (" + tipA.X.ToString("0.##") + ", " + tipA.Y.ToString("0.##") +
                    ", " + tipA.Z.ToString("0.##") + ")   r " +
                    Math.Sqrt(tipA.X * tipA.X + tipA.Y * tipA.Y).ToString("0.##"));
      sb.AppendLine("  control tip (" + tipC.X.ToString("0.##") + ", " + tipC.Y.ToString("0.##") +
                    ", " + tipC.Z.ToString("0.##") + ")   r " +
                    Math.Sqrt(tipC.X * tipC.X + tipC.Y * tipC.Y).ToString("0.##"));
      sb.AppendLine("  SET ON THE HOUSING: anchor slot at the anchor tip above");
    }
    sb.AppendLine("  keep the collar clear of z " + zOff.ToString("0.###") +
                  " or it will trap the anchor tang");
    sb.AppendLine("--");
    sb.AppendLine("holding torque req  " + Treq.ToString("0.###") + " N.m");
    sb.AppendLine("capacity            " + cap.ToString("0.#") + " N.m   (mu = " + mu.ToString("0.##") + ")");
    sb.AppendLine("initial tension     " + T0.ToString("0.#") + " N");
    sb.AppendLine("peak wire tension   " + Tpk.ToString("0.#") + " N");
    sb.AppendLine("wire stress         " + sTot.ToString("0") + " N/mm2   (tension " +
                  sTen.ToString("0") + " + bending " + sBnd.ToString("0") + ")");
    sb.AppendLine("hub pressure        " + pres.ToString("0.#") + " N/mm2");
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string t in log) sb.AppendLine("! " + t); }

    Spring = spring;
    Rail   = rail;
    Info   = sb.ToString();
