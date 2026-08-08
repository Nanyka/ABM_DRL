"""
Simple 2-line comparison: v7.3 seed 42 (With MRS) vs v6.7 no-MRS (Without MRS).
Raw data, no smoothing — same style as plot_multiseed.py.
"""

import os, struct, glob
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.ticker as ticker

RESULTS_DIR = os.path.join(os.path.dirname(__file__), "results")
OUTPUT_DIR  = os.path.join(os.path.dirname(__file__), "figures")
AGENT_DIR   = "TradingAgent"

RUNS = {
    "7.3.1":       ("With MRS",    "#E05A2B"),   # orange-red
    "v6_7_no_mrs": ("Without MRS", "#2878BD"),   # blue
}

METRICS = {
    "Environment/Cumulative Reward": "Cumulative Reward",
    "Policy/Entropy":                "Policy Entropy",
    "Losses/Policy Loss":            "Policy Loss",
    "Losses/Value Loss":             "Value Loss",
}


# ── tfevents reader ────────────────────────────────────────────────────────────
def _read_records(path):
    with open(path, "rb") as f:
        while True:
            hdr = f.read(12)
            if len(hdr) < 12: break
            dl = struct.unpack("<Q", hdr[:8])[0]
            if dl > 10_000_000: break
            data = f.read(dl); f.read(4)
            if len(data) < dl: break
            yield data


def _parse(path):
    out = []
    for raw in _read_records(path):
        pos = 0; step = 0; sb = None
        while pos < len(raw):
            ft = 0; sh = 0
            while True:
                b = raw[pos]; pos += 1; ft |= (b & 0x7F) << sh; sh += 7
                if not (b & 0x80): break
            fn = ft >> 3; wt = ft & 7
            if wt == 0:
                v = 0; sh = 0
                while True:
                    b = raw[pos]; pos += 1; v |= (b & 0x7F) << sh; sh += 7
                    if not (b & 0x80): break
                if fn == 2: step = v
            elif wt == 1: pos += 8
            elif wt == 2:
                ln = 0; sh = 0
                while True:
                    b = raw[pos]; pos += 1; ln |= (b & 0x7F) << sh; sh += 7
                    if not (b & 0x80): break
                chunk = raw[pos:pos + ln]; pos += ln
                if fn == 5: sb = chunk
            elif wt == 5: pos += 4
            else: break
        if sb is None: continue
        sp = 0
        while sp < len(sb):
            ft = 0; sh = 0
            while True:
                b = sb[sp]; sp += 1; ft |= (b & 0x7F) << sh; sh += 7
                if not (b & 0x80): break
            fn = ft >> 3; wt = ft & 7
            if wt == 2:
                ln = 0; sh = 0
                while True:
                    b = sb[sp]; sp += 1; ln |= (b & 0x7F) << sh; sh += 7
                    if not (b & 0x80): break
                vb = sb[sp:sp + ln]; sp += ln
                if fn == 1:
                    vp = 0; vtag = None; vval = None
                    while vp < len(vb):
                        ft2 = 0; sh = 0
                        while True:
                            b = vb[vp]; vp += 1; ft2 |= (b & 0x7F) << sh; sh += 7
                            if not (b & 0x80): break
                        fn2 = ft2 >> 3; wt2 = ft2 & 7
                        if wt2 == 2:
                            ln2 = 0; sh = 0
                            while True:
                                b = vb[vp]; vp += 1; ln2 |= (b & 0x7F) << sh; sh += 7
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
                    if vtag and vval is not None:
                        out.append((step, vtag, vval))
            elif wt == 0:
                while True:
                    b = sb[sp]; sp += 1
                    if not (b & 0x80): break
            elif wt == 5: sp += 4
            elif wt == 1: sp += 8
            else: break
    return out


def load_scalar(run_name, tag):
    d = os.path.join(RESULTS_DIR, run_name, AGENT_DIR)
    files = sorted(glob.glob(os.path.join(d, "events.out.tfevents*")))
    rows = []
    for f in files:
        rows.extend(_parse(f))
    rows = sorted((s, v) for s, t, v in rows if t == tag)
    if not rows:
        return None, None
    steps  = np.array([r[0] for r in rows], dtype=float)
    values = np.array([r[1] for r in rows], dtype=float)
    return steps, values


def fmt_step(x, _):
    return f"{int(x)}M" if x >= 1 else f"{x:.1f}M"


# ── style ──────────────────────────────────────────────────────────────────────
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


