"""
Comparison plot: v7_3 (with MRS, survival reward) vs v6_7_no_mrs (without MRS, delta-welfare).

v7_3: mean ± std band across 5 independent seeds.
v6_7_no_mrs: single training run.
"""

import os
import struct
import glob
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.ticker as ticker

# ── configuration ──────────────────────────────────────────────────────────────
RESULTS_DIR = os.path.join(os.path.dirname(__file__), "results")
AGENT_DIR   = "TradingAgent"
OUTPUT_DIR  = os.path.join(os.path.dirname(__file__), "figures")

V73_RUNS = ["v7_3_s1", "v7_3_s2", "v7_3_s3", "7.3.1", "v7_3_s123"]

V67_RUN  = "v6_7_no_mrs"

V73_COLORS = ["#E8A838", "#D4841A", "#F0BF60", "#B86A10", "#F5D080"]  # 5 amber shades
V73_MEAN_COLOR = "#8B4500"   # dark amber – v7.3 mean line
V67_COLOR  = "#2E7BB5"       # steel blue – v6.7 no-MRS

METRICS = {
    "Environment/Cumulative Reward": "Cumulative Reward",
    "Policy/Entropy":                "Policy Entropy",
    "Losses/Policy Loss":            "Policy Loss",
    "Losses/Value Loss":             "Value Loss",
}

N_RESAMPLE = 500   # points to interpolate all curves onto before averaging

# ── tfevents reader (no TensorBoard dependency) ────────────────────────────────
def _read_records(path: str):
    with open(path, "rb") as f:
        while True:
            hdr = f.read(12)               # uint64 len + uint32 crc_len
            if len(hdr) < 12:
                break
            data_len = struct.unpack("<Q", hdr[:8])[0]
            if data_len > 10_000_000:      # sanity: no event should be >10 MB
                break
            data = f.read(data_len)
            f.read(4)                      # crc_data
            if len(data) < data_len:
                break
            yield data


def _parse_scalar_events(path: str):
    results = []
    for raw in _read_records(path):
        pos = 0; step = 0; summary_bytes = None
        while pos < len(raw):
            field_tag = 0; shift = 0
            while True:
                b = raw[pos]; pos += 1
                field_tag |= (b & 0x7F) << shift; shift += 7
                if not (b & 0x80): break
            fn = field_tag >> 3; wt = field_tag & 0x07
            if wt == 0:
                val = 0; shift = 0
                while True:
                    b = raw[pos]; pos += 1
                    val |= (b & 0x7F) << shift; shift += 7
                    if not (b & 0x80): break
                if fn == 2: step = val
            elif wt == 1: pos += 8
            elif wt == 2:
                ln = 0; shift = 0
                while True:
                    b = raw[pos]; pos += 1
                    ln |= (b & 0x7F) << shift; shift += 7
                    if not (b & 0x80): break
                chunk = raw[pos:pos + ln]; pos += ln
                if fn == 5: summary_bytes = chunk
            elif wt == 5: pos += 4
            else: break

        if summary_bytes is None:
            continue
        sp = 0
        while sp < len(summary_bytes):
            ft = 0; shift = 0
            while True:
                b = summary_bytes[sp]; sp += 1
                ft |= (b & 0x7F) << shift; shift += 7
                if not (b & 0x80): break
            fn = ft >> 3; wt = ft & 0x07
            if wt == 2:
                ln = 0; shift = 0
                while True:
                    b = summary_bytes[sp]; sp += 1
                    ln |= (b & 0x7F) << shift; shift += 7
                    if not (b & 0x80): break
                vb = summary_bytes[sp:sp + ln]; sp += ln
                if fn == 1:
                    vp = 0; vtag = None; vval = None
                    while vp < len(vb):
                        ft2 = 0; shift = 0
                        while True:
                            b = vb[vp]; vp += 1
                            ft2 |= (b & 0x7F) << shift; shift += 7
                            if not (b & 0x80): break
                        fn2 = ft2 >> 3; wt2 = ft2 & 0x07
                        if wt2 == 2:
                            ln2 = 0; shift = 0
                            while True:
                                b = vb[vp]; vp += 1
                                ln2 |= (b & 0x7F) << shift; shift += 7
                                if not (b & 0x80): break
                            s = vb[vp:vp + ln2]; vp += ln2
                            if fn2 == 1: vtag = s.decode("utf-8", errors="replace")
                        elif wt2 == 5:
                            vval = struct.unpack("<f", vb[vp:vp + 4])[0]; vp += 4
                        elif wt2 == 0:
                            while True:
                                b = vb[vp]; vp += 1
                                if not (b & 0x80): break
                        else: break
                    if vtag is not None and vval is not None:
                        results.append((step, vtag, vval))
            elif wt == 0:
                while True:
                    b = summary_bytes[sp]; sp += 1
                    if not (b & 0x80): break
            elif wt == 5: sp += 4
            elif wt == 1: sp += 8
            else: break
    return results


