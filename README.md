# Axiome's Toolbox
An experimental mod adding features primarily intended for speedrun practice and labbing.

## Features

### Freeze the timer when the game is paused

Testing movement and strat alternatives often requires frame advance and pause buffering, but the timer running during pause causes timing inaccuracies. This option halts the timer while the game is paused, ensuring accurate timing.

### Checkpoint

Similar to srt endpoint but doesn't stop the timer. They count as a room in regards to srt room count.

### Timeline

A frame-accurate HUD bar showing gameplay events as colored spans in a scrolling window. Tracks dash (with extension frames), jump held, coyote frames, half-gravity, freeze frames, and input marks (dash press, jump press, jump fired). Room transitions, cutscene starts, and cutscene skips are also marked.

Hold the **Inspect** hotkey to freeze the display and scroll through history with the mouse wheel. Hover over a span to see its label and frame count. Release to resume live tracking.

### Detection rules inspired by [MovementLinter](https://github.com/6point28etc/MovementLinter)

- Bad cornerboost detection
- Frames lost to menu actions (skip cutscene, retry, S&Q and RTM)
- Frames lost to late death confirm
- Failed waterboost
- 2nd blockless
