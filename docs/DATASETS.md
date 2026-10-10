# Dataset recording and evaluation

English | [简体中文](DATASETS_CN.md)

## Recording

Open the target window and select **开发选项 → 录制数据集** in the GUI. Choose an output folder, capture interval, and scenario description. The target title comes from **配置**.

The default target title is `MaaEnd v`, matched without case sensitivity. **窗口标题区分大小写** applies to both live recognition and recording, independently of **锚点区分大小写**.

The default output folder is `records\` next to the executable. Folder fields display paths relative to that directory and resolve relative input from it. Each recording creates a `session-yyyyMMdd-HHmmss` subdirectory, for example `session-20261010-121022`. Recordings started in the same second use numeric suffixes such as `-2`, without random IDs in folder names. Select **浏览** to choose another location.

After **停止**, stay on the recording page and select **保存录制** or **删除录制**. Capture first writes a temporary `.pending-session-*` directory inside the output folder. Saving moves it to a final `session-*` directory and opens evaluation; deleting removes only that temporary session. Resolve it before starting another recording. Recordings with no usable frames can only be deleted. Closing the main window offers save, delete, or cancel if a recording is still pending.

The recorder shares the production `ScreenCapture` implementation: `PrintWindow`, with DWM thumbnail fallback. It captures only the configured target window, saving the entire target window at its original size as a lossless PNG sequence, independently of OCR and anchor detection. It does not capture the full screen. Recordings play inside the application; the format is not MP4 and requires no FFmpeg or external recorder.

**采集间隔（毫秒）** is the target interval between full-window captures, in milliseconds. It defaults to 100ms (about 10 frames per second), configurable from 33 to 60000ms. Smaller values capture more frames and use more storage; larger values may miss brief changes. This setting controls recording capture, not OCR processing.

A monotonic clock schedules capture; overdue slots are skipped instead of captured in a burst. Actual timestamps, missed slots, and capture errors are recorded. This is not a hard realtime frame-rate guarantee.

A saved recording has this directory structure:

```text
session-20261010-121022/
  dataset.json
  annotations.json
  frames/
    000000.png
    000001.png
  reports/
    evaluation-date-time-unique-id/
      report.json
      report.md
