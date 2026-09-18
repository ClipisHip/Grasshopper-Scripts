# OPTIONS — every string the components accept

Generated from the tables inside the scripts, so it cannot drift from them.

The **ScrewOptions** component hands all of these back as lists: drop a *List Item*
and a slider on each and you can pick instead of type.

---

## Head — `ScrewGenerator.Head`

| value | what you get | standard | drive |
|---|---|---|---|
| `socket` | cylindrical head, straight sides, small top chamfer | ISO 4762 / DIN 912 / ASME B18.3 | hex socket |
| `button` | low dome on a short land | ISO 7380-1 | hex socket |
| `flat` | countersunk cone, finishes flush | ISO 10642 (90°) / ASME B18.3 (82°) | hex socket |
| `hex` | external hexagon, chamfered top | ISO 4017 / DIN 933 / ASME B18.2.1 | spanner |
| `pan` | dome on a land | ISO 7045 / DIN 7985 | straight slot |
| `none` | no head — threaded rod, stud, grub screw | — | — |

Anything unrecognised falls back to `socket`. Matching is on the first few letters,
so `countersunk`, `csk` and `flathead` all reach `flat`.

## Fit — `ScrewGenerator.Fit`

Sets the **clearance bore** in `Prep`. Nothing to do with the thread.

| value | series | M5 bore | 1/4 bore | use it when |
|---|---|---|---|---|
| `close` | ISO 273 fine / close drill | 5.3 | 0.257 | dowels or a shoulder locate the joint |
| `normal` | ISO 273 medium / free drill | 5.5 | 0.266 | the default |
| `loose` | ISO 273 coarse / loose drill | 5.8 | 0.277 | frames, printed parts, stacked tolerances |

`Tol` is added on top, on the **radius**, so the bore grows by `2 × Tol`.

## Recess — `ScrewGenerator.Recess`

| value | `Prep` gets | head ends up |
|---|---|---|
| `auto` | `sink` for `flat`, `none` for everything else | flush / proud |
| `none` | a plain through bore | proud of the face |
| `bore` | counterbore cylinder at head diameter, flat floor, 45° relief into the bore | sunk in a pocket |
| `sink` | countersink cone at the head's own angle | flush |

`auto` deliberately does **not** counterbore socket and button heads — those are
normally meant to sit proud. Ask for `bore` when you want them sunk.

## Opts — `ScrewGenerator.Opts`

The three you rarely touch, as `key=value`, space separated. Leave it empty for defaults.

| key | default | what it does |
|---|---|---|
| `sink=<n>` | `0` | extra recess depth below the face. `0` finishes the head flush. |
| `prep=<n>` | shank + 2 mm | how far `Prep` reaches below the seating face |
| `seg=<n>` | `96` | thread samples per turn, snapped to a multiple of 16 |

Example: `sink=2 seg=192`. Unknown keys are ignored and reported in `Info`.

## Spec — `ScrewGenerator.Spec` → `ThreadTap.Spec`

One wire carries four settings, which is why `ThreadTap` has six inputs instead of nine.

```
M5 hand=R tol=0.200 seg=96
```
| token | meaning | if absent |
|---|---|---|
| first token | the size | `M5` |
| `hand=R\|L\|none` | thread hand; `none` gives a plain bore | right |
| `tol=<n>` | radial clearance, used unless `ThreadTap.Tol` > 0 | `0` |
| `seg=<n>` | samples per turn | `96` |
| `class=6H\|6G` | ISO tolerance position, metric only | `6H` |

A bare `M5` typed into a panel works too — the rest fall back to defaults.
`class=` is the only one `ScrewGenerator` does not emit; type it yourself if you want 6G,
though on an M5 that is only 0.012 mm on the radius, which `Tol` covers more honestly.

## Threads — `ScrewGenerator.Threads`

| value | result |
|---|---|
| `0` | plain shank at the major diameter. Fast, for laying a model out. |
| `1` | right hand (default) |
| `2` | left hand |

---

## Sizes — `Size` / `Spec`

