"""
Multi-seed training curves for paper figures.
Produces mean ± std shading across all v7_3_s* runs.
"""

import os
import glob
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.ticker as ticker

# ── configuration ──────────────────────────────────────────────────────────────
RESULTS_DIR = os.path.join(os.path.dirname(__file__), "results")
# Explicit run names — 7.3.1 is seed=42 (the original v7_3 run)
RUN_NAMES = ["v7_3_s1", "v7_3_s2", "v7_3_s3", "7.3.1", "v7_3_s123"]
SEED_LABELS = {
    "v7_3_s1":   "seed 1",
    "v7_3_s2":   "seed 2",
    "v7_3_s3":   "seed 3",
    "7.3.1":     "seed 42",
    "v7_3_s123": "seed 123",
}
AGENT_DIR = "TradingAgent"

METRICS = {
    "Environment/Cumulative Reward": "Cumulative Reward",
    "Policy/Entropy":                "Policy Entropy",
    "Losses/Policy Loss":            "Policy Loss",
    "Losses/Value Loss":             "Value Loss",
}

SMOOTH_SIGMA = 0    # 0 = no smoothing (raw data)
N_POINTS     = 0    # 0 = keep original x-axis per seed (no resampling needed)
OUTPUT_DIR   = os.path.join(os.path.dirname(__file__), "figures")


# ── minimal tfevents reader (no TensorBoard / TF dependency) ──────────────────
import struct, glob as _glob

def _masked_crc32c(data: bytes) -> int:
    import crcmod
    crc_fn = crcmod.predefined.mkCrcFun("crc-32c")
    crc = crc_fn(data) & 0xFFFFFFFF
    return (((crc >> 15) | (crc << 17)) + 0xa282ead8) & 0xFFFFFFFF

def _read_records(path: str):
    """Yield raw Event bytes from a tfevents file."""
    with open(path, "rb") as f:
        while True:
            hdr = f.read(12)
            if len(hdr) < 12:
                break
            data_len = struct.unpack("<Q", hdr[:8])[0]
            _crc_len  = struct.unpack("<I", hdr[8:12])[0]
            data      = f.read(data_len)
            _crc_data = f.read(4)
            if len(data) < data_len:
                break
            yield data