```

Metadata includes the session ID, start/end times, operating system, .NET version, available OCR languages, recognition settings, scenario, and per-frame window title, dimensions, DPI, capture method/duration, UTC and relative timestamps, and SHA-256. Actual Webhook URLs and body templates are excluded from the recording snapshot.

Unchanged frames are retained for temporal deduplication tests. The manifest is updated after every capture; stopping adds the end time. A crash does not automatically delete the temporary session; persisted frames can still be opened using its `dataset.json`.

Record multiple sessions covering DPI, scaling, window width, log colors, wrapped lines, empty/single-line logs, rapid updates, scrolling transitions, and task completion. The shared pipeline relocates the anchor when frame dimensions change. A layout change without a dimension change retains the cached crop, matching live behavior.

## Browsing and ground truth

Select **开发选项 → 评估数据集 → 浏览** and choose `dataset.json`, or enter a session directory and select **载入**. Browsing defaults to `records\` next to the executable and uses the corresponding directory when a path is already entered. **打开目录** opens that folder in File Explorer. **载入** opens the dataset picker when the path is empty or points to the recording root.

Loading reads the session's `annotations.json` automatically and selects the first frame with text ground truth. The status shows how many frames have text and how many are reviewed; each frame also shows its ground-truth status, including reviewed empty logs. **重载标注** refreshes the current frame after external annotation changes, saving any pending edits first. Saving a frame preserves other frames and event annotations already on disk.

- Play the recording, seek with the slider, or step through frames. **原尺寸** displays original pixels.
- The editable table shows the current frame's annotations. The **标注变化** selector and **上一组 / 下一组** skip consecutive frames with identical text ground truth. **全部标注** lists text from the entire session, including unreviewed annotations, showing each timestamp/content pair once. Double-click or select **定位帧** to locate its frame; browsing does not change review status.
- Annotate only logical log lines below the anchor, in display order, with separate timestamp and content fields. Wrapped continuations belong to the same logical log line.
- **OCR 草稿** fills a prediction from the shared pipeline. It does not confirm ground truth. Manually correct the text, then check **真值已确认**.
- Keep the actual visible text; uncertain frames should remain unreviewed.
- Empty logs can be reviewed with zero rows. **锚点可见** indicates that the configured anchor is visible in the full window.
- Scenario labels are editable. Switching frames or closing the main window saves changes; **保存标注** saves explicitly.

Frame annotations measure text recognition. Session event annotations measure output, omissions, and duplicates.

Select **事件真值** to annotate every expected event with its timestamp, content, and first visible offset in milliseconds. Identical content at different times represents distinct events. You may generate a draft from reviewed frames, but must complete omissions from the full recording and review the entire session before confirming it.

## Shared pipeline and replay modes

**开始评估** runs only the current branch's production algorithm. This branch recognizes two 8× candidates: detail-preserving color contrast/sharpening and high-contrast binarization. It selects the complete result with the highest heuristic score, breaking ties by shorter recognition duration. Production and evaluation share the `RollingOcrRecognizer` entry point; reports include both candidates and their scores. Master uses the original single detail-preserving candidate.

Evaluation and live recognition share `FrameRecognitionPipeline` for full-window anchor detection (4× high contrast), cropping, the OCR engine, tiling, line reconstruction, timestamp/content parsing, and `RollingOcrOutputFilter` cleanup, rejection, and temporal deduplication. Evaluation substitutes recorded frames for window input and local reports for delivery; it sends no Webhook requests.

- Set **线程** to 1–8 workers; the CPU-based default is capped at 4. The recording is split into equal time segments, one per worker, without balancing frame counts or workload. Empty segments are skipped. All results are filtered, rejected if unstable, and deduplicated chronologically afterward, including across segment boundaries.
- **按正式频率采样** is enabled by default. Within each segment, select the next frame at or after the previous sample's measured localization, cropping, and OCR duration plus the recorded delay. Skipped images are neither read nor recognized, and replay does not wait for real time. Failed localization uses the production retry delay, with a retry limit per segment. Text metrics use only sampled reviewed frames.
- Each segment starts sampling independently and relocates the anchor. Segment boundaries and CPU contention can change sampled frames. Global filtering happens after recognition; its duration does not affect the next-frame schedule. Use **1 worker** for continuous replay and standalone performance measurements.
- Disable **按正式频率采样** for **all-frames mode**. Every frame runs the current algorithm, allowing comparison of worker counts on identical inputs; this takes longer. Worker count is independent of sampling mode.

Live processing now waits after rejected frames as well, correcting the old bypass of the configured delay. Replay and production share the same delay rules.

Each selected image is checked against its SHA-256 before decoding; damaged or modified samples cause failure. Skipped images are not checked. Progress aggregates sample counts, recording time coverage, and finished segments. Stopping or any segment failing cancels and awaits all workers. Cancellation does not produce a complete report and leaves annotations intact.

## Metrics

| Metric | Definition |
| --- | --- |
| CER | Character edit distance of ordered log content joined with newlines / ground-truth character count; can exceed 100% |
| Exact content line rate | Ground-truth lines with exact aligned content / all ground-truth lines; ignores timestamps, omissions are errors |
| Exact line rate | Ground-truth lines with correct time and content / all ground-truth lines; omissions are errors |
| Time accuracy | Correct timestamps after content-sequence alignment / ground-truth lines |
| Anchor success | Reviewed processed frames with visible anchors where the shared pipeline obtains the log area, including cached localization |
| Precision P | Correct output events / all output events |
| Recall R | Correct output events / expected events |
| F1 | 2 × correct events / (output events + expected events) |
| Event latency | Replay detection completion time − first visible ground-truth time; median reported |
| Duration | Median and P95 of the complete shared processing path, including necessary localization, cropping, all candidates, scoring, and filtering |

Events match one to one by exact timestamp and content, and must already be visible at the sampled input time. Duplicate outputs are false positives. Heuristic scores are not accuracy. Unreviewed frames are excluded; event P/R/F1 are absent until session truth is reviewed. Undefined ratios display `—`, never a fabricated 100%. Reports show total frames, reviewed frames, processed frames, and annotated processed frames.

Strict metrics retain original text. Layout-normalized metrics apply only Unicode FormKC and whitespace normalization. Duration excludes image loading, PNG recording, and network delivery; simulated latency excludes network time. Results depend on the current machine and installed Windows OCR languages. Events between captured frames can remain missing.

`report.json` records the worker setting (`RequestedWorkers`), actual segment count (`WorkerCount`), execution mode (`ExecutionMode`), and total evaluation time (`WallMilliseconds`, excluding report writing). Its single `Result` includes segment ranges/timings, per-frame output, candidate text/scores/durations, total duration, crop coordinates, and errors. `report.md` explains segment boundaries and timing limitations. Parallel durations include CPU contention; simulated event latency does not represent the parallel evaluation's wall-clock latency.

Separate tuning and final evaluation data by recording session to prevent adjacent-frame leakage. The application evaluates the selected session; it does not automatically split datasets or import videos from other recorders.
