# MOP GRIPPER — MASTER SPEC
Verified configuration. Every number below has been checked against the others.

    Overall height   40 mm          Footprint   27.4 mm (the gear)
    Jaw travel       15 mm          Grip force  160 N total, 80 N per rack
    Push stroke      16 mm          Push force  150 N assumed

Wire the RACK script's `GearPlane` output into the `BasePlane` / `Pln` input of
every other component. Everything then lands coaxial and correctly stacked.

--------------------------------------------------------------------------------
## 1. GEAR                                   (InvoluteGearProfile / GearBlock_paste)

    INPUTS      Module        1.5
                Teeth         16
                Pressure      20
                Backlash      3
                Pln           <- rack GearPlane   (type hint: Plane)
    CONSTANTS   xs            0.12      <- profile shift, avoids undercut at z16
                ha            1.00
                hf            1.25
                rc            0.38
                ns            48
                faceW         6        <- extrusion thickness
                bore          6.4      <- MUST match the cam and hub
    OUTPUT      Solid is already extruded AND bored - union it with the cam
                and hub to make the rotor. Gear is still the raw curve.

    NOTE        GearBlock_paste.cs is the SHORT variant: only Module, Teeth,
                Pressure, Backlash and Pln are wired; Shift, Addendum,
                Dedendum, RootFillet and Samples are constants in the source.
                InvoluteGearProfile.cs exposes all ten as inputs instead -
                same maths, longer parameter list.

    RESULT      pitch 24.00   tip 27.36   root 20.61   form (TIF) ~21.6
                undercut limit 15.10 teeth -> z16 clear
                tooth stress 30 MPa Lewis, 45 MPa with the fillet factor (POM ~65)

--------------------------------------------------------------------------------
## 2. RACK  x2, opposed                              (InvoluteRackProfile)

    INPUTS      Module        1.5
                Pressure      20
                Backlash      0        <- all 3% backlash is on the gear
                RackTeeth     12
                GearTeeth     16
    CONSTANTS   xs            0.12     <- MUST match the gear
                ha            1.00
                hf            1.25
                rc            0.38
                tr            0.00
                bh            0.0
                pl            WorldXY

    RESULT      linear pitch 4.7124   gear axis at (0, 12.18) from the datum line
                two racks halve the tooth load and cancel the bearing side force

--------------------------------------------------------------------------------
## 3. CAM                                          (HelicalCamCylinder)

    INPUTS      Diameter      18
                Height        24
                Stroke        0        <- 0 = groove spans the full run
                GrooveWidth   4
                GrooveDepth   2
                RackTeeth     3.183    <- = 15 mm / (pi x 1.5)
                GearTeeth     16
                GearPlane     <- rack GearPlane
    CONSTANTS   mod           1.5
                zOff          6        <- sits on the gear
                margin        4        <- groove inset, so the sleeve clears the gear
                turnsO        0
                starts        2        <- MUST equal PinCount
                bore          6.4      <- 0.4 running clearance on the 6.0 shaft
                phi0          90       <- MUST equal the sleeve's phi0
                hand          +1       <- flip if it locks on the push
                spt           64
                over          0
                botR          0        <- 0 = full U
                pinClr        0.2
                endOverTop    0        <- pipe cutter has round ends, none needed
                endOverBot    0        <- same

    RESULT      body z 6..30, groove z 10..26, blind with hemispherical ends
                turns 0.1989 (71.6 deg)   lead 80.4   lead angle 54.9 deg
                land between passes 19.1   core radius 7.0 -> max bore 9.0

--------------------------------------------------------------------------------
## 4. FOLLOWER SLEEVE - oval                       (CamFollowerSleeve)

    INPUTS      CamDiameter   18
                Clearance     0.25
                WaistDia      26       <- short axis
                Length        50       <- long axis
                Height        8
                GrooveWidth   4
                GrooveDepth   2
                PinCount      2
                BasePlane     <- rack GearPlane
    CONSTANTS   pinClr        0.2
                starts        2        <- MUST equal the cam's starts
                lead          80.4     <- from the cam report
                pinZ          4
                hand          +1       <- match the cam
                phi0          90       <- pins on the WAIST axis; cam phi0 must match
                zOff          6 = closed (pushed down)
                              22 = open (up).  Slider this to animate.
                pinsSeparate  true for the real part (see ASSEMBLY ORDER)
                pockR/Dia/Dep 18 / 6.6 / 6      <- spring pockets, in the lobes
                keyN/W/Dep    2 / 6 / 2         <- key grooves, at the lobe tips

    RESULT      oval 50 x 26 x 8, bore 18.5, waist wall 3.75
                pin dia 3.8, ball tip 1.9, ball centre radius 9.0
                ball-to-groove-bottom gap 0.10
                130 N per pin: bearing 17.2 MPa, bending 27 MPa

    WHY OVAL    A spring rising from the base cannot reach a ROUND sleeve:
                anything clearing the gear (radius 13.68) sits further out
                than a round sleeve is wide (radius 13). Stretching the body
                puts material past the gear, and the pockets go there.

    PINS AND POCKETS MUST NOT SHARE AN ANGLE, or the pin shaft breaks into
    a pocket. Pockets and key grooves on the LONG axis, pins on the WAIST.

