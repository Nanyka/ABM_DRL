"""
Outcome-level evaluation of trained policies in the pure-exchange setting (config/exchange).

Runs a trained policy for R evaluation episodes and records downstream outcomes per episode:
survival / carrying capacity, trades, prices, consumption, welfare and inequality.

Matching: the Unity side draws every episode's layout and endowments from one engine RNG stream that
the policy never touches (actions are sampled here, in Python), so episode k starts from the same
agents, positions and endowments whichever policy is evaluated. Each record stores a fingerprint of
the initial endowments so this can be checked instead of assumed.

  .venv_train/bin/python eval_exchange.py run   <run_name> [--episodes 50] [--port 7200]
  .venv_train/bin/python eval_exchange.py table <run_name> <run_name> ...
"""

import argparse, hashlib, json, os, sys
import numpy as np

ROOT = os.path.dirname(os.path.abspath(__file__))
RESULTS_DIR = os.path.join(ROOT, "results")
OUT_DIR = os.path.join(RESULTS_DIR, "exchange_eval")
ENV_PATH = os.path.join(ROOT, "Builds", "sugarscape_train_server_v6", "ABM_DRL")
BEHAVIOR_DIR = "TradingAgent"


def gini(x):
    x = np.sort(np.asarray(x, dtype=float))
    n = len(x)
    if n == 0 or x.sum() == 0:
        return float("nan")
    return float((2 * np.arange(1, n + 1) - n - 1).dot(x) / (n * x.sum()))


