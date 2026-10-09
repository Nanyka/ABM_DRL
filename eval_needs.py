"""
Outcome-level evaluation of trained policies in the complementary-needs setting (config/needs), seven-action build.

Each policy is run for the same evaluation episodes; the Unity side draws layouts and agent types from an engine RNG
stream the policy never touches (actions are sampled here), so episode k starts identically for every policy.
A fingerprint of the initial holdings and metabolisms is stored per episode so this can be checked.

  .venv_train/bin/python eval_needs.py run   <run_name> [--episodes 10] [--port 7800]
  .venv_train/bin/python eval_needs.py table <run_name> <run_name> ...
"""

import argparse, hashlib, json, os
import numpy as np

ROOT = os.path.dirname(os.path.abspath(__file__))
RESULTS_DIR = os.path.join(ROOT, "results")
OUT_DIR = os.path.join(RESULTS_DIR, "needs_eval")
ENV_PATH = os.path.join(ROOT, "Builds", "sugarscape_train_server_v9", "ABM_DRL")
BEHAVIOR_DIR = "TradingAgent"


def gini(x):
    x = np.sort(np.asarray(x, dtype=float))
    n = len(x)
    if n == 0 or x.sum() == 0:
        return float("nan")
    return float((2 * np.arange(1, n + 1) - n - 1).dot(x) / (n * x.sum()))


def run(run_name, episodes, port, scripted_actions=True):
    import torch, yaml
    from mlagents_envs.environment import UnityEnvironment
    from mlagents_envs.base_env import ActionTuple
    from mlagents_envs.side_channel.environment_parameters_channel import EnvironmentParametersChannel
    from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel
    from mlagents_envs.side_channel.stats_side_channel import StatsSideChannel
    from mlagents.trainers.settings import NetworkSettings
    from mlagents.trainers.torch_entities.networks import SharedActorCritic

    run_dir = os.path.join(RESULTS_DIR, run_name)
    config = yaml.safe_load(open(os.path.join(run_dir, "configuration.yaml")))
    net_cfg = config["behaviors"]["TradingAgent"]["network_settings"]

    params, stats, engine = EnvironmentParametersChannel(), StatsSideChannel(), EngineConfigurationChannel()
    env = UnityEnvironment(file_name=ENV_PATH, seed=1, base_port=port, no_graphics=True,
                           side_channels=[params, stats, engine], additional_args=["--seek-action"] if scripted_actions else [])
    engine.set_configuration_parameters(time_scale=20)
    for key, spec in config["environment_parameters"].items():
        params.set_float_parameter(key, float(spec["curriculum"][0]["value"]["sampler_parameters"]["value"]))
    env.reset()

    name = list(env.behavior_specs)[0]
    spec = env.behavior_specs[name]
    n_actions = spec.action_spec.discrete_branches[0]
    actor = SharedActorCritic(
        spec.observation_specs,
        NetworkSettings(normalize=net_cfg["normalize"], hidden_units=net_cfg["hidden_units"],
                        num_layers=net_cfg["num_layers"]),
        spec.action_spec, stream_names=["extrinsic"])
    actor.load_state_dict(torch.load(os.path.join(run_dir, BEHAVIOR_DIR, "checkpoint.pt"), map_location="cpu")["Policy"])
    actor.eval()
    # the 11-value vector observation starts with sugar, spice, sugar metabolism, spice metabolism
    vec = next(i for i, o in enumerate(spec.observation_specs) if o.shape == (11,))

    records, ep, cohort = [], None, set()
    index = -1  # episode 0 runs before the environment parameters apply, so it is discarded

    def close_episode(pending):
        if ep is None:
            return
        ep["trades"] += len(pending.get("Trade/Trades", []))
        if index >= 1:
            horizon = len(ep["alive"])
            life = np.array([ep["ticks"][a] for a in ep["ticks"]])
            survivors = [a for a, n in ep["ticks"].items() if n >= horizon]
            wealth, welfare = [], []
            for a in survivors:
                su, sp, ms, mp = ep["last"][a]
                wealth.append(su + sp)
                share = ms / (ms + mp)
                welfare.append(float(max(su, 0) ** share * max(sp, 0) ** (1 - share)))
            records.append({
                "episode": index, "ticks": horizon, "agents": len(life),
                "fingerprint": ep["fingerprint"],
                "survivors": len(survivors),
                "mean_ticks_alive": float(life.mean()),
                "alive": ep["alive"],
                "trades": ep["trades"],
                "seek_rate": float(np.mean(ep["seek"])) if ep["seek"] else float("nan"),
                "forage_rate": float(np.mean(ep["forage"])) if ep["forage"] else float("nan"),
                "seek_found_partner": float(np.mean(ep["found"])) if ep["found"] else float("nan"),
                "consumption": float(sum(ep["ticks"][a] * (ep["metab"][a][0] + ep["metab"][a][1]) for a in ep["ticks"])),
                "total_welfare_end": float(np.sum(welfare)),
                "mean_welfare_end": float(np.mean(welfare)) if welfare else 0.0,
                "gini_wealth_end": gini(wealth),
            })
            print(f"{run_name} episode {index}: {len(survivors)}/{len(life)} survive", flush=True)

    try:
        while len(records) < episodes:
            decision, _ = env.get_steps(name)
            step_stats = stats.get_and_reset_stats()
            ids = [int(i) for i in decision.agent_id]
            if ids and not cohort.issuperset(ids):  # a fresh set of agents: the previous episode is over
                close_episode(step_stats)
                step_stats = {}
                index += 1
                cohort = set(ids)
                start = decision.obs[vec][:, :4]
                ep = {"fingerprint": hashlib.sha1(np.ascontiguousarray(start).tobytes()).hexdigest()[:12],
                      "ticks": {a: 0 for a in ids}, "metab": {}, "last": {}, "alive": [], "trades": 0,
                      "seek": [], "forage": [], "found": []}
                torch.manual_seed(index)  # same action-noise stream for every policy on episode k
            if ep is not None:
                ep["trades"] += len(step_stats.get("Trade/Trades", []))
                ep["seek"].extend(v for v, _ in step_stats.get("Action/Seek", []))
                ep["forage"].extend(v for v, _ in step_stats.get("Action/Forage", []))
                ep["found"].extend(v for v, _ in step_stats.get("Action/SeekFoundPartner", []))
                if ids:
                    ep["alive"].append(len(ids))
                    for a, row in zip(ids, decision.obs[vec]):
                        ep["ticks"][a] += 1
                        ep["metab"][a] = (float(row[2]), float(row[3]))
                        ep["last"][a] = (float(row[0]), float(row[1]), float(row[2]), float(row[3]))
            if ids:
                with torch.no_grad():
                    action, _, _ = actor.get_action_and_stats(
                        [torch.as_tensor(o) for o in decision.obs], masks=torch.ones(len(ids), n_actions))
                env.set_actions(name, ActionTuple(discrete=action.discrete_tensor.numpy().reshape(len(ids), 1)))
            env.step()
    finally:
        env.close()

    os.makedirs(OUT_DIR, exist_ok=True)
    out = os.path.join(OUT_DIR, f"{run_name}.json")
    json.dump({"run": run_name, "episodes": records}, open(out, "w"))
    print(f"{run_name}: {len(records)} episodes -> {out}")


