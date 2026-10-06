"""
Matched MRS ablation: v7.3 with the neighbour-MRS channel vs v7.3 with it zeroed.
Both arms share reward, architecture, hyperparameters and environment settings
(config/sugarscrape_v7_3.yaml vs config/sugarscrape_v7_3_no_mrs.yaml differ only in
disable_neighbor_mrs). Mean +/- 1 SD across training seeds, raw data, no smoothing.
"""

import os, struct, glob
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.ticker as ticker

RESULTS_DIR = os.path.join(os.path.dirname(__file__), "results")
OUTPUT_DIR  = os.path.join(os.path.dirname(__file__), "figures")
AGENT_DIR   = "TradingAgent"

# 7.3.1 was launched without --seed (configuration.yaml records seed: -1)
ARMS = {
    "With MRS":    (["v7_3_s1", "v7_3_s2", "v7_3_s3", "7.3.1", "v7_3_s123"], "#E05A2B"),
    "Without MRS": (["v7_3_no_mrs_s1", "v7_3_no_mrs_s2", "v7_3_no_mrs_s3",
                     "v7_3_no_mrs_s42", "v7_3_no_mrs_s123"], "#2878BD"),
}

METRICS = {
    "Environment/Cumulative Reward": "Cumulative Reward",
    "Environment/Episode Length":    "Episode Length (steps survived)",
    "Policy/Entropy":                "Policy Entropy",
}

FINAL_WINDOW = 500_000   # steps averaged for the end-of-training summary


# ── tfevents reader (no TensorBoard / TF dependency) ──────────────────────────
def _varint(buf, pos):
    val = 0; shift = 0
    while True:
        b = buf[pos]; pos += 1
        val |= (b & 0x7F) << shift
        shift += 7
        if not (b & 0x80):
            return val, pos

def _fields(buf):
    """Yield (field_number, wire_type, value) for one protobuf message."""
    pos = 0
    while pos < len(buf):
        tag, pos = _varint(buf, pos)
        num, wire = tag >> 3, tag & 0x07
        if wire == 0:
            val, pos = _varint(buf, pos)
        elif wire == 1:
            val = buf[pos:pos + 8]; pos += 8
        elif wire == 2:
            ln, pos = _varint(buf, pos)
            val = buf[pos:pos + ln]; pos += ln
        elif wire == 5:
            val = buf[pos:pos + 4]; pos += 4
        else:
            return
        yield num, wire, val

def _read_records(path):
    with open(path, "rb") as f:
        while True:
            hdr = f.read(12)
            if len(hdr) < 12: break
            dl = struct.unpack("<Q", hdr[:8])[0]
            data = f.read(dl); f.read(4)
            if len(data) < dl: break
            yield data

def _parse(path):
    """Return list of (step, tag, value) scalars from one tfevents file."""
    rows = []
    for raw in _read_records(path):
        step = 0; summary = None
        for num, wire, val in _fields(raw):
            if num == 2 and wire == 0: step = val
            elif num == 5 and wire == 2: summary = val
        if summary is None:
            continue
        for num, wire, val in _fields(summary):
            if num != 1 or wire != 2:
                continue
            tag = None; value = None
            for vnum, vwire, vval in _fields(val):
                if vnum == 1 and vwire == 2: tag = vval.decode("utf-8", "replace")
                elif vnum == 2 and vwire == 5: value = struct.unpack("<f", vval)[0]
            if tag is not None and value is not None:
                rows.append((step, tag, value))
    return rows

_cache = {}
def load_scalar(run_name, tag):
    """Return {step: value} for one run / one tag."""
    if run_name not in _cache:
        rows = []
        for f in sorted(glob.glob(os.path.join(RESULTS_DIR, run_name, AGENT_DIR, "events.out.tfevents*"))):
            rows.extend(_parse(f))
        _cache[run_name] = rows
    return {s: v for s, t, v in _cache[run_name] if t == tag}


def load_arm(run_names, tag):
    """Return (steps, values[seed, step]) on the steps shared by every available seed."""
    series = [(name, load_scalar(name, tag)) for name in run_names]
    missing = [name for name, d in series if not d]
    for name in missing:
        print(f"  [warning] no data for run: {name}")
    series = [(name, d) for name, d in series if d]
    if not series:
        return None, None, []
    steps = sorted(set.intersection(*(set(d) for _, d in series)))
    values = np.array([[d[s] for s in steps] for _, d in series])
    return np.array(steps, dtype=float), values, [name for name, _ in series]


def fmt_step(x, _):
    return f"{int(x)}M" if x >= 1 else f"{x:.1f}M"


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

fig, axes = plt.subplots(1, len(METRICS), figsize=(4.6 * len(METRICS), 3.3))

for ax, (tag, label) in zip(axes, METRICS.items()):
    finals = {}
    for arm, (run_names, color) in ARMS.items():
        steps, values, names = load_arm(run_names, tag)
        if steps is None:
            continue
        mean, sd = values.mean(axis=0), values.std(axis=0, ddof=1) if len(names) > 1 else 0
        for row in values:
            ax.plot(steps / 1e6, row, color=color, linewidth=0.5, alpha=0.25, zorder=1)
        ax.fill_between(steps / 1e6, mean - sd, mean + sd, color=color, alpha=0.18, linewidth=0, zorder=2)
        ax.plot(steps / 1e6, mean, color=color, linewidth=1.6, zorder=3,
                label=f"{arm} (n={len(names)} seeds)")

        window = steps > steps[-1] - FINAL_WINDOW
        finals[arm] = values[:, window].mean(axis=1)
        print(f"{label:34s} {arm:12s} last {FINAL_WINDOW // 1000}k steps, to step {int(steps[-1]):>8d}: "
              f"mean {finals[arm].mean():7.3f}  SD {finals[arm].std(ddof=1) if len(names) > 1 else float('nan'):6.3f}  "
              f"per seed {np.round(finals[arm], 3).tolist()}")

    if len(finals) == 2 and all(len(v) > 1 for v in finals.values()):
        from scipy import stats
        a, b = finals["With MRS"], finals["Without MRS"]
        t, p = stats.ttest_ind(a, b, equal_var=False)
        print(f"{label:34s} difference {a.mean() - b.mean():+.3f}  Welch t = {t:.2f}, p = {p:.4f}\n")

    ax.set_xlabel("Training Steps", labelpad=4)
    ax.set_ylabel(label, labelpad=4)
    ax.xaxis.set_major_formatter(ticker.FuncFormatter(fmt_step))
    ax.tick_params(length=3)

axes[0].legend(fontsize=8, loc="upper left")
fig.tight_layout()
out = os.path.join(OUTPUT_DIR, "fig_mrs_ablation_matched")
fig.savefig(out + ".pdf", bbox_inches="tight")
fig.savefig(out + ".png", bbox_inches="tight", dpi=200)
print(f"Saved: {out}.pdf  +  .png")