63 entries. `Filter` on **ScrewOptions** trims the list: `metric` (28), `inch` (35),
`coarse` (32), `common` (18), empty for all. ★ marks the `common` set.

### Metric — thread and holes

| size | ★ | pitch | pitch Ø | minor Ø | tap drill | close | normal | loose |
|---|---|---|---|---|---|---|---|---|
| `M1.6` |  | 0.35 | 1.373 | 1.221 | 1.25 | 1.7 | 1.8 | 2 |
| `M2` |  | 0.4 | 1.740 | 1.567 | 1.60 | 2.2 | 2.4 | 2.6 |
| `M2.5` |  | 0.45 | 2.208 | 2.013 | 2.05 | 2.7 | 2.9 | 3.1 |
| `M3` | ★ | 0.5 | 2.675 | 2.459 | 2.50 | 3.2 | 3.4 | 3.6 |
| `M3.5` |  | 0.6 | 3.110 | 2.850 | 2.90 | 3.7 | 3.9 | 4.2 |
| `M4` | ★ | 0.7 | 3.545 | 3.242 | 3.30 | 4.3 | 4.5 | 4.8 |
| `M5` | ★ | 0.8 | 4.480 | 4.134 | 4.20 | 5.3 | 5.5 | 5.8 |
| `M6` | ★ | 1 | 5.351 | 4.917 | 5.00 | 6.4 | 6.6 | 7 |
| `M8` | ★ | 1.25 | 7.188 | 6.647 | 6.75 | 8.4 | 9 | 10 |
| `M10` | ★ | 1.5 | 9.026 | 8.376 | 8.50 | 10.5 | 11 | 12 |
| `M12` | ★ | 1.75 | 10.863 | 10.106 | 10.25 | 13 | 13.5 | 14.5 |
| `M14` |  | 2 | 12.701 | 11.835 | 12.00 | 15 | 15.5 | 16.5 |
| `M16` | ★ | 2 | 14.701 | 13.835 | 14.00 | 17 | 17.5 | 18.5 |
| `M20` |  | 2.5 | 18.376 | 17.294 | 17.50 | 21 | 22 | 24 |
| `M24` |  | 3 | 22.052 | 20.752 | 21.00 | 25 | 26 | 28 |

Fine pitches available on the same diameters (head and clearance data comes from the
nominal size, only the pitch changes):

`M3x0.35`, `M4x0.5`, `M5x0.5`, `M6x0.75`, `M8x1`, `M10x1.25`, `M10x1`, `M12x1.5`, `M12x1.25`, `M14x1.5`, `M16x1.5`, `M20x1.5`, `M24x2`

### Metric — head dimensions

| size | socket Ø×h | socket hex | button Ø×h | flat Ø | hex A/F ×h | pan Ø×h |
|---|---|---|---|---|---|---|
| `M1.6` | 3 × 1.6 | 1.5 | 3 × 1.05 | 3.2 | 3.2 × 1.1 | 3.2 × 1.3 |
| `M2` | 3.8 × 2 | 1.5 | 3.8 × 1.3 | 4 | 4 × 1.4 | 4 × 1.6 |
| `M2.5` | 4.5 × 2.5 | 2 | 4.7 × 1.5 | 5 | 5 × 1.7 | 5 × 2 |
| `M3` | 5.5 × 3 | 2.5 | 5.7 × 1.65 | 6 | 5.5 × 2 | 6 × 2.4 |
| `M3.5` | 6.5 × 3.5 | 2.5 | 6.6 × 1.95 | 7 | 6 × 2.4 | 7 × 2.6 |
| `M4` | 7 × 4 | 3 | 7.6 × 2.2 | 8 | 7 × 2.8 | 8 × 3.1 |
| `M5` | 8.5 × 5 | 4 | 9.5 × 2.75 | 10 | 8 × 3.5 | 10 × 3.8 |
| `M6` | 10 × 6 | 5 | 10.5 × 3.3 | 12 | 10 × 4 | 12 × 4.6 |
| `M8` | 13 × 8 | 6 | 14 × 4.4 | 16 | 13 × 5.3 | 16 × 6 |
| `M10` | 16 × 10 | 8 | 17.5 × 5.5 | 20 | 16 × 6.4 | 20 × 7.5 |
| `M12` | 18 × 12 | 10 | 21 × 6.6 | 24 | 18 × 7.5 | 24 × 9 |
| `M14` | 21 × 14 | 12 | 24 × 7.7 | 27 | 21 × 8.8 | 28 × 10.5 |
| `M16` | 24 × 16 | 14 | 28 × 8.8 | 30 | 24 × 10 | 32 × 12 |
| `M20` | 30 × 20 | 17 | 35 × 11 | 36 | 30 × 12.5 | 40 × 15 |
| `M24` | 36 × 24 | 19 | 42 × 13.2 | 43 | 36 × 15 | 48 × 18 |