# ── per-metric individual PNGs ─────────────────────────────────────────────────
for tag, label in METRICS.items():
    fig, ax = plt.subplots(figsize=(5.8, 3.4))
    all_vals = []
    for run_name, (run_label, color) in RUNS.items():
        steps, values = load_scalar(run_name, tag)
        if steps is None: continue
        ax.plot(steps / 1e6, values, color=color, linewidth=0.9, alpha=0.9,
                label=run_label)
        all_vals.append(values)
    if all_vals:
        combined = np.concatenate(all_vals)
        lo, hi = np.percentile(combined, 2), np.percentile(combined, 98)
        pad = (hi - lo) * 0.10
        ax.set_ylim(lo - pad, hi + pad)
    ax.set_xlabel("Training Steps (millions)", labelpad=4)
    ax.set_ylabel(label, labelpad=4)
    ax.xaxis.set_major_formatter(ticker.FuncFormatter(fmt_step))
    ax.tick_params(length=3)
    ax.legend(fontsize=9, loc="best")

    fig.tight_layout()
    slug = label.lower().replace(" ", "_")
    out = os.path.join(OUTPUT_DIR, f"{slug}_mrs_simple.png")
    fig.savefig(out, bbox_inches="tight", dpi=200)
    print(f"  Saved: figures/{os.path.basename(out)}")
    plt.close(fig)


# ── 4-panel combined ───────────────────────────────────────────────────────────
fig, axes = plt.subplots(2, 2, figsize=(11, 7))
panel_titles = ["(a) Cumulative Reward", "(b) Policy Entropy",
                "(c) Policy Loss",       "(d) Value Loss"]

for ax, (tag, label), title in zip(axes.flat, METRICS.items(), panel_titles):
    all_vals = []
    for run_name, (run_label, color) in RUNS.items():
        steps, values = load_scalar(run_name, tag)
        if steps is None: continue
        ax.plot(steps / 1e6, values, color=color, linewidth=0.9, alpha=0.9,
                label=run_label)
        all_vals.append(values)
    # clip y-axis to 2nd–98th percentile to remove rare spikes (matches TensorBoard)
    if all_vals:
        combined = np.concatenate(all_vals)
        lo, hi = np.percentile(combined, 2), np.percentile(combined, 98)
        pad = (hi - lo) * 0.10
        ax.set_ylim(lo - pad, hi + pad)
    ax.set_xlabel("Training Steps (millions)", labelpad=4)
    ax.set_ylabel(label, labelpad=4)
    ax.xaxis.set_major_formatter(ticker.FuncFormatter(fmt_step))
    ax.tick_params(length=3)
    ax.set_title(title, fontsize=10, pad=6)
    ax.spines["top"].set_visible(False)
    ax.spines["right"].set_visible(False)
    ax.legend(fontsize=8.5, loc="best")

fig.suptitle(
    "Training comparison: With MRS observation vs. Without MRS observation",
    fontsize=10, y=1.01
)
fig.tight_layout()
out = os.path.join(OUTPUT_DIR, "fig_mrs_simple_all4.png")
fig.savefig(out, bbox_inches="tight", dpi=200)
print(f"  Saved: figures/fig_mrs_simple_all4.png")
plt.close(fig)


# ── 2-panel (reward + entropy) ─────────────────────────────────────────────────
fig, axes = plt.subplots(1, 2, figsize=(11, 3.8))
for ax, (tag, label), title in zip(
        axes,
        list(METRICS.items())[:2],
        ["(a) Cumulative Reward", "(b) Policy Entropy"]):
    all_vals = []
    for run_name, (run_label, color) in RUNS.items():
        steps, values = load_scalar(run_name, tag)
        if steps is None: continue
        ax.plot(steps / 1e6, values, color=color, linewidth=0.9, alpha=0.9,
                label=run_label)
        all_vals.append(values)
    if all_vals:
        combined = np.concatenate(all_vals)
        lo, hi = np.percentile(combined, 2), np.percentile(combined, 98)
        pad = (hi - lo) * 0.10
        ax.set_ylim(lo - pad, hi + pad)
    ax.set_xlabel("Training Steps (millions)", labelpad=4)
    ax.set_ylabel(label, labelpad=4)
    ax.xaxis.set_major_formatter(ticker.FuncFormatter(fmt_step))
    ax.tick_params(length=3)
    ax.set_title(title, fontsize=10, pad=6)
    ax.spines["top"].set_visible(False)
    ax.spines["right"].set_visible(False)
    ax.legend(fontsize=9, loc="best")

fig.suptitle(
    "Training comparison: With MRS observation vs. Without MRS observation",
    fontsize=10, y=1.03
)
fig.tight_layout()
out = os.path.join(OUTPUT_DIR, "fig_mrs_simple_2panel.png")
fig.savefig(out, bbox_inches="tight", dpi=200)
print(f"  Saved: figures/fig_mrs_simple_2panel.png")
plt.close(fig)
