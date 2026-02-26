using OneDriver.PowerSupply.Abstract.Channels;

namespace OneDriver.PowerSupply.Basic.Channels
{
    /// <summary>
    /// Unused class
    /// </summary>
    public class Channel(ChannelParams parameters, ChannelProcessData processData)
        : CommonChannel<ChannelParams, ChannelProcessData>(parameters, processData);
}
