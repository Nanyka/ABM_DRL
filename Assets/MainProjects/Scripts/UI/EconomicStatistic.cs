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
        [SerializeField] private IntStorage tradeCount;
        [SerializeField] private IntStorage aliveAgentsCount;
        [SerializeField] private EntitiesStorage entitiesStorage;
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private TextMeshProUGUI tradeCountText;
        [SerializeField] private TextMeshProUGUI aliveCountText;
        [SerializeField] private TextMeshProUGUI averageSSDistanceText;
        [SerializeField] private TextMeshProUGUI averageMoneyText;
        [SerializeField] private TextMeshProUGUI stepCountText;

        [Header("Settings")] [SerializeField] private bool isUpdateStatistic;
        
        private (int totalSugar, int totalSpice) m_CurrentResource;
        private TcpClient client;
        private NetworkStream stream;
        private bool isSendStatistic;
        private int counter;
        
        // private MetricSideChannel m_MetricChannel;

        // private void Awake()
        // {
        //     // m_MetricChannel = new MetricSideChannel();
        //     // SideChannelManager.RegisterSideChannel(m_MetricChannel);
        // }

        private void OnEnable()
        {
            OnEndStep.AddListener(UpdateCount);
            OnReset.AddListener(ResetCount);
            OnSetup.AddListener(ResetCount);
        }

        private void OnDisable()
        {
            OnEndStep.RemoveListener(UpdateCount);
            OnReset.RemoveListener(ResetCount);
            OnSetup.AddListener(ResetCount);

            stream?.Close();
            client?.Close();
            // SideChannelManager.UnregisterSideChannel(m_MetricChannel);
        }
        
        void Start()
        {
            Thread thread = new Thread(ConnectToPython);
            thread.Start();
        }

        private void UpdateCount()
        {
            if (!isUpdateStatistic)  return;
            
            tradeCountText.text = $"Trade: {tradeCount.GetValue().ToString()}";
            aliveCountText.text = $"Alive: {aliveAgentsCount.GetValue().ToString()}";
            var aliveAgents = entitiesStorage.GetAgents().Where(a => a != null && a.IsAlive());
            stepCountText.text = $"Step: {counter++.ToString()}";

            if (Academy.Instance.IsCommunicatorOn) return;
            averageSSDistanceText.text = $"AverageSS: {AverageSugarSpiceDistance(aliveAgents):0.00}";
            averageMoneyText.text = $"AvgMoney: { (aliveAgents.Any() ? aliveAgents.Average(a => a.Money()) : 0f):0.00}";
            
            if (isSendStatistic) UpdateStatistics();

            // m_MetricChannel.SendMetric("trade_count", tradeCount.GetValue());
            // m_MetricChannel.SendMetric("agent_count", aliveAgentsCount.GetValue());
        }

        private void ResetCount()
        {
            tradeCount.SetValue(0);
            m_CurrentResource = stateStorage.GetValue().CountResources();
            counter = 1;
        }

        #region COMMUNICATE METHODS

        private void ConnectToPython()
        {
            try
            {
                client = new TcpClient("127.0.0.1", 50007); // Match Python port
                stream = client.GetStream();
                isSendStatistic = true;
            }
            catch (SocketException e)
            {
                Debug.Log("Socket error: " + e.Message);
                isSendStatistic = false;
            }
        }

        private void UpdateStatistics()
        {
            var aliveAgents = entitiesStorage.GetAgents().Where(a => a != null && a.IsAlive());
            var marketPrice = CalculateMarketPrice(aliveAgents);
            var inequality = CalculateInequality(aliveAgents);
            var averageWelfare = CalculateAverageWelfare(aliveAgents);
            var crRatio = ConsumptionRegrowthRatio();
            var hardCodeAgentPercentage = HardCodeAgentPercentage(aliveAgents);
            var isEnd = counter == gameSettings.numberOfEpisode;
            
            string msg = JsonUtility.ToJson(new MetricData {
                TradeCount = tradeCount.GetValue(),
                AliveAgent = aliveAgentsCount.GetValue(),
                MarketPrice = marketPrice,
                Inequality = inequality,
                AverageWelfare = averageWelfare,
                CRRatio = crRatio,
                HardCodeAgentPercentage = hardCodeAgentPercentage,
                IsEnd = isEnd,
            });
            byte[] data = Encoding.UTF8.GetBytes(msg + "\n");
            stream.Write(data, 0, data.Length);
        }

        #endregion

        #region STATIC METHODS

        private float CalculateMarketPrice(IEnumerable<IAgentController> agents)
        {
            var totalPrice = 0f;
            var totalTrade = 0;
            foreach (var agent in agents)
            {
                var price = agent.GetTradeComp().GetPrice();
                if (price > 0)
                {
                    totalPrice += price;
                    totalTrade++;
                }
            }
            return totalTrade == 0 ? 0f : totalPrice/totalTrade;
        }

        private float CalculateInequality(IEnumerable<IAgentController> agents)
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
            return (2f * cumulative) / (n * sum) - (n + 1f) / n;
        }

        private float CalculateAverageWelfare(IEnumerable<IAgentController> agents)
        {
            var totalWelfare = agents
                .Select(a => a.GetTradeComp().CalculateWelfare(a.RemainSugar(), a.RemainSpice()))
                .Sum();
            
            return totalWelfare/agents.Count();
        }
        
        private float ConsumptionRegrowthRatio()
        {
            var countResources = stateStorage.GetValue().CountResources();
            
            // Debug.Log($"Regrowth of sugar: {countResources.totalSugar-m_CurrentResource.totalSugar}");
            var sugarRegrown = countResources.totalSugar - m_CurrentResource.totalSugar;
            var spiceRegrown = countResources.totalSpice - m_CurrentResource.totalSpice;
            var sugarConsumed = aliveAgentsCount.GetValue() * gameSettings.metabolismSugar;
            var spiceConsumed =  aliveAgentsCount.GetValue() * gameSettings.metabolismSpice;

            float consumed = sugarConsumed + spiceConsumed;
            float regrown = sugarRegrown + spiceRegrown;
            if (Mathf.Abs(regrown) < Mathf.Epsilon)
                return 0f;
            return consumed / regrown;
        }

        private float HardCodeAgentPercentage(IEnumerable<IAgentController> agents)
        {
            var hardCodeAgents = agents.Count(a => a.IsHardCodeAgent());
            return hardCodeAgents*1f/aliveAgentsCount.GetValue();
        }

        private float AverageSugarSpiceDistance(IEnumerable<IAgentController> agents)
        {
            if (agents == null || !agents.Any()) return 0f;
            return agents.Average(a => a.SugarSpiceDistance());
        }

        #endregion
    }
}