def run(run_name, episodes, port):
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
    behavior_cfg = config["behaviors"]["TradingAgent"]
    net_cfg = behavior_cfg["network_settings"]

    params, stats, engine = EnvironmentParametersChannel(), StatsSideChannel(), EngineConfigurationChannel()
    env = UnityEnvironment(file_name=ENV_PATH, seed=1, base_port=port, no_graphics=True,
                           side_channels=[params, stats, engine])
    engine.set_configuration_parameters(time_scale=20)
    env_params = {}
    for key, spec in config["environment_parameters"].items():
        value = float(spec["curriculum"][0]["value"]["sampler_parameters"]["value"])
        env_params[key] = value
        params.set_float_parameter(key, value)
    env.reset()

    name = list(env.behavior_specs)[0]
    spec = env.behavior_specs[name]
    actor = SharedActorCritic(
        spec.observation_specs,
        NetworkSettings(normalize=net_cfg["normalize"], hidden_units=net_cfg["hidden_units"],
                        num_layers=net_cfg["num_layers"]),
        spec.action_spec, stream_names=["extrinsic"])
    checkpoint = torch.load(os.path.join(run_dir, BEHAVIOR_DIR, "checkpoint.pt"), map_location="cpu")
    actor.load_state_dict(checkpoint["Policy"])
    actor.eval()
    # holdings are the first two entries of the 11-value vector observation
    vec = next(i for i, o in enumerate(spec.observation_specs) if o.shape == (11,))

    records, ep, cohort = [], None, set()
    index = -1  # episode 0 runs before the environment parameters apply, so it is discarded

    def close_episode(pending_stats):
        if ep is None:
            return
        ep["trades"] += len(pending_stats.get("Trade/Trades", []))
        ep["log_prices"][-1].extend(v for v, _ in pending_stats.get("Trade/LogPrice", []))
        if index >= 1:
            life = np.array(list(ep["ticks_alive"].values()))
            horizon = len(ep["alive"])
            last = ep["last"]
            survivors = [a for a, n in ep["ticks_alive"].items() if n >= horizon]
            wealth = [last[a][0] + last[a][1] for a in survivors]
            welfare = [float(np.sqrt(max(last[a][0], 0) * max(last[a][1], 0))) for a in survivors]
            top = sorted(wealth, reverse=True)
            records.append({
                "episode": index, "ticks": horizon, "agents": len(life),
                "endowment_fingerprint": ep["fingerprint"],
                "survivors": len(survivors),
                "mean_ticks_alive": float(life.mean()),
                "lifetimes": life.tolist(),
                "alive": ep["alive"],
                "trades": ep["trades"],
                "geo_mean_price": float(np.exp(np.mean(sum(ep["log_prices"], [])))) if ep["trades"] else float("nan"),
                "log_price_sum_by_tick": [float(np.sum(t)) for t in ep["log_prices"]],
                "trades_by_tick": [len(t) for t in ep["log_prices"]],
                "consumption": int(2 * life.sum()),  # one sugar + one spice per agent-tick, nothing is harvested
                "total_welfare_end": float(np.sum(welfare)),
                "mean_welfare_end": float(np.mean(welfare)) if welfare else 0.0,
                "gini_wealth_end": gini(wealth),
                "top10_share_end": float(sum(top[:max(1, len(top) // 10)]) / sum(top)) if top else float("nan"),
            })

    try:
        while len(records) < episodes:
            decision, terminal = env.get_steps(name)
            step_stats = stats.get_and_reset_stats()
            ids = [int(i) for i in decision.agent_id]
            if ids and not cohort.issuperset(ids):  # a fresh set of agents: the previous episode is over
                close_episode(step_stats)
                step_stats = {}
                index += 1
                cohort = set(ids)
                start = decision.obs[vec][:, :2]
                ep = {"fingerprint": hashlib.sha1(np.ascontiguousarray(start).tobytes()).hexdigest()[:12],
                      "ticks_alive": {a: 0 for a in ids}, "last": {}, "alive": [], "trades": 0, "log_prices": []}
                torch.manual_seed(index)  # same action-noise stream for every policy on episode k
            if ep is not None:
                if ep["log_prices"]:
                    ep["trades"] += len(step_stats.get("Trade/Trades", []))
                    ep["log_prices"][-1].extend(v for v, _ in step_stats.get("Trade/LogPrice", []))
                if ids:
                    ep["alive"].append(len(ids))
                    ep["log_prices"].append([])
                    for a, row in zip(ids, decision.obs[vec]):
                        ep["ticks_alive"][a] += 1
                        ep["last"][a] = (float(row[0]), float(row[1]))
            if ids:
                with torch.no_grad():
                    action, _, _ = actor.get_action_and_stats(
                        [torch.as_tensor(o) for o in decision.obs], masks=torch.ones(len(ids), 5))  # no action is masked
                env.set_actions(name, ActionTuple(discrete=action.discrete_tensor.numpy().reshape(len(ids), 1)))
            env.step()
    finally:
        env.close()

    os.makedirs(OUT_DIR, exist_ok=True)
    out = os.path.join(OUT_DIR, f"{run_name}.json")
    json.dump({"run": run_name, "environment_parameters": env_params, "episodes": records}, open(out, "w"))
    print(f"{run_name}: {len(records)} episodes -> {out}")


def table(run_names):
    data = {r: json.load(open(os.path.join(OUT_DIR, f"{r}.json")))["episodes"] for r in run_names}
    metrics = [("survivors", "Survivors at end"), ("mean_ticks_alive", "Mean ticks alive"),
               ("trades", "Trades per episode"), ("geo_mean_price", "Geometric-mean price"),
               ("consumption", "Consumption"), ("total_welfare_end", "Total welfare at end"),
               ("mean_welfare_end", "Mean welfare of survivors"), ("gini_wealth_end", "Gini of wealth at end"),
               ("top10_share_end", "Top-10% wealth share")]
    print(f"{'':28s}" + "".join(f"{r[-22:]:>24s}" for r in run_names))
    for key, label in metrics:
        cells = []
        for r in run_names:
            v = np.array([e[key] for e in data[r]], dtype=float)
            cells.append(f"{np.nanmean(v):12.2f} ± {np.nanstd(v):<8.2f}")
        print(f"{label:28s}" + "".join(f"{c:>24s}" for c in cells))
    ref = [e["endowment_fingerprint"] for e in data[run_names[0]]]
    for r in run_names[1:]:
        same = sum(a == b["endowment_fingerprint"] for a, b in zip(ref, data[r]))
        print(f"matched starts vs {run_names[0][-22:]}: {r[-22:]} {same}/{min(len(ref), len(data[r]))} episodes identical")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["run", "table"])
    parser.add_argument("runs", nargs="+")
    parser.add_argument("--episodes", type=int, default=50)
    parser.add_argument("--port", type=int, default=7200)
    args = parser.parse_args()
    if args.mode == "run":
        run(args.runs[0], args.episodes, args.port)
    else:
        table(args.runs)