--------------------------------------------------------------------------------
## 5. SPRING — PURCHASED PART

    Type          helical TORSION spring, straight tangential legs
    OD            20
    ID            17.0            <- free, undeflected
    Wire          1.5
    Coils         5
    Legs          >= 16 mm both ends
    Material      STAINLESS 302/316   (it is a mop - do not use music wire)
    Hand          either; flip the cam's `hand` to suit

    Verified against the supplier deflection table to 0.01 mm.
    DO NOT use the "safe mandrel diameter" column - that is a clearance figure
    for the opposite use case. The hub must GRIP.

--------------------------------------------------------------------------------
## 6. SPRING MODEL — visual only                   (WrapSpringCoil)

    INPUTS      WireDia       1.5
                HubDia        17.15
                Interference  0.15
                Coils         5
                TangLength    16
                Torque        1.92
                BasePlane     <- rack GearPlane
    CONSTANTS   E 200000   sAll 1200   mu 0.30   pHub 20
                gap 0   hand +1
                phi0          90       <- tangs on the WAIST, running along X
                tlA           24       <- ANCHOR tang, reaches the housing slot
                tlC           16       <- CONTROL tang, only clears the collar
                zOff          31

--------------------------------------------------------------------------------
## 7. SPRING HUB — part of the rotor               (WrapSpringHub)

    INPUTS      HubDia        17.15    <- free ID 17.0 + 0.15 interference
                HubLength     10
                BoreDia       6.4      <- 0.4 running clearance on the 6.0 shaft
                FlatDepth     0
                FlangeDia     0        <- the gear is the flange
                FlangeThk     0
                BasePlane     <- rack GearPlane
    CONSTANTS   zOff          30

    RESULT      clutch torque 1.92 N.m, hub pressure 15.1 MPa (limit 20) - OK,
                so a moulded hub is fine, no metal sleeve needed.
                Gripped surface must be smooth: mould/print axis vertical,
                no flats, no seam, no sanding.

--------------------------------------------------------------------------------
## 8. RELEASE COLLAR                               (WrapSpringRelease)

    INPUTS      CoilMeanDia   18.65    <- 17.15 hub + 1.5 wire
                WireDia       1.5
                Clearance     0.4
                WallThk       2        <- 3 clashes with the housing's oval waist
                Height        4        <- SHORT: must not cover the anchor tang
                LeverLength   25
                LeverWidth    8
                BasePlane     <- rack GearPlane
    CONSTANTS   slotFit       0.3
                tangAng       0
                leverAng      90
                tangAng       90       <- MUST equal the coil's phi0
                slotZ         2.5      <- puts the slot at z 38.5 = the control tang
                zOff          36       <- spans 36..40
                openTop       true     <- notch open to the top edge, so the collar
                                          can drop on over the tang

    RESULT      bore 20.95, OD 24.95, min tang reach 11.3 (so 16 mm legs are fine)
                clears the housing bore waist (13.40 radius) by 0.93
                release: 29 deg of swing, 19.4 mm of lever travel, 2.2 N
    NEEDS       axial retention (lip or circlip) + a light return spring so the
                lever parks engaged

--------------------------------------------------------------------------------
## 9. BASE PLATE                                   (BasePlate.cs)

    INPUTS      PlateDia      62       <- must cover the lugs, which reach r29
                PlateThk      2
                ShaftDia      6
                ShaftLen      46       <- must reach the retainer cap
                PinCount      2
                BasePlane     <- rack GearPlane
    CONSTANTS   pinR          18       <- spring pin circle RADIUS
                pinDia        3
                pinLen        8
                scN/scR/scPhi 2 / 24 / 90   <- MUST match the housing's lugs
                scDia         3.4      <- CLEARANCE (housing has the 2.5 pilot)

    RESULT      plate z -2 .. 0; its TOP is the gear plane.
                springs at radius 18 clear the gear (radius 13.68) by 1.32

--------------------------------------------------------------------------------
## 10. HOUSING - oval, with a belly                (Housing.cs)

    INPUTS      BoreWaist     26.8     <- sleeve 26 + 0.4 clearance each side
                BoreLength    50.8     <- sleeve 50 + 0.4
                WallThk       2
                Height        44
                BellyWaist    36       <- widened at gear level to clear the racks
                LeverSlotDeg  60
                RackSlotW     7
                RackSlotH     7
                BasePlane     <- rack GearPlane
    CONSTANTS   bellyHi       7        <- belly spans z 0 .. 7
                lugN/Phi/R    2 / 90 / 24   <- MUST match the plate's screws
                lugW/lugT     9 / 3
                scDia         2.5      <- PILOT (plate has the 3.4 clearance)
                ribN          2
                ribW          5.6      <- sleeve key groove 6.0 - 0.4 play
                ribDep        2.2
                ribZlo/Zhi    2 / 32   <- sleeve travels 6..30, keyed throughout
                ancOn/X/Y/Z/W true / 19 / 9.33 / 31 / 1.9   <- clutch anchor slot
                leverPhi      90
                leverZlo/Zhi  35 / 41
                rackN         2
                rackOff       13.87    <- centre of the rack's radial span
                rackZlo       0

    RESULT      upper  z 7..44   bore 50.8 x 26.8,  outer 54.8 x 30.8
                belly  z 0..7    bore 50.8 x 36.0,  outer 54.8 x 40.0
                The housing sits ON the plate (z 0 up), it no longer starts
                at -2 and interpenetrate it.

                The UPPER shell is extruded over the full 0..44 and the belly
                OVERLAPS it. Extruding them to butt at z 7 leaves a coplanar
                union face, which often fails to fuse and shows up as a
                horizontal seam right at the belly step.
                Both stadiums share the LENGTH, so the lobe tips line up at
                +/-25.4 and one rib section serves both levels.

    ANTI-ROTATION is the two RIBS at the lobe tips, running in the sleeve's
    key grooves from z 2 to 32 - the sleeve is keyed over its whole travel.

    RIB BUILD ORDER MATTERS. They stand proud INTO the bore, so they are
    unioned AFTER the bore is cut. Union them first and the bore cut simply
    swallows them again and the sleeve is left free to spin.