Flat-head height is not listed because the script works it out from the diameter and the
90° angle — check `Info` for the value it used, and see SPEC.md §4 for why.

### Inch — thread and holes

| size | ★ | UNC | UNF | major Ø | close | normal | loose |
|---|---|---|---|---|---|---|---|
| `#0` |  | — | 80 | 0.06 | 0.067 | 0.073 | 0.076 |
| `#1` |  | 64 | 72 | 0.073 | 0.081 | 0.086 | 0.089 |
| `#2` |  | 56 | 64 | 0.086 | 0.0938 | 0.099 | 0.104 |
| `#3` |  | 48 | 56 | 0.099 | 0.1065 | 0.113 | 0.116 |
| `#4` | ★ | 40 | 48 | 0.112 | 0.116 | 0.1285 | 0.136 |
| `#5` |  | 40 | 44 | 0.125 | 0.136 | 0.1405 | 0.1495 |
| `#6` | ★ | 32 | 40 | 0.138 | 0.144 | 0.1495 | 0.159 |
| `#8` | ★ | 32 | 36 | 0.164 | 0.1695 | 0.177 | 0.185 |
| `#10` | ★ | 24 | 32 | 0.19 | 0.196 | 0.201 | 0.213 |
| `#12` |  | 24 | 28 | 0.216 | 0.221 | 0.228 | 0.242 |
| `1/4` | ★ | 20 | 28 | 0.25 | 0.257 | 0.266 | 0.277 |
| `5/16` | ★ | 18 | 24 | 0.3125 | 0.323 | 0.332 | 0.348 |
| `3/8` | ★ | 16 | 24 | 0.375 | 0.386 | 0.397 | 0.413 |
| `7/16` |  | 14 | 20 | 0.4375 | 0.4531 | 0.4687 | 0.4843 |
| `1/2` | ★ | 13 | 20 | 0.5 | 0.5156 | 0.5312 | 0.5625 |
| `5/8` |  | 11 | 18 | 0.625 | 0.6406 | 0.6562 | 0.6875 |
| `3/4` |  | 10 | 16 | 0.75 | 0.7656 | 0.7812 | 0.8125 |
| `1` |  | 8 | 12 | 1 | 1.0156 | 1.0312 | 1.0625 |

Write them with the tpi — `1/4-20`, `#8-32`, `1/2-20`. Without a tpi you get UNC.

> **One trap, handled.** `1-8` is the one-inch bolt but `1-64` is the #1 gauge screw,
> and neither writes a `#`. The tpi settles it: #1 comes in 64 and 72, and nothing an
> inch across is finer than 20, so under 40 tpi on a bare `1` reads as one inch.
> Write `#1` if you want the gauge size.

### Inch — head dimensions

