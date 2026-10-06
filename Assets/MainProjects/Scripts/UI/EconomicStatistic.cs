using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.SideChannels;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sugarscape
{
    public class EconomicStatistic : MonoBehaviour
    {
        [SerializeField] private VoidChannel OnEndStep;
        [SerializeField] private VoidChannel OnReset;
        [SerializeField] private VoidChannel OnSetup;
        [SerializeField] private VoidChannel OnApplyConfig;
        [SerializeField] private VoidChannel OnEndEpisode;
        [SerializeField] private VoidChannel OnNewSimulation;
        
        [SerializeField] private IntStorage tradeCount;
        [SerializeField] private IntStorage aliveAgentsCount;
        [SerializeField] private IntStorage regainStorage;
        [SerializeField] private EntitiesStorage entitiesStorage;
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private GameSettings gameSettings;
        
        [SerializeField] private TextMeshProUGUI tradeCountText;
        [SerializeField] private TextMeshProUGUI aliveCountText;
        [SerializeField] private TextMeshProUGUI marketPriceText;
        [SerializeField] private TextMeshProUGUI inequalityText;
        [SerializeField] private TextMeshProUGUI welfareText;
        [SerializeField] private TextMeshProUGUI stepCountText;
        [SerializeField] private TextMeshProUGUI simulationCountText;

        [Header("Settings")] [SerializeField] private bool isUpdateStatistic;
        
        private (int totalSugar, int totalSpice) m_CurrentResource;
        private TcpClient client;
        private NetworkStream stream;
        private bool isSendStatistic;
        private int counter;
        private float marketPrice;
        private float inequality;
        private int simulationCount;

        private void OnEnable()
        {
            OnEndStep.AddListener(UpdateCount);
            OnReset.AddListener(ResetCount);
            OnSetup.AddListener(ResetCount);
            OnApplyConfig.AddListener(UpdateSimulationCount);
            OnEndEpisode.AddListener(AutoRunNewSimulation);
        }

        private void OnDisable()
        {
            OnEndStep.RemoveListener(UpdateCount);
            OnReset.RemoveListener(ResetCount);
            OnSetup.RemoveListener(ResetCount);
            OnApplyConfig.RemoveListener(UpdateSimulationCount);
            OnEndEpisode.RemoveListener(AutoRunNewSimulation);


            stream?.Close();
            client?.Close();
            // SideChannelManager.UnregisterSideChannel(m_MetricChannel);
        }

        private void AutoRunNewSimulation()
        {
            // Debug.Log("AutoRunNewSimulation");
            if (isSendStatistic)
                OnNewSimulation.ExecuteChannel();
        }

        void Start()
        {
            Thread thread = new Thread(ConnectToPython);
            thread.Start();
        }

        private void UpdateCount()
        {
            if (!isUpdateStatistic)  return;
            if (Academy.Instance.IsCommunicatorOn) return;
            
            tradeCountText.text = $"Trade: {tradeCount.GetValue().ToString()}";
            aliveCountText.text = $"Alive: {aliveAgentsCount.GetValue().ToString()}";
            var aliveAgents = entitiesStorage.GetAgents().Where(a => a != null && a.IsAlive()).ToArray();
            stepCountText.text = $"Step: {(++counter).ToString()}";
            marketPriceText.text = $"Price: {CalculateMarketPrice(aliveAgents):0.00}";
            inequalityText.text = $"Gini: {CalculateInequality(aliveAgents):0.00}";
            welfareText.text = $"Average welfare: {CalculateAverageWelfare(aliveAgents):0.00}";
            
            if (isSendStatistic) UpdateStatistics(aliveAgents);
        }

        private void ResetCount()
        {
            tradeCount.SetValue(0);
            TradeLog.Clear();
            m_CurrentResource = stateStorage.GetValue().CountResources();
            counter = 0;
        }
        
        private void UpdateSimulationCount()
        {
            if (!isUpdateStatistic)  return;
            if (Academy.Instance.IsCommunicatorOn) return;
            simulationCountText.text = (++simulationCount).ToString();
        }

        #region COMMUNICATE METHODS

        private void ConnectToPython()
        {
            try
            {
                client = new TcpClient("127.0.0.1", 50007); // Match Python port
                stream = client.GetStream();
                isSendStatistic = true;
                TradeLog.Enabled = true;
            }
            catch (SocketException e)
            {
                Debug.Log("Socket error: " + e.Message);
                isSendStatistic = false;
            }
        }

        private void UpdateStatistics(IAgentController[] aliveAgents)
        {
            // Debug.Log($"Is end: {counter >= gameSettings.numberOfEpisode} at {counter}");
            var averageWelfare = CalculateAverageWelfare(aliveAgents);
            var crRatio = ConsumptionRegrowthRatio();
            var hardCodeAgentPercentage = HardCodeAgentPercentage(aliveAgents);
            var isEnd = counter == gameSettings.numberOfEpisode;
            var agentsInfo = aliveAgents.Select(a => new AgentInfo {
                agentId = a.GetAgentID(),
                remainSugar = a.RemainSugar(),
                remainSpice = a.RemainSpice(),
                Age = a.GetAge(),
                SugarMetabolism = a.SugarMetabolism(),
                SpiceMetabolism = a.SpiceMetabolism(),
                SugarCapacity = a.SugarStorage(),
                SpiceCapacity = a.SpiceStorage(),
                currentMrs = a.CurrentMrs(),
                currentPrice = a.GetTradeComp().GetPrice()
            }).ToArray();
            
            string msg = JsonUtility.ToJson(new MetricData {
                TradeCount = tradeCount.GetValue(),
                AliveAgent = aliveAgentsCount.GetValue(),
                MarketPrice = marketPrice,
                Inequality = inequality,
                AverageWelfare = averageWelfare,
                CRRatio = crRatio,
                HardCodeAgentPercentage = hardCodeAgentPercentage,
                IsEnd = isEnd,
                Agents = agentsInfo,
                TradePrices = TradeLog.Drain(),
            });
            byte[] data = Encoding.UTF8.GetBytes(msg + "\n");
            stream.Write(data, 0, data.Length);
        }

        #endregion

        #region STATIC METHODS

        private float CalculateMarketPrice(IAgentController[] agents)
        {
            var productionPrice = 1f;
            var totalTrade = 0;
            // var priceString = "";
            foreach (var agent in agents)
            {
                var price = agent.GetTradeComp().GetPrice();
                if (price > 0)
                {
                    productionPrice *= price;
                    totalTrade++;
                    // priceString += $"{price}, ";
                }
            }
            marketPrice = totalTrade == 0 ? marketPrice : Mathf.Pow(productionPrice, 1.0f / totalTrade*1f);
            // Debug.Log($"Marget price: {marketPrice} \n {priceString}");

            return marketPrice;
        }

        private float CalculateInequality(IAgentController[] agents)
        {
            if (agents == null) return 0f;
            
            var welfares = agents
                .Select(a => a.GetTradeComp().CalculateWelfare(a.RemainSugar(), a.RemainSpice()));

            var array = welfares.Where(v => v >= 0f).OrderBy(v => v).ToArray();
            int n = array.Length;
            if (n == 0) return 0f;
                
            float sum = array.Sum();
            if (Mathf.Abs(sum) < Mathf.Epsilon) return 0f;
            
            float cumulative = 0f;
            for (int i = 0; i < n; i++)
                cumulative += (i + 1) * array[i];
            inequality = (2f * cumulative) / (n * sum) - (n + 1f) / n;
            return inequality;
        }

        private float CalculateAverageWelfare(IAgentController[] agents)
        {
            var totalWelfare = agents
                .Select(a => a.GetTradeComp().CalculateWelfare(a.RemainSugar(), a.RemainSpice()))
                .Sum();
            
            return totalWelfare/agents.Count();
        }
        
        private float ConsumptionRegrowthRatio()
        {
            // var countResources = stateStorage.GetValue().CountResources();
            
            // Debug.Log($"Regrowth of sugar: {countResources.totalSugar-m_CurrentResource.totalSugar}");
            // var sugarRegrown = countResources.totalSugar - m_CurrentResource.totalSugar;
            // var spiceRegrown = countResources.totalSpice - m_CurrentResource.totalSpice;
            
            var totalRegain = regainStorage.GetValue();
            var sugarConsumed = aliveAgentsCount.GetValue() * gameSettings.metabolismSugar;
            var spiceConsumed =  aliveAgentsCount.GetValue() * gameSettings.metabolismSpice;

            float consumed = sugarConsumed + spiceConsumed;
            if (totalRegain < Mathf.Epsilon)
                return 0f;
            return consumed / totalRegain;
        }

        private float HardCodeAgentPercentage(IAgentController[] agents)
        {
            var hardCodeAgents = agents.Count(a => a.IsHardCodeAgent());
            return hardCodeAgents*1f/aliveAgentsCount.GetValue();
        }

        // private float AverageSugarSpiceDistance(IAgentController[] agents)
        // {
        //     if (agents == null || !agents.Any()) return 0f;
        //     return agents.Average(a => a.SugarSpiceDistance());
        // }

        #endregion
    }
}
