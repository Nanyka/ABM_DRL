"""
Multi-seed training curves for paper figures.
Produces mean ± std shading across all v7_3_s* runs.
"""

import os
import glob
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.ticker as ticker
from tensorboard.backend.event_processing.event_accumulator import EventAccumulator

# ── configuration ──────────────────────────────────────────────────────────────
RESULTS_DIR = os.path.join(os.path.dirname(__file__), "results")
RUN_PATTERN = "v7_3_s*"          # matches v7_3_s1, v7_3_s2, v7_3_s3, v7_3_s123, etc.
AGENT_DIR   = "TradingAgent"

METRICS = {
    "Environment/Cumulative Reward": "Cumulative Reward",
    "Policy/Entropy":                "Policy Entropy",
    "Losses/Policy Loss":            "Policy Loss",
    "Losses/Value Loss":             "Value Loss",
}

# smoothing window (Gaussian σ in steps); set to 0 to disable
SMOOTH_SIGMA = 30
# number of points to resample all runs onto
N_POINTS     = 500
OUTPUT_DIR   = os.path.join(os.path.dirname(__file__), "figures")


# ── helpers ────────────────────────────────────────────────────────────────────
def load_scalar(run_dir: str, tag: str):
    """Return (steps, values) arrays for one run / one tag."""
    ea = EventAccumulator(run_dir, size_guidance={"scalars": 0})
    ea.Reload()
    if tag not in ea.Tags()["scalars"]:
        return None, None
    events = ea.Scalars(tag)
    steps  = np.array([e.step  for e in events], dtype=float)
    values = np.array([e.value for e in events], dtype=float)
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
run_dirs = sorted(glob.glob(os.path.join(RESULTS_DIR, RUN_PATTERN, AGENT_DIR)))
if not run_dirs:
    raise FileNotFoundError(f"No runs found matching {RUN_PATTERN} in {RESULTS_DIR}")

print(f"Found {len(run_dirs)} runs:")
for d in run_dirs:
    print(f"  {os.path.basename(os.path.dirname(d))}")

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

SEED_COLORS = ["#E07B54", "#5B9BD5", "#A8C878", "#C084B4", "#73BFB8"]
MEAN_COLOR  = "#2C3E7A"
BAND_COLOR  = "#7B9ED9"

for tag, label in METRICS.items():
    all_steps  = []
    all_values = []

    for run_dir in run_dirs:
        steps, values = load_scalar(run_dir, tag)
        if steps is None or len(steps) < 5:
            continue
        all_steps.append(steps)
        all_values.append(values)

    if not all_steps:
        print(f"  [skip] {tag} — no data")
        continue

    # common x grid
    x_min = max(s[0]  for s in all_steps)
    x_max = min(s[-1] for s in all_steps)
    x_grid = np.linspace(x_min, x_max, N_POINTS)

    # resample and smooth every seed
    mat = []
    for steps, values in zip(all_steps, all_values):
        v = resample(steps, values, x_grid)
        v = gaussian_smooth(v, SMOOTH_SIGMA)
        mat.append(v)
    mat = np.array(mat)          # (n_seeds, N_POINTS)

    mean = np.nanmean(mat, axis=0)
    std  = np.nanstd(mat, axis=0)

    fig, ax = plt.subplots(figsize=(5.5, 3.2))

    # individual seed traces (faint)
    for i, row in enumerate(mat):
        color = SEED_COLORS[i % len(SEED_COLORS)]
        ax.plot(x_grid / 1e6, row,
                color=color, alpha=0.25, linewidth=0.9, zorder=1)

    # mean ± std band
    ax.fill_between(x_grid / 1e6,
                    mean - std, mean + std,
                    color=BAND_COLOR, alpha=0.30, linewidth=0, zorder=2)

    # mean line
    ax.plot(x_grid / 1e6, mean,
            color=MEAN_COLOR, linewidth=1.8, zorder=3,
            label=f"Mean (n={len(mat)})")

    ax.set_xlabel("Training Steps (×10⁶)", labelpad=4)
    ax.set_ylabel(label, labelpad=4)
    ax.xaxis.set_major_formatter(ticker.FormatStrFormatter("%.1f"))
    ax.tick_params(length=3)
    ax.legend(fontsize=9, loc="upper left")
    ax.set_title(f"{label} — {len(mat)} independent seeds", fontsize=10, pad=6)

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
    all_steps, all_values = [], []
    for run_dir in run_dirs:
        steps, values = load_scalar(run_dir, tag)
        if steps is None or len(steps) < 5:
            continue
        all_steps.append(steps)
        all_values.append(values)

    if not all_steps:
        continue

    x_min  = max(s[0]  for s in all_steps)
    x_max  = min(s[-1] for s in all_steps)
    x_grid = np.linspace(x_min, x_max, N_POINTS)

    mat = []
    for steps, values in zip(all_steps, all_values):
        v = resample(steps, values, x_grid)
        v = gaussian_smooth(v, SMOOTH_SIGMA)
        mat.append(v)
    mat = np.array(mat)

    mean = np.nanmean(mat, axis=0)
    std  = np.nanstd(mat, axis=0)

    for i, row in enumerate(mat):
        ax.plot(x_grid / 1e6, row,
                color=SEED_COLORS[i % len(SEED_COLORS)],
                alpha=0.20, linewidth=0.8, zorder=1)

    ax.fill_between(x_grid / 1e6, mean - std, mean + std,
                    color=BAND_COLOR, alpha=0.30, linewidth=0, zorder=2)
    ax.plot(x_grid / 1e6, mean,
            color=MEAN_COLOR, linewidth=1.8, zorder=3)

    ax.set_xlabel("Training Steps (×10⁶)", labelpad=4)
    ax.set_ylabel(label, labelpad=4)
    ax.xaxis.set_major_formatter(ticker.FormatStrFormatter("%.1f"))
    ax.tick_params(length=3)
    ax.spines["top"].set_visible(False)
    ax.spines["right"].set_visible(False)

axes[0].set_title("(a) Utility scheme: cumulative reward.", fontsize=10, pad=6)
axes[1].set_title("(b) Utility scheme: policy entropy.",   fontsize=10, pad=6)

fig.suptitle(
    f"Fig. 1 (updated): Learning diagnostics — {len(run_dirs)} independent training seeds\n"
    f"Mean (dark) ± 1 std (shaded), individual seeds (faint)",
    fontsize=9, y=1.02
)

fig.tight_layout()
combined_pdf = os.path.join(OUTPUT_DIR, "fig1_multiseed_combined.pdf")
combined_png = os.path.join(OUTPUT_DIR, "fig1_multiseed_combined.png")
fig.savefig(combined_pdf, bbox_inches="tight")
fig.savefig(combined_png, bbox_inches="tight", dpi=200)
print(f"\nCombined figure saved:\n  {combined_pdf}\n  {combined_png}")
plt.close(fig)
