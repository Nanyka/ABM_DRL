"""
Direct 1-vs-1 comparison matching TensorBoard's view:
  v7.3.1 (with MRS, survival reward, seed 42) vs v6_7_no_mrs (without MRS, delta-welfare)

Both shown with:
  - faded thin line = raw data (like TensorBoard's light background trace)
  - solid thick line = EMA-smoothed (TensorBoard default factor 0.6)
"""

import os, struct, glob
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.ticker as ticker

RESULTS_DIR = os.path.join(os.path.dirname(__file__), "results")
OUTPUT_DIR  = os.path.join(os.path.dirname(__file__), "figures")
AGENT_DIR   = "TradingAgent"

V73_RUN  = "7.3.1"       # the original v7.3 run shown in TensorBoard
V67_RUN  = "v6_7_no_mrs"

V73_COLOR = "#E05A2B"    # TensorBoard-like orange-red
V67_COLOR = "#2878BD"    # TensorBoard-like blue

TB_SMOOTH = 0.85         # TensorBoard EMA smoothing — matches slider ~0.6 appearance

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


def tb_smooth(values, factor=TB_SMOOTH):
    """
    TensorBoard's exact EMA algorithm: accumulator starts at 0 (not values[0])
    with debias correction. This matches TensorBoard's visual output.
    """
    smoothed = np.empty_like(values, dtype=float)
    acc = 0.0
    for i, v in enumerate(values):
        acc = factor * acc + (1 - factor) * float(v)
        debias = 1.0 - factor ** (i + 1)
        smoothed[i] = acc / debias
    return smoothed


def fmt_step(x, _):
    return f"{int(x)}M" if x >= 1 else f"{x:.1f}M"


# ── styling ────────────────────────────────────────────────────────────────────
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


def draw(ax, tag, label, show_legend=True):
    v73_steps, v73_vals = load_scalar(V73_RUN, tag)
    v67_steps, v67_vals = load_scalar(V67_RUN, tag)

    smoothed_all = []
    for steps, vals, color, name in [
        (v73_steps, v73_vals, V73_COLOR, "v7.3 — with MRS (survival reward)"),
        (v67_steps, v67_vals, V67_COLOR, "v6.7 — without MRS (delta-welfare reward)"),
    ]:
        if steps is None: continue
        sm = tb_smooth(vals)
        smoothed_all.append(sm)
        # faded raw trace
        ax.plot(steps / 1e6, vals, color=color, linewidth=0.6, alpha=0.18, zorder=1)
        # smoothed solid line
        ax.plot(steps / 1e6, sm, color=color, linewidth=2.0,
                alpha=0.95, zorder=3, label=name)

    # y-axis: scale to the smooth lines + 10% padding (ignore raw spikes)
    if smoothed_all:
        combined = np.concatenate(smoothed_all)
        ymin, ymax = combined.min(), combined.max()
        pad = (ymax - ymin) * 0.12
        ax.set_ylim(ymin - pad, ymax + pad)

    ax.set_xlabel("Training Steps (millions)", labelpad=4)
    ax.set_ylabel(label, labelpad=4)
    ax.xaxis.set_major_formatter(ticker.FuncFormatter(fmt_step))
    ax.tick_params(length=3)
    if show_legend:
        ax.legend(fontsize=9, loc="best")


# ── 4-panel ────────────────────────────────────────────────────────────────────
fig, axes = plt.subplots(2, 2, figsize=(12, 7.5))
titles = ["(a) Cumulative Reward", "(b) Policy Entropy",
          "(c) Policy Loss",       "(d) Value Loss"]

for i, (ax, (tag, label)) in enumerate(zip(axes.flat, METRICS.items())):
    draw(ax, tag, label, show_legend=(i == 0))
    ax.set_title(titles[i], fontsize=10, pad=6)

fig.suptitle(
    "v7.3 (with MRS observation, survival reward)  vs  "
    "v6.7 (without MRS observation, delta-welfare reward)",
    fontsize=10, y=1.01
)
fig.tight_layout()
p = os.path.join(OUTPUT_DIR, "fig_direct_comparison_all4.png")
fig.savefig(p, bbox_inches="tight", dpi=200)
print(f"Saved: figures/fig_direct_comparison_all4.png")
plt.close(fig)


# ── 2-panel (reward + entropy) ─────────────────────────────────────────────────
fig, axes = plt.subplots(1, 2, figsize=(12, 4.2))
for i, (ax, (tag, label), title) in enumerate(zip(
        axes,
        list(METRICS.items())[:2],
        ["(a) Cumulative Reward", "(b) Policy Entropy"])):
    draw(ax, tag, label, show_legend=(i == 1))
    ax.set_title(title, fontsize=10, pad=6)

fig.suptitle(
    "v7.3 (with MRS observation, survival reward)  vs  "
    "v6.7 (without MRS observation, delta-welfare reward)",
    fontsize=10, y=1.03
)
fig.tight_layout()
p = os.path.join(OUTPUT_DIR, "fig_direct_comparison_2panel.png")
fig.savefig(p, bbox_inches="tight", dpi=200)
print(f"Saved: figures/fig_direct_comparison_2panel.png")
plt.close(fig)
