using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.MLAgents.Sensors;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sugarscape
{
    public class SugarscrapeSensorComp : SensorComponent, IDisposable, IObservationProvider
    {
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private WorkerStorage workerStorage;
        
        public ISensor[] m_Sensors;
        private SugarscrapeVisual _visual;
        private SugarscrapeFloat  _float;
        private float[] _visualObs;
        private float[] _floatObs;
        
        public override ISensor[] CreateSensors()
        {
            Dispose();
        
            var ctrl = GetComponent<IAgentController>();
            _visual = new SugarscrapeVisual(stateStorage, ctrl, "SugarscrapeVisual");
            _float  = new SugarscrapeFloat(ctrl, "SugarscrapeFloat", stateStorage);
        
            m_Sensors = new ISensor[] { _visual, _float };
        
            // Force specs once so lengths are known and buffers allocated
            var vSpec = _visual.GetObservationSpec();
            var fSpec = _float.GetObservationSpec();
            _visualObs = new float[vSpec.Shape[0]];
            _floatObs = new float[fSpec.Shape[0]];
        
            return m_Sensors;
        }
        
        public void Dispose()
        {
            if (m_Sensors != null)
            {
                for (int i = 0; i < m_Sensors.Length; i++)
                    if (m_Sensors[i] is IDisposable d) d.Dispose();
                m_Sensors = null;
            }
            _visualObs = null;
            _floatObs = null;
            _visual = null;
            _float = null;
        }
        
        // IObservationProvider
        // public float[] GetObservation()
        // {
        //     // assumes Write() has already populated each sensor's cached buffer this frame
        //     var v = _visual?.GetCached();
        //     var f = _float?.GetCached();
        //
        //     int vi = _visual?.Length ?? 0;
        //     int fi = _float?.Length ?? 0;
        //
        //     if (_visualObs == null || _visualObs.Length != vi + fi)
        //         _visualObs = new float[vi + fi];
        //
        //     if (v != null) System.Array.Copy(v, 0, _visualObs, 0, vi);
        //     if (f != null) System.Array.Copy(f, 0, _visualObs, vi, fi);
        //
        //     return _visualObs;
        // }
        
        public float[] GetVisualObs()
        {
            var v = _visual?.GetCached();
            int vi = _visual?.Length ?? 0;
            if (_visualObs == null)
                _visualObs = new float[vi];
            if (v != null) System.Array.Copy(v, 0, _visualObs, 0, vi);
            return _visualObs;
        }

        public float[] GetFloatObs()
        {
            var f = _float?.GetCached();
            int fi = _float?.Length ?? 0;
            if (_floatObs == null)
                _floatObs = new float[fi];
            if (f != null) System.Array.Copy(f, 0, _floatObs, 0, fi);
            return _floatObs;
        }

        public Worker GetWorker()
        {
            return workerStorage.GetValue();
        }

        // [SerializeField] private StateStorage stateStorage;
        // private string sensorVisualName = "SugarscrapeVisual";
        // private string sensorFloatName = "SugarscrapeFloat";
        //
        // public ISensor[] m_Sensors;
        //
        // public override ISensor[] CreateSensors()
        // {
        //     Dispose();
        //
        //     var agentController = GetComponent<IAgentController>();
        //     m_Sensors = new ISensor[]
        //     {
        //         new SugarscrapeVisual(stateStorage, agentController, sensorVisualName),
        //         new SugarscrapeFloat(agentController, sensorFloatName, stateStorage)
        //     };
        //
        //     return m_Sensors;
        // }
        //
        // public void Dispose()
        // {
        //     if (m_Sensors != null)
        //     {
        //         for (var i = 0; i < m_Sensors.Length; i++)
        //         {
        //             // ((SugarscrapeVisual)m_Sensors[i]).Dispose();
        //             if (m_Sensors[i] is IDisposable disposable) disposable.Dispose();
        //         }
        //
        //         m_Sensors = null;
        //     }
        // }
    }

    public class SugarscrapeVisual : ISensor, IDisposable
    {
        private readonly StateStorage m_State;
        private readonly IAgentController m_AgentController;
        private readonly string m_Name;
        private readonly int minModelIndex = 1; // min model index to observe absolute map
        private readonly int maxIndex = 9; // according to textbook and zero-based index

        // channel 0: welfare surplus moving left
        // channel 1: welfare surplus moving right
        // channel 2: welfare surplus moving down
        // channel 3: welfare surplus moving up
        // channel 4 --> 7: sugar amount
        // channel 8 --> 11: spice amount
        // channel 12 --> 15: other traders' MRS
        private int m_NumberOfChannels;
        private float[] _buffer;      // cached obs
        private int _obsLen;

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
            // var state = m_State.GetValue();

            m_NumberOfChannels = 4;
            // if (m_AgentController.UsingModel() < minModelIndex)
            // {
            //     var maxVision = 5; // use for old model (v4.1 and before)
            //     var range = maxVision * 2 + 1;
            //     return ObservationSpec.Vector(m_NumberOfChannels * range * range);
            // }
            
            m_NumberOfChannels += 12;
            _obsLen = m_NumberOfChannels * (maxIndex + 1);
            _buffer ??= new float[_obsLen];
            return ObservationSpec.Vector(_obsLen);
        }

        // row=y, col=x, channel=c
        public int Write(ObservationWriter writer)
        {
            var state = m_State.GetValue();
            var vision = m_AgentController.GetVision();
            var agentPos = m_AgentController.GetPosition();
            int width = state.width;
            int height = state.height;
            // var perfectInfo = m_AgentController.IsPerfectInfo();
            // var sb = new StringBuilder();

            // float[] buffer = new float[m_NumberOfChannels * (maxIndex + 1)];
            Array.Clear(_buffer, 0, _obsLen);
            
            // float[] buffer = new float[m_NumberOfChannels * (maxIndex + 1)];

            int remainSugar = m_AgentController.RemainSugar();
            int remainSpice = m_AgentController.RemainSpice();
            var trade = m_AgentController.GetTradeComp();
            float currentWelfare = trade.CalculateWelfare(remainSugar, remainSpice);

            var directions = new (int dx, int dy, int action)[]
            {
                (-1, 0, 1), // left
                (1, 0, 2), // right
                (0, -1, 3), // down
                (0, 1, 4) // up
            };

            for (var i = 0; i < 4; i++) // 4 directions
            {
                int sugar = 0;
                int spice = 0;
                for (var step = 1; step <= vision; step++)
                {
                    int worldX = agentPos.xCoor + directions[i].dx * step;
                    int worldY = agentPos.yCoor + directions[i].dy * step;
                    // int ndist = current.dist + 1;

                    if (worldX < 0 || worldX >= width || worldY < 0 || worldY >= height)
                    {
                        _buffer[i * maxIndex + (step - 1)] = 0; // transform vision to zero-base
                        _buffer[(4 + i) * maxIndex + (step - 1)] = 0;
                        _buffer[(8 + i) * maxIndex + (step - 1)] = 0;
                        _buffer[(12 + i) * maxIndex + (step - 1)] = 0;
                        // sb.AppendFormat("{0:F2} ", 0);
                        continue;
                    }

                    sugar += state.GetSugar(worldX, worldY);
                    spice += state.GetSpice(worldX, worldY);

                    // channel 0 ==> 3: welfare surplus
                    _buffer[i * maxIndex + (step - 1)] =
                        m_AgentController.PredictWelfare(sugar, spice, step) - currentWelfare;
                    // channel 4 ==> 7: sugar amount
                    _buffer[(4 + i) * maxIndex + (step - 1)] = state.GetSugar(worldX, worldY);
                    // channel 8 ==> 11: spice amount
                    _buffer[(8 + i) * maxIndex + (step - 1)] = state.GetSugar(worldX, worldY);
                    // channel 12 ==> 15: other traders' MRS
                    float otherMrs = 0f;
                    var agents = state.GetAgents(worldX, worldY)?
                        .Where(a => a.agentId != m_AgentController.GetAgentID() && a.isOccupied);
                    if (agents != null && agents.Any())
                        otherMrs = agents.Max(a => a.currentMrs);
                    _buffer[(12 + i) * maxIndex + (step - 1)] = otherMrs;  // For trading strategies
                    // sb.AppendFormat("{0:F2} ", state.GetSugar(nx, ny));
                }

                if (vision - 1 < maxIndex)
                {
                    for (var step = vision; step <= maxIndex; step++)
                    {
                        _buffer[i * maxIndex + step] = 0; // transform vision to zero-base
                        _buffer[(4 + i) * maxIndex + step] = 0;
                        _buffer[(8 + i) * maxIndex + step] = 0;
                        _buffer[(12 + i) * maxIndex + step] = 0;
                        // sb.AppendFormat("{0:F2} ", 0);
                    }
                }
            }
            // Debug.Log($"Agent {m_AgentController.GetAgentID()} channel0 (surplus):\n{sb}");
            
            writer.AddList(_buffer);

            return _buffer.Length;
            
            // if (m_AgentController.UsingModel() >= minModelIndex)
            // {
            //     float[] buffer = new float[m_NumberOfChannels * (maxIndex + 1)];
            //
            //     int remainSugar = m_AgentController.RemainSugar();
            //     int remainSpice = m_AgentController.RemainSpice();
            //     var trade = m_AgentController.GetTradeComp();
            //     float currentWelfare = trade.CalculateWelfare(remainSugar, remainSpice);
            //
            //     var directions = new (int dx, int dy, int action)[]
            //     {
            //         (-1, 0, 1), // left
            //         (1, 0, 2), // right
            //         (0, -1, 3), // down
            //         (0, 1, 4) // up
            //     };
            //
            //     for (var i = 0; i < 4; i++) // 4 directions
            //     {
            //         int sugar = 0;
            //         int spice = 0;
            //         for (var step = 1; step <= vision; step++)
            //         {
            //             int worldX = agentPos.xCoor + directions[i].dx * step;
            //             int worldY = agentPos.yCoor + directions[i].dy * step;
            //             // int ndist = current.dist + 1;
            //
            //             if (worldX < 0 || worldX >= width || worldY < 0 || worldY >= height)
            //             {
            //                 buffer[i * maxIndex + (step - 1)] = 0; // transform vision to zero-base
            //                 buffer[(4 + i) * maxIndex + (step - 1)] = 0;
            //                 buffer[(8 + i) * maxIndex + (step - 1)] = 0;
            //                 buffer[(12 + i) * maxIndex + (step - 1)] = 0;
            //                 // sb.AppendFormat("{0:F2} ", 0);
            //                 continue;
            //             }
            //
            //             sugar += state.GetSugar(worldX, worldY);
            //             spice += state.GetSpice(worldX, worldY);
            //
            //             // channel 0 ==> 3: welfare surplus
            //             buffer[i * maxIndex + (step - 1)] =
            //                 m_AgentController.PredictWelfare(sugar, spice, step) - currentWelfare;
            //             // channel 4 ==> 7: sugar amount
            //             buffer[(4 + i) * maxIndex + (step - 1)] = state.GetSugar(worldX, worldY);
            //             // channel 8 ==> 11: spice amount
            //             buffer[(8 + i) * maxIndex + (step - 1)] = state.GetSugar(worldX, worldY);
            //             // channel 12 ==> 15: other traders' MRS
            //             float otherMrs = 0f;
            //             var agents = state.GetAgents(worldX, worldY)?
            //                 .Where(a => a.agentId != m_AgentController.GetAgentID() && a.isOccupied);
            //             if (agents != null && agents.Any())
            //                 otherMrs = agents.Max(a => a.currentMrs);
            //             buffer[(12 + i) * maxIndex + (step - 1)] = otherMrs;  // For trading strategies
            //             // sb.AppendFormat("{0:F2} ", state.GetSugar(nx, ny));
            //         }
            //
            //         if (vision - 1 < maxIndex)
            //         {
            //             for (var step = vision; step <= maxIndex; step++)
            //             {
            //                 buffer[i * maxIndex + step] = 0; // transform vision to zero-base
            //                 buffer[(4 + i) * maxIndex + step] = 0;
            //                 buffer[(8 + i) * maxIndex + step] = 0;
            //                 buffer[(12 + i) * maxIndex + step] = 0;
            //                 // sb.AppendFormat("{0:F2} ", 0);
            //             }
            //         }
            //     }
            //     // Debug.Log($"Agent {m_AgentController.GetAgentID()} channel0 (surplus):\n{sb}");
            //     
            //     writer.AddList(buffer);
            //
            //     return buffer.Length;
            // }
            // else
            // {
            //     var maxVision = 5; // since the old models using hard-coded vision = 5
            //     var range = maxVision * 2 + 1;
            //     float[] buffer = new float[m_NumberOfChannels * range * range];
            //     
            //     int idx = 0;
            //     for (int dy = -maxVision; dy <= maxVision; dy++)
            //     {
            //         for (int dx = -maxVision; dx <= maxVision; dx++)
            //         {
            //             int worldX = agentPos.xCoor + dx;
            //             int worldY = agentPos.yCoor + dy;
            //             
            //             if (worldX < 0 || worldX >= width || worldY < 0 || worldY >= height)
            //             {
            //                 buffer[idx++] = 0f;
            //                 buffer[idx++] = 0f;
            //                 buffer[idx++] = 0f;
            //                 buffer[idx++] = 0f;
            //                 continue;
            //             }
            //
            //             buffer[idx++] = state.GetSugar(worldX, worldY);
            //             buffer[idx++] = state.GetSpice(worldX, worldY);
            //             
            //             var selfMrs = (dx == 0 && dy == 0) ? m_AgentController.CurrentMrs() : 0f;
            //             buffer[idx++] = selfMrs;
            //
            //             float otherMrs = 0f;
            //             var agents = state.GetAgents(worldX, worldY)?
            //                 .Where(a => a.agentId != m_AgentController.GetAgentID() && a.isOccupied);
            //             if (agents != null && agents.Any())
            //                 otherMrs = agents.Max(a => a.currentMrs);
            //             buffer[idx++] = otherMrs;
            //         }
            //     }
            //     
            //     writer.AddList(buffer);
            //     return buffer.Length;
            // }

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
        
        // helper to expose cached buffer
        public float[] GetCached() => _buffer;
        public int Length => _obsLen;
    }

    public class SugarscrapeFloat : ISensor, IDisposable
    {
        private readonly IAgentController m_AgentController;
        private readonly string m_Name;
        private readonly int minModelIndex = 1; // min model index to observe absolute map
        private readonly int minModelIndexWithoutStorage = 6; // min model index to remove storage data
        private readonly int minModelIndexWithoutNeighboor = 9; // min model index to record neighboor
        private readonly StateStorage m_State;
        private float[] _buffer;

        public SugarscrapeFloat(IAgentController agentController, string name = "FloatSensor", StateStorage stateStorage = null)
        {
            m_AgentController = agentController;
            m_Name = name;
            if (stateStorage != null) m_State = stateStorage;
        }

        public ObservationSpec GetObservationSpec()
        {
            // if (m_AgentController.UsingModel() < minModelIndex)
            //     return ObservationSpec.Vector(2);
            // if (m_AgentController.UsingModel() < minModelIndexWithoutStorage)
            //     return ObservationSpec.Vector(8); // from v6.4 and below
            // if(m_AgentController.UsingModel() < minModelIndexWithoutNeighboor)
            //     return ObservationSpec.Vector(6); // from v6.5
            _buffer ??= new float[11];
            return ObservationSpec.Vector(11);
        }

        public int Write(ObservationWriter writer)
        {
            // Debug.Log(
            //     $"Agent {m_AgentController.GetAgentID()}: ({m_AgentController.RemainSugarStorage()}," +
            //     $"{m_AgentController.RemainSpiceStorage()})");
            // if (m_AgentController.UsingModel() < minModelIndex)
            // {
            //     writer[0] = m_AgentController.ObserveSugarStarve();
            //     writer[1] = m_AgentController.ObserveSpiceStarve();
            //     return 2;
            // }

            // if (m_AgentController.UsingModel() < minModelIndexWithoutStorage)
            // {
            //     // from v6.4 and below
            //     writer[0] = m_AgentController.RemainSugar();
            //     writer[1] = m_AgentController.RemainSpice();
            //     writer[2] = m_AgentController.SugarStorage();
            //     writer[3] = m_AgentController.SpiceStorage();
            //     writer[4] = m_AgentController.SugarMetabolism();
            //     writer[5] = m_AgentController.SpiceMetabolism();
            //     writer[6] = m_AgentController.GetVision();
            //     writer[7] = m_AgentController.CurrentMrs(); // For trading strategies
            //     return 8;
            // }
            
            // Debug.Log("6 scalar");
            // from v6.5
            _buffer[0] = m_AgentController.RemainSugar();
            _buffer[1] = m_AgentController.RemainSpice();
            _buffer[2] = m_AgentController.SugarMetabolism();
            _buffer[3] = m_AgentController.SpiceMetabolism();
            _buffer[4] = m_AgentController.GetVision();
            _buffer[5] = m_AgentController.CurrentMrs(); // For trading strategies
            // if(m_AgentController.UsingModel() < minModelIndexWithoutNeighboor)
            //     return 6;
            
            var directions = new (int dx, int dy, int action)[]
            {
                (0, 0, 0), // idle
                (-1, 0, 1), // left
                (1, 0, 2), // right
                (0, -1, 3), // down
                (0, 1, 4) // up
            };

            var agentPos = m_AgentController.GetPosition();
            var state = m_State.GetValue();
            for (var i = 0; i < 5; i++) // 5 directions
            {
                int worldX = agentPos.xCoor + directions[i].dx;
                int worldY = agentPos.yCoor + directions[i].dy;
                var agents = state.GetAgents(worldX, worldY)?
                    .Where(a => a.agentId != m_AgentController.GetAgentID() && a.isOccupied);
                int neighboor = agents != null && agents.Any() ? 1 : 0;
                _buffer[6+i] = neighboor;
                // Debug.Log($"Write {6+i}: {neighboor}");
            }
            
            return 11;
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
        
        public float[] GetCached() => _buffer;
        public int Length => 11;
    }
}