def load_scalar(run_dir: str, tag: str):
    files = sorted(glob.glob(os.path.join(run_dir, "events.out.tfevents*")))
    if not files:
        return None, None
    rows = []
    for f in files:
        rows.extend(_parse_scalar_events(f))
    rows = [(s, v) for s, t, v in rows if t == tag]
    if not rows:
        return None, None
    rows.sort()
    steps  = np.array([r[0] for r in rows], dtype=float)
    values = np.array([r[1] for r in rows], dtype=float)
    return steps, values


# ── load data ──────────────────────────────────────────────────────────────────
def load_group(run_names, tag):
    """Return list of (steps, values) for each found run."""
    curves = []
    for name in run_names:
        d = os.path.join(RESULTS_DIR, name, AGENT_DIR)
        steps, values = load_scalar(d, tag)
        if steps is not None and len(steps) >= 5:
            curves.append((steps, values))
        else:
            print(f"  [warn] no data for {name} / {tag}")
    return curves


def band(curves, n=N_RESAMPLE):
    """
    Interpolate all curves to a common grid, return (x, mean, std).
    x_min/x_max uses the range covered by ALL curves (inner join).
    """
    x_min = max(c[0][0]  for c in curves)
    x_max = min(c[0][-1] for c in curves)
    if x_min >= x_max:
        x_min = min(c[0][0]  for c in curves)
        x_max = max(c[0][-1] for c in curves)
    x = np.linspace(x_min, x_max, n)
    mat = []
    for steps, values in curves:
        mat.append(np.interp(x, steps, values))
    mat = np.array(mat)
    return x, mat.mean(axis=0), mat.std(axis=0)


# ── plot style ─────────────────────────────────────────────────────────────────
plt.rcParams.update({
    "font.family":       "serif",
    "font.size":         11,
    "axes.linewidth":    0.8,
    "axes.spines.top":   False,
    "axes.spines.right": False,
    "legend.frameon":    False,
    "figure.dpi":        150,
})

os.makedirs(OUTPUT_DIR, exist_ok=True)


SEED_LABELS = {
    "v7_3_s1":   "v7.3 seed 1",
    "v7_3_s2":   "v7.3 seed 2",
    "v7_3_s3":   "v7.3 seed 3",
    "7.3.1":     "v7.3 seed 42",
    "v7_3_s123": "v7.3 seed 123",
}

SMOOTH_SIGMA = 2   # match TensorBoard's default smoothing appearance


def fmt_step(x, _):
    return f"{int(x)}M" if x >= 1 else f"{x:.1f}M"


def smooth(values, sigma=SMOOTH_SIGMA):
    if sigma <= 0 or len(values) < 3:
        return values
    from scipy.ndimage import gaussian_filter1d
    return gaussian_filter1d(values.astype(float), sigma=sigma)


def draw_panel(ax, tag, label, v73_curves, v73_run_names,
               v67_steps, v67_values, show_legend=True):
    """Render one subplot: individual v7.3 seeds (thin) + mean (thick) + v6.7 (thick blue)."""
    # ── individual v7.3 seeds (thin, raw as faded; smoothed as solid) ──
    if v73_curves:
        for i, ((steps, values), name) in enumerate(zip(v73_curves, v73_run_names)):
            color = V73_COLORS[i % len(V73_COLORS)]
            slabel = SEED_LABELS.get(name, name)
            # faded raw line
            ax.plot(steps / 1e6, values,
                    color=color, linewidth=0.6, alpha=0.25, zorder=1)
            # smoothed solid line
            ax.plot(steps / 1e6, smooth(values),
                    color=color, linewidth=1.0, alpha=0.75, zorder=2,
                    label=slabel)

        # ── v7.3 mean (thick, dark amber) ──
        x73, mu73, _ = band(v73_curves)
        ax.plot(x73 / 1e6, smooth(mu73),
                color=V73_MEAN_COLOR, linewidth=2.2, zorder=4,
                label="v7.3 — with MRS (mean, 5 seeds)")

    # ── v6.7 no-MRS (thick blue; faded raw + solid smooth) ──
    if v67_values is not None:
        ax.plot(v67_steps / 1e6, v67_values,
                color=V67_COLOR, linewidth=0.6, alpha=0.25, zorder=1)
        ax.plot(v67_steps / 1e6, smooth(v67_values),
                color=V67_COLOR, linewidth=2.2, zorder=4,
                label="v6.7 — without MRS\n(delta-welfare reward)")

    ax.set_xlabel("Training Steps (millions)", labelpad=4)
    ax.set_ylabel(label, labelpad=4)
    ax.xaxis.set_major_formatter(ticker.FuncFormatter(fmt_step))
    ax.tick_params(length=3)
    ax.spines["top"].set_visible(False)
    ax.spines["right"].set_visible(False)
    if show_legend:
        ax.legend(fontsize=7.5, loc="best", ncol=1)


