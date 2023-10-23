using System.Net.NetworkInformation;

namespace ModbusTcp;

/// <summary>
/// 检测类
/// </summary>
internal static class ExamineClass
{
    /// <summary>
    /// 检查PLC链接状况
    /// </summary>
    /// <param name="ip">IP地址</param>
    /// <param name="timeOut">超时时间</param>
    /// <returns>是否链接</returns>
    internal static bool PingExamine(string ip, int timeOut)
    {
        try
        {
            // 创建一个 Ping 实例
            var ping = new Ping();

            // 发送 ICMP 回显请求到指定的 IP 地址，并等待响应
            var pingReply = ping.Send(ip, timeOut);

            // 判断响应的状态是否为 Success，是否成功收到 ICMP 回显响应
            // 返回 true 表示 Ping 成功，否则返回 false 表示 Ping 失败
            return pingReply.Status == IPStatus.Success;
        }
        catch (Exception)
        {
            return false;
        }
    }
}