# RECOMMENDATIONS — what to actually set, and why

The numbers here are the `ScrewOptions` presets written out with their reasoning.
They are **starting points that work**, not guarantees: your printer, your
filament and your slicer all move the answer.

---

## 0. Do this once, before you trust any of it

Print a **test pair**, not the real part.

    ScrewGenerator   Size M5   Head socket   Length 12   Tol <preset>
    ThreadTap        Spec <- from the screw   Length 6   OuterDia 12   Chamfer 1

Six millimetres of M5 is about 90 seconds of printing and it tells you
everything. Run three at `Tol` 0.15 / 0.20 / 0.25 and keep the one that turns
with light finger pressure and no wobble. Write the number on the wall. It is a
property of **your machine**, and it will hold across every size you print after.

A thread that needs pliers is too tight; one you can rock side to side has lost
most of its flank contact and will strip under load.

---

## 1. Materials

`Tol` is a **radial** clearance in millimetres, applied to the internal thread,
so the hole opens by twice this on diameter. It is the same knob in both scripts.

| preset | Tol | Fit | Chamfer | why that number |
|---|---|---|---|---|
| `machined` | 0 | close | 0.5 | Cut with a real tap and die. The basic profile is correct as-is; the tap carries its own allowance. |
| `tap-it` | 0 | close | 0.5 | Print or mill the `Drill` output, then run a real tap through it. |
| `resin` | 0.08 | close | 0.5 | SLA/DLP holds fine features, so it needs the least. Wash and cure **fully** first — green parts creep and the fit drifts for a day. |
| `resin-tough` | 0.12 | normal | 0.5 | Tough and engineering resins stay softer longer. More room. |
| `pla` | 0.20 | normal | 1.0 | The FDM baseline. Stiff and brittle, so threads shear instead of yielding — buy back strength with length, not with tightness. |
| `petg` | 0.25 | normal | 1.0 | Stringing and ooze land in the thread roots, which is exactly where you cannot reach to clean. Always more than PLA. |
| `abs` / `asa` | 0.22 | normal | 1.0 | Plus shrinkage. On anything over ~60 mm go to 0.30, because the hole shrinks toward its own centre and a long part does it unevenly. |
| `pc` | 0.22 | normal | 1.0 | Strong threads, but only with a hot chamber — without one the layers part before the thread strips. |
| `nylon` | 0.30 | loose | 1.0 | Swells with moisture, so the fit moves with the weather. Dry it, and expect it to loosen again. Very forgiving once it fits. |
| `sls-mjf` | 0.25 | normal | 1.0 | Powder packs into thread roots and does not all come out. Bead blast, then test. |
| `first-try` | 0.35 | loose | 1.0 | Untuned printer, unknown filament, or a part you cannot afford to reprint. Sloppy, but it **will** go together. |
| `render` | 0 | close | 1.0 | Presentation only. Zero clearance and a heavy mesh. Do not send this to a printer. |

---

## 2. How much thread to engage

Depth of the tapped hole, as a multiple of the screw diameter. This matters far
more than tightness — a loose thread that is long enough holds; a perfect thread
that is 3 mm deep does not.

| material the thread is cut in | depth | M5 works out at |
|---|---|---|
| steel | 1.0 d | 5 mm |
| aluminium, brass | 1.5 d | 7.5 mm |
| printed PLA / PETG / ABS | 2.0 d | 10 mm |
| printed nylon, soft resin | 2.5 d | 12.5 mm |
| end grain in wood | don't — use an insert | |

Past about 3 d you gain almost nothing: the first few turns carry most of the
load and the rest is along for the ride.

`ThreadTap` warns in `Info` when `Length` is under one diameter.

---

## 3. When **not** to print the thread

Printed threads are a real option, and they are also the wrong answer surprisingly often.

**Use a heat-set insert instead when:**
- the size is M3 or smaller — the thread form is finer than a 0.4 mm nozzle can resolve honestly
- the joint will be taken apart more than a handful of times
- it carries real load in PLA

Model it with `ThreadTap` at `Threads`/`hand=none` (a plain bore) and set the
diameter from your insert's datasheet, or just use the `Drill` output.

**Tap it instead when** the part is machined, or when it is printed but the load
matters. Take `Drill` from `ThreadTap`, subtract that, and run a real tap in.
Cutting a thread through printed layers gives a much better flank than printing
one, because there are no bridges and no elephant's foot in the root.

**Use a captive nut instead when** the load is high and the part is plastic.
Print a hexagonal pocket, drop a steel nut in. Nothing printed will beat it.

---

## 4. The mouth chamfer — what it is and what to set

`ThreadTap.Chamfer` is **in pitches, not millimetres**. `1` means one pitch on an
M3 and one pitch on a 1/2-13 alike, and `ThreadTap` does the multiply itself
because it already knows the pitch from `Spec`. So there is nothing to look up.

    0.5   cutting metal        the machinist's chamfer, opens the mouth to D + P
    1.0   printing             removes the unsupported first thread outright
    0     none                 valid, but a screw will be fiddly to start

### What it actually does

Over the first `Chamfer × pitch` of depth, every thread radius is pushed outward,
tapering from full flare at the mouth back to nothing at the bottom of the
chamfer. The thread form is unchanged — it just gets shallower as it nears the
surface. That is exactly what a **taper tap** leaves behind, which is why taper
taps exist.

    Chamfer = 0                       Chamfer = 1   (one pitch, M5)

   ──┬──────────┬──                  ──┬──╲            ╱──┬──
     │WWWWWWWWWW│                      │   ╲WW      WW╱   │   <- flared, 0.8 deep
     │WWWWWWWWWW│                      │WWWWWWWWWWWWWW│
     │WWWWWWWWWW│                      │WWWWWWWWWWWWWW│