--------------------------------------------------------------------------------
## 11. RETAINER CAP                                (RetainerCap.cs)

    INPUTS      CapDia        26
                CapThk        3
                ShaftDia      6
                Gap           0.3
                BasePlane     <- rack GearPlane
    CONSTANTS   rotorTop      40
                collarOD      24.95
                boreWaist     26.8
                pressFit      0.10     <- bore = ShaftDia - this
                screwDia      0        <- >0 for a screw into the shaft end

    RESULT      cap z 40.3 .. 43.3, pressed on the shaft top
                overhangs the collar by 0.525 all round -> retains it
                clears the housing bore by 0.400
                0.3 gap above both tops, so nothing is clamped

    It retains the ROTOR and the COLLAR at once: both tops sit at z 40, just
    at different radii - hub out to 8.575, collar 10.475..12.475.

    Needs ShaftLen 46 on the base plate, up from 42, to reach it.

    FOR THE RATCHET BUILD this part changes - CapDia 20, rotorTop 38,
    collarOD 22.40, and ShaftLen goes back to 42. See WHAT THE RATCHET BUILD
    CHANGES ELSEWHERE.

================================================================================
#  RATCHET OPTION
#
#  Sections 12-14 replace the wrap spring (sections 5-8 and the RETAINER CAP's
#  collar duty) with a ratchet and pawl. Build ONE or the OTHER, never both.
#  Sections 1-4 and 9 are shared unchanged; 10 and 11 need the small edits
#  listed under WHAT THE RATCHET BUILD CHANGES ELSEWHERE.
#
#  Why: the wrap spring hit two independent blockers, both a thin steel wire
#  against soft plastic - the anchor tang undersized 3.4x, and the hub
#  thread-forming on the bench. See OPEN PROBLEM - THE ANCHOR TANG. The pawl
#  loads a tooth flank in COMPRESSION instead, which printed plastic takes.
#  Cost: adjustment goes from stepless to 1.885 mm steps at the rack.
================================================================================

--------------------------------------------------------------------------------
## 12. RATCHET WHEEL - part of the rotor           (RatchetWheel.cs)

    INPUTS      OuterDia      24       <- 1.4 clear in the 26.8 bore waist
                Teeth         40
                ToothDepth    0.8
                Thickness     8        <- see WHY 8 below
                BoreDia       6.4      <- MUST match the gear and cam
                LockAngle     8        <- undercut; 0 lets the pawl climb out
                RootFillet    0
                BasePlane     <- rack GearPlane
    CONSTANTS   mod           1.5      <- report only
                zGear         16       <- report only
                Treq          1.92     <- holding torque, report only
                hand          +1       <- flip if it locks on the push
                zOff          30       <- sits ON the cam top

    RESULT      wheel z 30..38, tip dia 24, root dia 22.40
                tooth pitch 1.885, step 9.000 deg
                ONE CLICK = 1.885 mm of rack (m1.5 z16)
                pawl contact 168 N at 1.92 N.m
                tooth bending 37.9 MPa on the 6 mm the pawl engages (POM ~65)

    WHY 8 THICK, STARTING AT 30. The pawl swings at z 32..38, so the teeth have
    to be there - but the cam tops out at 30, and a wheel starting at 32 would
    leave the rotor in two disconnected pieces with nothing carrying torque
    across the gap. Spanning 30..38 butts the cam and still presents 6 mm of
    tooth to the 6 mm pawl. The spare 30..32 band earns its keep as the
    sleeve's top end-stop: the sleeve tops out at exactly 30 and the wheel's
    24 dia overhangs its 18.5 bore.

    ONE MOULDED ROTOR = gear + cam + ratchet wheel, single 6.4 bore throughout.
    Union gear.Solid + cam.Body + wheel.Body. No hub, no coil, no collar.

