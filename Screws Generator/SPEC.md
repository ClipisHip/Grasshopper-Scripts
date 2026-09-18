# SCREWS GENERATOR — SPEC

Three paste-in Grasshopper C# components that share one placement convention and
one thread profile, so their output mates.

    ScrewGenerator.cs   the screw  +  the hole prep for the part it passes through
    ThreadTap.cs        the internal thread for the part it screws INTO
    ScrewOptions.cs     every valid string as a LIST, plus material presets

    OPTIONS.md          every option written out, with the dimension charts
    RECOMMENDATIONS.md  what to set for PLA, resin, aluminium, and why

Wire ONE plane into both `Pln` inputs and the screw lands in its own hole, head
bearing on the face. Wire `ScrewGenerator.Spec` into `ThreadTap.Spec` and the
nut side inherits the size, the hand, the tolerance and the segment count. That
is the whole workflow.

    ScrewOptions.Sizes    -> List Item -> ScrewGenerator.Size
    ScrewOptions.Heads    -> List Item -> ScrewGenerator.Head
    ScrewOptions.Recesses -> List Item -> ScrewGenerator.Recess
    ScrewOptions.Presets  -> List Item -> ScrewOptions.Preset
    ScrewOptions.Tol / .Fit / .Opts    -> ScrewGenerator
    ScrewGenerator.Spec                -> ThreadTap.Spec
    ScrewOptions.Chamfer               -> ThreadTap.Chamfer

A slider into each List Item and you can walk the catalogue without typing.
Right-click the list outputs on ScrewOptions and set them to List access.

--------------------------------------------------------------------------------
## 0. THE FRAME, BECAUSE EVERYTHING ELSE DEPENDS ON IT

    Pln.Origin is the SEATING FACE — the underside of the head, and the mouth of
    the tapped hole. Both scripts drive along -Pln.ZAxis.

              +Z   [][]  head                 <- k tall, ABOVE the plane
          ----------------------  Pln         <- z = 0
              -Z   |WWW|  shank               <- Length, BELOW the plane
                   |WWW|
                    \ /   45 deg point

    Prep is built in the same frame. The clearance bore runs DOWN from the plane.
    A counterbore or countersink runs UP from it — that is the material you take
    off to seat the head, so for those two settings part of Prep pokes above the
    plane. That is correct, not a bug.

    Countersunk screws follow the trade convention: `Length` is the TOTAL length,
    head included. Every other head measures under the head.

--------------------------------------------------------------------------------
## 1. SCREW GENERATOR                                      (ScrewGenerator.cs)

    INPUTS      Size        string   "M5"  "M8x1"  "M10x1.25"  "1/4-20"  "#8-32"  "3/8"
                Head        string   socket | button | flat | hex | pan | none
                Length      double   under-head length (total, for flat)
                ThreadLen   double   0 = fully threaded
                Threads     int      0 plain shank · 1 right hand · 2 left hand
                Fit         string   close | normal | loose      (Prep bore)
                Tol         double   extra radial clearance on Prep
                Recess      string   none | bore | sink | auto
                Opts        string   key=value, space separated, all optional:
                                     sink=<n>  extra recess depth, 0 = flush
                                     prep=<n>  Prep reach, default shank + 2 mm
                                     seg=<n>   samples/turn, default 96
                Pln         Plane
    OUTPUTS     Screw       Brep     solid fastener
                Prep        Brep     Boolean-DIFFERENCE this out of the part
                Spec        string   size + hand + tol + seg — wire it to ThreadTap
                Info        string   report + warnings

    HEADS       socket   ISO 4762 / DIN 912 / ASME B18.3 cap screw, hex socket
                button   ISO 7380-1, hex socket
                flat     countersunk. 90 deg metric (ISO 10642), 82 deg inch (B18.3)
                hex      ISO 4017 / DIN 933 / ASME B18.2.1, external hex
                pan      ISO 7045 / DIN 7985, straight slot
                none     threaded rod, stud, grub screw

    RECESS      auto  ->  sink for flat heads, none for everything else. Socket
                and button heads are normally meant to sit proud, so "auto" does
                NOT counterbore them — ask for "bore" when you want them sunk.