| size | socket Ø×h | socket hex | button Ø×h | flat Ø | hex A/F ×h | pan Ø×h |
|---|---|---|---|---|---|---|
| `#0` | 0.096 × 0.06 | 0.05 | 0.114 × 0.032 | 0.138 | 0.125 × 0.044 | 0.116 × 0.039 |
| `#1` | 0.118 × 0.073 | 0.062 | 0.139 × 0.039 | 0.168 | 0.156 × 0.05 | 0.142 × 0.044 |
| `#2` | 0.14 × 0.086 | 0.078 | 0.164 × 0.046 | 0.197 | 0.188 × 0.055 | 0.167 × 0.053 |
| `#3` | 0.161 × 0.099 | 0.078 | 0.188 × 0.052 | 0.226 | 0.188 × 0.06 | 0.193 × 0.06 |
| `#4` | 0.183 × 0.112 | 0.094 | 0.213 × 0.059 | 0.255 | 0.188 × 0.05 | 0.219 × 0.068 |
| `#5` | 0.205 × 0.125 | 0.094 | 0.238 × 0.066 | 0.281 | 0.219 × 0.06 | 0.245 × 0.075 |
| `#6` | 0.226 × 0.138 | 0.109 | 0.262 × 0.073 | 0.307 | 0.25 × 0.068 | 0.27 × 0.082 |
| `#8` | 0.27 × 0.164 | 0.141 | 0.312 × 0.087 | 0.359 | 0.281 × 0.084 | 0.322 × 0.096 |
| `#10` | 0.312 × 0.19 | 0.156 | 0.361 × 0.101 | 0.411 | 0.312 × 0.1 | 0.373 × 0.11 |
| `#12` | 0.343 × 0.216 | 0.156 | 0.41 × 0.114 | 0.462 | 0.344 × 0.111 | 0.425 × 0.125 |
| `1/4` | 0.375 × 0.25 | 0.188 | 0.437 × 0.132 | 0.531 | 0.438 × 0.156 | 0.492 × 0.144 |
| `5/16` | 0.469 × 0.312 | 0.25 | 0.547 × 0.166 | 0.656 | 0.5 × 0.203 | 0.615 × 0.178 |
| `3/8` | 0.562 × 0.375 | 0.312 | 0.656 × 0.199 | 0.781 | 0.562 × 0.234 | 0.74 × 0.212 |
| `7/16` | 0.656 × 0.438 | 0.375 | 0.766 × 0.232 | 0.844 | 0.625 × 0.281 | 0.863 × 0.247 |
| `1/2` | 0.75 × 0.5 | 0.375 | 0.875 × 0.265 | 0.938 | 0.75 × 0.312 | 0.987 × 0.281 |
| `5/8` | 0.938 × 0.625 | 0.5 | 1.094 × 0.331 | 1.188 | 0.938 × 0.391 | 1.23 × 0.35 |
| `3/4` | 1.125 × 0.75 | 0.625 | 1.312 × 0.397 | 1.438 | 1.125 × 0.469 | 1.48 × 0.42 |
| `1` | 1.5 × 1 | 0.75 | 1.75 × 0.53 | 1.938 | 1.5 × 0.641 | 1.97 × 0.56 |

Button and pan rows are scaled from the metric ratios — no inch standard covers them as
widely. `Info` says so when you land on one.

---

## Presets — `ScrewOptions.Preset`

`Chamfer` is in **pitches**, so the one number suits every size — `ThreadTap`
multiplies by its own pitch. See RECOMMENDATIONS.md §4.

| preset | Tol (radial mm) | Fit | Chamfer (pitches) | Segments |
|---|---|---|---|---|
| `machined` | 0.00 | `close` | 0.5 | 96 |
| `tap-it` | 0.00 | `close` | 0.5 | 96 |
| `resin` | 0.08 | `close` | 0.5 | 128 |
| `resin-tough` | 0.12 | `normal` | 0.5 | 128 |
| `pla` | 0.20 | `normal` | 1.0 | 96 |
| `petg` | 0.25 | `normal` | 1.0 | 96 |
| `abs` | 0.22 | `normal` | 1.0 | 96 |
| `asa` | 0.22 | `normal` | 1.0 | 96 |
| `pc` | 0.22 | `normal` | 1.0 | 96 |
| `nylon` | 0.30 | `loose` | 1.0 | 96 |
| `sls-mjf` | 0.25 | `normal` | 1.0 | 96 |
| `first-try` | 0.35 | `loose` | 1.0 | 96 |
| `render` | 0.00 | `close` | 1.0 | 240 |

What each one is for, and the reasoning, is in RECOMMENDATIONS.md.