--------------------------------------------------------------------------------
## 13. RATCHET PAWL                                (RatchetPawl.cs)

    INPUTS      RatchetDia    24       <- MUST match the wheel
                Teeth         40       <- MUST match the wheel
                ToothDepth    0.8      <- MUST match the wheel
                PivotDist     16       <- MUST equal the housing's pawlX
                ContactAngle  60
                ArmWidth      5
                Thickness     6
                PivotHoleDia  3.2
                TailLength    12
                BasePlane     <- rack GearPlane
    CONSTANTS   bossDia       0        <- 0 -> PivotHoleDia + 6 = 9.2
                tipFrac       0.75     <- contact radius 11.4
                Treq          1.92
                zOff          32       <- level with the wheel's upper 6 mm

    RESULT      pawl z 32..38, pivot at (16, 0), tip at (5.70, 9.87)
                arm reach 14.267, tip width 1.16 (tooth pitch 1.789 at r11.4)
                SELF-ENGAGING - the tooth load drives the tip further in
                contact force 168 N at 1.92 N.m
                tail tip at radius 26.02, angle -18.6 deg

    PivotDist 16 / ContactAngle 60 ARE NOT FREE. They are the shortest arm that
    passes the self-engagement check: the tooth load has to drive the tip in,
    not lever it out. 0 and 45 deg both FAIL - the Info output says which.
    Sweeping the pair is the only way to find a working combination; do not
    move the pivot without re-reading that line.

    The tail at -18.6 deg is what sets the housing's release gap at -19 deg.
    Pushing the tail lifts the tip out and the rotor spins back under the
    return springs.

--------------------------------------------------------------------------------
## 14. HOUSING - RATCHET VARIANT                   (HousingRatchet.cs)

    Housing.cs is the wrap-spring version and is LEFT ALONE. This is a separate
    file: same oval shell and belly, but carrying the pawl instead of the
    clutch's anchor slot and lever gap.

    INPUTS      BoreWaist     26.8
                BoreLength    50.8
                WallThk       2
                Height        42       <- was 44; no collar to house
                BellyWaist    36
                RelSlotDeg    -19      <- on the pawl tail's line
                RackSlotW     7
                RackSlotH     7
                BasePlane     <- rack GearPlane
    CONSTANTS   bellyHi       7
                lugN/Phi/R    2 / 90 / 24   <- MUST match the plate's screws
                lugW/lugT     9 / 3
                scDia         2.5      <- PILOT (plate has the 3.4 clearance)
                ribN          2
                ribW          5.6
                ribDep        2.2
                ribZlo/Zhi    2 / 30   <- was 2/32; stops short of the webs
                rackN         2
                rackOff       13.87
                rackZlo       0
                pawlX         16       <- MUST equal the pawl's PivotDist
                postDia       6        <- r3 vs the r12 wheel -> 1.0 clear
                pinDia        3.2      <- USE A METAL PIN
                postZlo/Zhi   30 / 40
                pawlZlo/Zhi   31.8 / 38.2   <- 6.4 gap for the 6.0 pawl
                webW          5
                relW          8
                relZlo/Zhi    31 / 39
                zOff          0

    RESULT      upper  z 7..42   bore 50.8 x 26.8,  outer 54.8 x 30.8
                belly  z 0..7    bore 50.8 x 36.0,  outer 54.8 x 40.0
                post dia 6 at (16, 0), z 30..40, clear of the wheel by 1.0
                pawl swings in z 31.8..38.2 - 0.2 clearance each face
                release gap 8 wide at -19 deg, z 31..39, out through the wall

    WHY THE WEB IS SPLIT. The pivot sits at x 16; the bore wall on that line is
    at 25.4, so the post floats 9.4 mm out in mid-air and needs a web to hold
    it. A web BESIDE the pawl would want the same corridor as the release tail,
    which swings through that band - only 1.3 mm left between them. So the web
    is split into one BELOW the pawl (z 30..31.8) and one ABOVE (z 38.2..40).
    The post is then a clevis, the pawl swings between the two, and the whole
    z 32..38 band is left clear for the tail.

    BUILD ORDER MATTERS - ribs, post and webs all stand proud INTO the bore, so
    they are unioned AFTER the bore is cut. Union them first and the bore cut
    swallows them again. The pin hole is cut LAST, through the finished post.

    NO PART OF THIS IS A STOP. The pawl is held in the teeth by a spring that
    is still missing - see the open items below.

--------------------------------------------------------------------------------
## RATCHET LOAD PATH - every number

        ratchet wheel  ->  pawl  ->  pin  ->  post  ->  webs  ->  shell

    Contact force 168 N (= 1.92 N.m / 11.4 mm contact radius). At the pivot
    that resolves into 145.9 N radially OUTWARD, toward the wall, and 84.2 N
    sideways - which is why the webs run radially: the big component is pure
    compression along them.

        wheel tooth bending      37.9 MPa    POM ~65
        pin double shear         10.5 MPa    METAL pin
        pin bearing in the pawl   8.8 MPa    6.0 mm of pawl
        pin bearing in the webs  14.6 MPa    3.6 mm of web, 1.8 each
        web compression           8.1 MPa    145.9 N over 2 x 5.0 x 1.8
        web bending              35.9 MPa    84.2 N, 6.4 span, Z 7.50

    WEB BENDING IS THE TIGHTEST NUMBER IN THE RATCHET - 35.9 against POM's ~65,
    a margin of 1.8. Everything else has 4x or better. If it needs more, widen
    webW before anything else: the stress goes as 1/webW^2.

    A PRINTED 3.2 PIN WOULD SEE 10.5 MPa IN DOUBLE SHEAR, which sounds fine
    until you remember it is printed across the layers. Use a metal pin - a
    3 mm dowel or a cut-down M3 - and drill the post 3.2.

