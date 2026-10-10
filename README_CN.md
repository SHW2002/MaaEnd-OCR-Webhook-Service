<p align="center">
  <img src="res/title.jpg" alt="MaaEnd OCR Webhook Service" width="100%">
</p>

# MaaEnd OCR Webhook Service

[English](README.md) | 简体中文

MaaEnd OCR Webhook Service 是一个使用轻量原生 WinForms GUI 的 Windows 应用，用于监听目标程序窗口，通过 OCR 读取 MaaEnd 风格的运行日志，并把识别到的日志推送到 Webhook。它适用于日志只显示在 GUI 窗口中，但需要被采集、缓存并转发到外部服务的场景。

由于 MaaEnd 的官方仓库对 Webhook 推送支持不佳，仅支持少数平台可用的 HTTP POST，且短期内没有此内容的开发计划，见 [MaaEnd issue #1421](https://github.com/MaaEnd/MaaEnd/issues/1421)，因此开发了本程序。

相比 [MaaEnd-Webhook-Retransmitter](https://github.com/SHthemW/MaaEnd-Webhook-Retransmitter)，基于 OCR 识别的推送器能捕捉更多信息，也提供了更多自由度，因此后续主要维护本仓库。

## 主要功能

- 通过窗口标题查找目标窗口，支持精确匹配和模糊匹配。
- 通过 OCR 定位指定锚点文本，并从锚点下方开始滚动识别日志。
- 支持实时推送、结束后汇总推送，或两者同时启用。
- 支持 Webhook Body 模板，使用 `__CONTENT__` 作为日志正文占位符，使用可选的 `__TIME__` 作为日志时间占位符。
- 交互式配置 Webhook Body 时会自动打开记事本，便于编辑多行 JSON。
- 支持实时推送缓存，减少频繁 Webhook 请求。
- 关键日志必定单独推送。包含 `任务` 或 `重要通知` 的日志会先触发已有缓存立即推送并清空，随后该关键日志单独推送。
- 可保存截图和 OCR 预处理图片，用于排查识别问题。

## 快速使用

1. 运行 `MaaEnd-Log-Retransmitter.exe` 打开 GUI。
2. 点击 **配置**，编辑目标窗口、OCR 参数和 Webhook。
3. 在 **运行日志** 页点击 **开始识别**；**停止** 会结束识别并刷新已有推送缓存。
4. Webhook 地址留空时，可以只在本地识别而不推送。
5. 在 **开发选项 → 录制数据集** 采集目标窗口，停止后点击 **保存录制** 进入评估，或 **删除录制** 丢弃本次数据。

默认窗口标题为 `MaaEnd v`，匹配时默认忽略大小写，锚点为 `运行日志`。GUI 启动后等待手动开始，不会自动采集窗口。

仍可使用 CLI：

```powershell
dotnet run -- --cli --debug
```

`--cli` 保留交互式配置与 Ctrl+C 停止；`--debug` 开启详细日志和截图保存。非调试 CLI 仍要求有效的 Webhook 配置。

## 首次配置

默认配置已经覆盖大多数 MaaEnd 使用场景。用户第一次使用时重点配置下面两项即可：

- `WebhookUrl`：接收日志推送的 HTTP/HTTPS 地址。
- `WebhookBody`：推送请求体模板，必须包含 `__CONTENT__`，可选包含 `__TIME__`。

GUI 配置窗口提供多行 Webhook Body 编辑框；CLI 配置时仍使用记事本编辑 `webhook-body-template.json`。

示例 Body：

```json
{
  "time": "__TIME__",
  "content": "__CONTENT__"
}
```

如果你只是使用企业微信、Discord、Slack 或其他 Webhook 服务，通常只需要按对方平台要求调整这两个字段。

## 默认行为

- 目标窗口：标题中包含 `MaaEnd v` 的窗口，例如 `MaaEnd v2.13.0`，默认忽略大小写。
- 锚点文本：`运行日志`。
- 推送方式：`Realtime`，识别到新日志后实时推送。
- OCR 语言：`zh-Hans`。
- 推送缓存：默认关闭。
- 截图保存：默认关闭。

## 常用可改项

- 窗口标题：目标程序窗口标题，例如 MaaEnd 的主窗口标题。
- 查找文本：用于定位日志区域的锚点文本，例如界面中稳定出现的标题或字段。
- 匹配方式：程序会优先精确匹配窗口标题；默认短标题 `MaaEnd v` 可匹配到 `MaaEnd v2.13.0` 这类完整标题。
- 窗口标题大小写：**窗口标题区分大小写** 独立控制标题匹配，默认关闭，与 OCR 锚点的大小写设置分开。
- Webhook 推送方式：选择实时推送、汇总推送或全部推送。
- Webhook 推送缓存时间：单位秒，`0` 表示不启用缓存。
- 调试：通过 GUI 复选框或 `--debug` 保存截图与预处理图片。

## 完整配置参考

`config.json` 位于可执行文件旁边，是标准 JSON。主要字段如下：

| 字段 | 含义 | 合法值 |
| --- | --- | --- |
| `WindowTitle` | 目标窗口标题 | 非空字符串 |
| `WindowTitleCaseSensitive` | 识别和录制时匹配窗口标题是否区分大小写 | `true` / `false`，默认 `false` |
| `SearchText` | 锚点文本，程序从它下方开始滚动 OCR | 非空字符串 |
| `PartialMatch` | 是否强制允许窗口标题模糊匹配；即使为 `false`，短标题也会作为完整标题的包含匹配兜底 | `true` / `false` |
| `Retry` | 初始 OCR 重试次数 | `1` 到 `10` |
| `RetryInterval` | 初始 OCR 重试间隔，单位毫秒 | `100` 到 `60000` |
| `RollingIntervalMs` | 每轮处理结束后的等待时间，单位毫秒 | `500` 到 `60000` |
| `CaseSensitive` | 查找 `SearchText` 时是否区分大小写 | `true` / `false` |
| `Language` | Windows OCR 语言标签 | 例如 `zh-Hans` |
| `WebhookUrl` | Webhook 地址 | `http` / `https` URL；GUI/调试模式可留空以仅本地识别 |
| `WebhookBody` | Webhook 请求体模板 | 非空，必须包含 `__CONTENT__`；可选包含 `__TIME__` |
| `WebhookContentType` | Webhook 请求 Content-Type | 非空字符串，常用 `application/json` |
| `WebhookTimeoutMs` | Webhook 请求超时，单位毫秒 | `1000` 到 `60000` |
| `WebhookMode` | Webhook 推送模式 | `Realtime` / `Summary` / `All` |
| `WebhookPushCacheSeconds` | 实时推送缓存时间，单位秒 | `0` 到 `86400`，`0` 表示不启用 |

## 开发选项

- **录制数据集**：完整窗口无损 PNG 连续录制，保留实际时间戳、窗口尺寸/DPI、采集方式、环境、校验值和标注模板；默认 100ms 一帧，独立于 OCR 成败。
- **评估数据集**：浏览、播放录制，编辑逐帧真值与会话事件真值，仅评估当前分支正式方案，共用锚点定位、裁剪、OCR、解析、过滤和去重代码。
- 评估默认按正式处理耗时和等待间隔采样；取消 **按正式频率采样** 才逐帧对比，耗时更长。报告包含严格/统一排版 CER、整行/时间准确率、事件 P/R/F1、检测延迟、完整流程中位/P95 耗时及逐帧候选。
- **线程** 可设为 1–8，默认最多 4；按录制时间均分，每个线程评估一个连续片段，最后按时间顺序统一过滤和去重。报告记录片段边界、采样量与耗时；设为 1 可连续回放整个会话并测量单独运行性能。
- 准确度只根据人工确认的标注计算，预测文本和规则评分不会充当真值。评估不会发送 Webhook。

详见 [数据集录制与评估](docs/DATASETS_CN.md)，了解格式、标注流程、指标口径和限制。

## Webhook 推送模式

- `Realtime`：滚动识别过程中实时推送每条新日志。
- `Summary`：程序停止时汇总推送本次收集到的日志。
- `All`：同时启用实时推送和汇总推送。

Webhook 推送时，`__CONTENT__` 只会替换为日志正文，不再包含时间；如果模板中存在 `__TIME__`，程序会用识别到的时间字符串替换它。

当 `WebhookPushCacheSeconds` 大于 `0` 时，普通实时日志会先进入缓存。缓存开始后到达指定秒数时，程序会把缓存内容合并成一条 Webhook 推送。

关键日志不进入缓存合并。包含 `任务` 或 `重要通知` 的日志出现时，程序会先发送并清空已有缓存，然后单独发送该关键日志。

## 使用建议

- `SearchText` 应选择在目标窗口中稳定出现、且位于日志区域上方的文本。
- `WindowTitle` 可以填写完整标题，也可以填写稳定短标题；默认值为 `MaaEnd v`，`WindowTitleCaseSensitive` 为 `false` 时也可匹配 `maaend V`。
- 如果 OCR 找不到锚点文本，可以启用调试模式 后检查保存的截图和预处理图片。
- 如果 Webhook 平台要求 JSON，请确保 `WebhookContentType` 为 `application/json`，且 `WebhookBody` 是合法 JSON。
- 如果推送过于频繁，可以设置 `WebhookPushCacheSeconds`，例如 `10` 或 `30`。

## 排查问题

- 找不到窗口：检查 `WindowTitle`、`PartialMatch` 和 `WindowTitleCaseSensitive`。
- 找不到 `SearchText`：检查截图、OCR 语言和大小写配置。
- 截图内容异常：确认目标窗口可见且没有最小化。
- Webhook 推送失败：检查 `WebhookUrl`、`WebhookBody`、`WebhookContentType` 和 `WebhookTimeoutMs`。
- OCR 语言不可用：安装对应的 Windows OCR 语言包。

## 开发与打包

开发构建和集成验证：

```powershell
dotnet build
dotnet run --project tests/MaaEnd-Log-Retransmitter.Tests.csproj
```

Release 打包：

```powershell
dotnet publish -c Release
```

项目采用 .NET 8 WinForms，不额外引入 UI 框架或视频库。默认 Windows x64、自包含、单文件发布；每次 `dotnet publish` 还会生成 `程序名-平台-portable-yymmdd-hhmmss.zip` 并存入 `bin/Release-Archives/`。发布产物位于 `publish\`：

```text
publish\MaaEnd-Log-Retransmitter.exe
publish\MaaEnd-Log-Retransmitter.pdb
```

<p align="center">
  <img src="res/icon.png" alt="MaaEnd OCR Webhook Service Icon" width="33%">
</p>

<p align="center">
  <img src="https://img.shields.io/github/downloads/SHthemW/MaaEnd-OCR-Webhook-Service/total" alt="downloads" />
</p>
