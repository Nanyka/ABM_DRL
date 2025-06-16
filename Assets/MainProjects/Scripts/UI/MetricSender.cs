using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Sugarscape
{
    public class MetricSender : MonoBehaviour
    {
        [SerializeField] private IntStorage tradeCount;
        [SerializeField] private IntStorage aliveAgentsCount;

        private TcpClient client;
        private NetworkStream stream;

        void Start()
        {
            Thread thread = new Thread(ConnectToPython);
            thread.Start();
        }

        void ConnectToPython()
        {
            try
            {
                client = new TcpClient("127.0.0.1", 50007); // Match Python port
                stream = client.GetStream();

                while (true)
                {
                    string msg = JsonUtility.ToJson(new MetricData {
                        TradeCount = tradeCount.GetValue(),
                            AliveAgent = aliveAgentsCount.GetValue()
                        });
                    byte[] data = Encoding.UTF8.GetBytes(msg + "\n");
                    stream.Write(data, 0, data.Length);
                    Thread.Sleep(100); // 10 per second
                }
            }
            catch (SocketException e)
            {
                Debug.Log("Socket error: " + e.Message);
            }
        }

        void OnApplicationQuit()
        {
            stream?.Close();
            client?.Close();
        }
    }
    
    [System.Serializable]
    public class MetricData
    {
        public float TradeCount;
        public int AliveAgent;
    }
}