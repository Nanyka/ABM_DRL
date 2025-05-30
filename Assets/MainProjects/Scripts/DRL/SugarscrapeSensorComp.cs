using System;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Sugarscape
{
    public class SugarscrapeSensorComp : SensorComponent, IDisposable
    {
        [SerializeField] private StateStorage stateStorage;
        [SerializeField] private string sensorName = "SugarscrapeSensor";

        public ISensor[] m_Sensors;

        public override ISensor[] CreateSensors()
        {
            Dispose();
            
            m_Sensors = new ISensor[]
            {
                new SugarscrapeSensor(stateStorage, sensorName)
            };
            
            return m_Sensors;
        }

        public void Dispose()
        {
            if (m_Sensors != null)
            {
                for (var i = 0; i < m_Sensors.Length; i++)
                {
                    ((SugarscrapeSensor)m_Sensors[i]).Dispose();
                }

                m_Sensors = null;
            }
        }
    }

    public class SugarscrapeSensor : ISensor
    {
        private readonly StateStorage m_State;
        private readonly string m_Name;
        
        public SugarscrapeSensor(StateStorage stateStorage, string name = "GridSensor")
        {
            m_State = stateStorage;
            m_Name   = name;
        }

        // Tell ML-Agents to expect a (C, H, W) “image” of floats
        public ObservationSpec GetObservationSpec()
        {
            var state = m_State.GetValue();
            return ObservationSpec.Visual(state.channels,state.height, state.width);
        }

        // row=y, col=x, channel=c
        public int Write(ObservationWriter writer)
        {
            var state = m_State.GetValue();
            for (int y = 0; y < state.height; y++)
            {
                for (int x = 0; x < state.width; x++)
                {
                    // channel 0 = sugar
                    writer[0, y, x] = state.GetSugar(x, y);

                    // channel 1 = spice
                    writer[1, y, x] = state.GetSpice(x, y);

                    // channel 2 = agent present? (1 if an agent is on that cell, else 0)
                    writer[2, y, x] = state.GetAgent(x, y) != null ? 1f : 0f;
                }
            }
            // Total floats written = H*W*C
            return state.channels * state.height * state.width;
        }

        public byte[] GetCompressedObservation()    => null;
        public CompressionSpec GetCompressionSpec() => CompressionSpec.Default();
        public void Update()  { }
        public void Reset()   { }
        public string GetName() => m_Name;
        public void Dispose() { }
    }

}