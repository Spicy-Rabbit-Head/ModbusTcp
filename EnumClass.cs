namespace ModbusTcp;

/// <summary>
/// 寄存器类型,十六进制表示形式
/// </summary>
public enum PlcMemory
{
    CIO,
    WR,
    HR,
    AR,
    DM,
    CNT,
    TIM
}

/// <summary>
/// 地址类型
/// </summary>
public enum MemoryType
{
    BIT,
    WORD
}

/// <summary>
/// 数据类型,PLC字为16位数，最高位为符号位，负数表现形式为“取反加一”
/// </summary>
public enum DataType
{
    BIT,
    INT16,
    REAL
}

/// <summary>
/// bit位开关状态，on=1，off=0
/// </summary>
public enum BitState
{
    ON = 1,
    OFF = 0
}

/// <summary>
/// Modbus 功能码
/// </summary>
public enum FunctionCode
{
    /// <summary>
    /// 读取线圈
    /// </summary>
    READING_COIL = 0x01,

    /// <summary>
    /// 读取离散输入
    /// </summary>
    READING_DISCRETE_INPUT = 0x02,

    /// <summary>
    /// 读取保持寄存器
    /// </summary>
    READING_HOLDING_REGISTER = 0x03,

    /// <summary>
    /// 读取输入寄存器
    /// </summary>
    READING_INPUT_REGISTER = 0x04,

    /// <summary>
    /// 写入单个线圈
    /// </summary>
    WRITING_SINGLE_COIL = 0x05,

    /// <summary>
    /// 写入单个寄存器
    /// </summary>
    WRITING_SINGLE_REGISTER = 0x06,

    /// <summary>
    /// 写入多个线圈
    /// </summary>
    WRITING_MULTIPLE_COILS = 0x0F,

    /// <summary>
    /// 写入多个寄存器
    /// </summary>
    WRITING_MULTIPLE_REGISTERS = 0x10
}

/// <summary>
/// 寄存器排序方式
/// </summary>
public enum RegisterOrder
{
    /// <summary>
    /// 低位在前，高位在后
    /// </summary>
    LowHigh,

    /// <summary>
    /// 高位在前，低位在后
    /// </summary>
    HighLow,
}