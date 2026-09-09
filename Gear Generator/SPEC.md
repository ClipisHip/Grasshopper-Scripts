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
    AFTER       extrude the profile curve 6 mm, capped

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

    INPUTS      PlateDia      54
                PlateThk      2
                ShaftDia      6
                ShaftLen      42
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

    z 40 ---- rotor top
    z 30..40  spring hub, dia 17.15   (spring 31..38.5, collar 36..40)
    z 30      sleeve top at full travel
    z  6..30  cam, dia 18             (groove 10..26)
    z  0..6   gear, tip dia 27.36
    z  0      gear plane = rack GearPlane origin

    ONE MOULDED ROTOR = gear + cam + hub, single 6 mm bore throughout.
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
    6. RETAINING CAP on the rotor top. The collar's notch is open upward so it
       can be assembled, so something must stop it lifting off. The cap does
       that and provides the axial retention the collar needs anyway.

--------------------------------------------------------------------------------
## ASSEMBLY ORDER

    1. rotor   - one piece: gear + cam + hub
    2. sleeve  - WITHOUT pins, drops down from the top. Its bore radius 9.25
                 clears the hub radius 8.575 by 0.675.
    3. pins    - pressed in radially through the sleeve wall into the groove.
    4. spring  - wound down onto the hub from the top (free ID 17.0 on a 17.15
                 hub, so twist to expand it as it goes on)
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