# ── pre-load all curves once ───────────────────────────────────────────────────
all_tag_data = {}
for tag in METRICS:
    v73_curves, v73_names = [], []
    for name in V73_RUNS:
        d = os.path.join(RESULTS_DIR, name, AGENT_DIR)
        steps, values = load_scalar(d, tag)
        if steps is not None and len(steps) >= 5:
            v73_curves.append((steps, values))
            v73_names.append(name)
        else:
            print(f"  [warn] {name} missing {tag}")
    v67_steps, v67_values = load_scalar(
        os.path.join(RESULTS_DIR, V67_RUN, AGENT_DIR), tag)
    all_tag_data[tag] = (v73_curves, v73_names, v67_steps, v67_values)


# ── per-metric individual PNGs ─────────────────────────────────────────────────
for tag, label in METRICS.items():
    v73_curves, v73_names, v67_steps, v67_values = all_tag_data[tag]
    if not v73_curves and v67_values is None:
        print(f"  [skip] {tag}"); continue

    fig, ax = plt.subplots(figsize=(6.5, 3.8))
    draw_panel(ax, tag, label, v73_curves, v73_names, v67_steps, v67_values)
    fig.tight_layout()
    slug = label.lower().replace(" ", "_")
    png_path = os.path.join(OUTPUT_DIR, f"{slug}_mrs_comparison.png")
    fig.savefig(png_path, bbox_inches="tight", dpi=200)
    print(f"  Saved: figures/{os.path.basename(png_path)}")
    plt.close(fig)


# ── 2-panel combined figure (reward + entropy) ────────────────────────────────
panel_metrics = [
    ("Environment/Cumulative Reward", "Cumulative Reward"),
    ("Policy/Entropy",                "Policy Entropy"),
]

fig, axes = plt.subplots(1, 2, figsize=(12, 4.0))

for i, (ax, (tag, label)) in enumerate(zip(axes, panel_metrics)):
    v73_curves, v73_names, v67_steps, v67_values = all_tag_data[tag]
    draw_panel(ax, tag, label, v73_curves, v73_names, v67_steps, v67_values,
               show_legend=(i == 1))

axes[0].set_title("(a) Cumulative Reward", fontsize=10, pad=6)
axes[1].set_title("(b) Policy Entropy",    fontsize=10, pad=6)

fig.suptitle(
    "Training comparison: with MRS observation (v7.3, survival reward) "
    "vs. without MRS observation (v6.7, delta-welfare reward)",
    fontsize=9.5, y=1.03
)
fig.tight_layout()
combined_png = os.path.join(OUTPUT_DIR, "fig_mrs_comparison_combined.png")
fig.savefig(combined_png, bbox_inches="tight", dpi=200)
print(f"\nCombined figure saved: figures/fig_mrs_comparison_combined.png")
plt.close(fig)


# ── 4-panel combined figure (all metrics) ─────────────────────────────────────
fig, axes = plt.subplots(2, 2, figsize=(13, 8))

for i, (ax, (tag, label)) in enumerate(zip(axes.flat, METRICS.items())):
    v73_curves, v73_names, v67_steps, v67_values = all_tag_data[tag]
    draw_panel(ax, tag, label, v73_curves, v73_names, v67_steps, v67_values,
               show_legend=(i == 0))

axes[0, 0].set_title("(a) Cumulative Reward", fontsize=10, pad=6)
axes[0, 1].set_title("(b) Policy Entropy",    fontsize=10, pad=6)
axes[1, 0].set_title("(c) Policy Loss",       fontsize=10, pad=6)
axes[1, 1].set_title("(d) Value Loss",        fontsize=10, pad=6)

fig.suptitle(
    "Training comparison: with MRS observation (v7.3, survival reward) "
    "vs. without MRS observation (v6.7, delta-welfare reward)",
    fontsize=10, y=1.01
)
fig.tight_layout()
all4_png = os.path.join(OUTPUT_DIR, "fig_mrs_comparison_all4.png")
fig.savefig(all4_png, bbox_inches="tight", dpi=200)
print(f"4-panel figure saved:  figures/fig_mrs_comparison_all4.png")
plt.close(fig)