def _parse_scalar_events(path: str):
    """Return list of (step, tag, value) from a single tfevents file."""
    import struct
    results = []
    for raw in _read_records(path):
        # Minimal proto parse for Event:
        # field 2 (step)   = varint
        # field 5 (summary) = length-delimited
        # Summary.value[0].tag (field 1 = string), simple_value (field 2 = float)
        pos = 0
        step = 0
        summary_bytes = None

        while pos < len(raw):
            # read field tag (varint)
            field_tag = 0; shift = 0
            while True:
                b = raw[pos]; pos += 1
                field_tag |= (b & 0x7F) << shift
                shift += 7
                if not (b & 0x80):
                    break
            field_number = field_tag >> 3
            wire_type    = field_tag & 0x07

            if wire_type == 0:  # varint
                val = 0; shift = 0
                while True:
                    b = raw[pos]; pos += 1
                    val |= (b & 0x7F) << shift
                    shift += 7
                    if not (b & 0x80):
                        break
                if field_number == 2:  # step
                    step = val
            elif wire_type == 1:  # 64-bit
                pos += 8
            elif wire_type == 2:  # length-delimited
                ln = 0; shift = 0
                while True:
                    b = raw[pos]; pos += 1
                    ln |= (b & 0x7F) << shift
                    shift += 7
                    if not (b & 0x80):
                        break
                chunk = raw[pos:pos + ln]; pos += ln
                if field_number == 5:  # summary
                    summary_bytes = chunk
            elif wire_type == 5:  # 32-bit
                pos += 4
            else:
                break  # unknown wire type

        if summary_bytes is None:
            continue

        # Parse Summary
        sp = 0
        while sp < len(summary_bytes):
            field_tag = 0; shift = 0
            while True:
                b = summary_bytes[sp]; sp += 1
                field_tag |= (b & 0x7F) << shift
                shift += 7
                if not (b & 0x80):
                    break
            field_number = field_tag >> 3
            wire_type    = field_tag & 0x07

            if wire_type == 2:
                ln = 0; shift = 0
                while True:
                    b = summary_bytes[sp]; sp += 1
                    ln |= (b & 0x7F) << shift
                    shift += 7
                    if not (b & 0x80):
                        break
                value_bytes = summary_bytes[sp:sp + ln]; sp += ln

                if field_number == 1:  # value[] repeated
                    # Parse Value message: field 1=tag(str), field 2=simple_value(float)
                    vp = 0; vtag = None; vval = None
                    while vp < len(value_bytes):
                        ft = 0; shift = 0
                        while True:
                            b = value_bytes[vp]; vp += 1
                            ft |= (b & 0x7F) << shift; shift += 7
                            if not (b & 0x80): break
                        fn = ft >> 3; wt = ft & 0x07
                        if wt == 2:
                            ln2 = 0; shift = 0
                            while True:
                                b = value_bytes[vp]; vp += 1
                                ln2 |= (b & 0x7F) << shift; shift += 7
                                if not (b & 0x80): break
                            s = value_bytes[vp:vp + ln2]; vp += ln2
                            if fn == 1: vtag = s.decode("utf-8", errors="replace")
                        elif wt == 5:
                            vval = struct.unpack("<f", value_bytes[vp:vp + 4])[0]
                            vp += 4
                        elif wt == 0:
                            while True:
                                b = value_bytes[vp]; vp += 1
                                if not (b & 0x80): break
                        else:
                            break
                    if vtag is not None and vval is not None:
                        results.append((step, vtag, vval))
            elif wire_type == 0:
                while True:
                    b = summary_bytes[sp]; sp += 1
                    if not (b & 0x80): break
            elif wire_type == 5:
                sp += 4
            elif wire_type == 1:
                sp += 8
            else:
                break
    return results


def load_scalar(run_dir: str, tag: str):
    """Return (steps, values) arrays for one run / one tag."""
    tfevent_files = sorted(_glob.glob(os.path.join(run_dir, "events.out.tfevents*")))
    if not tfevent_files:
        return None, None
    rows = []
    for f in tfevent_files:
        rows.extend(_parse_scalar_events(f))
    rows = [(s, v) for s, t, v in rows if t == tag]
    if not rows:
        return None, None
    rows.sort()
    steps  = np.array([r[0] for r in rows], dtype=float)
    values = np.array([r[1] for r in rows], dtype=float)
    return steps, values


def gaussian_smooth(values: np.ndarray, sigma: float) -> np.ndarray:
    if sigma <= 0 or len(values) < 3:
        return values
    from scipy.ndimage import gaussian_filter1d
    return gaussian_filter1d(values, sigma=sigma)


def resample(steps, values, x_new):
    """Linearly interpolate onto x_new; clip to valid range."""
    mask = (x_new >= steps[0]) & (x_new <= steps[-1])
    out  = np.full(len(x_new), np.nan)
    out[mask] = np.interp(x_new[mask], steps, values)
    return out


# ── load all runs ──────────────────────────────────────────────────────────────
run_dirs = []
for name in RUN_NAMES:
    d = os.path.join(RESULTS_DIR, name, AGENT_DIR)
    if os.path.isdir(d):
        run_dirs.append(d)
    else:
        print(f"  [warning] run not found: {d}")

if not run_dirs:
    raise FileNotFoundError(f"No runs found in {RESULTS_DIR}")

print(f"Loaded {len(run_dirs)}/5 runs:")
for d in run_dirs:
    name = os.path.basename(os.path.dirname(d))
    print(f"  {name}  ({SEED_LABELS.get(name, '?')})")

os.makedirs(OUTPUT_DIR, exist_ok=True)

# ── plot each metric ───────────────────────────────────────────────────────────
plt.rcParams.update({
    "font.family":       "serif",
    "font.size":         11,
    "axes.linewidth":    0.8,
    "axes.spines.top":   False,
    "axes.spines.right": False,
    "legend.frameon":    False,
    "figure.dpi":        150,
})