--------------------------------------------------------------------------------
## WHAT THE RATCHET BUILD CHANGES ELSEWHERE

    Shared scripts stay as written for the wrap-spring build. Change these
    three values on the component (or in the source) when building the ratchet:

    part / script          value        wrap spring     ratchet
    BasePlate  ShaftLen    input        46              42
    RetainerCap CapDia     input        26              20
    RetainerCap rotorTop   constant     40              38
    RetainerCap collarOD   constant     24.95           22.40

    ShaftLen 42 - the cap now lands at z 38.3..41.3, so the shaft reaches 42
    and finishes flush with the top of the 42 housing.

    CapDia 20 - there is no collar to retain, only the rotor, whose top is the
    wheel's 22.40 root dia. A 20 cap still covers that rim with room to spare,
    and more to the point it clears the upper web, whose inner edge is at
    x 13. The wrap build's 26 cap (r13) would sit exactly on it.

    collarOD 22.40 - the cap script checks that it overhangs this. Pointed at
    the collar it will warn about a part that is not in this build.

    NOT CHANGED: gear (1), racks (2), cam (3), sleeve (4), base plate (9) apart
    from ShaftLen. The cam stays Height 24 / Stroke 0, body z 6..30 - do NOT
    stretch it to meet the wheel, because the script centres a short stroke in
    the available run and the groove would move up with it.

    DROPPED ENTIRELY: sections 5, 6, 7, 8 - the purchased spring, the coil
    model, the spring hub and the release collar.

--------------------------------------------------------------------------------
## RATCHET OPEN ITEMS

    1. THE PAWL SPRING IS STILL MISSING. Nothing currently holds the tip into
       the teeth, and a pawl resting on nothing does not ratchet. It needs only
       a gentle load - the self-engagement geometry does the rest once a tooth
       is caught. A small torsion spring over the 3.2 pin, one leg on the tail
       and one on the shell, is the obvious answer, but it is not modelled and
       there is no anchor feature for it in HousingRatchet.cs yet.
       NOTHING SHOULD BE PRINTED FOR A FUNCTIONAL TEST UNTIL THIS IS RESOLVED.

    2. RIB / WEB COPLANAR FACE. The rib at angle 0 tops out at z 30 and the
       lower web starts at z 30, overlapping over about 4.2 x 5.0 mm. They are
       unioned in the same call, and a coplanar face pair is exactly what left
       the horizontal seam at the belly step - see section 10. If the union
       fails or seams there, raise ribZhi to 30.5 so the rib runs INTO the web
       instead of butting it. 30.5 is still below pawlZlo 31.8, so the script's
       own check stays happy.

    3. DIRECTION, same as the wrap spring. Pushing down must turn the gear the
       way the pawl RIDES OVER the teeth. 50/50 and invisible in CAD. If it
       locks on the first push, flip `hand` on the cam - or on the wheel, which
       mirrors the teeth instead.

    4. PIN RETENTION. The script cuts ONE 3.2 hole straight through post, webs
       and all, so a 3.0 dowel is +0.2 loose in the webs as well as in the
       pawl - nothing stops it walking out sideways. Either drill the webs
       2.9 for a press fit and leave the pawl at 3.2, or use a pin with a head
       or an E-clip groove. Not a load problem, just a fall-out problem.

    5. RESOLUTION. One click is 1.885 mm at the rack, so the grip lands
       somewhere in a 1.885 mm band rather than exactly where the user stopped
       pushing. Cloth compresses far more than that, so this is expected to be
       invisible in use - but it is the one thing the wrap spring did better,
       and it is the reason to keep section 5-8 on file.

--------------------------------------------------------------------------------
## BENCH TEST FOR THE CLUTCH

    A wrap spring with NO interference has exactly zero capacity:

        capacity = rM x T0 x (e^(mu.theta) - 1),  T0 comes only from the
        interference.  Shaft under the free ID -> T0 = 0 -> capacity = 0.

    It then spins freely BOTH ways and looks broken. It is not - there is
    simply nothing for the exponential to amplify.

    Testing it:
      1. hold the hub, or any shaft LARGER than the spring's free ID
      2. twist the two legs apart ~30 deg, slide the spring on, release
      3. CHECK: it must not pull off by hand. If it slides on, stop - the
         test is invalid and everything after it is meaningless
      4. trap ONE leg against something solid - a vice or a pin, not fingers
      5. turn the hub both ways

    Expect a hard stop one way and free slip the other. WHICH LEG you hold
    decides the direction; swap legs and the locking direction reverses.

    A leg held in fingers can rotate bodily with the hub, which also reads as
    "no locking". Trap it properly.

    Unwinding widens the coil:  twist_deg = 360 x coils x dD / Dm
        1.5 wire, 5 coils, ID 17.0  ->  29 deg for +0.3 mm
        2.5 wire, 3 coils, ID 17.0  ->  17 deg for +0.3 mm

--------------------------------------------------------------------------------
## WHAT THE SPRING ACTUALLY GRIPS

    NOT the shaft. The spring wraps the HUB - the 17.15 boss that is part of
    the rotor. The 6 mm shaft passes through the middle, separated from the
    spring by the hub's 5.375 wall. They never touch.

        shaft 6.0  ->  hub bore 6.4  ->  hub OD 17.15  ->  spring ID 17.0
          fixed          rotor turns                        grips HERE

    While you push, the hub turns the unwinding way under the spring and
    friction drags the coils with it. The anchor tang resists, the control
    tang is free, so the collar and lever drift a little in the RELEASE
    direction. That is harmless and self-correcting - the spring has to
    unwind slightly to slip at all, and it winds straight back the moment the
    load reverses. The collar does not need a parking spring to function; one
    would only give the lever a tidier rest position.

