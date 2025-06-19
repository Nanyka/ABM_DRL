using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sugarscape
{
    public class SugarscrapeSensorComp : SensorComponent, IDisposable
    {
        [SerializeField] private StateStorage stateStorage;

        private string sensorVisualName = "SugarscrapeVisual";
        private string sensorFloatName = "SugarscrapeFloat";

        public ISensor[] m_Sensors;

        public override ISensor[] CreateSensors()
        {
            Dispose();

            var agentController = GetComponent<IAgentController>();
            m_Sensors = new ISensor[]
            {
                new SugarscrapeVisual(stateStorage, agentController, sensorVisualName),
                new SugarscrapeFloat(agentController, sensorFloatName)
            };

            return m_Sensors;
        }

        public void Dispose()
        {
            if (m_Sensors != null)
            {
                for (var i = 0; i < m_Sensors.Length; i++)
                {
                    // ((SugarscrapeVisual)m_Sensors[i]).Dispose();
                    if (m_Sensors[i] is IDisposable disposable) disposable.Dispose();
                }

                m_Sensors = null;
            }
        }
    }

    public class SugarscrapeVisual : ISensor, IDisposable
    {
        private readonly StateStorage m_State;
        private readonly IAgentController m_AgentController;

        private readonly string m_Name;

        // channel 0: welfare surplus
        // channel 1: MRS of this agent (only non-zero at agent's cell)
        // channel 2: MRS of other agents (only non-zero at their cells)
        private readonly int m_NumberOfChannels = 3;

        public SugarscrapeVisual(StateStorage stateStorage, IAgentController controller, string name = "GridSensor")
        {
            m_State = stateStorage;
            m_AgentController = controller;
            m_Name = name;
        }

        public ObservationSpec GetObservationSpec()
        {
            // var state = m_State.GetValue();
            // var range = m_AgentController.GetVision() * 2 + 1;

            var state = m_State.GetValue();
            var vision = m_AgentController.GetVision();
            int rangeX, rangeY;
            if (m_AgentController.IsPerfectInfo())
            {
                rangeX = state.width;
                rangeY = state.height;
            }
            else
            {
                rangeX = vision * 2 + 1;
                rangeY = vision * 2 + 1;
            }

            return ObservationSpec.Vector(m_NumberOfChannels * rangeX * rangeY);
        }

        // row=y, col=x, channel=c
        public int Write(ObservationWriter writer)
        {
            var state = m_State.GetValue();
            var vision = m_AgentController.GetVision();
            var agentPos = m_AgentController.GetPosition();
            var perfectInfo = m_AgentController.IsPerfectInfo();
            // var sb = new StringBuilder();

            int range = vision * 2 + 1;
            float[] buffer = new float[m_NumberOfChannels * range * range];

            int remainSugar = m_AgentController.RemainSugar();
            int remainSpice = m_AgentController.RemainSpice();
            int metabolismSugar = 0;
            int metabolismSpice = 0;
            float staveSugar = m_AgentController.ObserveSugarStave();
            float staveSpice = m_AgentController.ObserveSpiceStave();
            if (staveSugar > 0f) metabolismSugar = Mathf.RoundToInt(remainSugar / staveSugar);
            if (staveSpice > 0f) metabolismSpice = Mathf.RoundToInt(remainSpice / staveSpice);

            var trade = m_AgentController.GetTradeComp();
            float currentWelfare = trade.CalculateWelfare(remainSugar, remainSpice);

            var queue = new Queue<Vector2Int>();
            var steps = new Dictionary<Vector2Int, int>();
            var sugarDict = new Dictionary<Vector2Int, int>();
            var spiceDict = new Dictionary<Vector2Int, int>();

            Vector2Int start = new Vector2Int(agentPos.Item1, agentPos.Item2);
            queue.Enqueue(start);
            steps[start] = 0;
            sugarDict[start] = remainSugar;
            spiceDict[start] = remainSpice;

            int[] dir = { -1, 0, 1, 0, -1 };
            while (queue.Count > 0)
            {
                var pos = queue.Dequeue();
                int step = steps[pos];
                if (step >= vision) continue;
                for (int d = 0; d < 4; d++)
                {
                    int nextX = pos.x + dir[d];
                    int nextY = pos.y + dir[d + 1];
                    if (nextX < 0 || nextX >= state.width || nextY < 0 || nextY >= state.height) continue;
                    var info = state.GetAgents(nextX, nextY);
                    if (info != null && info.Any(a => a.isOccupied) &&
                        !(nextX == agentPos.Item1 && nextY == agentPos.Item2))
                        continue;

                    int sugar = Mathf.Max(0, sugarDict[pos] + state.GetSugar(nextX, nextY) - metabolismSugar);
                    int spice = Mathf.Max(0, spiceDict[pos] + state.GetSpice(nextX, nextY) - metabolismSpice);

                    Vector2Int nextPos = new Vector2Int(nextX, nextY);
                    float newWelfare = trade.CalculateWelfare(sugar, spice);
                    bool better = false;
                    if (!sugarDict.ContainsKey(nextPos))
                    {
                        better = true;
                    }
                    else
                    {
                        float oldWelfare = trade.CalculateWelfare(sugarDict[nextPos], spiceDict[nextPos]);
                        if (newWelfare > oldWelfare)
                            better = true;
                    }

                    if (better)
                    {
                        steps[nextPos] = step + 1;
                        sugarDict[nextPos] = sugar;
                        spiceDict[nextPos] = spice;
                        queue.Enqueue(nextPos);
                    }
                }
            }

            int idx = 0;
            int cellCount = range * range;
            
            // var sb0 = new StringBuilder();
            // var sb1 = new StringBuilder();
            // var sb2 = new StringBuilder();
            
            for (int dx = -vision; dx <= vision; dx++)
            {
                for (int dy = -vision; dy <= vision; dy++)
                {
                    int worldX = agentPos.Item1 + dx;
                    int worldY = agentPos.Item2 + dy;
                    Vector2Int p = new Vector2Int(worldX, worldY);
                    float surplus = 0f;
                    if (sugarDict.ContainsKey(p))
                    {
                        float welfare = trade.CalculateWelfare(sugarDict[p], spiceDict[p]);
                        surplus = welfare - currentWelfare;
                    }

                    // Channel 0: welfare surplus
                    buffer[idx] = surplus;

                    // Channel 1: this agent's MRS at its position
                    var selfMrs = (dx == 0 && dy == 0) ? m_AgentController.CurrentMrs() : 0f;
                    buffer[idx + cellCount] = selfMrs;

                    // Channel 2: other agents' MRS at their positions
                    float otherMrs = 0f;
                    var agents = state.GetAgents(worldX, worldY)?
                        .Where(a => a.agentId != m_AgentController.GetAgentID() && a.isOccupied);
                    if (agents != null && agents.Any())
                        otherMrs = agents.Max(a => a.currentMrs);
                    buffer[idx + 2 * cellCount] = otherMrs;
                    
                    // sb0.AppendFormat("{0:F2} ", surplus);
                    // sb1.AppendFormat("{0:F2} ", selfMrs);
                    // sb2.AppendFormat("{0:F2} ", otherMrs);

                    idx++;
                }
                // sb0.Append('\n');
                // sb1.Append('\n');
                // sb2.Append('\n');
            }

            // Debug.Log($"Agent {m_AgentController.GetAgentID()} channel0 (surplus):\n{sb0}");
            // Debug.Log($"Agent {m_AgentController.GetAgentID()} channel1 (self MRS):\n{sb1}");
            // Debug.Log($"Agent {m_AgentController.GetAgentID()} channel2 (others MRS):\n{sb2}");
            
            writer.AddList(buffer);

            return buffer.Length;

            // OLD VERSION
            // // Debug.Log($"Agent {m_AgentController.GetAgentID()} record with info perfection is {perfectInfo}");
            //
            // // If GameSettings.isPerfectInfo is true, observe the whole world. Else, observe agent's vision
            // int rangeX, rangeY;
            // if (perfectInfo)
            // {
            //     rangeX = state.width;
            //     rangeY = state.height;
            // }
            // else
            // {
            //     rangeX = vision * 2 + 1;
            //     rangeY = vision * 2 + 1;
            // }
            //
            // // Write into a temporary buffer and add it as a 1D observation.
            // // var range = vision * 2 + 1;
            // float[] buffer = new float[m_NumberOfChannels * rangeX * rangeY];
            // int idx = 0;
            //
            // if (perfectInfo)
            // {
            //     for (int y = 0; y < rangeY; y++)
            //     {
            //         for (int x = 0; x < rangeX; x++)
            //         {
            //             int worldX = x;
            //             int worldY = y;
            //
            //             buffer[idx++] = state.GetSugar(worldX, worldY);
            //             buffer[idx++] = state.GetSpice(worldX, worldY);
            //
            //             var info = state.GetAgent(worldX, worldY);
            //             if (info == null)
            //             {
            //                 buffer[idx++] = 0f;
            //                 buffer[idx++] = 0f;
            //                 // sb.Append(0f);
            //             }
            //             else
            //             {
            //                 if (worldX == agentPos.Item1 && worldY == agentPos.Item2)
            //                 {
            //                     buffer[idx++] = info.isOccupied ? info.currentMrs : 0f;
            //                     buffer[idx++] = 0f;
            //                     // sb.Append(0f);
            //                 }
            //                 else
            //                 {
            //                     buffer[idx++] = 0f;
            //                     buffer[idx++] = info.isOccupied ? info.currentMrs : 0f;
            //                     // sb.Append(info.isOccupied?info.currentMrs:0f);
            //                 }
            //             }
            //             // if (x < rangeX-1) sb.Append(' ');
            //             // if (x == rangeX-1) sb.Append('\n');
            //         }
            //     }
            // }
            // else
            // {
            //     for (int dy = -vision; dy <= vision; dy++)
            //     {
            //         for (int dx = -vision; dx <= vision; dx++)
            //         {
            //             int worldX = agentPos.Item1 + dx;
            //             int worldY = agentPos.Item2 + dy;
            //
            //             buffer[idx++] = state.GetSugar(worldX, worldY);
            //             buffer[idx++] = state.GetSpice(worldX, worldY);
            //
            //             var info = state.GetAgent(worldX, worldY);
            //             if (info == null)
            //             {
            //                 buffer[idx++] = 0f;
            //                 buffer[idx++] = 0f;
            //                 // sb.Append(0f);
            //             }
            //             else
            //             {
            //                 if (dx == 0 && dy == 0)
            //                 {
            //                     buffer[idx++] = info.isOccupied ? info.currentMrs : 0f;
            //                     buffer[idx++] = 0f;
            //                     // sb.Append(0f);
            //                 }
            //                 else
            //                 {
            //                     buffer[idx++] = 0f;
            //                     buffer[idx++] = info.isOccupied ? info.currentMrs : 0f;
            //                     // sb.Append(info.isOccupied?info.currentMrs:0f);
            //                 }
            //             }
            //
            //             // if (dx < vision) sb.Append(' ');
            //             // if (dx == vision) sb.Append('\n');
            //         }
            //     }
            // }
            //
            // // Debug.Log(sb.ToString());
            // writer.AddList(buffer);
            //
            // return buffer.Length;
        }

        public byte[] GetCompressedObservation() => null;
        public CompressionSpec GetCompressionSpec() => CompressionSpec.Default();

        public void Update()
        {
        }

        public void Reset()
        {
        }

        public string GetName() => m_Name;

        public void Dispose()
        {
        }
    }

    public class SugarscrapeFloat : ISensor, IDisposable
    {
        private readonly IAgentController m_AgentController;
        private readonly string m_Name;

        public SugarscrapeFloat(IAgentController agentController, string name = "FloatSensor")
        {
            m_AgentController = agentController;
            m_Name = name;
        }

        public ObservationSpec GetObservationSpec()
        {
            return ObservationSpec.Vector(2);
        }

        public int Write(ObservationWriter writer)
        {
            // Debug.Log(
            //     $"Agent {m_AgentController.GetAgentID()}: ({m_AgentController.ObserveSugarStave()}," +
            //     $"{m_AgentController.ObserveSpiceStave()})");
            writer[0] = m_AgentController.ObserveSugarStave();
            writer[1] = m_AgentController.ObserveSpiceStave();
            return 2;
        }

        public byte[] GetCompressedObservation() => null;
        public CompressionSpec GetCompressionSpec() => CompressionSpec.Default();

        public void Update()
        {
        }

        public void Reset()
        {
        }

        public string GetName() => m_Name;

        public void Dispose()
        {
        }
    }
}