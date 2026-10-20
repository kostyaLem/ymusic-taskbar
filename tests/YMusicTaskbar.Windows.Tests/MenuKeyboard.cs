using System;
using System.Runtime.InteropServices;

internal static class MenuKeyboard
{
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput
    { public ushort Key, Scan; public uint Flags, Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput
    { public int X, Y; public uint Data, Flags, Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Explicit)] private struct InputData
    { [FieldOffset(0)] public KeyboardInput Keyboard; [FieldOffset(0)] public MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] private struct Input
    { public uint Type; public InputData Data; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] input, int size);

    public static void Press(int key)
    {
        var down = new Input { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = (ushort)key } } };
        var up = new Input { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = (ushort)key, Flags = 2 } } };
        if (SendInput(2, [down, up], Marshal.SizeOf<Input>()) != 2) throw new InvalidOperationException("Keyboard test input failed");
    }
}
