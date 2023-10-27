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
    private TcpClient client = new();

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
    private readonly ModbusCommand modbusCommand = new();

    /// <summary>
    /// 监听线程
    /// </summary>
    private Thread listening;

    /// <summary>
    /// 线程状态
    /// </summary>
    private bool threadStatus = false;

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
            client = new TcpClient();
            // 连接 PLC
            client.Connect(ip, port);
            stream = client.GetStream();
            Console.WriteLine("连接成功");
            ValidateConnection(8000, Reconnect);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 重新连接
    /// </summary>
    /// <returns>是否连接成功</returns>
    private void Reconnect()
    {
        threadStatus = false;
        client.Close();
        stream.Close();
        Link();
    }

    /// <summary>
    /// 验证连接是否可用
    /// </summary>
    public void ValidateConnection(int interval, Action onFailure)
    {
        threadStatus = true;
        // 创建一个新的线程来定时验证连接
        listening = new Thread(() =>
        {
            while (threadStatus)
            {
                try
                {
                    // 验证连接
                    if (!client.Connected) throw new Exception();
                    if (!ExamineClass.PingExamine(DefaultIpAddress, DefaultTimeout)) throw new Exception();

                    // 休眠指定的时间间隔
                    Thread.Sleep(interval);
                }
                catch (Exception)
                {
                    // 连接验证失败，执行 onFailure 方法
                    onFailure();
                }
            }
        });
        listening.IsBackground = true;
        // 启动线程
        listening.Start();
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
            var length = stream.Read(receiveBytes, 0, receiveBytes.Length);
            Console.WriteLine("接收到的数据长度：" + length);
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
    /// 读取
    /// </summary>
    public void Read(FunctionCode functionCode, ushort startAddress, ushort count)
    {
        switch (functionCode)
        {
            case FunctionCode.READING_COIL:
                break;
            case FunctionCode.READING_DISCRETE_INPUT:
                break;
            case FunctionCode.READING_HOLDING_REGISTER:
                break;
            case FunctionCode.READING_INPUT_REGISTER:
                break;
            case FunctionCode.WRITING_SINGLE_COIL:
                break;
            case FunctionCode.WRITING_SINGLE_REGISTER:
                break;
            case FunctionCode.WRITING_MULTIPLE_COILS:
                break;
            case FunctionCode.WRITING_MULTIPLE_REGISTERS:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(functionCode), functionCode, "无效的功能码");
        }
    }

    /// <summary>
    /// 读取线圈
    /// </summary>
    /// <param name="startAddress">线圈起始地址</param>
    /// <param name="quantity">读取线圈数量</param>
    /// <returns>线圈状态</returns>
    public bool[] ReadCoil(ushort startAddress, ushort quantity)
    {
        var command = modbusCommand.GenerateCommand(FunctionCode.READING_COIL, startAddress, quantity);

        if (!SendData(command))
        {
            // 发送失败
            return Array.Empty<bool>();
        }

        Thread.Sleep(10);
        var buffer = new byte[10 + quantity / 8];
        ReceiveData(buffer);

        Console.WriteLine("转换后的数据");
        var result = new bool[quantity];
        for (var i = 0; i < quantity; i++)
        {
            result[i] = (buffer[9 + i / 8] & (1 << (i % 8))) != 0;
        }

        return result;
    }

    /// <summary>
    /// 读取离散输入寄存器
    /// </summary>
    /// <param name="startAddress">离散输入起始地址</param>
    /// <param name="quantity">读取离散输入数量</param>
    /// <returns>离散输入状态</returns>
    public bool[] ReadDiscreteInput(ushort startAddress, ushort quantity)
    {
        var command = modbusCommand.GenerateCommand(FunctionCode.READING_DISCRETE_INPUT, startAddress, quantity);

        if (!SendData(command))
        {
            return Array.Empty<bool>();
        }

        Thread.Sleep(10);
        var buffer = new byte[10 + quantity / 8];
        ReceiveData(buffer);

        var result = new bool[quantity];
        for (var i = 0; i < quantity; i++)
        {
            result[i] = (buffer[9 + i / 8] & (1 << (i % 8))) != 0;
        }

        return result;
    }


    /// <summary>
    /// 读取保持寄存器
    /// <param name="startAddress">寄存器起始地址</param>
    /// <param name="quantity">读取寄存器数量</param>
    /// </summary>
    public int[] ReadHoldingRegister(ushort startAddress, ushort quantity)
    {
        var command = modbusCommand.GenerateCommand(FunctionCode.READING_HOLDING_REGISTER, startAddress, quantity);

        if (!SendData(command))
        {
            return Array.Empty<int>();
        }

        Thread.Sleep(10);
        var buffer = new byte[9 + 2 * quantity];
        ReceiveData(buffer);

        var result = new int[quantity];
        for (var i = 0; i < quantity; i++)
        {
            var value = (buffer[9 + i * 2] << 8) | buffer[10 + i * 2];
            if ((buffer[9] & 0x80) != 0) value = -value;
            result[i] = value;
        }

        return result;
    }

    /// <summary>
    /// 读取输入寄存器
    /// </summary>
    /// <param name="startAddress">寄存器起始地址</param>
    /// <param name="quantity">读取寄存器数量</param>
    /// <returns>寄存器值</returns>
    public int[] ReadInputRegister(ushort startAddress, ushort quantity)
    {
        var command = modbusCommand.GenerateCommand(FunctionCode.READING_INPUT_REGISTER, startAddress, quantity);

        if (!SendData(command))
        {
            return Array.Empty<int>();
        }

        Thread.Sleep(10);
        var buffer = new byte[9 + 2 * quantity];
        ReceiveData(buffer);
        var result = new int[quantity];
        for (var i = 0; i < quantity; i++)
        {
            var value = (buffer[9 + i * 2] << 8) | buffer[10 + i * 2];
            if ((buffer[9] & 0x80) != 0) value = -value;
            result[i] = value;
        }

        return result;
    }

    /// <summary>
    /// 写入单个线圈
    /// </summary>
    /// <param name="address">线圈地址</param>
    /// <param name="status">线圈值</param>
    /// <returns>是否写入成功</returns>
    public bool WriteSingleCoil(ushort address, CoilStatus status)
    {
        var command = modbusCommand.GenerateCommand(FunctionCode.WRITING_SINGLE_COIL, address, status);

        if (!SendData(command))
        {
            return false;
        }

        Thread.Sleep(10);
        var buffer = new byte[12];
        ReceiveData(buffer);

        return !buffer.Where((t, i) => t != command[i]).Any();
    }

    /// <summary>
    /// 写入单个保持寄存器
    /// </summary>
    /// <param name="address">寄存器地址</param>
    /// <param name="value">寄存器值</param>
    /// <returns>是否写入成功</returns>
    public bool WriteSingleRegister(ushort address, short value)
    {
        var command = modbusCommand.GenerateCommand(FunctionCode.WRITING_SINGLE_REGISTER, address, (ushort)value);

        if (!SendData(command))
        {
            return false;
        }

        Thread.Sleep(10);
        var buffer = new byte[12];
        ReceiveData(buffer);
        return !buffer.Where((t, i) => t != command[i]).Any();
    }

    /// <summary>
    /// 写入多个线圈 <br />
    /// 并设定为一致的值
    /// </summary>
    /// <param name="address">线圈起始地址</param>
    /// <param name="quantity">线圈数量</param>
    /// <param name="status">线圈值</param>
    /// <returns>是否写入成功</returns>
    public bool WriteMultipleCoils(ushort address, ushort quantity, CoilStatus status)
    {
        var boolArray = new bool[quantity];
        Array.Fill(boolArray, status == CoilStatus.ON);
        return WriteMultipleCoils(address, quantity, boolArray);
    }

    /// <summary>
    /// 写入多个线圈
    /// </summary>
    /// <param name="address">线圈起始地址</param>
    /// <param name="quantity">线圈数量</param>
    /// <param name="status">线圈值</param>
    /// <returns>是否写入成功</returns>
    public bool WriteMultipleCoils(ushort address, ushort quantity, bool[] status)
    {
        var command = modbusCommand.GenerateCommand(FunctionCode.WRITING_MULTIPLE_COILS, address, quantity, status);

        if (!SendData(command))
        {
            return false;
        }

        Thread.Sleep(10);
        var buffer = new byte[12];
        return ReceiveData(buffer);
    }
}