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
                bore          6
                phi0          0
                hand          +1       <- flip if it locks on the push
                spt           64
                over          0
                botR          0        <- 0 = full U
                pinClr        0.2
                endOverTop    5        <- breaks out on top so the sleeve slides on
                endOverBot    3        <- clears the ball at the blind bottom end

    RESULT      body z 6..30, groove z 7..31 (pin centre travels 10..26)
                turns 0.1989 (71.6 deg)   lead 80.4   lead angle 54.9 deg
                land between passes 19.1   core radius 7.0 -> max bore 9.0

--------------------------------------------------------------------------------
## 4. FOLLOWER SLEEVE                              (CamFollowerSleeve)

    INPUTS      CamDiameter   18
                Clearance     0.25
                OuterDiameter 26
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
                phi0          0
                zOff          6 = closed (pushed down)
                              22 = open (up).  Slider this to animate.

    RESULT      pin dia 3.8, ball tip 1.9, ball centre radius 9.0
                ball-to-groove-bottom gap 0.10
                130 N per pin: bearing 17.2 MPa, bending 27 MPa
                NOTE the sleeve is short; the housing guide must stop it rotating

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
                gap 0   phi0 0   hand +1
                zOff          31

--------------------------------------------------------------------------------
## 7. SPRING HUB — part of the rotor               (WrapSpringHub)

    INPUTS      HubDia        17.15    <- free ID 17.0 + 0.15 interference
                HubLength     10
                BoreDia       6
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
                WallThk       3
                Height        4        <- SHORT: must not cover the anchor tang
                LeverLength   25
                LeverWidth    8
                BasePlane     <- rack GearPlane
    CONSTANTS   slotFit       0.3
                tangAng       0
                leverAng      90
                slotZ         2.5      <- puts the slot at z 38.5 = the control tang
                zOff          36       <- spans 36..40
                openTop       true     <- notch open to the top edge, so the collar
                                          can drop on over the tang

    RESULT      bore 20.95, OD 26.95, min tang reach 12.7 (so 16 mm legs are fine)
                release: 29 deg of swing, 19.4 mm of lever travel, 2.2 N
    NEEDS       axial retention (lip or circlip) + a light return spring so the
                lever parks engaged

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
    2. RETURN SPRING. Releasing the clutch only removes the lock. A torsion
       spring on the shaft is what actually opens the jaws.
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

    1. rotor (one piece: gear + cam + hub)
    2. sleeve  - drops down from the top; bore 18.5 clears the hub 17.15 and
                 the cam 18.0
    3. spring  - wound down onto the hub from the top (free ID 17.0 on a 17.15
                 hub, so twist to expand it as it goes on)
    4. collar  - drops down from the top; bore 20.95 clears the hub and the
                 spring OD 20.15, and the open notch passes the tang
    5. cap     - traps the collar axially, still free to rotate

    The tang is the ONLY thing that blocks the collar: its tip sits at radius
    18.52, which is 5.04 mm outside the collar's 13.47 OD radius. Hence the
    open notch.

--------------------------------------------------------------------------------
## GROOVE END CLEARANCE

    The cutter is a loft with FLAT end caps. The pin's ball reaches 1.9 mm past
    the pin centre, so if the groove stops where the pin centre stops, the ball
    punches through the end face.

        endOverBot must exceed the ball radius (1.9).  Set to 3.
        groove bottom moves to local 1.0 / world 7.0, still blind inside the
        body, and the pin centre still stops at world 10 when the sleeve
        lands on the gear.

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
