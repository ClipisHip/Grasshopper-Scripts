// ===========================================================================
//  SCREW OPTIONS  -  Grasshopper C# component (RhinoCommon)
//  Every string ScrewGenerator and ThreadTap will accept, handed back as LISTS
//  so you can drop a List Item on them and pick instead of guess.  Also turns a
//  material name into the numbers that go with it.
//  ---------------------------------------------------------------------
//  INPUTS (Item access, ALL optional - leave the component bare and it still
//          emits every list):
//     Preset  string  a material / process name.  Sets Tol, Fit, Chamfer, Opts.
//                     Wire Presets -> List Item -> here.
//     Filter  string  trims the Sizes list: "metric" | "inch" | "coarse"
//                     | "common" | "" (everything)
//  OUTPUTS (set these to LIST access where noted):
//     Sizes     list    every Size string, filtered          <- List
//     Heads     list    every Head string                    <- List
//     Fits      list    every Fit string                     <- List
//     Recesses  list    every Recess string                  <- List
//     Presets   list    every Preset name                    <- List
//     Tol       double  the preset's clearance, in model units
//     Fit       string  the preset's clearance class
//     Chamfer   double  the preset's mouth chamfer IN PITCHES, for ThreadTap.
//                       No size needed - ThreadTap does the multiply itself.
//     Opts      string  the preset's Opts string, for ScrewGenerator
//     Info      string  cheat sheet + what the chosen preset means and why
// ===========================================================================


// ===========================================================================
//  HOW TO WIRE IT - this is the whole point of the component
//
//     ScrewOptions.Sizes    -> List Item -> ScrewGenerator.Size
//     ScrewOptions.Heads    -> List Item -> ScrewGenerator.Head
//     ScrewOptions.Recesses -> List Item -> ScrewGenerator.Recess
//     ScrewOptions.Presets  -> List Item -> ScrewOptions.Preset
//     ScrewOptions.Tol   -> ScrewGenerator.Tol
//     ScrewOptions.Fit   -> ScrewGenerator.Fit
//     ScrewOptions.Opts  -> ScrewGenerator.Opts
//     ScrewGenerator.Spec   -> ThreadTap.Spec    <- carries size, hand, tol, seg
//     ScrewOptions.Chamfer  -> ThreadTap.Chamfer
//
//     A number slider into each List Item's i input and you can walk the whole
//     catalogue without typing a word.  Leave ThreadTap.Tol at 0: it inherits.
//
//  RIGHT-CLICK EACH LIST OUTPUT and set it to List access, or Grasshopper hands
//  you only the first item.
// ===========================================================================