### Why you want it

- **It stops cross-threading.** The screw self-centres instead of catching half
  a thread and jamming at an angle.
- **On a printed hole it removes the thread that was going to fail anyway.** The
  very first turn has nothing underneath it, so it droops into the bore. Better
  to not print it than to print it badly.
- **It survives elephant's foot.** A squashed first layer makes the mouth
  undersize; the chamfer is already wider than that.

### Don't overdo it on a shallow hole

The flared turns are shallower than full depth, so they carry little load — they
are sacrificial. Chamfer 1.5 mm at **both** ends of a 6 mm deep hole and half
your engagement is funnel. Keep the total under about a fifth of `Length`.
`ThreadTap` hard-clamps each end to `0.3 × Length` and says so in `Info`, but
that clamp is a backstop, not a target.

### What it works out to

You should never need this — it is here so the number is not a black box.
`Info` prints the figure it used every run.

| size | pitch | `0.5` = | `1.0` = |
|---|---|---|---|
| M3 | 0.50 | 0.25 | 0.50 |
| M4 | 0.70 | 0.35 | 0.70 |
| M5 | 0.80 | 0.40 | 0.80 |
| M6 | 1.00 | 0.50 | 1.00 |
| M8 | 1.25 | 0.62 | 1.25 |
| M10 | 1.50 | 0.75 | 1.50 |
| M12 | 1.75 | 0.88 | 1.75 |
| M16 | 2.00 | 1.00 | 2.00 |

Inch, in inches:

| size | pitch | `0.5` = | `1.0` = |
|---|---|---|---|
| #6-32 / #8-32 / #10-32 | 0.0312 | 0.016 | 0.031 |
| #10-24 | 0.0417 | 0.021 | 0.042 |
| 1/4-20 | 0.0500 | 0.025 | 0.050 |
| 1/4-28 | 0.0357 | 0.018 | 0.036 |
| 5/16-18 | 0.0556 | 0.028 | 0.056 |
| 3/8-16 | 0.0625 | 0.031 | 0.063 |
| 1/2-13 | 0.0769 | 0.038 | 0.077 |

> **On the name.** This used to be called `Lead`, which was a bad choice: in
> threading, *lead* properly means how far a screw advances in one turn (for a
> single-start thread, the pitch itself). It is a chamfer. It is now called one.

---

## 5. Printing them well

**Orientation is most of it.** A thread whose axis is vertical prints as a stack
of rings and comes out well. A thread lying on its side is a stack of
overhanging arcs, and the underside of every turn droops. If you can only print
it lying down, add 0.05–0.10 to `Tol` and expect to chase it with a tap.

**Chamfer earns its place.** Covered in full in §4 — set it to `1` for anything
printed. It costs nothing and it is the difference between a thread that starts
and one you fight.

**The screw side wants a chamfer too**, and gets one automatically: the tip is
chamfered 45° down to the root diameter, which is what a real screw has.

**Elephant's foot eats the first thread.** If your first layer squishes, the
mouth of a printed hole is undersize. Either chamfer the part itself, or raise
`Chamfer`.

**Segments**: leave it at 96. Drop it to 48 while you lay the model out and put
it back before you export — the mesh cost is real and you will notice it at
`seg=192` on a long screw.

---

## 6. Where each clearance actually goes

Worth being clear, because two different `Tol`s do two different jobs:

    ScrewGenerator.Fit + .Tol   ->  the CLEARANCE BORE in Prep
                                    the hole the screw passes THROUGH
    ThreadTap.Tol               ->  the THREAD in the part it screws INTO

They are usually the same number and `Spec` carries it across for you, which is
why `ThreadTap.Tol` can be left at 0. Override it when they should differ — a
sloppy pass-through hole with a snug thread, for instance.

The screw itself is always drawn at basic nominal. It is the reference part, and
none of the clearance lives on it.

---

## 7. When it does not fit

| symptom | what it is | fix |
|---|---|---|
| screw will not start | first thread curled, or elephant's foot | `Chamfer` to `1` on `ThreadTap` (§4) |
| starts then binds | `Tol` too small | +0.05 and print the test pair again |
| goes in but rocks | `Tol` too large | −0.05. Check you are not also on `loose` `Fit` |
| strips on first use | not enough engagement | `Length` to 2 d, or use an insert |
| fits in PLA, not in PETG | it is the material, not the model | use the preset for what you are actually printing |
| fine on one printer, tight on another | it always is | `Tol` is per-machine, re-run §0 |
| head does not sit flush | `Recess`, not `Length` | `sink` for flat heads, `bore` for the rest |
| head sits too deep | `sink=` in `Opts` is doing it | set `sink=0` or drop it from `Opts` |

---

## 8. Machining, for completeness

Set `Tol` to 0 and `Fit` to `close` or `normal`. The geometry is the basic ISO
profile and a real tap already carries its allowance — adding your own on top
gives you a sloppy thread for no reason.

`Drill` on `ThreadTap` is the tap-drill hole at `d − P`, which is the
~77 % engagement rule and lands on the standard drill for every size in the
table. `Info` prints the engagement it leaves. Below 100 % is not a compromise:
75–80 % is the usual target, because the last 20 % of thread depth buys about
5 % more strength and roughly doubles the torque needed to cut it.
