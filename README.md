<p align="center">
  <img src="res/title.jpg" alt="MaaEnd OCR Webhook Service" width="100%">
</p>

# MaaEnd OCR Webhook Service

English | [简体中文](README_CN.md)

MaaEnd OCR Webhook Service is a Windows application with a lightweight native WinForms GUI that watches a target application window, reads MaaEnd-style runtime logs through OCR, and forwards detected log lines to a Webhook endpoint. It is designed for workflows where logs are visible in a GUI window but still need to be collected, buffered, and forwarded to external services.

This program was created because the official MaaEnd repository has limited Webhook push support, currently relying on HTTP POST that is only usable on a small number of platforms, and there is no short-term development plan for broader notification support. See [MaaEnd issue #1421](https://github.com/MaaEnd/MaaEnd/issues/1421).

Compared with [MaaEnd-Webhook-Retransmitter](https://github.com/SHthemW/MaaEnd-Webhook-Retransmitter), this OCR-based pusher can capture more information and offers more flexibility, so this repository will be the primary maintained implementation going forward.

## Features

- Finds the target window by title, with exact or partial matching.
- Uses OCR to locate a configured anchor text, then reads log lines below it.
- Supports realtime pushes, summary pushes on exit, or both.
- Supports Webhook body templates with `__CONTENT__` as the log text placeholder and optional `__TIME__` as the log time placeholder.
- Opens Notepad for multi-line Webhook body editing during interactive setup.
- Supports realtime push caching to reduce frequent Webhook requests.
- Keeps critical logs as standalone pushes. Logs containing `任务` or `重要通知` flush pending cached logs first, then push the critical log separately.
- Can save screenshots and OCR preprocessing images for troubleshooting.

## Quick Start

1. Run `MaaEnd-Log-Retransmitter.exe` to open the GUI.
2. Select **配置** to edit the target window, OCR settings, and Webhook fields.
3. Select **运行日志 → 开始识别**; **停止** cancels recognition and flushes pending pushes.
4. A blank Webhook URL enables local recognition without delivery.
5. Select **开发选项 → 录制数据集** to capture the target window. After stopping, choose **保存录制** to open evaluation, or **删除录制** to discard this recording.

The default window title is `MaaEnd v`; the anchor is `运行日志`. Window titles ignore case by default.
The GUI waits for Start, so launching it does not begin capturing automatically.

CLI mode remains available:

```powershell
dotnet run -- --cli --debug
```

`--cli` uses the interactive configuration flow and Ctrl+C to stop. `--debug` enables detailed logs and saved screenshots. CLI mode without debugging still requires valid Webhook configuration.

## First-Time Setup

The default configuration covers most MaaEnd use cases. For first-time use, focus on these two fields:

- `WebhookUrl`: HTTP/HTTPS endpoint that receives log pushes.
- `WebhookBody`: request body template. It must contain `__CONTENT__` and can optionally contain `__TIME__`.

The GUI configuration dialog has a multi-line Webhook body editor. CLI setup opens `webhook-body-template.json` in Notepad.

Example body:

```json
{
  "time": "__TIME__",
  "content": "__CONTENT__"
}
```

If you use WeCom, Discord, Slack, or another Webhook service, these two fields are usually the only required changes.

## Default Behavior

- Target window: a window whose title contains `MaaEnd v`, such as `MaaEnd v2.13.0`; matching ignores case by default.
- Anchor text: `运行日志`.
- Push mode: `Realtime`; new logs are pushed as they are detected.
- OCR language: `zh-Hans`.
- Push cache: disabled by default.
- Screenshot saving: disabled by default.

## Common Options

- Window title: the title of the target application window.
- Search text: anchor text used to locate the log area.
- Match mode: the program prefers exact title matches; the default short title `MaaEnd v` can match a full title like `MaaEnd v2.13.0`.
- Window title case: **窗口标题区分大小写** controls title matching independently of the OCR anchor; disabled by default.
- Webhook mode: realtime push, summary push, or both.
- Webhook push cache time: seconds. `0` disables caching.
- Debug: enable the GUI checkbox or `--debug` to save screenshots and preprocessing images.

## Full Configuration Reference

`config.json` is located next to the executable and uses standard JSON. Important fields:

| Field | Meaning | Valid value |
| --- | --- | --- |
| `WindowTitle` | Target window title | Non-empty string |
| `WindowTitleCaseSensitive` | Match window titles case-sensitively during recognition and recording | `true` / `false`; default `false` |
| `SearchText` | Anchor text; rolling OCR starts below it | Non-empty string |
| `PartialMatch` | Force partial window title matching; even when `false`, short titles can still fall back to containment matching | `true` / `false` |
| `Retry` | Initial OCR retry count | `1` to `10` |
| `RetryInterval` | Initial OCR retry interval in milliseconds | `100` to `60000` |
| `RollingIntervalMs` | Delay after each processing round in milliseconds | `500` to `60000` |
| `CaseSensitive` | Match `SearchText` case-sensitively | `true` / `false` |
| `Language` | Windows OCR language tag | For example `zh-Hans` |
| `WebhookUrl` | Webhook endpoint | `http` / `https` URL; GUI/debug allows empty for local recognition |
| `WebhookBody` | Webhook request body template | Non-empty, must contain `__CONTENT__`, and can optionally contain `__TIME__` |
| `WebhookContentType` | Webhook request Content-Type | Non-empty string, commonly `application/json` |
| `WebhookTimeoutMs` | Webhook request timeout in milliseconds | `1000` to `60000` |
| `WebhookMode` | Webhook push mode | `Realtime` / `Summary` / `All` |
| `WebhookPushCacheSeconds` | Realtime push cache time in seconds | `0` to `86400`; `0` disables caching |

## Developer Options

- **录制数据集**: lossless full-window PNG recording with actual timestamps, window dimensions/DPI, capture method, environment, hashes, and annotation templates. The default interval is 100ms; recording is independent of OCR success.
- **评估数据集**: browse/play recordings, edit frame and session event ground truth, and evaluate only the current branch's production algorithm using the shared OCR, crop, parsing, filter, and deduplication code.
- Evaluation defaults to sampling with production processing durations and delays. Disable **按正式频率采样** for the slower all-frame comparison. Reports include strict/layout-normalized CER, exact-line/time accuracy, event P/R/F1, detection latency, median and P95 processing duration, and per-frame candidates.
- Set **线程** to 1–8 workers, with at most 4 by default. The recording is divided into equal time segments, one per worker; results are filtered and deduplicated in chronological order afterward. Reports record segment boundaries, sample counts, and timings. Use 1 worker for continuous replay and standalone performance measurements.
- Accuracy requires reviewed human annotations. Predictions and heuristic scores are never treated as ground truth. Evaluation sends no Webhooks.

See [dataset recording and evaluation](docs/DATASETS.md) for the recording format, workflow, metrics, and limitations.

## Webhook Modes

- `Realtime`: push each new log line during rolling recognition.
- `Summary`: push collected logs when the program stops.
- `All`: enable both realtime and summary pushes.

When sending a Webhook, `__CONTENT__` is replaced with the log text only and no longer includes the time. If the template contains `__TIME__`, it is replaced with the recognized time string.

When `WebhookPushCacheSeconds` is greater than `0`, normal realtime logs are buffered. Once the configured number of seconds has elapsed, cached logs are merged into one Webhook push.

Critical logs are never merged into the cache. When a log contains `任务` or `重要通知`, the program first sends and clears the existing cache, then sends that critical log as a separate Webhook push.

## Usage Tips

- Choose `SearchText` from stable text that appears above the log area.
- `WindowTitle` can be a full title or a stable short title. The default is `MaaEnd v`; `maaend V` also matches when `WindowTitleCaseSensitive` is `false`.
- If OCR cannot find the anchor text, enable debugging and inspect the saved screenshots and preprocessing images.
- If the Webhook platform expects JSON, set `WebhookContentType` to `application/json` and make sure `WebhookBody` is valid JSON.
- If pushes are too frequent, set `WebhookPushCacheSeconds`, such as `10` or `30`.

## Troubleshooting

- Window not found: check `WindowTitle`, `PartialMatch`, and `WindowTitleCaseSensitive`.
- `SearchText` not found: check screenshots, OCR language, and case-sensitive matching.
- Screenshot looks wrong: make sure the target window is visible and not minimized.
- Webhook push failed: check `WebhookUrl`, `WebhookBody`, `WebhookContentType`, and `WebhookTimeoutMs`.
- OCR language unavailable: install the corresponding Windows OCR language package.

## Development And Packaging

Development build and integration checks:

```powershell
dotnet build
dotnet run --project tests/MaaEnd-Log-Retransmitter.Tests.csproj
```

Release package:

```powershell
dotnet publish -c Release
```

The project uses .NET 8 WinForms without third-party UI or video libraries. It is configured for Windows x64 self-contained single-file publishing. Every `dotnet publish` also writes a ZIP to `bin/Release-Archives/`, named `program-platform-portable-yymmdd-hhmmss.zip`. Output files are written to `publish\`:

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