--------------------------------------------------------------------------------
## 2. THREAD TAP                                                (ThreadTap.cs)

    INPUTS      Spec        string   ScrewGenerator's Spec output, or a bare size.
                                     Carries size, hand, tol and seg, so this
                                     component needs none of them of its own.
                                     Full form: M5 hand=R tol=0.20 seg=96 class=6H
                Length      double   depth of thread
                OuterDia    double   outside of the Nut boss, 0 = auto (2 d)
                Tol         double   clearance OVERRIDE. Leave at 0 to use what
                                     Spec brought; set a number to overrule it.
                Chamfer     double   mouth funnel, IN PITCHES not mm. 0.5 to cut
                                     metal, 1 to print, 0 for none. ThreadTap
                                     multiplies by its own pitch, so one number
                                     suits every size.
                Pln         Plane
    OUTPUTS     Cutter      Brep     SUBTRACT from your part -> tapped hole
                Nut         Brep     cylindrical boss with the hole already in it
                Drill       Brep     tap-drill hole: cylinder + 118 deg point
                Info        string

    SIX INPUTS, NOT NINE. Size, Class, Threads and Segments all used to be here
    and all four now ride in on `Spec`. Typing a bare "M5" still works — the rest
    fall back to right hand, 6H, 96 segments and whatever `Tol` you set.

    CLASS DOES LESS THAN YOU THINK. A solid is one size, not a tolerance band, so
    only the fundamental deviation — where the band STARTS — can show up in the
    geometry. ISO 965-1 gives position H an EI of 0, so 6H, 7H and "nominal" are
    the same solid; the grade number only says how much bigger the hole may get.
    Position G is EI = +(15 + 11P) micrometres on diameter. Unified inch classes
    1B / 2B / 3B all sit on EI = 0, so on the inch side Class does nothing at all.

    WHICH MEANS TOL IS THE FIT. It opens every thread radius, so the hole grows
    by 2 x Tol on diameter.

            machining, real tap        0
            resin / SLA                0.05 - 0.10
            FDM, tuned                 0.15 - 0.20
            FDM, first try             0.25

    At Tol 0 and class H you get the BASIC profile — which is a zero-clearance
    fit against a nominal screw. Right for a Boolean, will not print.

--------------------------------------------------------------------------------
## 3. THE THREAD

    Both scripts draw the same zig-zag. It is the ISO 68-1 / ASME B1.1 BASIC
    profile, and it is genuinely one line: the external thread keeps the material
    inside it, the internal thread keeps it outside.

        H  = 0.86603 P            height of the fundamental triangle
        major   d                 crest of the screw / root of the nut, flat P/8
        minor   d - 1.0825 P      root of the screw / crest of the nut, flat P/4
        pitch   d - 0.6495 P
        flanks  60 deg included, so 30 deg off the axis

    The SCREW is drawn at basic nominal, no allowance. It is the reference part.
    All clearance lives on the other side: Fit + Tol in Prep, and Tol in
    ThreadTap. A real 6g screw is about 0.02 mm under basic — below any printer's
    noise floor, and if you care, add it to Tol on the mating side.

    HOW IT IS BUILT, AND WHY THERE IS NO BOOLEAN IN IT

    Cut a screw with a plane perpendicular to its axis and the section is a
    closed curve — the profile wrapped once around, because one turn is exactly
    one pitch and the profile repeats every pitch. Nothing jumps. So the whole
    threaded shank is that one section swept up a helix, and every point of the
    section traces a CONSTANT-RADIUS HELIX as it goes.

    That is what the code builds: one `NurbsCurve.CreateSpiral` per sample of the
    profile, then a ruled surface between each neighbouring pair. The rails are
    exact helices, so there is no approximation along the length at all — length
    costs nothing. The only error is faceting the profile, and at 96 samples that
    is 0.0013 mm on an M5 crest.

    It also means the tapers come free. A rail can start at one radius and finish
    at another, so the point chamfer, the thread run-out under the head and the
    lead-in on a tapped hole are just zones with different end radii. Zones share
    their boundary curves exactly, so the skin JOINS.

    `seg` (in Opts, default 96) is snapped to a multiple of 16 so the five breakpoints
    (1/16, 3/8, 5/8, 15/16 of a pitch) land on samples instead of being rounded
    off. 96 is plenty. 32 is a draft. 192 is for renders.

    The ONE Boolean in either script is the hex socket or slot in the head, and
    it only ever touches the head.

    Handedness is measured, not assumed: both scripts build a probe spiral, work
    out which way RhinoCommon wound it, and mirror at the end if that is not what
    you asked for.

