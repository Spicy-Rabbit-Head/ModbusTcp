namespace ModbusTcp;

public class ModbusCommand
{
    /// <summary>
    /// 默认站号 <br />
    /// 基本固定为 0x01
    /// </summary>
    public byte DefaultStationNumber { get; set; } = 0x01;

    /// <summary>
    /// 事务处理标识符
    /// </summary>
    private ushort transactionIdentifier = 0;

    /// <summary>
    /// 协议标识符
    /// </summary>
    private readonly byte[] protocolIdentifier = { 0x00, 0x00 };

    /// <summary>
    /// 单元标识符
    /// </summary>
    public byte UnitIdentifier { get; set; } = 0x01;

    /// <summary>
    /// 功能码
    /// </summary>
    private byte functionCode;

    /// <summary>
    /// 长度
    /// </summary>
    private ushort length;

    /// <summary>
    /// 生成 Modbus 命令
    /// </summary>
    /// <param name="functionCodeEnum">功能码枚举</param>
    /// <param name="start">写入的起始点</param>
    /// <param name="status">写入的状态</param>
    /// <returns>Modbus 命令</returns>
    public byte[] GenerateCommand(FunctionCode functionCodeEnum, ushort start, CoilStatus status)
    {
        return GenerateCommand(functionCodeEnum, start, status == CoilStatus.ON ? (ushort)0xFF00 : (ushort)0x0000);
    }

    /// <summary>
    /// 生成 Modbus 命令
    /// </summary>
    /// <param name="functionCodeEnum">功能码枚举</param>
    /// <param name="start">写入的起始点</param>
    /// <param name="status">写入的状态数组</param>
    /// <returns>Modbus 命令</returns>
    // public byte[] GenerateCommand(FunctionCode functionCodeEnum, ushort start, bool[] status)
    // {
    //     return GenerateCommand(functionCodeEnum, start, status == CoilStatus.ON ? (ushort)0xFF00 : (ushort)0x0000);
    // }

    /// <summary>
    /// 生成 Modbus 命令
    /// </summary>
    /// <param name="functionCodeEnum">功能码枚举</param>
    /// <param name="start">起始点</param>
    /// <param name="data">数据</param>
    /// <returns>Modbus 命令</returns>
    public byte[] GenerateCommand(FunctionCode functionCodeEnum, ushort start, ushort data)
    {
        try
        {
            // 功能码
            functionCode = (byte)functionCodeEnum;
            // 长度
            length = 6;
            var command = new List<byte>();
            // 生成 Modbus 命令头
            GenerateCommandHead(command);
            // 添加数据
            // 起始地址高低位
            command.AddRange(CalculateHighLow(start));
            // 读取的位数高低位
            command.AddRange(CalculateHighLow(data));

            return command.ToArray();
        }
        catch (Exception)
        {
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// 生成 Modbus 命令头
    /// </summary>
    /// <param name="command">命令实体</param>
    private void GenerateCommandHead(List<byte> command)
    {
        // 事务处理标识符
        command.AddRange(BitConverter.GetBytes(transactionIdentifier).Reverse());
        // 协议标识符
        command.AddRange(protocolIdentifier);
        // 添加长度
        command.AddRange(BitConverter.GetBytes(length).Reverse());
        // 单元标识符
        command.Add(UnitIdentifier);
        // 功能码
        command.Add(functionCode);
    }

    /// <summary>
    /// 计算长度
    /// </summary>
    /// <param name="functionCodeEnum">功能码枚举</param>
    /// <param name="quantity">读取的数量</param>
    /// <returns>长度</returns>
    private ushort CalculateLength(FunctionCode functionCodeEnum, ushort quantity)
    {
        switch (functionCodeEnum)
        {
            case FunctionCode.READING_COIL:
            case FunctionCode.READING_DISCRETE_INPUT:
            case FunctionCode.READING_HOLDING_REGISTER:
            case FunctionCode.READING_INPUT_REGISTER:
            case FunctionCode.WRITING_SINGLE_COIL:
            case FunctionCode.WRITING_SINGLE_REGISTER:
                functionCode = (byte)functionCodeEnum;
                return 6;
            case FunctionCode.WRITING_MULTIPLE_COILS:
            case FunctionCode.WRITING_MULTIPLE_REGISTERS:
                return (byte)(7 + quantity / 8 + (quantity % 8 == 0 ? 0 : 1));
            default:
                return 0;
        }
    }

    /// <summary>
    /// 计算高低位
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>高低位</returns>
    /// <remarks>高低位是指一个16位的值，将其分为高8位和低8位</remarks>
    private static IEnumerable<byte> CalculateHighLow(ushort value)
    {
        return new[] { CalculateHigh(value), CalculateLow(value) };
    }

    /// <summary>
    /// 计算高位
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>高位</returns>
    private static byte CalculateHigh(ushort value)
    {
        return (byte)((value >> 8) & 0xFF);
    }

    /// <summary>
    /// 计算低位
    /// </summary>
    /// <param name="value">值</param>
    /// <returns>低位</returns>
    private static byte CalculateLow(ushort value)
    {
        return (byte)(value & 0xFF);
    }

    /// <summary>
    /// 将高低位转换为 16 位有符号整数
    /// </summary>
    /// <param name="high">高位</param>
    /// <param name="low">低位</param>
    /// <returns>16 位有符号整数</returns>
    private short HighLowToInt16(byte high, byte low)
    {
        // 将高位和低位合并为一个整数
        var value = (high << 8) | low;

        // 如果符号位是 1，则整数是负数
        return (high & 0x80) != 0 ? (short)-value : (short)value;
    }

    /// <summary>
    /// 计算校验码
    /// </summary>
    /// <param name="command">数据</param>
    /// 
    private byte CalculateChecksum(byte[] command)
    {
        var checksum = command.Aggregate(0, (current, t) => current + t);

        return (byte)(0xFF - checksum);
    }

    /// <summary>
    /// 验证 Modbus 命令
    /// </summary>
    public bool VerifyCommand(byte[] receiveBytes)
    {
        return false;
    }
}