// ---------------------------------------------------------------------------
//  paste into RunScript( ... )
//    private void RunScript(string Preset, string Filter,
//                           ref object Sizes, ref object Heads, ref object Fits,
//                           ref object Recesses, ref object Presets,
//                           ref object Tol, ref object Fit, ref object Chamfer,
//                           ref object Opts, ref object Info)
// ---------------------------------------------------------------------------

    System.Globalization.CultureInfo IC = System.Globalization.CultureInfo.InvariantCulture;
    List<string> log = new List<string>();

    // one millimetre, in document units - the preset clearances are metric
    double mm = 1.0;
    if (Rhino.RhinoDoc.ActiveDoc != null)
    {
      UnitSystem doc = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem;
      if (doc != UnitSystem.None && doc != UnitSystem.Unset)
        mm = RhinoMath.UnitScale(UnitSystem.Millimeters, doc);
    }
    if (mm <= 0.0 || double.IsNaN(mm)) mm = 1.0;

    // =======================================================================
    //  SIZES.  Coarse / UNC first for each diameter, then the fine pitches.
    //  "*" marks the ones most people actually keep in a drawer.
    // =======================================================================
    string[] SZ = new string[]{
    //  name       pitch     fam  common  series (c = coarse/UNC, f = fine/UNF)
      "M1.6       0.35      m  0  c",
      "M2         0.40      m  0  c",
      "M2.5       0.45      m  0  c",
      "M3         0.50      m  1  c",
      "M3x0.35    0.35      m  0  f",
      "M3.5       0.60      m  0  c",
      "M4         0.70      m  1  c",
      "M4x0.5     0.50      m  0  f",
      "M5         0.80      m  1  c",
      "M5x0.5     0.50      m  0  f",
      "M6         1.00      m  1  c",
      "M6x0.75    0.75      m  0  f",
      "M8         1.25      m  1  c",
      "M8x1       1.00      m  0  f",
      "M10        1.50      m  1  c",
      "M10x1.25   1.25      m  0  f",
      "M10x1      1.00      m  0  f",
      "M12        1.75      m  1  c",
      "M12x1.5    1.50      m  0  f",
      "M12x1.25   1.25      m  0  f",
      "M14        2.00      m  0  c",
      "M14x1.5    1.50      m  0  f",
      "M16        2.00      m  1  c",
      "M16x1.5    1.50      m  0  f",
      "M20        2.50      m  0  c",
      "M20x1.5    1.50      m  0  f",
      "M24        3.00      m  0  c",
      "M24x2      2.00      m  0  f",
      "#0-80      0.012500  i  0  f",
      "#1-64      0.015625  i  0  c",
      "#1-72      0.013889  i  0  f",
      "#2-56      0.017857  i  0  c",
      "#2-64      0.015625  i  0  f",
      "#3-48      0.020833  i  0  c",
      "#3-56      0.017857  i  0  f",
      "#4-40      0.025000  i  1  c",
      "#4-48      0.020833  i  0  f",
      "#5-40      0.025000  i  0  c",
      "#5-44      0.022727  i  0  f",
      "#6-32      0.031250  i  1  c",
      "#6-40      0.025000  i  0  f",
      "#8-32      0.031250  i  1  c",
      "#8-36      0.027778  i  0  f",
      "#10-24     0.041667  i  1  c",
      "#10-32     0.031250  i  1  f",
      "#12-24     0.041667  i  0  c",
      "#12-28     0.035714  i  0  f",
      "1/4-20     0.050000  i  1  c",
      "1/4-28     0.035714  i  1  f",
      "5/16-18    0.055556  i  1  c",
      "5/16-24    0.041667  i  0  f",
      "3/8-16     0.062500  i  1  c",
      "3/8-24     0.041667  i  0  f",
      "7/16-14    0.071429  i  0  c",
      "7/16-20    0.050000  i  0  f",
      "1/2-13     0.076923  i  1  c",
      "1/2-20     0.050000  i  0  f",
      "5/8-11     0.090909  i  0  c",
      "5/8-18     0.055556  i  0  f",
      "3/4-10     0.100000  i  0  c",
      "3/4-16     0.062500  i  0  f",
      "1-8        0.125000  i  0  c",
      "1-12       0.083333  i  0  f"
    };

    string[] HD = new string[]{
      "socket", "button", "flat", "hex", "pan", "none"
    };
    string[] FT = new string[]{ "close", "normal", "loose" };
    string[] RC = new string[]{ "auto", "none", "bore", "sink" };

    // =======================================================================
    //  PRESETS.  tol is a RADIAL clearance in millimetres; lead is a multiple
    //  of the pitch; seg is thread samples per turn.  These are starting points
    //  that print, not guarantees - see RECOMMENDATIONS.md for the reasoning
    //  and for the one test you should run before committing.
    //     name          tol    fit      lead  seg  note
    // =======================================================================
    string[] PR = new string[]{
      "machined       0.00   close    0.5    96   Cut with a real tap and die. Nominal geometry, no allowance.",
      "tap-it         0.00   close    0.5    96   Print or mill the Drill output, then run a real tap in. Strongest option in plastic.",
      "resin          0.08   close    0.5   128   SLA/DLP standard resin. Fine features hold; wash and cure fully before testing fit.",
      "resin-tough    0.12   normal   0.5   128   Tough/engineering resin. More green creep, so a little more room.",
      "pla            0.20   normal   1.0    96   The FDM baseline. Stiff and brittle - threads shear rather than yield, so give length.",
      "petg           0.25   normal   1.0    96   Stringing and ooze fill thread roots. More room than PLA, always.",
      "abs            0.22   normal   1.0    96   Add shrinkage on anything over 60 mm; go to 0.30 on a big part.",
      "asa            0.22   normal   1.0    96   As ABS. Warps less, same clearance.",
      "pc             0.22   normal   1.0    96   Polycarbonate. Tough threads, but it needs a hot chamber to bond layers.",
      "nylon          0.30   loose    1.0    96   Swells with moisture and is very forgiving. Dry it or the fit drifts.",
      "sls-mjf        0.25   normal   1.0    96   Powder packs into thread roots. Bead blast, then test.",
      "first-try      0.35   loose    1.0    96   Untuned printer or unknown filament. Sloppy but it WILL assemble.",
      "render         0.00   close    1.0   240   Presentation only. Do not print this - zero clearance, heavy mesh."
    };

    // =======================================================================
    //  BUILD THE LISTS
    // =======================================================================
    string flt = (Filter == null) ? "" : Filter.Trim().ToLowerInvariant();
    bool onlyM = flt.StartsWith("m");
    bool onlyI = flt.StartsWith("i");
    bool onlyC = flt.StartsWith("coa");     // coarse / UNC only
    bool onlyK = flt.StartsWith("com");     // the everyday ones only

    List<string> sizes = new List<string>();
    for (int i = 0; i < SZ.Length; i++)
    {
      string[] f = SZ[i].Split(new char[]{' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
      if (onlyM && f[2] != "m") continue;
      if (onlyI && f[2] == "m") continue;
      if (onlyK && f[3] != "1") continue;
      if (onlyC && f[4] != "c") continue;
      sizes.Add(f[0]);
    }
    if (sizes.Count == 0) { sizes.Add("M5"); log.Add("Filter \"" + Filter + "\" matched nothing - showing M5."); }

    List<string> heads = new List<string>(HD);
    List<string> fits  = new List<string>(FT);
    List<string> recs  = new List<string>(RC);

    List<string> presets = new List<string>();
    for (int i = 0; i < PR.Length; i++)
      presets.Add(PR[i].Split(new char[]{' ', '\t'}, StringSplitOptions.RemoveEmptyEntries)[0]);

    // =======================================================================
    //  RESOLVE THE CHOSEN PRESET
    // =======================================================================
    double oTol = 0.0;
    string oFit = "normal", oOpts = "", pName = "", pNote = "";
    int    oSeg = 96;
    double chamP = 0.0;                             // mouth chamfer, in PITCHES

    string pw = (Preset == null) ? "" : Preset.Trim().ToLowerInvariant();
    if (pw.Length > 0)
    {
      int hit = -1;
      for (int i = 0; i < PR.Length; i++)
      {
        string[] f = PR[i].Split(new char[]{' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
        if (f[0] == pw) { hit = i; break; }
      }
      if (hit < 0)
        for (int i = 0; i < PR.Length; i++)
        {
          string[] f = PR[i].Split(new char[]{' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
          if (f[0].StartsWith(pw)) { hit = i; break; }
        }

      if (hit < 0) log.Add("Preset \"" + Preset + "\" is not one of the " + PR.Length + " names - defaults used.");
      else
      {
        string[] f = PR[hit].Split(new char[]{' ', '\t'}, 6, StringSplitOptions.RemoveEmptyEntries);
        pName = f[0];
        oTol  = double.Parse(f[1], IC) * mm;
        oFit  = f[2];
        chamP = double.Parse(f[3], IC);
        oSeg  = (int)double.Parse(f[4], IC);
        pNote = (f.Length > 5) ? f[5].Trim() : "";
        oOpts = (oSeg == 96) ? "" : "seg=" + oSeg;
      }
    }


    // =======================================================================
    //  CHEAT SHEET
    // =======================================================================
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.AppendLine("SIZES      " + sizes.Count + " of " + SZ.Length +
                  (flt.Length > 0 ? "   filter \"" + flt + "\"" : "   (no filter)"));
    sb.AppendLine("           filters: metric | inch | coarse | common | <empty>");
    sb.AppendLine();
    sb.AppendLine("HEAD       socket   ISO 4762 / DIN 912 / B18.3 cap screw, hex socket");
    sb.AppendLine("           button   ISO 7380-1 dome, hex socket");
    sb.AppendLine("           flat     countersunk. 90 deg metric, 82 deg inch. Sits flush.");
    sb.AppendLine("           hex      ISO 4017 / B18.2.1 external hex, spanner drive");
    sb.AppendLine("           pan      ISO 7045 dome on a land, straight slot");
    sb.AppendLine("           none     threaded rod / stud / grub screw");
    sb.AppendLine();
    sb.AppendLine("FIT        close    ISO 273 fine.    M5 -> 5.3   dowel-located joints");
    sb.AppendLine("           normal   ISO 273 medium.  M5 -> 5.5   the default");
    sb.AppendLine("           loose    ISO 273 coarse.  M5 -> 5.8   sloppy frames, printed parts");
    sb.AppendLine();
    sb.AppendLine("RECESS     auto     sink for flat heads, none for the rest");
    sb.AppendLine("           none     head sits proud of the face");
    sb.AppendLine("           bore     counterbore: head sunk in a flat-bottomed pocket");
    sb.AppendLine("           sink     countersink cone, head finishes flush");
    sb.AppendLine();
    sb.AppendLine("OPTS       ScrewGenerator only, key=value, space separated, all optional");
    sb.AppendLine("           sink=<n>  extra recess depth below the face");
    sb.AppendLine("           prep=<n>  how far Prep reaches down");
    sb.AppendLine("           seg=<n>   thread samples per turn (96 default)");
    sb.AppendLine();
    sb.AppendLine("CHAMFER    ThreadTap.Chamfer is IN PITCHES, so one number fits every size:");
    sb.AppendLine("           0.5 cutting metal, 1.0 printing, 0 none. ThreadTap does the");
    sb.AppendLine("           multiply, so there is no table to look anything up in.");
    sb.AppendLine();
    sb.AppendLine("SPEC       ScrewGenerator -> ThreadTap. Carries size, hand, tol and seg,");
    sb.AppendLine("           so ThreadTap needs no Size, Class, Threads or Segments of its own.");
    sb.AppendLine("           Leave ThreadTap.Tol at 0 and it uses the tol Spec brought.");
    sb.AppendLine("--");

    if (pName.Length > 0)
    {
      sb.AppendLine("PRESET  " + pName);
      sb.AppendLine("   Tol   " + oTol.ToString("0.###") + "   radial, both scripts");
      sb.AppendLine("   Fit   " + oFit);
      sb.AppendLine("   Cham  " + chamP.ToString("0.##") + " x pitch" +
                    (chamP >= 0.9 ? "   printed: the first thread has nothing under it, so lose it"
                                  : "   cut: the machinist's chamfer, opens the mouth to D + P"));
      sb.AppendLine("   Opts  " + (oOpts.Length > 0 ? oOpts : "(none needed)"));
      sb.AppendLine("   " + pNote);
      if (oTol <= 0.0)
        sb.AppendLine("   Zero clearance. Right for cutting metal, WILL NOT assemble if printed.");
    }
    else
    {
      sb.AppendLine("PRESET  none chosen. Wire Presets -> List Item -> Preset.");
      sb.AppendLine("   " + string.Join(", ", presets.ToArray()));
    }
    if (log.Count > 0) { sb.AppendLine("--"); foreach (string s2 in log) sb.AppendLine("! " + s2); }

    Sizes    = sizes;
    Heads    = heads;
    Fits     = fits;
    Recesses = recs;
    Presets  = presets;
    Tol      = oTol;
    Fit      = oFit;
    Chamfer  = chamP;
    Opts     = oOpts;
    Info     = sb.ToString();