# 5 shades of orange/amber — matches manuscript style
SEED_COLORS = ["#E8A838", "#D4841A", "#F0BF60", "#B86A10", "#F5D080"]

for tag, label in METRICS.items():
    seed_data = []
    for run_dir in run_dirs:
        steps, values = load_scalar(run_dir, tag)
        if steps is None or len(steps) < 5:
            continue
        seed_data.append((steps, values, run_dir))

    if not seed_data:
        print(f"  [skip] {tag} — no data")
        continue

    fig, ax = plt.subplots(figsize=(5.5, 3.2))

    for i, (steps, values, run_dir) in enumerate(seed_data):
        color  = SEED_COLORS[i % len(SEED_COLORS)]
        name   = os.path.basename(os.path.dirname(run_dir))
        slabel = SEED_LABELS.get(name, name)
        ax.plot(steps / 1e6, values,
                color=color, linewidth=0.8, alpha=0.85, zorder=2,
                label=slabel)

    ax.set_xlabel("Training Steps", labelpad=4)
    ax.set_ylabel(label, labelpad=4)
    ax.xaxis.set_major_formatter(ticker.FuncFormatter(
        lambda x, _: f"{int(x)}M" if x >= 1 else f"{x:.1f}M"))
    ax.tick_params(length=3)
    ax.legend(fontsize=8, loc="upper left", ncol=2)

    fig.tight_layout()
    fname = label.lower().replace(" ", "_") + "_multiseed.pdf"
    fig.savefig(os.path.join(OUTPUT_DIR, fname), bbox_inches="tight")
    fname_png = fname.replace(".pdf", ".png")
    fig.savefig(os.path.join(OUTPUT_DIR, fname_png), bbox_inches="tight", dpi=200)
    print(f"  Saved: figures/{fname}  +  figures/{fname_png}")
    plt.close(fig)

# ── combined 2-panel figure (paper style, matches Fig 1) ──────────────────────
fig, axes = plt.subplots(1, 2, figsize=(9, 3.2))

for ax, (tag, label) in zip(axes, list(METRICS.items())[:2]):
    seed_data = []
    for run_dir in run_dirs:
        steps, values = load_scalar(run_dir, tag)
        if steps is None or len(steps) < 5:
            continue
        seed_data.append((steps, values, run_dir))

    if not seed_data:
        continue

    for i, (steps, values, run_dir) in enumerate(seed_data):
        color  = SEED_COLORS[i % len(SEED_COLORS)]
        name   = os.path.basename(os.path.dirname(run_dir))
        slabel = SEED_LABELS.get(name, name)
        ax.plot(steps / 1e6, values,
                color=color, linewidth=0.8, alpha=0.85,
                label=slabel)

    ax.set_xlabel("Training Steps", labelpad=4)
    ax.set_ylabel(label, labelpad=4)
    ax.xaxis.set_major_formatter(ticker.FuncFormatter(
        lambda x, _: f"{int(x)}M" if x >= 1 else f"{x:.1f}M"))
    ax.tick_params(length=3)
    ax.spines["top"].set_visible(False)
    ax.spines["right"].set_visible(False)

axes[0].set_title("(a) Utility scheme: cumulative reward.", fontsize=10, pad=6)
axes[1].set_title("(b) Utility scheme: policy entropy.",   fontsize=10, pad=6)

axes[1].legend(fontsize=7, loc="upper right", ncol=1)
fig.suptitle(
    f"Fig. 1 (updated): Learning diagnostics — {len(run_dirs)} independent training seeds",
    fontsize=9, y=1.02
)

fig.tight_layout()
combined_pdf = os.path.join(OUTPUT_DIR, "fig1_multiseed_combined.pdf")
combined_png = os.path.join(OUTPUT_DIR, "fig1_multiseed_combined.png")
fig.savefig(combined_pdf, bbox_inches="tight")
fig.savefig(combined_png, bbox_inches="tight", dpi=200)
print(f"\nCombined figure saved:\n  {combined_pdf}\n  {combined_png}")
plt.close(fig)
