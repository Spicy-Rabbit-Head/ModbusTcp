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
    /// 生成 Modbus 命令
    /// <param name="functionCode">功能码枚举</param>
    /// <param name="start">读取的起始点</param>
    /// <param name="quantity">读取的数量</param>
    /// </summary>
    public byte[] GenerateCommand(FunctionCode functionCode, ushort start, ushort quantity)
    {
        try
        {
            var command = new List<byte>();
            // 事务处理标识符
            command.AddRange(BitConverter.GetBytes(transactionIdentifier).Reverse());
            // 协议标识符
            command.AddRange(protocolIdentifier);
            // 添加长度
            command.AddRange(BitConverter.GetBytes(ComputeLength(functionCode, quantity)).Reverse());
            // 单元标识符
            command.Add(UnitIdentifier);
            // 功能码
            command.Add((byte)functionCode);
            // 添加数据
            // 起始地址高8位
            command.Add((byte)((start >> 8) & 0xFF));
            // 起始地址低8位
            command.Add((byte)(start & 0xFF));
            // 读取的位数高8位
            command.Add((byte)((quantity >> 8) & 0xFF));
            // 读取的位数低8位
            command.Add((byte)(quantity & 0xFF));
            transactionIdentifier++;
            // command.Add(CalculateChecksum(command.ToArray()));

            return command.ToArray();
        }
        catch (Exception)
        {
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// 计算长度
    /// </summary>
    /// <param name="functionCode">功能码枚举</param>
    /// <param name="quantity">读取的数量</param>
    /// <returns>长度</returns>
    private short ComputeLength(FunctionCode functionCode, ushort quantity)
    {
        switch (functionCode)
        {
            case FunctionCode.READING_COIL:
            case FunctionCode.READING_DISCRETE_INPUT:
                return (short)(quantity / 8 + (quantity % 8 > 0 ? 1 : 0) + 1);
            case FunctionCode.READING_HOLDING_REGISTER:
            case FunctionCode.READING_INPUT_REGISTER:
                return 6;
            case FunctionCode.WRITING_SINGLE_COIL:
            case FunctionCode.WRITING_SINGLE_REGISTER:
                return 4;
            case FunctionCode.WRITING_MULTIPLE_COILS:
            case FunctionCode.WRITING_MULTIPLE_REGISTERS:
                return (short)(quantity * 2 + 5);
        }

        return 0;
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
        if (receiveBytes[6] == UnitIdentifier)
        {
        }

        return false;
    }
}