--------------------------------------------------------------------------------
## RACKS - THEY MESH TANGENTIALLY

    This is easy to get wrong. A rack's pitch line is TANGENT to the gear's
    pitch circle, so it sits at a perpendicular offset of

        rp + x*m = 12 + 0.18 = 12.18

    from the gear axis, and slides along its OWN LENGTH. It never passes near
    the axis, so a radial opening is the wrong shape in the wrong place.

    Rack body, measured perpendicular from the axis:

        tip line   10.68   (closest approach to the axis)
        datum      12.18
        root line  14.06
        back face  17.05   (with body depth bh = 3)

    The root line ALONE is past the 13.40 bore waist, so no amount of thinning
    the rack makes it fit the upper section. Hence the belly: 36 wide over
    z 0..7 gives 0.95 of clearance behind the rack.

    WINDOWS are tangential boxes offset +/-13.87 from the axis, 7 x 7, one
    each end - so they sit DIAGONALLY, not diametrically opposed. They are at
    y +/-10.37..17.37 while the ribs are at y +/-2.80, so the two never meet.

    They are LOCAL to the wall crossing, but sized from the EXTREMES of the
    window's own width, not its centre. The wall is curved, so where it
    crosses depends on y:

        y 10.37  wall at x 22.11 .. 24.50
        y 13.87  wall at x 18.87 .. 21.81
        y 17.37  wall at x 12.12 .. 17.31

    Sizing from the centre alone gave x 16.87..23.81 and left solid wall in
    the way at the far edge - the rack would not pass. The window now spans
    x 11.12 .. 26.50 (15.4 mm), penetrating across its whole width.

    They used to run x 0..35, carving a slot back to the axis and through the
    flange, which is what weakened the part and made it awkward to print.

    Assumed layout: two parallel racks either side of the gear, moving in
    opposite directions along the long axis, jaws at the two ends.

--------------------------------------------------------------------------------
## HOW THE CLUTCH IS HELD - THE ANCHOR SLOT

    A wrap spring only grips because ONE tang is held still while the hub
    turns underneath it. Without an anchor the spring simply spins with the
    hub and never tightens. The anchor is a SLOT IN THE HOUSING WALL.

        anchor tang  = helix START, z 31, runs +X, length 24
                       reaches x 24.0; the bore wall is at x 21.62,
                       so it lands 2.4 into the slot
        control tang = helix END,   z 38.5, runs -X, length 16
                       tip at r 18.52 - inside the bore, free to swing

    Housing constants: ancOn true, ancX 19, ancY 9.33, ancZ 31, ancW 1.9.
    Take them from the coil script's report, which now prints both tip
    coordinates.

    TANG DIRECTION MATTERS. At phi0 = 0 the tangs run along Y, straight at
    the bore WAIST (half-width 13.4), and hit the wall after 9.5 mm - before
    they have even cleared the collar, which needs 11.3. At phi0 = 90 they
    run along X toward the LOBES, where the bore reaches 25.4, and there is
    room for both.

--------------------------------------------------------------------------------
## THE WRAP SPRING NEEDS A METAL HUB - CONFIRMED ON THE BENCH

    A 2.5 mm spring, tang trapped rigidly in a drill bit, turned freely in
    BOTH directions on a printed shaft. Pulling it off showed helical grooves
    the coils had cut into the plastic.

    That is the mechanism failing, not the test. The coils thread-form a
    groove of pitch = wire diameter. Once each coil sits in the groove it cut,
    relative rotation no longer slides it around the shaft - it SCREWS ALONG
    it. A 2.5 mm pitch on a 17 mm shaft is a coarse, freely back-drivable
    thread, so it turns easily both ways and no tension ever builds.

    It is unavoidable in plastic. Round wire on a cylinder is LINE contact:

        material   wire 1.5   wire 2.5   yield
        PLA          195        152        60
        PETG         154        120        50
        POM          174        136        70      (MPa)

    2-3x over yield in every case. The 9-15 MPa quoted elsewhere in this
    file is a SMEARED AVERAGE over the coil pitch - the real local stress is
    an order of magnitude higher.

    To use the wrap spring the hub needs a METAL gripping surface: a steel
    sleeve pressed over the printed boss. Steel on steel runs ~1500 MPa
    against ~2000 yield. This is a requirement, not a material preference.

