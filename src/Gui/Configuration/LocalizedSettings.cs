using System.ComponentModel;
using MaaEnd_Log_Retransmitter.App;

namespace MaaEnd_Log_Retransmitter.Gui;

internal sealed class LocalizedSettings(Arguments config) : ICustomTypeDescriptor
{
    private static readonly Dictionary<string, (string Label, string Category)> Labels = new()
    {
        ["WindowTitle"] = ("窗口标题", "窗口"), ["SearchText"] = ("日志区域锚点", "窗口"),
        ["WindowTitleCaseSensitive"] = ("窗口标题区分大小写", "窗口"),
        ["PartialMatch"] = ("允许模糊匹配", "窗口"), ["CaseSensitive"] = ("锚点区分大小写", "窗口"),
        ["Language"] = ("OCR 语言", "识别"), ["Retry"] = ("锚点重试次数", "识别"),
        ["RetryInterval"] = ("重试等待（毫秒）", "识别"), ["RollingIntervalMs"] = ("每轮结束后等待（毫秒）", "识别"),
        ["WebhookUrl"] = ("推送地址", "Webhook"), ["WebhookContentType"] = ("请求内容类型", "Webhook"),
        ["WebhookTimeoutMs"] = ("超时（毫秒）", "Webhook"), ["WebhookMode"] = ("推送方式（Realtime / Summary / All）", "Webhook"),
        ["WebhookPushCacheSeconds"] = ("缓存时间（秒）", "Webhook")
    };
    public PropertyDescriptorCollection GetProperties(Attribute[]? attributes) => GetProperties();
    public PropertyDescriptorCollection GetProperties()
        => new(Labels.Select(x => new SettingProperty(config, x.Key, x.Value.Label, x.Value.Category)).ToArray());
    public AttributeCollection GetAttributes() => AttributeCollection.Empty;
    public string? GetClassName() => null;
    public string? GetComponentName() => null;
    public TypeConverter GetConverter() => new();
    public EventDescriptor? GetDefaultEvent() => null;
    public PropertyDescriptor? GetDefaultProperty() => null;
    public object? GetEditor(Type editorBaseType) => null;
    public EventDescriptorCollection GetEvents(Attribute[]? attributes) => EventDescriptorCollection.Empty;
    public EventDescriptorCollection GetEvents() => EventDescriptorCollection.Empty;
    public object GetPropertyOwner(PropertyDescriptor? pd) => config;

    private sealed class SettingProperty(Arguments config, string name, string label, string category)
        : PropertyDescriptor(name, [new DisplayNameAttribute(label), new CategoryAttribute(category)])
    {
        private readonly System.Reflection.PropertyInfo _property = typeof(Arguments).GetProperty(name)!;
        public override Type ComponentType => typeof(Arguments);
        public override bool IsReadOnly => false;
        public override Type PropertyType => _property.PropertyType;
        public override bool CanResetValue(object component) => false;
        public override object? GetValue(object? component) => _property.GetValue(config);
        public override void SetValue(object? component, object? value) => _property.SetValue(config, value);
        public override void ResetValue(object component) { }
        public override bool ShouldSerializeValue(object component) => false;
    }
}
