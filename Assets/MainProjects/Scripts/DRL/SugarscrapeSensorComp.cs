using System;
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
        private readonly int m_NumberOfChannels=4;

        public SugarscrapeVisual(StateStorage stateStorage, IAgentController controller, string name = "GridSensor")
        {
            m_State = stateStorage;
            m_AgentController = controller;
            m_Name = name;
        }

        public ObservationSpec GetObservationSpec()
        {
            // var state = m_State.GetValue();
            var range = m_AgentController.GetVision() * 2 + 1;
            return ObservationSpec.Vector(m_NumberOfChannels * range * range);
        }

        // row=y, col=x, channel=c
        public int Write(ObservationWriter writer)
        {
            // Debug.Log($"Agent {m_AgentController.GetAgentID()}");
            var state = m_State.GetValue();
            var vision = m_AgentController.GetVision();
            var agentPos = m_AgentController.GetPosition();
            // var sb = new StringBuilder();

            // Write into a temporary buffer and add it as a 1D observation.
            var range = vision * 2 + 1;
            float[] buffer = new float[m_NumberOfChannels * range * range];
            int idx = 0;

            for (int dy = -vision; dy <= vision; dy++)
            {
                for (int dx = -vision; dx <= vision; dx++)
                {
                    int worldX = agentPos.Item1 + dx;
                    int worldY = agentPos.Item2 + dy;
                    buffer[idx++] = state.GetSugar(worldX, worldY);
                    buffer[idx++] = state.GetSpice(worldX, worldY);

                    var info = state.GetAgent(worldX, worldY);
                    if (info == null)
                    {
                        buffer[idx++] = 0f;
                        buffer[idx++] = 0f;
                        
                        // sb.Append(0f);
                    }
                    else
                    {
                        if (dx == 0 && dy == 0)
                        {
                            buffer[idx++] = info.isOccupied ? info.currentMrs : 0f;
                            buffer[idx++] = 0f;
                            // sb.Append(0f);
                        }
                        else
                        {
                            buffer[idx++] = 0f;
                            buffer[idx++] = info.isOccupied ? info.currentMrs : 0f;
                            // sb.Append(info.isOccupied?info.currentMrs:0f);
                        }
                    }
                    
                    // if (dx < vision) sb.Append(' ');
                    // if (dx == vision) sb.Append('\n');
                }
            }

            writer.AddList(buffer);

            return buffer.Length;
            
            // // Debug.Log($"Agent {m_AgentController.GetAgentID()}");
            // var state = m_State.GetValue();
            // var vision = m_AgentController.GetVision();
            // var agentPos = m_AgentController.GetPosition();
            // var range = vision * 2 + 1;
            // float[] buffer = new float[m_NumberOfChannels * range * range];
            // int idx = 0;
            // // var sb = new StringBuilder();
            //
            // for (int dy = -vision; dy <= vision; dy++)
            // {
            //     for (int dx = -vision; dx <= vision; dx++)
            //     {
            //         // int worldX = Mathf.Clamp(agentPos.Item1 + dx, 0, state.width - 1);
            //         // int worldY = Mathf.Clamp(agentPos.Item2 + dy, 0, state.height - 1);
            //         int worldX = agentPos.Item1 + dx;
            //         int worldY = agentPos.Item2 + dy;
            //         int ix = dx + vision;
            //         int iy = dy + vision;
            //         writer[0, iy, ix] = state.GetSugar(worldX, worldY);
            //         writer[1, iy, ix] = state.GetSpice(worldX, worldY);
            //
            //         var info = state.GetAgent(worldX, worldY);
            //         if (info == null)
            //         {
            //             writer[2, iy, ix] = 0f;
            //             writer[3, iy, ix] = 0f;
            //             
            //             // sb.Append(0f);
            //         }
            //         else
            //         {
            //             if (dx == 0 && dy == 0)
            //             {
            //                 writer[2, iy, ix] = info.isOccupied ? info.currentMrs : 0f;
            //                 writer[3, iy, ix] = 0f;
            //                 // sb.Append(0f);
            //             }
            //             else
            //             {
            //                 writer[2, iy, ix] = 0f;
            //                 writer[3, iy, ix] = info.isOccupied ? info.currentMrs : 0f;
            //                 // sb.Append(info.isOccupied?info.currentMrs:0f);
            //             }
            //         }
            //         
            //         // if (dx < vision) sb.Append(' ');
            //         // if (dx == vision) sb.Append('\n');
            //     }
            // }
            //
            // // Total floats written = H*W*C
            // return m_NumberOfChannels * range * range;
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