def table(run_names):
    from scipy import stats as st
    data = {r: json.load(open(os.path.join(OUT_DIR, f"{r}.json")))["episodes"] for r in run_names}
    metrics = [("survivors", "Survivors at end"), ("mean_ticks_alive", "Mean steps alive"),
               ("trades", "Trades per episode"), ("forage_rate", "Share of forage-action choices"),
               ("seek_rate", "Share of seek-action choices"), ("seek_found_partner", "Seek action found a partner"), ("consumption", "Consumption"),
               ("total_welfare_end", "Total welfare at end"), ("mean_welfare_end", "Mean welfare per survivor"),
               ("gini_wealth_end", "Gini of wealth at end")]
    print(f"{'':30s}" + "".join(f"{r[-24:]:>28s}" for r in run_names) + ("   paired p" if len(run_names) == 2 else ""))
    for key, label in metrics:
        cols = [np.array([e[key] for e in data[r]], dtype=float) for r in run_names]
        line = f"{label:30s}" + "".join(f"{np.nanmean(c):16.2f} ± {np.nanstd(c, ddof=1):<9.2f}" for c in cols)
        if len(cols) == 2 and len(cols[0]) == len(cols[1]) and not np.isnan(cols[0]).any() and not np.isnan(cols[1]).any():
            line += f"   {st.ttest_rel(cols[0], cols[1]).pvalue:.1e}"
        print(line)
    ref = [e["fingerprint"] for e in data[run_names[0]]]
    for r in run_names[1:]:
        same = sum(a == b["fingerprint"] for a, b in zip(ref, data[r]))
        print(f"identical starting conditions: {same} of {min(len(ref), len(data[r]))} episodes")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["run", "table"])
    parser.add_argument("runs", nargs="+")
    parser.add_argument("--episodes", type=int, default=10)
    parser.add_argument("--port", type=int, default=7800)
    parser.add_argument("--plain", action="store_true", help="policy trained with the five moves only (no scripted actions)")
    args = parser.parse_args()
    if args.mode == "run":
        run(args.runs[0], args.episodes, args.port, scripted_actions=not args.plain)
    else:
        table(args.runs)
