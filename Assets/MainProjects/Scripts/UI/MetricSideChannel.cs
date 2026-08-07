using System;
using Unity.MLAgents.SideChannels;

namespace Sugarscape
{
    public class MetricSideChannel : SideChannel
    {
        public MetricSideChannel()
        {
            // Use a unique GUID here
            ChannelId = new Guid("c1a7f728-bd32-4b68-b5e8-000000000001");
        }

        public void SendMetric(string key, float value)
        {
            using (var msgOut = new OutgoingMessage())
            {
                msgOut.WriteString(key);
                msgOut.WriteFloat32(value);
                QueueMessageToSend(msgOut);
            }
        }

        protected override void OnMessageReceived(IncomingMessage msg)
        {
            // Not used (only sending data to Python)
        }
    }
}