--------------------------------------------------------------------------------
## 4. THE DATABASE

    Metric M1.6 - M24, inch #0 - 1". Any fine pitch works by writing it out —
    "M10x1.25", "1/4-28" — the head and clearance data comes from the nominal
    size and only the pitch changes.

    WRITING THE SIZE. Leading "M" and a digit means metric; anything else is
    read as inch. Case and a trailing UNC / UNF are ignored.

        M5  M2.5  M24            coarse pitch from the table
        M8x1  M10x1.25           any fine pitch, written out
        1/4-20  3/8-24  1/2-13   fraction and tpi
        #8-32  8-32  #10-24      gauge number, with or without the #
        1/4  3/8                 no tpi given -> UNC
        0.25-20                  a decimal diameter works too

    One trap, handled: "1-8" is the ONE INCH bolt, but "1-64" is the #1 gauge
    screw, and neither writes a "#". The tpi settles it — #1 comes in 64 and 72,
    and nothing an inch across is finer than 20 — so anything under 40 tpi on a
    bare "1" is read as one inch. Write "#1" if you want the gauge size.

    metric      thread          ISO 261 / ISO 262 coarse series, ISO 724 derived
                clearance       ISO 273 fine / medium / coarse
                socket cap      ISO 4762 / DIN 912
                button          ISO 7380-1
                countersunk     ISO 10642 / DIN 7991, 90 deg included
                hex             ISO 4017 / DIN 933
                pan             ISO 7045 / DIN 7985

    inch        thread          ASME B1.1 UNC / UNF
                clearance       standard close / free / loose drill chart
                socket cap      ASME B18.3 (1960 series)
                countersunk     ASME B18.3, 82 deg included
                hex             ASME B18.2.1 above 1/4, hex machine screw below

    EXTRAPOLATED, and the script says so in Info when you land on one:
        button head above M16, and every inch button
        pan head above M10, and every inch pan
    There is no button or pan standard with the reach of the metric tables, so
    those rows carry the same ratios as the sizes that ARE tabulated. Everything
    else is off a published table.

    THE COUNTERSUNK HEAD IS MODELLED SHALLOWER THAN THE STANDARD, on purpose.
    ISO 10642 lists M5 as dk 10, k 2.8; a pure 90 deg cone from dk 10 down to the
    5 mm shank is only 2.5 tall. The standard's extra 0.3 is the blend at the
    bottom of the cone, which a cone cannot have. Head DIAMETER and head ANGLE
    are what decide where a screw seats, and both are exact — so a real ISO 10642
    screw sits flush in the countersink this makes. Check the Info report for the
    k it actually used.

--------------------------------------------------------------------------------
## 5. TWO WORKED SETUPS

    M5 SOCKET CAP through a 6 mm plate into a printed boss

        ScrewOptions     Preset pla   -> Tol 0.2, Fit normal, Chamfer 1
        ScrewGenerator   Size M5   Head socket   Length 16   ThreadLen 0
                         Threads 1   Fit <- normal   Tol <- 0.2
                         Recess none   Opts "prep=8"   Pln <- face plane
            -> Prep is a 5.9 dia bore. Subtract from the plate.
        ThreadTap        Spec <- ScrewGenerator.Spec      ("M5 hand=R tol=0.2 seg=96")
                         Length 10   OuterDia 10   Tol 0   Chamfer <- 1
                         Pln <- same plane
            -> Tol 0 means it inherits the 0.2. Subtract Cutter from the boss:
               5.9 shank clearance, 0.4 on the thread diameter, one pitch of chamfer.

    1/4-20 FLAT HEAD, flush in a plate, into tapped aluminium

        ScrewGenerator   Size 1/4-20   Head flat   Length 0.75  (TOTAL)
                         Threads 1   Fit close   Tol 0   Recess auto   Opts ""
            -> "auto" gives the 82 deg countersink. Head finishes flush.
        ThreadTap        Spec <- ScrewGenerator.Spec   Length 0.5
                         Tol 0   Chamfer 0.5
            -> Drill output is the 0.201 hole to make before the tap goes in.
               Info reports the engagement it leaves.

--------------------------------------------------------------------------------
## 6. IF SOMETHING GOES WRONG

    "not watertight"        raise seg= in Opts, or loosen the model tolerance.
                            Rhino's default 0.001 mm is tight for a 96-sided
                            helical skin on a long screw.
    it crawls               turns x seg is the cost. A 40 mm M3 at seg=192 is
                            26 000 samples per rail set. Use seg=48 while you
                            lay the model out, raise it to bake.
    screw will not go in    Tol on the ThreadTap side, not the screw side.
    head sinks too far      Recess, and sink= in Opts. Not Length.
    thread winds backwards  Threads 2. The script measures its own handedness,
                            so 1 is right hand on every Rhino build.