--------------------------------------------------------------------------------
## OPEN PROBLEM - THE ANCHOR TANG IS UNDERSIZED

    The clutch does not STORE energy. The energy is in the squashed cloth and
    the return springs; the clutch only refuses to let the rotor turn back.
    Load path when the sleeve is held down:

        sleeve -> pins -> cam groove -> rotor -> hub -> spring coil
               -> ANCHOR TANG -> slot in the housing

    So that one tang carries the entire grip.

    For a TANGENTIAL tang, base at radius rc and force F at distance L:

        torque about the axis = F x L
        bending at the root   = F x L

    They are the same number. The root moment therefore EQUALS the full
    torque, whatever the tang's length and wherever it is supported. That is
    an equilibrium result, so the usual fixes - shorten it, capture it closer
    to the coil, support it along its length - do nothing at all.

        clutch torque      1.92 N.m
        1.5 mm wire, Z     0.331 mm3
        root stress        5795 MPa   vs ~1700 yield   -> over by 3.4x

    Wire needed for 1.92 N.m: 2.26 mm minimum.

        wire 1.5 -> 0.56 N.m  =  47 N of rack force
        wire 2.0 -> 1.34 N.m
        wire 2.5 -> 2.61 N.m  (coil OD 22.15, still fits the 26.8 bore)
        wire 3.0 -> 4.51 N.m

    A 2.5 mm wire also drops hub pressure from 15.1 to 9.2 MPa.

    ALTERNATIVE: the ratchet and pawl already in the repo. At radius 15 the
    pawl sees 128 N in COMPRESSION along its arm rather than bending a thin
    wire. Costs infinite adjustment, but 60 teeth gives 1.9 mm rack steps and
    cloth compresses more than that.

    RESOLVED - the ratchet was chosen and is now built out in full: sections
    12, 13 and 14, with its own housing file. This section is kept because the
    wrap spring is still the better mechanism IF a steel hub sleeve is worth an
    extra part - it is the only version with stepless adjustment.

    RECOMMENDATION: the ratchet. The wrap spring has now hit two independent
    blockers, both tracing to a thin steel wire loaded against soft plastic -
    the tang undersized 3.4x, and the hub thread-forming. The ratchet's pawl
    bears 128 N in COMPRESSION on a tooth flank, which printed plastic takes
    without complaint.

    The wrap spring stays viable WITH a steel hub sleeve, if stepless
    adjustment is worth the extra part.

--------------------------------------------------------------------------------
## RETURN SPRINGS

    A spring rising from the base CANNOT push on a ROUND sleeve: anything
    clearing the gear (radius 13.68) sits further out than a round sleeve is
    wide (radius 13). Stretching the sleeve into an OVAL puts real material
    past the gear, and the springs go into blind pockets in its lobes.

    Sleeve constants: pockR 18, pockDia 6.6, pockDep 6.
    The pocket CEILING is the spring seat, 2 mm below the sleeve top. Pocket
    depth does the job a tab height would:

        pocket 0 deep (flat bottom): spring 22 -> 6,  solid under 6
        pocket 6 deep:               spring 28 -> 12, solid under 12

    The seat travels z 12 (closed) to 28 (open), so the spring works over
    16 mm - the stroke.

    Springs: 2 off, OD about 6, free length about 30, on the base pins at
    radius 18. They also drive the jaws open, since the 54.9 deg lead angle
    back-drives easily.

--------------------------------------------------------------------------------
## THE STACK

    WRAP SPRING BUILD                   RATCHET BUILD
    z 40 ---- rotor top                 z 42 ---- housing top, shaft flush
    z 30..40  hub, dia 17.15            z 38.3..41.3  cap, dia 20
       (spring 31..38.5,                z 38..40  upper web
        collar 36..40)                  z 32..38  pawl, in the wheel's teeth
                                        z 30..38  ratchet wheel, dia 24
    z 30      sleeve top, full travel   z 30..31.8 lower web
    z  6..30  cam, dia 18               z 30      sleeve top, full travel
              (groove 10..26)           z  6..30  cam, dia 18 (groove 10..26)
    z  0..6   gear, tip dia 27.36       z  0..6   gear, tip dia 27.36
    z  0      gear plane = rack GearPlane origin, both builds

    The ratchet build is 2 mm SHORTER overall (42 vs 44 housing) despite
    stacking more parts, because the wheel shares the 30..38 band with the
    pawl instead of the hub and collar needing separate levels.

    ONE MOULDED ROTOR = gear + cam + hub, single 6.4 bore throughout on a
    6.0 shaft. Union gear.Solid + cam.Body + hub.Body.
    The gear needs the bore too - without it the disc is solid through the
    middle and the shaft has nowhere to go.
    SEPARATE = sleeve + 2 pins, spring, collar, 2 racks, shaft, return spring,
               enclosure.

    Sleeve travels z 6..22 (base). It CANNOT go below the gear top face at z 6 -
    that limit is what sets the whole height budget:
        overall = gear face + stroke + FULL sleeve height + gap + hub

--------------------------------------------------------------------------------
## FITS - every mating pair

    male                  female                gap    note
    shaft 6.00            rotor bore 6.40      +0.40   ROTOR TURNS ON THIS
    cam 18.00             sleeve bore 18.50    +0.50   sleeve slides
    spring free ID 17.00  hub 17.15            -0.15   INTERFERENCE, intended
    spring OD 20.15       collar bore 20.95    +0.80   collar rotates
    sleeve waist 26.00    housing bore 26.80   +0.80   sleeve slides
    sleeve length 50.00   housing bore 50.80   +0.80   sleeve slides
    rib 5.60              key groove 6.00      +0.40   sleeve slides
    collar OD 24.95       housing waist 26.80  +1.85   collar rotates
    pin 3.80              cam groove 4.00      +0.20   pin slides
    rack 6.37             window 7.00          +0.63   rack slides

    RATCHET BUILD, in place of the spring / collar rows above:
    pawl 6.00             web gap 6.40         +0.40   pawl swings, 0.2 a face
    pin 3.00 metal        post hole 3.20       +0.20   pawl pivots - see below
    wheel tip 24.00       housing waist 26.80  +2.80   wheel turns
    post 6.00 at x16      wheel tip r 12.00    +1.00   post clears the wheel
    cap 20.00             web inner edge x13   +3.00   cap clears the web

    The shaft fit was 6.00 into 6.00 - no clearance at all on the one surface
    the whole rotor turns on. The clearance is put in the BORE, not the shaft,
    so a real 6 mm steel rod can replace the printed one later.

    Printed holes come out undersized and printed posts oversized, so 0.4
    nominal often ends up near zero in the part. Ream or drill to 6.1-6.2 if
    it binds - easier than adding material back.

