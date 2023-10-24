using System.Net.Sockets;

namespace ModbusTcp;

/// <summary>
/// Modbus TCP 客户端
/// </summary>
public class ModbusTcpClient : IDisposable
{
    /// <summary>
    /// TCP 客户端
    /// </summary>
    private readonly TcpClient client = new();

    /// <summary>
    /// 默认IP地址 <br />
    /// 默认为150.110.60.6
    /// </summary>
    private const string DefaultIpAddress = "150.110.60.6";

    /// <summary>
    /// 默认端口 <br />
    /// 默认为502
    /// </summary>
    private const int DefaultPort = 502;

    /// <summary>
    /// 默认超时时间 <br />
    /// 默认为3000ms
    /// </summary>
    private const short DefaultTimeout = 3000;

    /// <summary>
    /// 网络流
    /// </summary>
    private NetworkStream stream;

    /// <summary>
    /// Modbus 命令
    /// </summary>
    private ModbusCommand modbusCommand = new();

    /// <summary>
    /// 连接到 PLC Modbus TCP 服务器
    /// </summary>
    /// <param name="ip">IP地址</param>
    /// <param name="port">PLC端口</param>
    /// <param name="timeout">超时时间</param>
    /// <returns>是否连接成功</returns>
    public bool Link(string ip = DefaultIpAddress, int port = DefaultPort, short timeout = DefaultTimeout)
    {
        try
        {
            // 检查 PLC 是否在线
            if (!ExamineClass.PingExamine(ip, timeout)) return false;

            // 连接 PLC
            client.Connect(ip, port);
            stream = client.GetStream();
            Console.WriteLine("连接成功");

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    private void Disconnect()
    {
        if (!client.Connected) return;
        client.Close();
        stream.Close();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Disconnect();
        // 调用 GC 机制，释放资源
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 将寄存器转换为浮点数
    /// </summary>
    /// <param name="registers">接收到的寄存器</param>
    /// <returns>转换后的浮点数</returns>
    /// <exception cref="ArgumentException">输入数组长度无效 - 数组长度必须为'2'</exception>
    public static float ConvertRegistersToFloat(int[] registers)
    {
        var num = registers.Length == 2
            ? registers[1]
            : throw new ArgumentException("输入数组长度无效 - 数组长度必须为'2'");
        var register = registers[0];
        var bytes1 = BitConverter.GetBytes(num);
        var bytes2 = BitConverter.GetBytes(register);
        return BitConverter.ToSingle(new[]
        {
            bytes2[0],
            bytes2[1],
            bytes1[0],
            bytes1[1]
        }, 0);
    }


    /// <summary>
    /// 将寄存器转换为浮点数 <br />
    /// 可以指定寄存器顺序
    /// </summary>
    /// <param name="registers">接收到的寄存器</param>
    /// <param name="registerOrder">寄存器优先顺序</param>
    /// <returns>转换后的浮点数</returns>
    public static float ConvertRegistersToFloat(int[] registers, RegisterOrder registerOrder)
    {
        var resultRegisters = new[]
        {
            registers[0],
            registers[1]
        };
        if (registerOrder == RegisterOrder.HighLow)
            resultRegisters = new[]
            {
                registers[1],
                registers[0]
            };
        return ConvertRegistersToFloat(resultRegisters);
    }


    /// <summary>
    /// 发送数据
    /// </summary>
    /// <returns>
    /// true: 发送成功 <br />
    /// false: 发送失败或发送异常
    /// </returns>
    private bool SendData(byte[] sendBytes)
    {
        // 检查网络流是否可用
        if (!stream.CanWrite) return false;
        // 检查连接是否可用
        if (!client.Connected) return false;
        try
        {
            // 向服务器发送数据
            stream.Write(sendBytes, 0, sendBytes.Length);
            Console.WriteLine("发送成功");
            return true;
        }
        catch (Exception)
        {
            // 发送失败或发送异常
            return false;
        }
    }

    /// <summary>
    /// 接收数据
    /// </summary>
    /// <returns>是否接收成功</returns>
    private bool ReceiveData(byte[] receiveBytes)
    {
        // 检查网络流是否可用
        if (!stream.CanWrite) return false;
        // 检查连接是否可用
        if (!client.Connected) return false;

        try
        {
            Console.WriteLine("等待接收数据");
            var length = stream.Read(receiveBytes, 0, 10);
            Console.WriteLine(length);
            // 读取不到数据时、网络连接异常、数据读取不完整 就跳出
            Console.WriteLine(receiveBytes);

            // 接收成功
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 读取寄存器
    /// <param name="slaveAddress">从站地址</param>
    /// <param name="startAddress">寄存器起始地址</param>
    /// <param name="count">读取寄存器数量</param>
    /// </summary>
    public void ReadRegister(byte slaveAddress, ushort startAddress, ushort count)
    {
        modbusCommand.DefaultStationNumber = slaveAddress;
        ReadRegister(slaveAddress, count);
    }

    /// <summary>
    /// 读取寄存器
    /// <param name="startAddress">寄存器起始地址</param>
    /// <param name="count">读取寄存器数量</param>
    /// </summary>
    public void ReadRegister(ushort startAddress, ushort count)
    {
        var command = modbusCommand.GenerateCommand(FunctionCode.READING_INPUT_REGISTER, startAddress, count);
        foreach (var b in command)
        {
            Console.WriteLine(b);
        }

        SendData(command);
        Thread.Sleep(10);
        var buffer = new byte[2100];
        var receiveData = ReceiveData(buffer);
        Console.WriteLine(receiveData);
        foreach (var b in buffer)
        {
            Console.WriteLine(b);
        }
    }
}