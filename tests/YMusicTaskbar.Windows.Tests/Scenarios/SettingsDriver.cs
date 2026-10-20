using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Automation;
using YMusicTaskbar;

internal sealed class SettingsDriver(int processId)
{
    public AutomationElement? Find(string name, ControlType type)
    {
        var windows = new List<IntPtr>();
        Native.EnumWindows((handle, _) =>
        {
            Native.GetWindowThreadProcessId(handle, out uint owner);
            if (owner == processId && Native.IsWindowVisible(handle)) windows.Add(handle);
            return true;
        }, IntPtr.Zero);
        foreach (var handle in windows)
        {
            var root = AutomationElement.FromHandle(handle);
            var found = root.FindFirst(TreeScope.Subtree, new AndCondition(new PropertyCondition(AutomationElement.NameProperty, name), new PropertyCondition(AutomationElement.ControlTypeProperty, type)));
            if (found is not null && !found.Current.IsOffscreen) return found;
        }
        return null;
    }
    public async Task Invoke(string name, ControlType type)
    {
        var item = Find(name, type) ?? throw new Exception("Control not found: " + name);
        await Task.Run(() => ((InvokePattern)item.GetCurrentPattern(InvokePattern.Pattern)).Invoke());
        await Task.Delay(250);
    }
    public async Task Open()
    {
        await Invoke("Настройки и выход", ControlType.Button);
        await Invoke("Настройки…", ControlType.MenuItem);
    }
    public async Task Select(string comboName, string value)
    {
        var combo = Find(comboName, ControlType.ComboBox) ?? throw new Exception("Combo not found: " + comboName);
        await Task.Run(() => ((ExpandCollapsePattern)combo.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand());
        await Task.Delay(150);
        string[] choices = comboName == "Тема" ? new[] { "Как панель задач Windows", "Светлая", "Тёмная" } : new[] { "Полный — с перемоткой", "Компактный" };
        int index = Array.IndexOf(choices, value);
        if (index < 0) throw new ArgumentException("Unknown choice: " + value);
        await Task.Run(combo.SetFocus);
        MenuKeyboard.Press(0x24);
        for (int i = 0; i < index; i++) MenuKeyboard.Press(0x28);
        MenuKeyboard.Press(0x0D);
        await Task.Delay(200);
    }
    public Task Save() => Invoke("Сохранить", ControlType.Button);
    public async Task Mode(string monitor, bool compact)
    {
        await Open();
        int index = Array.FindIndex(System.Windows.Forms.Screen.AllScreens, s => s.DeviceName == monitor);
        var monitors = Find("Экраны", ControlType.List) ?? throw new Exception("Monitor tiles not found");
        await Task.Run(monitors.SetFocus); MenuKeyboard.Press(0x24);
        for (int i = 0; i < index; i++) MenuKeyboard.Press(0x27);
        await Task.Delay(150);
        await Select("Вид: " + monitor, compact ? "Компактный" : "Полный — с перемоткой"); await Save();
    }
}