--------------------------------------------------------------------------------
## CHECKS — all with margin

    gear tooth stress     45 MPa      POM ~65
    undercut limit        15.10       z16 clear
    lead angle            54.9 deg    >25 needed, no jamming
    land between passes   19.1 mm     >0 needed
    pin bearing           17.2 MPa
    pin bending           27 MPa
    cam max bore          9.0 mm      6 mm shaft used
    clutch hub pressure   15.1 MPa    limit 20
    release force         2.2 N       holding 1.92 N.m

--------------------------------------------------------------------------------
## OPEN ITEMS

    1. DIRECTION. Pushing down must turn the gear the way the spring SLIPS.
       50/50 and invisible in CAD. If it locks on the first push, flip `hand`
       on the cam. Check on the bench before cutting the enclosure.
    2. RETURN SPRINGS - now solved: 2 compression springs on the base pins,
       pushing up on the sleeve tabs. See the RETURN SPRINGS section.
    3. MOULDING. The helical groove cannot be pulled straight from a tool -
       it needs an unscrewing core, or split the rotor into cam + gear/hub
       joined by a keyed press-fit. Price both.
    4. SLEEVE PINS. Inward pins are an undercut too. Either a clamshell sleeve,
       or press metal dowels into radial holes (stronger, and they are the
       highest-stress part).
    5. HOUSING GUIDE. The sleeve must be held from rotating, or it will spin
       instead of driving the cam.
    6. RETAINING CAP - now built, see section 11.

--------------------------------------------------------------------------------
## ASSEMBLY ORDER

    1. rotor   - one piece: gear + cam + hub
    2. sleeve  - WITHOUT pins, drops down from the top. Its bore radius 9.25
                 clears the hub radius 8.575 by 0.675.
    3. pins    - pressed in radially through the sleeve wall into the groove.
    4. spring  - TWISTED open and slid on. Free ID 17.0 on a 17.15 hub is an
                 INTERFERENCE fit: it is meant to refuse to push on. Twist the
                 two legs apart about 30 degrees to widen the coil, slide it
                 down, release. It should then be impossible to pull off by
                 hand - if it slides on freely the hub is too small and the
                 clutch will not grip at all.
    5. collar  - drops down from the top; bore 20.95 clears the hub and the
                 spring OD 20.15, and the open notch passes the tang
    6. cap     - traps the collar axially, still free to rotate
    7. springs - 2 compression springs dropped over the base pins
    8. housing - lowered over the lot, sleeve tabs entering the slots and the
                 lever entering its gap; 3 screws up through the plate

    WHY THE PINS GO IN LAST. A ball pin reaches inward to radius 7.10, but the
    spring hub is radius 8.575. A sleeve with the pins already in it cannot be
    passed over the hub - it fouls by 1.475 mm. This is the same undercut that
    stops the pins being moulded into the sleeve anyway, so press-in dowels
    solve both problems at once (and metal dowels take the load better).

    The tang is the only thing blocking the COLLAR: its tip sits at radius
    18.52, which is 5.04 mm outside the collar's 13.47 OD radius. Hence the
    open notch.

--------------------------------------------------------------------------------
## GROOVE CUTTER

    A ball-nose pin sweeping the path carves exactly a PIPE of radius gw/2
    centred on the skin: gw wide, gw/2 deep, hemispherical ends. When the
    groove is a full U (gd = gw/2, botR 0) the script pipes it as one
    primitive rather than lofting sections.

        pipe radius 2.0 on the skin at radius 9.0
        cuts radius 7.0 .. 11.0  ->  2 deep, 2 proud (its own overshoot)
        end cap is a hemisphere r 2.0; the r 1.9 ball nests with 0.10 clearance

    So no end extension is needed and no flat end face is left sitting a
    fraction of a millimetre inside the blank - which is what was breaking the
    geometry. endOverTop / endOverBot are both 0 and only apply to the loft
    path, which is still used for grooves deeper than half their width.

--------------------------------------------------------------------------------
## TANG / SLOT ALIGNMENT

    The coil script puts the ANCHOR tang at the helix START (bottom of the coil)
    and the CONTROL tang at the helix END (top). Both are reported by the script.

        collar tangAng = coil phi0 + frac(Coils) x 360
        collar slotZ   = (coil zOff + Coils x pitch) - collar zOff

    For this build: control tang at z 38.5, angle 0
        collar zOff 36, slotZ 2.5  ->  slot at z 38.5   MATCHES

    The collar must NOT cover z 31, where the anchor tang leaves the coil on its
    way to the housing - which is why the collar is 4 tall and starts at 36.
